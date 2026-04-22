[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackageVersion,

    [string]$ReleaseTag,

    [string]$RepositoryOwner = "yukin371",

    [string]$RepositoryName = "open-video-toolbox",

    [Parameter(Mandatory = $true)]
    [string]$License,

    [Parameter(Mandatory = $true)]
    [string]$LicenseUrl,

    [string]$AssetName = "ovt-win-x64.exe",

    [string]$OutputDirectory = ".\out"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ReleaseTag)) {
    $ReleaseTag = "v$PackageVersion"
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$installerUrl = "https://github.com/$RepositoryOwner/$RepositoryName/releases/download/$ReleaseTag/$AssetName"
$releaseNotesUrl = "https://github.com/$RepositoryOwner/$RepositoryName/releases/tag/$ReleaseTag"
$tempFile = Join-Path ([System.IO.Path]::GetTempPath()) "$AssetName.$([guid]::NewGuid().ToString('N')).download"

try {
    Invoke-WebRequest -Uri $installerUrl -OutFile $tempFile
    $hash = (Get-FileHash -LiteralPath $tempFile -Algorithm SHA256).Hash

    & (Join-Path $scriptDirectory "Render-WinGetManifest.ps1") `
        -PackageVersion $PackageVersion `
        -InstallerUrl $installerUrl `
        -InstallerSha256 $hash `
        -ReleaseNotesUrl $releaseNotesUrl `
        -License $License `
        -LicenseUrl $LicenseUrl `
        -OutputDirectory $OutputDirectory
}
finally {
    if (Test-Path -LiteralPath $tempFile) {
        Remove-Item -LiteralPath $tempFile -Force
    }
}
