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

You can render the template set locally with:

```powershell
.\Prepare-WinGetSubmission.ps1 `
  -PackageVersion 0.1.0 `
  -License <license> `
  -LicenseUrl <license-url> `
  -OutputDirectory .\out
```

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
