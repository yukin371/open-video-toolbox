[CmdletBinding()]
param(
    [string]$PackageVersion = "0.1.0",

    [string]$ReleaseTag,

    [string]$PackageIdentifier = "OpenVideoToolbox.Cli",

    [string]$License = "MIT",

    [string]$LicenseUrl,

    [string]$OutputRoot = ".\\submissions"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ReleaseTag)) {
    $ReleaseTag = "v$PackageVersion"
}

if ([string]::IsNullOrWhiteSpace($LicenseUrl)) {
    $LicenseUrl = "https://github.com/yukin371/open-video-toolbox/blob/$ReleaseTag/LICENSE"
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$invocationDirectory = (Get-Location).ProviderPath
$packageIdSegments = $PackageIdentifier.Split('.')
if ($packageIdSegments.Length -ne 2) {
    throw "PackageIdentifier '$PackageIdentifier' is expected to use Publisher.Application format for winget-pkgs path export."
}

$publisherName = $packageIdSegments[0]
$applicationName = $packageIdSegments[1]
$letterDirectory = $publisherName.Substring(0, 1).ToLowerInvariant()
$resolvedOutputRoot = if ([System.IO.Path]::IsPathRooted($OutputRoot)) {
    [System.IO.Path]::GetFullPath($OutputRoot)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $invocationDirectory $OutputRoot))
}
$manifestOutputPath = Join-Path $resolvedOutputRoot ("manifests\" + $letterDirectory + "\" + $publisherName + "\" + $applicationName + "\" + $PackageVersion)
$tempRenderPath = Join-Path ([System.IO.Path]::GetTempPath()) ("ovt-winget-export-" + [guid]::NewGuid().ToString("N"))

try {
    & (Join-Path $scriptDirectory "Test-WinGetSubmissionReadiness.ps1") `
        -PackageVersion $PackageVersion `
        -ReleaseTag $ReleaseTag

    & (Join-Path $scriptDirectory "Prepare-WinGetSubmission.ps1") `
        -PackageVersion $PackageVersion `
        -ReleaseTag $ReleaseTag `
        -License $License `
        -LicenseUrl $LicenseUrl `
        -OutputDirectory $tempRenderPath

    New-Item -ItemType Directory -Force -Path $manifestOutputPath | Out-Null
    Get-ChildItem -LiteralPath $tempRenderPath -File | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $manifestOutputPath -Force
    }

    $notesPath = Join-Path $resolvedOutputRoot ("submission-notes-" + $PackageVersion + ".md")
    @"
# WinGet Submission Notes

- PackageIdentifier: $PackageIdentifier
- PackageVersion: $PackageVersion
- ReleaseTag: $ReleaseTag
- ManifestPath: manifests/$letterDirectory/$publisherName/$applicationName/$PackageVersion
- License: $License
- LicenseUrl: $LicenseUrl
- ReleaseNotesUrl: https://github.com/yukin371/open-video-toolbox/releases/tag/$ReleaseTag

Next step:

1. Copy `manifests/$letterDirectory/$publisherName/$applicationName/$PackageVersion` into your `winget-pkgs` fork.
2. Run `winget validate <path-to-manifest-folder>`.
3. Optionally run `winget install --manifest <path-to-manifest-folder>`.
4. Submit the PR to `microsoft/winget-pkgs`.
"@ | Set-Content -LiteralPath $notesPath -Encoding utf8

    Write-Host "Exported winget submission bundle to '$manifestOutputPath'."
    Write-Host "Wrote submission notes to '$notesPath'."
}
finally {
    if (Test-Path -LiteralPath $tempRenderPath) {
        Remove-Item -LiteralPath $tempRenderPath -Recurse -Force
    }
}
