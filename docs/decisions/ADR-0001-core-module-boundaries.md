# ADR-0001: Core Owns Tool Invocation And Execution Models

日期：2026-04-15

## 状态

Accepted

## 背景

仓库已经形成 `Core`、`Cli`、`Desktop` 三层结构，但后续 Desktop MVP 开发会快速引入文件导入、任务编排、日志展示和预设编辑。如果没有明确长期边界，最容易出现的问题是：

- `Cli` 和 `Desktop` 各自拼接 `ffmpeg` / `ffprobe` 命令
- UI 层为了赶进度直接拥有进程启动、路径拼接和输出采集
- 预设语义、命令映射和界面编辑状态互相耦合

这些问题都会让测试、日志、回归验证和后续迁移成本迅速上升。

## 决策

- `OpenVideoToolbox.Core` 是任务模型、预设模型、媒体探测、命令构建、进程执行和执行结果模型的唯一 owner。
- `OpenVideoToolbox.Cli` 只负责参数解析、调试入口、JSON 输出和退出码。
- `OpenVideoToolbox.Desktop` 只负责 UI 交互、状态管理、任务展示和日志展示，不直接拼接外部命令，也不直接启动外部进程。
- 所有新的共享能力，默认先落在最贴近语义的 `Core` 子模块；只有出现第二个稳定消费者且 owner 清晰时，才讨论抽成更通用的共享层。

## 结果

正面影响：

- 命令构建和进程执行可以用单元测试回归，而不是绑定某个入口层
- CLI 与 Desktop 可以复用同一套核心能力，减少重复实现
- 错误上下文、原始输出和执行结果模型能够统一沉淀

代价：

- `Cli` / `Desktop` 需要接受较薄的角色，不把“临时方便的逻辑”留在入口层
- 任何跨层需求都需要先判断 canonical owner，初期看起来比直接写代码更慢

## 不采纳的方案

- 让 `Cli` 继续拥有一部分命令拼接逻辑
  - 原因：会让桌面层复用困难，并造成命令构建规则分叉
- 让 `Desktop` 直接调用 `Process` 启动外部工具
  - 原因：会把 UI 状态、日志与取消/超时逻辑耦合在一起

## 后续动作

- Desktop MVP 接入时优先复用 `Core.Media`、`Core.Presets`、`Core.Execution`
- 若后续引入持久化、配置或任务历史，需新增 ADR 明确新的 canonical owner
