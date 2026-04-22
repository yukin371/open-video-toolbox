# Contract Snapshots

This directory stores machine-facing golden files for CLI contract regression checks.

## Purpose

Use these snapshots to detect unintended contract drift in outputs that are:

- machine-independent
- stable across environments
- frequently consumed by external AI agents or scripts
- safe to replay in CI

These files are not a generic dumping ground for every JSON output in the CLI.

## Current Coverage

The current snapshot set intentionally focuses on stable read-oriented or guide-oriented outputs:

- `presets`
- `templates` catalog
- `templates shorts-captioned`
- `templates beat-montage`
- `validate-plan` machine-independent payload shape

`validate-plan` is compared after removing path-like environment-specific fields instead of relying on a raw full-text snapshot.

## When To Add A New Snapshot

A command is a good candidate for snapshot coverage when all of the following are true:

1. Its output shape is expected to be part of the long-lived external contract.
2. The output is largely machine-independent.
3. The command is frequently consumed by automation or AI orchestration.
4. The result can be reproduced reliably in CI.

Typical good candidates:

- catalog / listing outputs
- template guide or summary outputs
- validation payloads after normalization
- other read-style commands with stable schema and low environment noise

## When Not To Add A New Snapshot

Prefer structural assertions or normalized comparisons instead of a full snapshot when output contains:

- absolute paths
- temp directories
- timestamps
- execution logs
- environment-specific process details
- large execution payloads that are stable only after partial normalization

Typical poor candidates:

- outputs dominated by machine-local path values
- commands whose payloads mostly reflect execution side effects
- commands better verified by targeted field assertions

## Update Rules

Do not update a golden file just because a test failed.

Golden files should change only when:

- a backward-compatible field is intentionally added
- an incorrect output is intentionally corrected and downstream impact is understood
- a breaking change has been explicitly accepted

If a snapshot changes, the PR should explain:

1. why the file changed
2. whether the change is breaking
3. whether `CHANGELOG.md` also needs an update

## Preferred Test Strategy

Choose the narrowest stable assertion strategy that still protects the contract:

- full snapshot for stable, machine-independent outputs
- normalized structural comparison for outputs with a few volatile fields
- targeted assertions for highly variable execution payloads

If normalization can remove noise, prefer normalization over expanding snapshot churn.
