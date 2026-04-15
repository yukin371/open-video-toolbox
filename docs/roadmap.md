# Roadmap

最后更新：2026-04-15

本文件只保留当前版本目标和活跃工作面，不记录完整历史流水账。

## 当前版本目标

- 交付 Desktop MVP 的第一条闭环链路：
  - 文件导入
  - 媒体信息展示
  - 预设选择
  - 任务生成
  - 执行进度与日志查看
- 保持 `Core` 作为唯一业务 owner，避免在 `Cli` 或 `Desktop` 重复实现命令构建和外部工具调用。

## Active Tracks

- `Desktop shell`
  - 为 `OpenVideoToolbox.Desktop` 建立真实 UI 外壳、导航和后续状态管理落点
- `Core integration`
  - 把现有 `Media`、`Presets`、`Execution` 能力以可复用方式接入桌面层
- `Command and log visibility`
  - 保证桌面端能预览命令、展示执行状态、查看原始输出和错误信息
- `Repository guardrails`
  - 维持仓库级文档、模块 owner 和验证路径，避免后续 UI 开发把边界打散

## 最近进展

- `.NET 8` 解决方案骨架、`Core` / `Cli` / `Desktop` / `Core.Tests` 项目已建立
- `Core` 已具备任务模型、预设模型、`ffprobe` 解析、`ffmpeg` 命令构建、进程执行和作业执行能力
- `Cli` 已提供 `presets`、`probe`、`plan`、`run` 四个命令，用于最小可运行验证
- 仓库级 bootstrap 文档已补齐，包含项目画像、架构护栏、模块 `MODULE.md`、plan 和 ADR 入口

## 待验证项

- `ffprobe` / `ffmpeg` 真实二进制在当前开发机上的端到端执行仍需完成
- Desktop 真实 UI 框架、状态管理和日志展示方案尚未落地
- 如后续引入持久化、任务历史或用户配置，需要先补 canonical owner 和验证方案

## 下一步

1. 为 `OpenVideoToolbox.Desktop` 引入实际桌面 UI 框架并替换占位入口。
2. 先接通只读链路：导入文件 -> 探测媒体信息 -> 展示规范化结果。
3. 再接通任务链路：选择内置预设 -> 生成 `JobDefinition` / `CommandPlan` -> 展示预览。
4. 最后接通执行链路：运行任务、展示日志、补最小桌面端回归验证。
