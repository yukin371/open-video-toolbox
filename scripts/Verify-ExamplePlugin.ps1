<#
.SYNOPSIS
Verifies the repository example plugin end-to-end.

.DESCRIPTION
Generates a temporary sample input, then runs the existing CLI plugin flow
against `examples/plugin-example`:
`validate-plugin` -> `templates --summary` -> `templates <id> --write-examples`
-> `init-plan` -> `validate-plan`.

The script emits a structured JSON summary so contributors and CI can verify
that the example plugin still represents a working submission baseline.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\scripts\Verify-ExamplePlugin.ps1

.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\scripts\Verify-ExamplePlugin.ps1 -OutputJsonPath .artifacts\example-plugin-check.json -KeepArtifacts
#>

param(
    [string]$Project = "src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj",
    [string]$PluginDirectory = "examples/plugin-example",
    [string]$TemplateId = "quick-subtitle",
    [string]$Ffmpeg = "ffmpeg",
    [string]$OutputJsonPath,
    [switch]$KeepArtifacts,
    [bool]$NoBuild = $true,
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

function Get-TemporaryDirectory {
    $tempDirectory = [System.IO.Path]::GetTempPath()
    if ([string]::IsNullOrWhiteSpace($tempDirectory)) {
        throw "Could not resolve a temporary directory."
    }

    return $tempDirectory
}

function Get-ProjectRuntimeEntryPoint {
    param(
        [string]$ProjectPath,
        [string]$BuildConfiguration
    )

    $resolvedProjectPath = (Resolve-Path $ProjectPath).Path
    [xml]$projectXml = Get-Content -Raw $resolvedProjectPath

    $assemblyName = $projectXml.Project.PropertyGroup.AssemblyName | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($assemblyName)) {
        $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($resolvedProjectPath)
    }

    $targetFramework = $projectXml.Project.PropertyGroup.TargetFramework | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($targetFramework)) {
        throw "Could not resolve TargetFramework from project file: $resolvedProjectPath"
    }

    $projectDirectory = Split-Path -Parent $resolvedProjectPath
    $entryPointPath = Join-Path $projectDirectory ("bin/" + $BuildConfiguration + "/" + $targetFramework + "/" + $assemblyName + ".dll")

    if (-not (Test-Path $entryPointPath)) {
        throw "Expected built CLI entry point was not found: $entryPointPath"
    }

    return $entryPointPath
}

function Invoke-CliCommand {
    param(
        [string]$ProjectPath,
        [string[]]$Arguments,
        [bool]$SkipBuild,
        [string]$BuildConfiguration
    )

    $executablePath = "dotnet"
    if ($SkipBuild) {
        $dotnetArgs = @((Get-ProjectRuntimeEntryPoint -ProjectPath $ProjectPath -BuildConfiguration $BuildConfiguration))
        $dotnetArgs += $Arguments
    }
    else {
        $dotnetArgs = @("run", "--configuration", $BuildConfiguration, "--project", $ProjectPath, "--")
        $dotnetArgs += $Arguments
    }

    $tempDirectory = Get-TemporaryDirectory
    $stdoutPath = Join-Path $tempDirectory ("ovt-plugin-out-" + [guid]::NewGuid().ToString("N") + ".log")
    $stderrPath = Join-Path $tempDirectory ("ovt-plugin-err-" + [guid]::NewGuid().ToString("N") + ".log")

    try {
        $duration = Measure-Command {
            $process = Start-Process -FilePath $executablePath -ArgumentList $dotnetArgs -Wait -PassThru -NoNewWindow -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
            if ($process.ExitCode -ne 0) {
                $stdout = if (Test-Path $stdoutPath) { Get-Content -Raw $stdoutPath } else { "" }
                $stderr = if (Test-Path $stderrPath) { Get-Content -Raw $stderrPath } else { "" }
                $detail = if ([string]::IsNullOrWhiteSpace($stderr)) { $stdout } else { $stderr }
                throw "CLI command failed with exit code $($process.ExitCode): dotnet $($dotnetArgs -join ' ')`n$detail"
            }
        }

        return [ordered]@{
            durationMs = [int][Math]::Round($duration.TotalMilliseconds)
            stdout = if (Test-Path $stdoutPath) { Get-Content -Raw $stdoutPath } else { "" }
            stderr = if (Test-Path $stderrPath) { Get-Content -Raw $stderrPath } else { "" }
        }
    }
    finally {
        if (Test-Path $stdoutPath) {
            Remove-Item -Force $stdoutPath
        }

        if (Test-Path $stderrPath) {
            Remove-Item -Force $stderrPath
        }
    }
}

