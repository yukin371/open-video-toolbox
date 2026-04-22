[CmdletBinding()]
param(
    [string]$PackageVersion,

    [string]$ReleaseTag,

    [string]$RepositoryOwner = "yukin371",

    [string]$RepositoryName = "open-video-toolbox",

    [string]$CliProjectPath = "src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj",

    [string]$OutputJsonPath,

    [switch]$NoFailOnBlockers
)

$ErrorActionPreference = "Stop"

function New-ReadinessCheck {
    param(
        [string]$Id,
        [string]$Title,
        [string]$Status,
        [bool]$Blocker,
        [string]$Detail,
        $Metadata = $null
    )

    return [pscustomobject]@{
        id = $Id
        title = $Title
        status = $Status
        blocker = $Blocker
        detail = $Detail
        metadata = $Metadata
    }
}

function Invoke-GitCommand {
    param(
        [string[]]$Arguments,
        [string]$WorkingDirectory
    )

    $output = & git -C $WorkingDirectory @Arguments 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $null
    }

    return $output
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptDirectory "..\.."))
$resolvedCliProjectPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $CliProjectPath))

if (-not (Test-Path -LiteralPath $resolvedCliProjectPath)) {
    throw "CLI project file was not found: $resolvedCliProjectPath"
}

[xml]$cliProject = Get-Content -LiteralPath $resolvedCliProjectPath -Raw
$projectProperties = $cliProject.Project.PropertyGroup
$projectVersion = [string]($projectProperties.VersionPrefix | Select-Object -First 1)
if ([string]::IsNullOrWhiteSpace($projectVersion)) {
    $projectVersion = [string]($projectProperties.Version | Select-Object -First 1)
}

if ([string]::IsNullOrWhiteSpace($projectVersion)) {
    throw "Could not resolve project version from $resolvedCliProjectPath"
}

if ([string]::IsNullOrWhiteSpace($PackageVersion)) {
    $PackageVersion = $projectVersion
}

if ([string]::IsNullOrWhiteSpace($ReleaseTag)) {
    $ReleaseTag = "v$PackageVersion"
}

$checks = New-Object System.Collections.Generic.List[object]

if ($PackageVersion -eq $projectVersion) {
    $checks.Add((New-ReadinessCheck -Id "package-version-match" -Title "Package version matches CLI project version" -Status "pass" -Blocker $false -Detail "PackageVersion '$PackageVersion' matches project version '$projectVersion'." -Metadata ([ordered]@{
        packageVersion = $PackageVersion
        projectVersion = $projectVersion
    })))
}
else {
    $checks.Add((New-ReadinessCheck -Id "package-version-match" -Title "Package version matches CLI project version" -Status "fail" -Blocker $true -Detail "PackageVersion '$PackageVersion' does not match project version '$projectVersion'." -Metadata ([ordered]@{
        packageVersion = $PackageVersion
        projectVersion = $projectVersion
    })))
}

$licenseFiles = @(Get-ChildItem -LiteralPath $repoRoot -File -Filter "LICENSE*" -ErrorAction SilentlyContinue)
if ($licenseFiles.Count -gt 0) {
    $checks.Add((New-ReadinessCheck -Id "license-source" -Title "Repository license source exists" -Status "pass" -Blocker $false -Detail "Found repository license source file(s)." -Metadata ([ordered]@{
        files = @($licenseFiles | ForEach-Object { $_.Name })
    })))
}
else {
    $checks.Add((New-ReadinessCheck -Id "license-source" -Title "Repository license source exists" -Status "fail" -Blocker $true -Detail "No LICENSE* file was found at the repository root. Winget manifest license metadata still has no stable source." -Metadata ([ordered]@{
        repositoryRoot = $repoRoot
    })))
}

$localTag = Invoke-GitCommand -Arguments @("tag", "-l", $ReleaseTag) -WorkingDirectory $repoRoot
if ($null -ne $localTag -and ($localTag | Select-Object -First 1) -eq $ReleaseTag) {
    $checks.Add((New-ReadinessCheck -Id "git-tag" -Title "Release tag exists locally" -Status "pass" -Blocker $false -Detail "Found local git tag '$ReleaseTag'." -Metadata ([ordered]@{
        releaseTag = $ReleaseTag
    })))
}
else {
    $checks.Add((New-ReadinessCheck -Id "git-tag" -Title "Release tag exists locally" -Status "fail" -Blocker $true -Detail "Local git tag '$ReleaseTag' was not found." -Metadata ([ordered]@{
        releaseTag = $ReleaseTag
    })))
}

