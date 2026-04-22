<#
.SYNOPSIS
Verifies the current dependency baseline for the CLI runtime.

.DESCRIPTION
Runs doctor plus the filtered Core and CLI real-media smoke tests and prints
a JSON summary for maintainers.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\scripts\Verify-DependencyBaseline.ps1

.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\scripts\Verify-DependencyBaseline.ps1 -OutputJsonPath .artifacts\dependency-baseline.json
#>

param(
    [string]$CliProject = "src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj",
    [string]$CoreTestsProject = "src/OpenVideoToolbox.Core.Tests/OpenVideoToolbox.Core.Tests.csproj",
    [string]$CliTestsProject = "src/OpenVideoToolbox.Cli.Tests/OpenVideoToolbox.Cli.Tests.csproj",
    [string]$OutputJsonPath,
    [bool]$NoBuild = $false
)

$ErrorActionPreference = "Stop"

function Invoke-DotnetObservedCommand {
    param(
        [string[]]$Arguments
    )

    $stdoutPath = Join-Path $env:TEMP ("ovt-dotnet-out-" + [guid]::NewGuid().ToString("N") + ".log")
    $stderrPath = Join-Path $env:TEMP ("ovt-dotnet-err-" + [guid]::NewGuid().ToString("N") + ".log")

    try {
        $duration = Measure-Command {
            $process = Start-Process -FilePath "dotnet" -ArgumentList $Arguments -Wait -PassThru -NoNewWindow -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
            if ($process.ExitCode -ne 0) {
                $stdout = if (Test-Path $stdoutPath) { Get-Content -Raw $stdoutPath } else { "" }
                $stderr = if (Test-Path $stderrPath) { Get-Content -Raw $stderrPath } else { "" }
                $detail = if ([string]::IsNullOrWhiteSpace($stderr)) { $stdout } else { $stderr }
                throw "dotnet command failed with exit code $($process.ExitCode): dotnet $($Arguments -join ' ')`n$detail"
            }
        }

        return [ordered]@{
            durationMs = [int][Math]::Round($duration.TotalMilliseconds)
            stdout = if (Test-Path $stdoutPath) { Get-Content -Raw $stdoutPath } else { "" }
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

$doctorJsonPath = Join-Path $env:TEMP ("ovt-doctor-" + [guid]::NewGuid().ToString("N") + ".json")

try {
    $doctorArgs = @("run")
    if ($NoBuild) {
        $doctorArgs += "--no-build"
    }

    $doctorArgs += @("--project", $CliProject, "--", "doctor", "--json-out", $doctorJsonPath)
    $doctorResult = Invoke-DotnetObservedCommand -Arguments $doctorArgs
    $doctorPayload = (Get-Content -Raw $doctorJsonPath | ConvertFrom-Json).payload

    $coreArgs = @(
        "test", $CoreTestsProject,
        "--filter", "FullyQualifiedName~RealMediaSmokeTests",
        "-v", "minimal"
    )
    if ($NoBuild) {
        $coreArgs += "--no-build"
    }
    $coreResult = Invoke-DotnetObservedCommand -Arguments $coreArgs

    $cliArgs = @(
        "test", $CliTestsProject,
        "--filter", "FullyQualifiedName~CliRealMediaSmokeTests",
        "-v", "minimal"
    )
    if ($NoBuild) {
        $cliArgs += "--no-build"
    }
    $cliResult = Invoke-DotnetObservedCommand -Arguments $cliArgs

    $summary = [ordered]@{
        schemaVersion = 1
        observedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        doctor = [ordered]@{
            durationMs = $doctorResult.durationMs
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
        tests = [ordered]@{
            coreRealMediaSmoke = [ordered]@{
                durationMs = $coreResult.durationMs
                project = $CoreTestsProject
                filter = "FullyQualifiedName~RealMediaSmokeTests"
            }
            cliRealMediaSmoke = [ordered]@{
                durationMs = $cliResult.durationMs
                project = $CliTestsProject
                filter = "FullyQualifiedName~CliRealMediaSmokeTests"
            }
        }
    }

    $json = $summary | ConvertTo-Json -Depth 10

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
    if (Test-Path $doctorJsonPath) {
        Remove-Item -Force $doctorJsonPath
    }
}
