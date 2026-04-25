# WinGet Packaging Draft

This directory keeps the repository-local draft for the `E2-A2` winget submission path.

It is intentionally a staging area, not a second release pipeline:

- GitHub Release remains the canonical asset source.
- The official submission target remains `microsoft/winget-pkgs`.
- These files are templates and process notes for preparing a future submission.

## Chosen Direction

The current preferred installer strategy is `portable`.

Reason:

- `ovt` is already published as a self-contained single executable on Windows.
- `portable` matches the CLI shape more closely than pretending it is a traditional setup installer.
- The repository still keeps a `.zip` asset for manual download, but winget should target the raw `ovt-win-x64.exe` asset.

## Current State

As of `v0.1.0`:

- the repository now has a canonical MIT license source at the repo root
- GitHub detects the repository license as `MIT`
- the `v0.1.0` GitHub Release now exists
- the release includes both `ovt-win-x64.zip` and `ovt-win-x64.exe`
- `Test-WinGetSubmissionReadiness.ps1` now passes for the current version
- the manifest set can be rendered locally from the release asset URL
- the exported `v0.1.0` manifest bundle now also passes local `winget validate`

## Expected Release Assets

For the Windows release path, the repository now aims to publish both:

- `ovt-win-x64.zip`
- `ovt-win-x64.exe`

The `ovt-win-x64.exe` asset is intended for the winget `portable` installer manifest.

## Draft Workflow

1. Tag and publish a new release from this repository.
2. Download the published `ovt-win-x64.exe` asset for that tag.
3. Compute the SHA256 hash of the exact asset referenced by the release URL.
4. Render the manifest set from the repository-local templates.
5. Copy the rendered files into a versioned folder in a `winget-pkgs` fork.
6. Run local validation:

```powershell
winget validate <path-to-manifest-folder>
winget install --manifest <path-to-manifest-folder>
```

7. If local validation passes, submit the manifest PR to `microsoft/winget-pkgs`.

If you want the repository to prepare a `winget-pkgs`-shaped folder for you directly, run:

```powershell
.\Export-WinGetSubmissionBundle.ps1 -PackageVersion 0.1.0 -ReleaseTag v0.1.0
```

This exports:

- `submissions/manifests/o/OpenVideoToolbox/Cli/0.1.0/`
- `submissions/submission-notes-0.1.0.md`

Relative `-OutputDirectory` / `-OutputRoot` paths are resolved from the directory where you invoke the script, so running from the repository root with `.\packaging\winget\... -OutputRoot .artifacts\winget-submission` writes to repo-root `.artifacts\winget-submission`.

Before rendering manifests, you can run a repository-local readiness check:

```powershell
.\Test-WinGetSubmissionReadiness.ps1
```

This check is intended to fail fast when the repository is not actually ready yet, for example:

- no stable `LICENSE*` source exists at the repo root
- the target git tag is missing
- the matching GitHub Release does not exist
- the expected Windows release assets are incomplete

For the current `v0.1.0` release, the readiness check now passes:

```powershell
.\Test-WinGetSubmissionReadiness.ps1 -PackageVersion 0.1.0 -ReleaseTag v0.1.0
```

You can render the template set locally with:

```powershell
.\Prepare-WinGetSubmission.ps1 `
  -PackageVersion 0.1.0 `
  -License <license> `
  -LicenseUrl <license-url> `
  -OutputDirectory .\out
```

Current `v0.1.0` example:

```powershell
.\Prepare-WinGetSubmission.ps1 `
  -PackageVersion 0.1.0 `
  -License MIT `
  -LicenseUrl https://github.com/yukin371/open-video-toolbox/blob/v0.1.0/LICENSE `
  -OutputDirectory .\out\v0.1.0
```

Current `v0.1.0` validation sample from the repository root:

```powershell
.\packaging\winget\Export-WinGetSubmissionBundle.ps1 `
  -PackageVersion 0.1.0 `
  -ReleaseTag v0.1.0 `
  -OutputRoot .artifacts\winget-submission

winget validate .artifacts\winget-submission\manifests\o\OpenVideoToolbox\Cli\0.1.0
```

Observed on 2026-04-25:

- repository-local export succeeded
- `winget validate` returned `Manifest validation succeeded.`

## Template Files

- `OpenVideoToolbox.Cli.yaml.template`
  - version manifest
- `OpenVideoToolbox.Cli.locale.en-US.yaml.template`
  - default locale manifest
- `OpenVideoToolbox.Cli.installer.yaml.template`
  - installer manifest using `portable`
- `Render-WinGetManifest.ps1`
  - deterministic local renderer for the three template files
- `Prepare-WinGetSubmission.ps1`
  - downloads the release asset, computes SHA256, and invokes the renderer
- `Export-WinGetSubmissionBundle.ps1`
  - renders the current release and writes a `winget-pkgs`-shaped submission folder
- `Test-WinGetSubmissionReadiness.ps1`
  - checks release/tag/license readiness before manifest rendering or submission
- `SUBMISSION_CHECKLIST.md`
  - repository-local pre-submit checklist and current blockers

## Placeholder Rules

Replace these values before validation or submission:

- `<PACKAGE_VERSION>`
- `<INSTALLER_URL>`
- `<INSTALLER_SHA256>`
- `<LICENSE>`
- `<LICENSE_URL>`
- `<RELEASE_NOTES_URL>`

## Notes

- Keep `InstallerUrl` pointing directly to the publisher-controlled GitHub Release asset.
- Keep one version per submission.
- Fill license metadata from the repository's real license source; do not guess it in the manifest.
- If the winget manifest schema version changes, update the template files before submission.
- Local `winget validate` passing is a useful readiness signal, but still re-run validation in the actual submission environment before opening the `winget-pkgs` PR.
