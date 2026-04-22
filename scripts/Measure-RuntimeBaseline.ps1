<#
.SYNOPSIS
Observes a lightweight runtime baseline for the current CLI.

.DESCRIPTION
Generates a temporary sample video, runs doctor / probe / render --preview,
and prints a JSON summary that can be checked into notes or compared later.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\scripts\Measure-RuntimeBaseline.ps1

.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\scripts\Measure-RuntimeBaseline.ps1 -OutputJsonPath .artifacts\runtime-baseline.json -KeepArtifacts
#>

param(
    [string]$Project = "src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj",
    [string]$Ffmpeg = "ffmpeg",
    [string]$Ffprobe = "ffprobe",
    [string]$OutputJsonPath,
    [switch]$KeepArtifacts,
    [bool]$NoBuild = $true
)

$ErrorActionPreference = "Stop"

function Invoke-CliObservedCommand {
    param(
        [string]$ProjectPath,
        [string[]]$Arguments,
        [bool]$SkipBuild
    )

    $dotnetArgs = @("run")
    if ($SkipBuild) {
        $dotnetArgs += "--no-build"
    }

    $dotnetArgs += @("--project", $ProjectPath, "--")
    $dotnetArgs += $Arguments

    $duration = Measure-Command {
        & dotnet @dotnetArgs | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "CLI command failed with exit code ${LASTEXITCODE}: dotnet $($dotnetArgs -join ' ')"
        }
    }

    return [int][Math]::Round($duration.TotalMilliseconds)
}

function New-TemporaryWorkingDirectory {
    $path = Join-Path $env:TEMP ("ovt-runtime-baseline-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $path | Out-Null
    return $path
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

    $stdoutPath = Join-Path $env:TEMP ("ovt-native-out-" + [guid]::NewGuid().ToString("N") + ".log")
    $stderrPath = Join-Path $env:TEMP ("ovt-native-err-" + [guid]::NewGuid().ToString("N") + ".log")

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

$workingDirectory = New-TemporaryWorkingDirectory

try {
    $mediaInputPath = Join-Path $workingDirectory "input.mp4"
    $planPath = Join-Path $workingDirectory "edit.json"
    $renderOutputPath = Join-Path $workingDirectory "final.mp4"
    $doctorJsonPath = Join-Path $workingDirectory "doctor.json"
    $probeJsonPath = Join-Path $workingDirectory "probe.json"
    $renderPreviewJsonPath = Join-Path $workingDirectory "render-preview.json"

    Invoke-NativeCommandQuiet -ExecutablePath $Ffmpeg -Arguments @(
        "-y",
        "-f", "lavfi",
        "-i", "testsrc=size=640x360:rate=30",
        "-f", "lavfi",
        "-i", "sine=frequency=1000:sample_rate=48000",
        "-t", "2",
        "-c:v", "libx264",
        "-pix_fmt", "yuv420p",
        "-c:a", "aac",
        $mediaInputPath
    )

    $plan = [ordered]@{
        schemaVersion = 1
        source = [ordered]@{
            inputPath = $mediaInputPath
        }
        clips = @(
            [ordered]@{
                id = "clip-001"
                in = "00:00:00"
                out = "00:00:01"
            }
        )
        audioTracks = @()
        output = [ordered]@{
            path = $renderOutputPath
            container = "mp4"
        }
    }

    $plan | ConvertTo-Json -Depth 10 | Set-Content -Path $planPath -Encoding utf8

    $doctorDurationMs = Invoke-CliObservedCommand -ProjectPath $Project -Arguments @("doctor", "--json-out", $doctorJsonPath) -SkipBuild:$NoBuild
    $probeDurationMs = Invoke-CliObservedCommand -ProjectPath $Project -Arguments @("probe", $mediaInputPath, "--ffprobe", $Ffprobe, "--json-out", $probeJsonPath) -SkipBuild:$NoBuild
    $renderPreviewDurationMs = Invoke-CliObservedCommand -ProjectPath $Project -Arguments @("render", "--plan", $planPath, "--output", $renderOutputPath, "--preview", "--json-out", $renderPreviewJsonPath) -SkipBuild:$NoBuild

    $doctorPayload = (Get-Content -Raw $doctorJsonPath | ConvertFrom-Json).payload

    $result = [ordered]@{
        schemaVersion = 1
        observedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        workingDirectoryKept = [bool]$KeepArtifacts
        workingDirectory = if ($KeepArtifacts) { $workingDirectory } else { $null }
        sample = [ordered]@{
            durationSeconds = 2
            inputPath = if ($KeepArtifacts) { $mediaInputPath } else { "temp://sample.mp4" }
            renderPlanPath = if ($KeepArtifacts) { $planPath } else { "temp://edit.json" }
        }
        commands = [ordered]@{
            doctor = [ordered]@{
                durationMs = $doctorDurationMs
                jsonOut = if ($KeepArtifacts) { $doctorJsonPath } else { "temp://doctor.json" }
            }
            probe = [ordered]@{
                durationMs = $probeDurationMs
                jsonOut = if ($KeepArtifacts) { $probeJsonPath } else { "temp://probe.json" }
            }
            renderPreview = [ordered]@{
                durationMs = $renderPreviewDurationMs
                jsonOut = if ($KeepArtifacts) { $renderPreviewJsonPath } else { "temp://render-preview.json" }
            }
        }
        doctorSummary = [ordered]@{
            isHealthy = $doctorPayload.isHealthy
            missingRequiredCount = $doctorPayload.missingRequiredCount
            missingOptionalCount = $doctorPayload.missingOptionalCount
            dependencies = @($doctorPayload.dependencies | ForEach-Object {
                [ordered]@{
                    id = $_.id
                    required = $_.required
                    isAvailable = $_.isAvailable
                    source = $_.source
                    resolvedValue = $_.resolvedValue
                }
            })
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
