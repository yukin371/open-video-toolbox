<#
.SYNOPSIS
Checks the observed runtime baseline against repository thresholds.

.DESCRIPTION
Consumes the JSON output from Measure-RuntimeBaseline.ps1 and compares the
observed command durations with the tracked threshold configuration. The script
prints a structured JSON result, optionally writes it to disk, and can fail
when any threshold is exceeded.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\scripts\Test-RuntimeBaselineThresholds.ps1 `
    -RuntimeBaselinePath .artifacts\runtime-baseline.json

.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\scripts\Test-RuntimeBaselineThresholds.ps1 `
    -RuntimeBaselinePath .artifacts\runtime-baseline.json `
    -OutputJsonPath .artifacts\runtime-threshold-check.json `
    -FailOnExceeded
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$RuntimeBaselinePath,
    [string]$ThresholdConfigPath = "scripts/runtime-baseline.thresholds.json",
    [string]$OutputJsonPath,
    [switch]$FailOnExceeded
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $RuntimeBaselinePath)) {
    throw "Runtime baseline JSON not found: $RuntimeBaselinePath"
}

if (-not (Test-Path $ThresholdConfigPath)) {
    throw "Runtime threshold config not found: $ThresholdConfigPath"
}

$runtimeBaseline = Get-Content -Raw $RuntimeBaselinePath | ConvertFrom-Json
$thresholdConfig = Get-Content -Raw $ThresholdConfigPath | ConvertFrom-Json

$commandChecks = [ordered]@{}
$exceededCommands = New-Object System.Collections.Generic.List[object]

foreach ($commandProperty in $thresholdConfig.commands.PSObject.Properties) {
    $commandId = $commandProperty.Name
    $thresholdEntry = $commandProperty.Value
    $runtimeEntry = $runtimeBaseline.commands.$commandId

    if ($null -eq $runtimeEntry) {
        throw "Runtime baseline is missing command entry '$commandId'."
    }

    $durationMs = [int]$runtimeEntry.durationMs
    $maxDurationMs = [int]$thresholdEntry.maxDurationMs
    $isWithinThreshold = $durationMs -le $maxDurationMs

    $commandChecks[$commandId] = [ordered]@{
        durationMs = $durationMs
        maxDurationMs = $maxDurationMs
        isWithinThreshold = $isWithinThreshold
    }

    if (-not $isWithinThreshold) {
        $exceededCommands.Add([ordered]@{
            id = $commandId
            durationMs = $durationMs
            maxDurationMs = $maxDurationMs
        })
    }
}

$isWithinThresholds = ($exceededCommands.Count -eq 0)
$exceededCommandsArray = @($exceededCommands.ToArray())

$result = [ordered]@{
    schemaVersion = 1
    checkedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    runtimeBaselinePath = $RuntimeBaselinePath
    thresholdConfigPath = $ThresholdConfigPath
    isWithinThresholds = $isWithinThresholds
    commands = $commandChecks
    exceededCommands = $exceededCommandsArray
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

if ($FailOnExceeded -and -not $result.isWithinThresholds) {
    $messages = $exceededCommands | ForEach-Object {
        "$($_.id): $($_.durationMs)ms > $($_.maxDurationMs)ms"
    }
    throw "Runtime baseline exceeded thresholds: $($messages -join '; ')"
}