$releaseApiUrl = "https://api.github.com/repos/$RepositoryOwner/$RepositoryName/releases/tags/$ReleaseTag"
$releaseResponse = $null
$releaseError = $null

try {
    $releaseResponse = Invoke-RestMethod -Uri $releaseApiUrl -Headers @{ "User-Agent" = "OpenVideoToolbox-WinGet-Readiness" }
}
catch {
    $releaseError = $_.Exception.Message
}

if ($null -ne $releaseResponse) {
    $checks.Add((New-ReadinessCheck -Id "github-release" -Title "GitHub Release exists for tag" -Status "pass" -Blocker $false -Detail "Found GitHub Release for '$ReleaseTag'." -Metadata ([ordered]@{
        releaseUrl = $releaseResponse.html_url
        assets = @($releaseResponse.assets | ForEach-Object { $_.name })
    })))

    $assetNames = @($releaseResponse.assets | ForEach-Object { $_.name })
    foreach ($expectedAsset in @("ovt-win-x64.exe", "ovt-win-x64.zip")) {
        if ($assetNames -contains $expectedAsset) {
            $checks.Add((New-ReadinessCheck -Id ("release-asset-" + $expectedAsset) -Title "Release asset '$expectedAsset' exists" -Status "pass" -Blocker $false -Detail "Found '$expectedAsset' in GitHub Release assets." -Metadata ([ordered]@{
                assetName = $expectedAsset
                releaseTag = $ReleaseTag
            })))
        }
        else {
            $checks.Add((New-ReadinessCheck -Id ("release-asset-" + $expectedAsset) -Title "Release asset '$expectedAsset' exists" -Status "fail" -Blocker $true -Detail "GitHub Release '$ReleaseTag' is missing expected asset '$expectedAsset'." -Metadata ([ordered]@{
                assetName = $expectedAsset
                releaseTag = $ReleaseTag
                assets = $assetNames
            })))
        }
    }
}
else {
    $checks.Add((New-ReadinessCheck -Id "github-release" -Title "GitHub Release exists for tag" -Status "fail" -Blocker $true -Detail "GitHub Release for '$ReleaseTag' was not found or could not be queried." -Metadata ([ordered]@{
        releaseApiUrl = $releaseApiUrl
        error = $releaseError
    })))
}

$blockerCount = @($checks | Where-Object { $_.blocker }).Count
$warningCount = @($checks | Where-Object { $_.status -eq "warn" }).Count
$checkArray = @($checks | ForEach-Object { $_ })
$repositoryInfo = New-Object PSObject
$repositoryInfo | Add-Member -NotePropertyName "owner" -NotePropertyValue $RepositoryOwner
$repositoryInfo | Add-Member -NotePropertyName "name" -NotePropertyValue $RepositoryName

$result = New-Object PSObject
$result | Add-Member -NotePropertyName "schemaVersion" -NotePropertyValue 1
$result | Add-Member -NotePropertyName "observedAtUtc" -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString("O"))
$result | Add-Member -NotePropertyName "repository" -NotePropertyValue $repositoryInfo
$result | Add-Member -NotePropertyName "packageVersion" -NotePropertyValue $PackageVersion
$result | Add-Member -NotePropertyName "releaseTag" -NotePropertyValue $ReleaseTag
$result | Add-Member -NotePropertyName "isReady" -NotePropertyValue ($blockerCount -eq 0)
$result | Add-Member -NotePropertyName "blockerCount" -NotePropertyValue $blockerCount
$result | Add-Member -NotePropertyName "warningCount" -NotePropertyValue $warningCount
$result | Add-Member -NotePropertyName "checks" -NotePropertyValue $checkArray

$json = $result | ConvertTo-Json -Depth 10

if ($OutputJsonPath) {
    $outputDirectory = Split-Path -Parent $OutputJsonPath
    if ($outputDirectory) {
        New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
    }

    $json | Set-Content -LiteralPath $OutputJsonPath -Encoding utf8
}

$json

if (-not $NoFailOnBlockers -and -not $result.isReady) {
    exit 1
}
