# Repo Bootstrap Plan

日期：2026-04-15

## 目标

按 `AI_REPO_BOOTSTRAP_PLAYBOOK` 为仓库补齐最小治理骨架，让后续 AI 与人工协作有统一的边界、owner 和文档入口。

## 范围

- 建立 `docs/PROJECT_PROFILE.md`
- 重写根目录 `AGENTS.md`
- 收敛 `docs/roadmap.md` 为 active-only 视图
- 建立 `docs/ARCHITECTURE_GUARDRAILS.md`
- 为 3 个关键模块补 `MODULE.md`
- 建立 `docs/decisions/ADR-0001-core-module-boundaries.md`

## 非目标

- 不修改当前 `Core` / `Cli` / `Desktop` 生产代码
- 不补 Desktop UI 实现
- 不为缺失事实编造 CI、打包或发布流程

## 事实来源

- `README.md`
- `Directory.Build.props`
- `OpenVideoToolbox.sln`
- `src/*/*.csproj`
- `src/OpenVideoToolbox.Cli/Program.cs`
- `src/OpenVideoToolbox.Desktop/Program.cs`
- `src/OpenVideoToolbox.Core/*`
- 现有 `docs/*.md`

## 验证方案

- 检查文档中的命令、模块边界与代码现状是否一致
- 执行 `dotnet build E:\Github\open-video-toolbox\OpenVideoToolbox.sln`
- 执行 `dotnet test E:\Github\open-video-toolbox\OpenVideoToolbox.sln`

## 收敛规则

- 本计划完成后，不再把仓库初始化细节继续堆到 roadmap
- 后续若出现长期有效的边界决策，写入新的 ADR
- 后续具体实施设计写入新的 `docs/plans/YYYY-MM-DD-*.md`