function Join-ProcessArguments {
    param(
        [string[]]$Arguments
    )

    return ($Arguments | ForEach-Object {
        if ($_ -notmatch '[\s"]') {
            $_
        }
        else {
            '"' + ($_ -replace '(\\*)"', '$1$1\"') + '"'
        }
    }) -join ' '
}

function Invoke-NativeCommandQuiet {
    param(
        [string]$ExecutablePath,
        [string[]]$Arguments
    )

    $tempDirectory = Get-TemporaryDirectory
    $stdoutPath = Join-Path $tempDirectory ("ovt-native-out-" + [guid]::NewGuid().ToString("N") + ".log")
    $stderrPath = Join-Path $tempDirectory ("ovt-native-err-" + [guid]::NewGuid().ToString("N") + ".log")

    try {
        $process = Start-Process -FilePath $ExecutablePath -ArgumentList (Join-ProcessArguments -Arguments $Arguments) -Wait -PassThru -NoNewWindow -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
        if ($process.ExitCode -ne 0) {
            $stdout = if (Test-Path $stdoutPath) { Get-Content -Raw $stdoutPath } else { "" }
            $stderr = if (Test-Path $stderrPath) { Get-Content -Raw $stderrPath } else { "" }
            $detail = if ([string]::IsNullOrWhiteSpace($stderr)) { $stdout } else { $stderr }
            throw "Native command failed with exit code $($process.ExitCode): $ExecutablePath`n$detail"
        }
    }
    finally {
        if (Test-Path $stdoutPath) {
            Remove-Item -Force $stdoutPath
        }

        if (Test-Path $stderrPath) {
            Remove-Item -Force $stderrPath
        }
    }
}

function Read-JsonFile {
    param(
        [string]$Path
    )

    return Get-Content -Raw $Path | ConvertFrom-Json
}

