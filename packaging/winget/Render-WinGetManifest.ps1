[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackageVersion,

    [Parameter(Mandatory = $true)]
    [string]$InstallerUrl,

    [Parameter(Mandatory = $true)]
    [string]$InstallerSha256,

    [Parameter(Mandatory = $true)]
    [string]$ReleaseNotesUrl,

    [Parameter(Mandatory = $true)]
    [string]$License,

    [Parameter(Mandatory = $true)]
    [string]$LicenseUrl,

    [string]$OutputDirectory = ".\out"
)

$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$resolvedOutputDirectory = [System.IO.Path]::GetFullPath((Join-Path $scriptDirectory $OutputDirectory))

$templateFiles = @(
    "OpenVideoToolbox.Cli.yaml.template",
    "OpenVideoToolbox.Cli.locale.en-US.yaml.template",
    "OpenVideoToolbox.Cli.installer.yaml.template"
)

foreach ($templateFile in $templateFiles) {
    $templatePath = Join-Path $scriptDirectory $templateFile
    if (-not (Test-Path -LiteralPath $templatePath)) {
        throw "Template file '$templatePath' was not found."
    }
}

if ($License.StartsWith("<") -or $LicenseUrl.StartsWith("<")) {
    throw "License metadata must be resolved before rendering winget manifests."
}

New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null

$replacements = @{
    "<PACKAGE_VERSION>" = $PackageVersion
    "<INSTALLER_URL>" = $InstallerUrl
    "<INSTALLER_SHA256>" = $InstallerSha256.ToUpperInvariant()
    "<RELEASE_NOTES_URL>" = $ReleaseNotesUrl
    "<LICENSE>" = $License
    "<LICENSE_URL>" = $LicenseUrl
}

foreach ($templateFile in $templateFiles) {
    $templatePath = Join-Path $scriptDirectory $templateFile
    $content = Get-Content -LiteralPath $templatePath -Raw

    foreach ($placeholder in $replacements.Keys) {
        $content = $content.Replace($placeholder, $replacements[$placeholder])
    }

    $outputFileName = [System.IO.Path]::GetFileNameWithoutExtension($templateFile)
    $outputPath = Join-Path $resolvedOutputDirectory $outputFileName
    Set-Content -LiteralPath $outputPath -Value $content -NoNewline
}

Write-Host "Rendered winget manifests to '$resolvedOutputDirectory'."
