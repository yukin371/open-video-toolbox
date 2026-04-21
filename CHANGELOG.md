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
