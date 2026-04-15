# Architecture Guardrails

最后更新：2026-04-15

## 模块划分

- `OpenVideoToolbox.Core`
  - 唯一承接任务模型、预设模型、媒体探测、命令构建、进程执行和执行结果模型的核心层
- `OpenVideoToolbox.Cli`
  - 面向脚本与调试的薄入口层，只负责参数解析、调用编排、退出码和 JSON 输出
- `OpenVideoToolbox.Desktop`
  - 面向交互的桌面层，只负责文件导入、任务列表、预设编辑、日志查看与后续 UI 状态管理
- `OpenVideoToolbox.Core.Tests`
  - 核心行为回归保护层，不承载生产逻辑

## 允许的依赖方向

- `OpenVideoToolbox.Cli` -> `OpenVideoToolbox.Core`
- `OpenVideoToolbox.Desktop` -> `OpenVideoToolbox.Core`
- `OpenVideoToolbox.Core.Tests` -> `OpenVideoToolbox.Core`
- `OpenVideoToolbox.Core` 不得反向依赖 `Cli` 或 `Desktop`
- `Cli` 与 `Desktop` 之间不得直接依赖

## Canonical Owners

- 命令构建：`OpenVideoToolbox.Core/Execution/FfmpegCommandBuilder.cs`
- 外部进程执行与原始输出采集：`OpenVideoToolbox.Core/Execution/DefaultProcessRunner.cs`
- 转码任务执行编排：`OpenVideoToolbox.Core/Execution/TranscodeJobRunner.cs`
- `ffprobe` 调用与规范化探测结果：`OpenVideoToolbox.Core/Media`
- 预设模型与内置预设目录：`OpenVideoToolbox.Core/Presets`
- CLI 参数解析、帮助输出、退出码：`OpenVideoToolbox.Cli/Program.cs`
- 桌面 UI primitives / 状态管理：`TBD`
  - 确认路径：Desktop MVP 引入实际 UI 框架后落具体 owner

## 跨切关注点 Owner

- `logging`
  - 原始进程输出与执行日志模型由 `OpenVideoToolbox.Core/Execution` 统一承接
  - 应用层结构化日志门面：`TBD`
  - 确认路径：当 CLI 或 Desktop 引入统一日志抽象时补充
- `config`
  - CLI 命令行参数由 `OpenVideoToolbox.Cli` 承接
  - 跨应用持久化配置：`TBD`
  - 确认路径：引入用户配置文件或桌面设置页时明确
- `persistence`
  - 当前仓库未建立持久化层，owner 为 `TBD`
  - 确认路径：引入任务历史、预设存储或缓存后落到单一模块
- `HTTP / API client`
  - 当前仓库未建立 HTTP client，owner 为 `TBD`
- `shared utilities`
  - 默认不建全局 `Utils`；先放在 owning module，出现第二个稳定消费者后再讨论抽取
- `UI primitives`
  - 未来由 `OpenVideoToolbox.Desktop` 统一承接；当前为 `TBD`
- `error mapping`
  - `Core` 负责保留外部工具上下文与失败原因
  - `Cli` / `Desktop` 负责把错误映射为退出码或界面提示
- `file / path helpers`
  - 与转码输出路径相关的规则由 `OpenVideoToolbox.Core/Execution` 承接
  - 与媒体探测输入路径相关的规则由 `OpenVideoToolbox.Core/Media` 承接
- `feature flags`
  - 当前不存在，owner 为 `TBD`

## Forbidden Ownership

- `Desktop` 不得直接拼接 `ffmpeg` / `ffprobe` 命令
- `Desktop` 不得直接启动外部进程以绕过 `Core.Execution`
- `Cli` 不得复制 `Core` 中的命令构建、媒体探测或预设映射逻辑
- `Core.Media` 不得拥有预设选择、输出路径策略或 UI 展示格式
- `Core.Execution` 不得拥有 UI 状态、视图模型或交互逻辑
- 任意模块不得引入“万能工具类”来跨越既有 owner

## 新增共享能力前的强制检查

在新增 shared helper / service / adapter 前，必须回答：

1. 搜索了哪些现有实现？
2. 为什么现有实现不能复用？
3. 新能力的 canonical owner 是谁？
4. 是否会导致两个模块同时拥有相同职责？
5. 需要同步哪些文档？
6. 是否需要 ADR 才能安全落地？

任何一项答不清，先不要实现。

## 文档同步触发条件

- 模块边界变化：更新本文档与相关 `MODULE.md`
- 新增长期有效的 owner 规则或依赖方向：新增或更新 `docs/decisions/ADR-*.md`
- 当前版本重点变化：更新 `docs/roadmap.md`
