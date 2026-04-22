[CmdletBinding()]
param(
    [switch]$VerifyOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$hooksDirectory = Join-Path $repoRoot '.githooks'

if (-not (Test-Path $hooksDirectory -PathType Container)) {
    throw "Git hooks directory '$hooksDirectory' was not found."
}

$preCommitHook = Join-Path $hooksDirectory 'pre-commit'
$commitMsgHook = Join-Path $hooksDirectory 'commit-msg'

foreach ($hookPath in @($preCommitHook, $commitMsgHook)) {
    if (-not (Test-Path $hookPath -PathType Leaf)) {
        throw "Required hook '$hookPath' was not found."
    }
}

$configuredPath = git -C $repoRoot config --get core.hooksPath 2>$null
if ($LASTEXITCODE -ne 0) {
    $configuredPath = $null
}

if ($VerifyOnly) {
    if ($configuredPath -eq '.githooks') {
        Write-Host 'Git hooks are enabled via core.hooksPath=.githooks'
        exit 0
    }

    Write-Error "Git hooks are not enabled for this repository. Current core.hooksPath: '$configuredPath'"
    exit 1
}

git -C $repoRoot config --local core.hooksPath .githooks
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to set core.hooksPath to .githooks.'
}

$verifiedPath = git -C $repoRoot config --get core.hooksPath
if ($LASTEXITCODE -ne 0 -or $verifiedPath -ne '.githooks') {
    throw "core.hooksPath verification failed. Current value: '$verifiedPath'"
}

Write-Host 'Git hooks enabled.'
Write-Host "Repository root : $repoRoot"
Write-Host "Hooks directory : $hooksDirectory"
Write-Host "core.hooksPath  : $verifiedPath"
