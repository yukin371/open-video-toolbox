## Summary

- What changed?
- Why is this change needed?

## Validation

- [ ] `dotnet test OpenVideoToolbox.sln`
- [ ] Relevant manual verification completed or explicitly not needed

## Contract Check

- [ ] This PR does not change CLI output contract
- [ ] Or, if it does, I evaluated whether the change is breaking
- [ ] `stdout` and `--json-out` remain consistent for affected commands
- [ ] `CHANGELOG.md` was updated if migration notes or version impact are required

## Snapshot Check

- [ ] This PR does not modify `src/OpenVideoToolbox.Cli.Tests/snapshots/*.json`
- [ ] Or, if it does, I explained why golden files changed
- [ ] Golden file updates are not being used to hide an unintended regression
- [ ] I considered whether structural normalization is better than expanding snapshot noise

## Docs Check

- [ ] No doc updates needed
- [ ] Or, if needed, I updated the relevant docs (`roadmap`, `plans`, `PROJECT_PROFILE`, `README`, `MODULE.md`, etc.)

## External Tool Check

- [ ] This PR does not change external tool execution boundaries
- [ ] Or, if it does, I verified explicit overwrite / timeout / logging / `ProducedPaths` behavior for affected commands
- [ ] CLI / Desktop layers still do not bypass `OpenVideoToolbox.Core/Execution`

## Plugin Contribution Check

- [ ] Not applicable
- [ ] Or, if this PR adds/changes a template plugin, the contribution is static-only (`plugin.json` / `template.json` / docs) with no scripts, binaries, or install hooks
- [ ] `templates --plugin-dir` and `templates <id> --plugin-dir` were verified for the contributed plugin
- [ ] `init-plan --plugin-dir` and `validate-plan --plugin-dir` were verified for the contributed plugin
- [ ] The plugin does not require new private execution semantics or Core-only fields
- [ ] Relevant contributor-facing docs were updated when submission/self-test expectations changed

## Risks

- Main risk of this PR:
- Rollback path:
