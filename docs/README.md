# Documentation

## By Audience

### CLI Users

- [Quick Start](QUICK_START.md) — Shortest path from dependency check to template render
- [Features & Usage Guide](FEATURES_AND_USAGE.md) — Complete feature overview, installation notes, command groups, workflows, and troubleshooting
- [Command Reference](COMMAND_REFERENCE.md) — Exact CLI signatures grouped by command family
- [External Dependencies](external-dependencies.md) — How to install ffmpeg, whisper-cli, demucs
- [CLI MVP & Command Reference](CLI_MVP.md) — Command design and usage patterns

### AI Agent Orchestrators

- [PRD](PRD.md) — Product positioning and user stories
- [AI Editor Positioning](decisions/ADR-0002-cli-ai-editor-positioning.md) — How external AI should interact with CLI
- [Edit Plan Manual Pass](decisions/ADR-0003-edit-plan-manual-pass.md) — Human post-editing workflow

### Plugin Developers

- [Plugin Development Guide](plugin-development-guide.md) — How to create a template plugin
- [Example Plugin](../examples/plugin-example/) — Minimal working plugin
- [`scripts/Verify-ExamplePlugin.ps1`](../scripts/Verify-ExamplePlugin.ps1) — One-shot repository script that verifies the example plugin flow end-to-end
- [E2-A3 Community Plugin Contribution Path](plans/2026-04-22-e2-a3-community-plugin-contribution-path.md) — Submission expectations, local validation loop, and review checklist for community plugins
- [Community Plugin Submission Issue Form](../.github/ISSUE_TEMPLATE/community-plugin-submission.yml) — Repository-local submission entry for plugin summary, self-test evidence, and maintainer review context

### Contributors & Maintainers

- [Contribution Guide](../CONTRIBUTING.md) — Repository contribution entry point, validation expectations, and plugin submission basics
- [Community Plugin Submission Issue Form](../.github/ISSUE_TEMPLATE/community-plugin-submission.yml) — Fixed maintainer-facing intake form for community template plugin submissions
- [Architecture](architecture.md) — System architecture and module boundaries
- [Architecture Guardrails](ARCHITECTURE_GUARDRAILS.md) — Owner rules, dependency directions, stage gates
- [Development Principles](development-principles.md) — Coding and review principles
- [`scripts/Measure-RuntimeBaseline.ps1`](../scripts/Measure-RuntimeBaseline.ps1) — Lightweight runtime baseline observation for doctor / probe / scaffold-template-batch / render-batch preview / render preview
- [`scripts/Verify-DependencyBaseline.ps1`](../scripts/Verify-DependencyBaseline.ps1) — One-shot dependency baseline verification for doctor + Core/CLI real-media smoke tests
- [`scripts/Test-RuntimeBaselineThresholds.ps1`](../scripts/Test-RuntimeBaselineThresholds.ps1) — Compares observed runtime baseline JSON against repository thresholds and reports exceeded commands
- [`scripts/Write-RuntimeBaselineSummary.ps1`](../scripts/Write-RuntimeBaselineSummary.ps1) — Renders runtime/dependency baseline JSON into maintainer-friendly markdown or GitHub Actions job summary
- [`runtime-baseline.yml`](../.github/workflows/runtime-baseline.yml) — Manual/weekly GitHub Actions workflow that runs baseline checks, evaluates runtime thresholds, writes job/artifact summaries, and uploads runtime baseline artifacts
- [Roadmap](roadmap.md) — Current priorities and stage checks
- [Project Profile](PROJECT_PROFILE.md) — Project scope and constraints
- [Tech Stack](tech-stack.md) — Technology choices and rationale

### Decision Records

- [ADR-0002: CLI AI Editor Positioning](decisions/ADR-0002-cli-ai-editor-positioning.md)
- [ADR-0003: Edit Plan Manual Pass](decisions/ADR-0003-edit-plan-manual-pass.md)

### Current Planning References

- [Long-term Evolution Roadmap](plans/2026-04-21-long-term-evolution-roadmap.md) — Current stage mapping and next-stage decision point
- [D1 Desktop MVP Start Checklist](plans/2026-04-22-d1-desktop-mvp-start-checklist.md) — Start gate checklist and current blocker assessment
- [E2 Ecosystem Sustainability Plan](plans/2026-04-22-e2-ecosystem-sustainability-plan.md) — Execution draft for ecosystem, compatibility, distribution, and sustainability work
- [E2-A1 Contract Compatibility Guardrails](plans/2026-04-22-e2-a1-contract-compatibility-guardrails.md) — Detailed checklist for snapshot, breaking change, and review guardrails
- [E2-A2 Distribution Channel Evaluation](plans/2026-04-22-e2-a2-distribution-channel-evaluation.md) — Channel evaluation and first-choice recommendation for package-manager distribution
- [E2-A3 Community Plugin Contribution Path](plans/2026-04-22-e2-a3-community-plugin-contribution-path.md) — Community template/plugin contribution expectations and self-test path
- [E2-A4 Runtime Baseline](plans/2026-04-22-e2-a4-runtime-baseline.md) — Dependency validation contract, lightweight performance observations, and external-tool safety checklist
- [Narrated Slides Video Spec](plans/v2/2026-04-25-narrated-slides-video-spec.md) — Candidate spec for explainer / slide-driven video assembly on top of v2 timeline plans
- [V2-P6-C2 Narrated Slides Plan](plans/v2/2026-04-25-v2-p6-c2-narrated-slides-plan.md) — Implementation planning draft for narrated / PPT-style video assembly and its stage placement
- [WinGet Packaging Draft](../packaging/winget/README.md) — Repository-local draft manifests, readiness checks, submission-bundle export, and submission notes for the preferred winget path
- [H2+T1 Contract Freeze](plans/2026-04-22-h2-t1-contract-freeze-checklist.md) — Latest contract freeze and template stability closure
- [Template Plugin Entry Boundary](plans/2026-04-19-template-plugin-entry-boundary.md) — Stable plugin boundary and constraints reference
- [CLI Maintainability Refactor](plans/2026-04-19-cli-maintainability-refactor-plan.md) — Closed refactor track kept as historical execution reference