$workingDirectory = Join-Path (Get-TemporaryDirectory) ("ovt-example-plugin-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $workingDirectory | Out-Null

try {
    $resolvedPluginDirectory = (Resolve-Path $PluginDirectory).Path
    $templateDirectory = Join-Path $resolvedPluginDirectory ("templates/" + $TemplateId)
    $inputPath = Join-Path $workingDirectory "input.mp4"
    $guideDirectory = Join-Path $workingDirectory ".plugin-guide"
    $planPath = Join-Path $workingDirectory "edit.json"
    $renderOutputPath = Join-Path $workingDirectory "final.mp4"
    $validatePluginJsonPath = Join-Path $workingDirectory "validate-plugin.json"
    $templatesSummaryJsonPath = Join-Path $workingDirectory "templates-summary.json"
    $templateGuideJsonPath = Join-Path $workingDirectory "template-guide.json"
    $validatePlanJsonPath = Join-Path $workingDirectory "validate-plan.json"

    Invoke-NativeCommandQuiet -ExecutablePath $Ffmpeg -Arguments @(
        "-y",
        "-f", "lavfi",
        "-i", "testsrc=size=640x360:rate=30",
        "-f", "lavfi",
        "-i", "sine=frequency=880:sample_rate=48000",
        "-t", "2",
        "-c:v", "libx264",
        "-pix_fmt", "yuv420p",
        "-c:a", "aac",
        $inputPath
    )

    $validatePluginResult = Invoke-CliCommand -ProjectPath $Project -Arguments @(
        "validate-plugin",
        "--plugin-dir", $resolvedPluginDirectory,
        "--json-out", $validatePluginJsonPath
    ) -SkipBuild:$NoBuild -BuildConfiguration $Configuration

    $templatesSummaryResult = Invoke-CliCommand -ProjectPath $Project -Arguments @(
        "templates",
        "--plugin-dir", $resolvedPluginDirectory,
        "--summary",
        "--json-out", $templatesSummaryJsonPath
    ) -SkipBuild:$NoBuild -BuildConfiguration $Configuration

    $templateGuideResult = Invoke-CliCommand -ProjectPath $Project -Arguments @(
        "templates",
        $TemplateId,
        "--plugin-dir", $resolvedPluginDirectory,
        "--json-out", $templateGuideJsonPath,
        "--write-examples", $guideDirectory
    ) -SkipBuild:$NoBuild -BuildConfiguration $Configuration

    $initPlanResult = Invoke-CliCommand -ProjectPath $Project -Arguments @(
        "init-plan",
        $inputPath,
        "--template", $TemplateId,
        "--plugin-dir", $resolvedPluginDirectory,
        "--output", $planPath,
        "--render-output", $renderOutputPath
    ) -SkipBuild:$NoBuild -BuildConfiguration $Configuration

    $validatePlanResult = Invoke-CliCommand -ProjectPath $Project -Arguments @(
        "validate-plan",
        "--plan", $planPath,
        "--plugin-dir", $resolvedPluginDirectory,
        "--json-out", $validatePlanJsonPath
    ) -SkipBuild:$NoBuild -BuildConfiguration $Configuration

    $validatePlugin = Read-JsonFile -Path $validatePluginJsonPath
    $templatesSummary = Read-JsonFile -Path $templatesSummaryJsonPath
    $templateGuide = Read-JsonFile -Path $templateGuideJsonPath
    $plan = Read-JsonFile -Path $planPath
    $validatePlan = Read-JsonFile -Path $validatePlanJsonPath

    foreach ($pluginSkeletonFile in @(
        (Join-Path $templateDirectory "artifacts.json"),
        (Join-Path $templateDirectory "template-params.json")
    )) {
        if (-not (Test-Path $pluginSkeletonFile)) {
            throw "Expected example plugin skeleton file was not found: $pluginSkeletonFile"
        }
    }

    if (-not $validatePlugin.payload.isValid) {
        throw "validate-plugin did not return payload.isValid = true."
    }

    if (-not $validatePlan.payload.isValid) {
        throw "validate-plan did not return payload.isValid = true."
    }

    if ($templateGuide.source.kind -ne "plugin") {
        throw "templates <id> did not return plugin source metadata."
    }

    if ($plan.template.source.kind -ne "plugin") {
        throw "init-plan did not persist template.source.kind = plugin."
    }

    foreach ($requiredFile in @(
        (Join-Path $guideDirectory "guide.json"),
        (Join-Path $guideDirectory "artifacts.json"),
        (Join-Path $guideDirectory "template-params.json"),
        (Join-Path $guideDirectory "commands.json"),
        (Join-Path $guideDirectory "commands.ps1"),
        (Join-Path $guideDirectory "commands.cmd"),
        (Join-Path $guideDirectory "commands.sh")
    )) {
        if (-not (Test-Path $requiredFile)) {
            throw "Expected example output file was not written: $requiredFile"
        }
    }

    $result = [ordered]@{
        schemaVersion = 1
        checkedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        pluginDirectory = $resolvedPluginDirectory
        templateId = $TemplateId
        workingDirectoryKept = [bool]$KeepArtifacts
        workingDirectory = if ($KeepArtifacts) { $workingDirectory } else { $null }
        checks = [ordered]@{
            validatePlugin = [ordered]@{
                durationMs = $validatePluginResult.durationMs
                isValid = $validatePlugin.payload.isValid
            }
            templatesSummary = [ordered]@{
                durationMs = $templatesSummaryResult.durationMs
                pluginCount = @($templatesSummary.plugins).Count
                templateCount = @($templatesSummary.templates).Count
            }
            templateGuide = [ordered]@{
                durationMs = $templateGuideResult.durationMs
                sourceKind = $templateGuide.source.kind
                wroteExamples = $true
            }
            initPlan = [ordered]@{
                durationMs = $initPlanResult.durationMs
                templateSourceKind = $plan.template.source.kind
                outputPath = if ($KeepArtifacts) { $planPath } else { "temp://edit.json" }
            }
            validatePlan = [ordered]@{
                durationMs = $validatePlanResult.durationMs
                isValid = $validatePlan.payload.isValid
            }
        }
    }

    $json = $result | ConvertTo-Json -Depth 10

    if ($OutputJsonPath) {
        $outputDirectory = Split-Path -Parent $OutputJsonPath
        if ($outputDirectory) {
            New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
        }

        $json | Set-Content -Path $OutputJsonPath -Encoding utf8
    }

    $json
}
finally {
    if (-not $KeepArtifacts -and (Test-Path $workingDirectory)) {
        Remove-Item -Recurse -Force $workingDirectory
    }
}
