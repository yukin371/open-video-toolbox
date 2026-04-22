# Contributing

本仓库当前优先接受两类贡献：

- CLI / Core / workflow 的可维护性、测试与发布链改进
- 基于现有 template schema 的静态模板插件

当前不接受：

- Desktop MVP 启动前的 UI 主导改动
- 运行时代码插件、脚本插件、远程插件市场
- 需要 `Core` 新增私有执行语义才能成立的模板扩展

## 修改前先读

提交前至少按这个顺序阅读：

1. `README.md`
2. `docs/PROJECT_PROFILE.md`
3. `docs/ARCHITECTURE_GUARDRAILS.md`
4. 目标目录下的 `MODULE.md`
5. 与任务直接相关的计划文档

如果你要提交模板插件，再补读：

1. `docs/plugin-development-guide.md`
2. `examples/plugin-example/README.md`
3. `docs/plans/2026-04-22-e2-a3-community-plugin-contribution-path.md`

## 开发与验证

通用验证：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Enable-GitHooks.ps1
dotnet build OpenVideoToolbox.sln
dotnet test OpenVideoToolbox.sln
```

首次 clone 后建议先运行 `scripts/Enable-GitHooks.ps1`，把仓库内 `.githooks/` 接到本地 `core.hooksPath`。只要没启用这一步，`pre-commit` 和 `commit-msg` 都不会生效。

可用下面这条命令检查是否已启用：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Enable-GitHooks.ps1 -VerifyOnly
```

如果改动影响 CLI 契约、文档或发布链，请同步更新对应文档与测试，而不是只改代码。

## 模板插件贡献

当前允许的社区模板插件必须满足：

- 只包含 `plugin.json`、`template.json` 和可选说明文件
- 完全复用现有 CLI 命令与 `Core.Editing` 语义
- 不包含脚本、二进制、下载逻辑或安装钩子
- 不要求 `Core` 解析未声明的私有字段

最小目录结构：

```text
<plugin-root>/
  plugin.json
  templates/
    <template-id>/
      template.json
```

提交前至少跑完这组本地自测：

```powershell
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- templates --plugin-dir <plugin-root> --summary
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- templates <template-id> --plugin-dir <plugin-root>
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- templates <template-id> --plugin-dir <plugin-root> --write-examples .plugin-guide
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- init-plan <input> --template <template-id> --plugin-dir <plugin-root> --output edit.json --render-output final.mp4
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- validate-plan --plan edit.json --plugin-dir <plugin-root>
```

提交说明里至少写清楚：

- 插件适用场景
- 推荐 seed 模式
- 需要的 supporting signals
- 本地自测结果

## PR 要求

提交 PR 时请确保：

- `dotnet test OpenVideoToolbox.sln` 已跑，或明确说明为什么没跑
- CLI 契约变化已在 PR 中说明
- 相关文档已同步，或明确说明无需同步
- 外部工具边界没有被 UI / 脚本层绕过

如果这是模板插件 PR，还要说明：

- 插件是否纯静态文件
- 是否与内置模板 ID 冲突
- `templates --plugin-dir` 到 `validate-plan --plugin-dir` 是否闭环通过

如果你希望先走仓库内固定提交入口，再补 PR，可直接使用：

- `.github/ISSUE_TEMPLATE/community-plugin-submission.yml`
  - 用于提交社区插件摘要、链接、本地自测结果和边界确认
  - maintainer 可以直接按其中的字段和清单做第一轮审核
  - 它不是新的插件市场或审核系统，只是把现有 `E2-A3` 清单固定成仓库入口

更细的边界与审核标准见：

- `docs/plugin-development-guide.md`
- `docs/plans/2026-04-22-e2-a3-community-plugin-contribution-path.md`
- `docs/plans/2026-04-19-template-plugin-entry-boundary.md`
