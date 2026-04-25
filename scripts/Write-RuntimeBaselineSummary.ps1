<#
.SYNOPSIS
Writes a concise markdown summary for runtime and dependency baseline results.

.DESCRIPTION
Consumes the JSON outputs from Measure-RuntimeBaseline.ps1 and
Verify-DependencyBaseline.ps1 and renders a maintainer-friendly markdown summary.
When GITHUB_STEP_SUMMARY is available, the script writes directly to the GitHub
Actions job summary file.

.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\scripts\Write-RuntimeBaselineSummary.ps1 `
    -RuntimeBaselinePath .artifacts\runtime-baseline.json `
    -DependencyBaselinePath .artifacts\dependency-baseline.json

.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\scripts\Write-RuntimeBaselineSummary.ps1 `
    -RuntimeBaselinePath .artifacts\runtime-baseline.json `
    -DependencyBaselinePath .artifacts\dependency-baseline.json `
    -SummaryPath .artifacts\runtime-baseline-summary.md
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$RuntimeBaselinePath,
    [Parameter(Mandatory = $true)]
    [string]$DependencyBaselinePath,
    [string]$ThresholdCheckPath,
    [string]$SummaryPath = $env:GITHUB_STEP_SUMMARY
)

$ErrorActionPreference = "Stop"

function Get-CommandDisplayName {
    param(
        [string]$CommandId
    )

    switch ($CommandId) {
        "doctor" { return "doctor" }
        "probe" { return "probe" }
        "scaffoldTemplateBatch" { return "scaffold-template-batch" }
        "renderBatchPreview" { return "render-batch --preview" }
        "renderPreview" { return "render --preview" }
        default { return $CommandId }
    }
}
function Get-MarkdownCellValue {
    param(
        $Value
    )

    if ($null -eq $Value) {
        return "-"
    }

    $stringValue = [string]$Value
    if ([string]::IsNullOrWhiteSpace($stringValue)) {
        return "-"
    }

    return $stringValue.Replace("|", "\|").Replace("`r", " ").Replace("`n", " ")
}

if ([string]::IsNullOrWhiteSpace($SummaryPath)) {
    throw "SummaryPath is required when GITHUB_STEP_SUMMARY is not set."
}

if (-not (Test-Path $RuntimeBaselinePath)) {
    throw "Runtime baseline JSON not found: $RuntimeBaselinePath"
}

if (-not (Test-Path $DependencyBaselinePath)) {
    throw "Dependency baseline JSON not found: $DependencyBaselinePath"
}

$runtimeBaseline = Get-Content -Raw $RuntimeBaselinePath | ConvertFrom-Json
$dependencyBaseline = Get-Content -Raw $DependencyBaselinePath | ConvertFrom-Json
$thresholdCheck = $null

if (-not [string]::IsNullOrWhiteSpace($ThresholdCheckPath)) {
    if (-not (Test-Path $ThresholdCheckPath)) {
        throw "Runtime threshold check JSON not found: $ThresholdCheckPath"
    }

    $thresholdCheck = Get-Content -Raw $ThresholdCheckPath | ConvertFrom-Json
}

$summaryLines = New-Object System.Collections.Generic.List[string]
$summaryLines.Add("# Runtime Baseline Summary")
$summaryLines.Add("")
$summaryLines.Add("- Runtime observed at: $(Get-MarkdownCellValue $runtimeBaseline.observedAtUtc)")
$summaryLines.Add("- Dependency observed at: $(Get-MarkdownCellValue $dependencyBaseline.observedAtUtc)")
$summaryLines.Add("- Doctor healthy: $(if ($dependencyBaseline.doctor.isHealthy) { "true" } else { "false" })")
$summaryLines.Add("- Missing required dependencies: $(Get-MarkdownCellValue $dependencyBaseline.doctor.missingRequiredCount)")
$summaryLines.Add("- Missing optional dependencies: $(Get-MarkdownCellValue $dependencyBaseline.doctor.missingOptionalCount)")
if ($null -ne $thresholdCheck) {
    $summaryLines.Add("- Runtime thresholds healthy: $(if ($thresholdCheck.isWithinThresholds) { "true" } else { "false" })")
}
$summaryLines.Add("")
$summaryLines.Add("## Command Durations")
$summaryLines.Add("")
$summaryLines.Add("| Command | Duration (ms) |")
$summaryLines.Add("| --- | ---: |")
foreach ($command in $runtimeBaseline.commands.PSObject.Properties) {
    $summaryLines.Add("| $(Get-MarkdownCellValue (Get-CommandDisplayName -CommandId $command.Name)) | $(Get-MarkdownCellValue $command.Value.durationMs) |")
}
$summaryLines.Add("")

if ($null -ne $thresholdCheck) {
    $summaryLines.Add("## Runtime Threshold Check")
    $summaryLines.Add("")
    $summaryLines.Add("| Command | Duration (ms) | Max (ms) | Within Threshold |")
    $summaryLines.Add("| --- | ---: | ---: | --- |")

    foreach ($command in $thresholdCheck.commands.PSObject.Properties) {
        $summaryLines.Add(
            "| $(Get-MarkdownCellValue (Get-CommandDisplayName -CommandId $command.Name)) | $(Get-MarkdownCellValue $command.Value.durationMs) | $(Get-MarkdownCellValue $command.Value.maxDurationMs) | $(Get-MarkdownCellValue $command.Value.isWithinThreshold) |"
        )
    }

    if ($thresholdCheck.exceededCommands.Count -gt 0) {
        $summaryLines.Add("")
        $summaryLines.Add("Exceeded commands:")
        foreach ($command in $thresholdCheck.exceededCommands) {
            $summaryLines.Add("- `$(Get-MarkdownCellValue (Get-CommandDisplayName -CommandId $command.id))`: $(Get-MarkdownCellValue $command.durationMs) ms > $(Get-MarkdownCellValue $command.maxDurationMs) ms")
        }
    }

    $summaryLines.Add("")
}
$summaryLines.Add("## Dependency Status")
$summaryLines.Add("")
$summaryLines.Add("| Dependency | Required | Available | Source | Resolved |")
$summaryLines.Add("| --- | --- | --- | --- | --- |")

foreach ($dependency in $dependencyBaseline.doctor.dependencies) {
    $summaryLines.Add(
        "| $(Get-MarkdownCellValue $dependency.id) | $(Get-MarkdownCellValue $dependency.required) | $(Get-MarkdownCellValue $dependency.isAvailable) | $(Get-MarkdownCellValue $dependency.source) | $(Get-MarkdownCellValue $dependency.resolvedValue) |"
    )
}

$summaryLines.Add("")
$summaryLines.Add("## Real Smoke Durations")
$summaryLines.Add("")
$summaryLines.Add("| Check | Duration (ms) | Filter |")
$summaryLines.Add("| --- | ---: | --- |")
$summaryLines.Add("| Core real-media smoke | $(Get-MarkdownCellValue $dependencyBaseline.tests.coreRealMediaSmoke.durationMs) | $(Get-MarkdownCellValue $dependencyBaseline.tests.coreRealMediaSmoke.filter) |")
$summaryLines.Add("| CLI real-media smoke | $(Get-MarkdownCellValue $dependencyBaseline.tests.cliRealMediaSmoke.durationMs) | $(Get-MarkdownCellValue $dependencyBaseline.tests.cliRealMediaSmoke.filter) |")
$summaryLines.Add("")
$summaryLines.Add("Artifacts:")
$summaryLines.Add(('- runtime baseline JSON: `' + (Get-MarkdownCellValue $RuntimeBaselinePath) + '`'))
$summaryLines.Add(('- dependency baseline JSON: `' + (Get-MarkdownCellValue $DependencyBaselinePath) + '`'))
if ($null -ne $thresholdCheck) {
    $summaryLines.Add(('- runtime threshold check JSON: `' + (Get-MarkdownCellValue $ThresholdCheckPath) + '`'))
}

$outputDirectory = Split-Path -Parent $SummaryPath
if ($outputDirectory) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$summaryLines -join [Environment]::NewLine | Set-Content -Path $SummaryPath -Encoding utf8

Get-Content -Raw $SummaryPath
