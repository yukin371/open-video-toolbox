# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Strategy

- **Major (x.0.0):** Breaking changes to CLI output contract (`--json-out` envelope structure, field removal/rename). Always announced with migration notes.
- **Minor (0.x.0):** New CLI commands, new template categories, new plugin capabilities. Backward compatible.
- **Patch (0.0.x):** Bug fixes, dependency updates, documentation corrections. No new features.

### Breaking Change Policy

A change is **breaking** if any of the following change for an existing command:

- Top-level envelope field names (`command`, `preview`, `payload`)
- Payload field names or types that were present in a released version
- Exit code semantics (e.g., 0/1/2 meaning changes)
- `--json-out` file content differs from stdout

Breaking changes **must**:
1. Bump the major version
2. Include migration notes in this changelog
3. Update contract snapshot golden files

### Contract Change Decision Table

| Change type | Breaking | Update golden files | Migration notes | Version impact |
|-------------|----------|---------------------|-----------------|----------------|
| Top-level envelope field removal / rename (`command`, `preview`, `payload`) | Yes | Yes | Required | Major |
| Existing payload field removal / rename / type change | Yes | Yes | Required | Major |
| Exit code semantic change | Yes | Yes if affected output expectations exist | Required | Major |
| `stdout` and `--json-out` content diverge | Yes | Yes after fixing behavior | Required if released behavior changes | Major |
| New optional payload field added without changing existing semantics | No | Yes | Usually not required | Minor |
| Field order change only | No | No unless snapshot intentionally stores ordered text | No | Patch |
| Machine-specific path / temp directory variance with unchanged semantic shape | No | Prefer test normalization, not golden file churn | No | Patch |

### Contract Snapshot Rules

- Golden files may be updated only when:
  - a backward-compatible field is intentionally added
  - an incorrect output is intentionally corrected and downstream impact has been reviewed
  - a breaking change has been explicitly accepted
- Updating golden files alone is not a sufficient fix when:
  - a field was removed or renamed without an explicit compatibility decision
  - the top-level envelope changed
  - `stdout` and `--json-out` no longer match
  - exit code semantics changed without migration notes
- Prefer machine-independent structural comparison over volatile full-text snapshots when output includes:
  - absolute paths
  - temp directories
  - other environment-specific values

## [Unreleased]

### Added
- CLI media toolbox with 21 commands for probe, render, cut, concat, audio analysis, transcription, template workflows, and plugin support
- Unified command envelope (`{ command, preview, payload }`) for all 21 commands
- `--json-out` support for all commands
- Structured failure envelope on all error paths (no usage text fallback)
- Template platform with 4 built-in categories (short-form, commentary, explainer, montage)
- Template plugin system via `--plugin-dir` with static manifest discovery
- `doctor` dependency pre-check for ffmpeg/ffprobe/whisper-cli/demucs
- Contract snapshot tests with golden files for breaking change detection
- Plugin developer guide and example plugin
- External dependency installation guide
- CI (GitHub Actions): build + test + ffmpeg smoke
- Release workflow: tag-triggered cross-platform single-file publish (win-x64, linux-x64, osx-x64)
