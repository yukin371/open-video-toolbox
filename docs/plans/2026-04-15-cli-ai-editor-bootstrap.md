# External-AI CLI Positioning Plan

日期：2026-04-15

## 目标

把项目从“媒体压制工具”收敛为“供外部 AI 代理调用的 CLI 媒体工具箱”，并撤回仓库内部 AI 骨架。

## 范围

- 更新 README、PRD、技术路线、架构草图、roadmap、项目画像、架构护栏
- 更新 README、PRD、技术路线、架构草图、roadmap、项目画像、架构护栏
- 撤回 `Pipeline` 与 `Providers` 骨架
- 恢复 CLI 到确定性命令面
- 新增一条 ADR 固化“AI 在仓库外部”的边界

## 非目标

- 不在仓库内接任何 AI provider
- 不实现完整时间线编辑器
- 不修改当前执行层和媒体探测层的既有行为

## 验证方案

- `dotnet build E:\Github\open-video-toolbox\OpenVideoToolbox.sln`
- `dotnet test E:\Github\open-video-toolbox\OpenVideoToolbox.sln`
- `dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- help`

## 收敛规则

- 后续编辑子命令单独开新的 plan
- 长期边界变化单独开新的 ADR
