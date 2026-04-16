# Roadmap

最后更新：2026-04-15

本文件只保留当前版本目标和活跃工作面，不记录完整历史流水账。

## 当前版本目标

- 交付可供外部 AI 代理稳定调用的 CLI 媒体工具链：
  - 文件探测
  - 任务规划
  - 剪辑计划
  - 执行
  - 结构化 JSON 输出
- 保持 `Core` 作为唯一业务 owner，避免在 `Cli` 或未来 Desktop 重复实现命令构建和外部工具调用。
- 当前实施计划：`docs/plans/2026-04-15-cli-mvp-implementation.md`

## Active Tracks

- `Wave 1 implementation`
  - 第一批命令：`cut`、`concat`、`extract-audio`、`render`
- `edit.json schema`
  - 固化 schema v1，让 AI 生成与人工二次修正共享同一边界
- `Template and extension space`
  - 为固定剪辑模板和外部工具整合预留稳定入口
- `Template guidance output`
  - 稳定 `templates <id>` 的单模板指南输出，减少外部 AI 猜测 artifact 和参数文件形状
  - 同时输出模板推荐 seed 模式，减少外部 AI 猜测 transcript / beats / manual 哪个更合适
  - 同时输出按推荐 seed 模式生成的 preview plan，减少外部 AI 猜测最终 `edit.json` 形状
  - 支持把模板指南直接写出为文件，减少外部 AI 二次拆解 JSON 输出
  - 支持按 `category` / `seed-mode` 先过滤模板列表，减少外部 AI 遍历成本
- `Template artifact bindings`
  - 稳定模板 slot 到 `edit.json.artifacts` 的绑定语义，减少 `init-plan` 的硬编码入口
- `Template parameter overrides`
  - 稳定模板默认参数到 `edit.json.template.parameters` 的覆盖语义，减少模板特定 flag
- `Transcript-assisted planning`
  - 稳定 `transcript.json -> edit.json.transcript -> init-plan` 的引用与按 segment seed 语义
- `Execution reuse`
  - 保持传统导出能力复用现有 `Media` / `Execution`
- `Beat-assisted planning`
  - 稳定 `beats.json -> edit.json.beats -> init-plan` 的引用与种子生成语义
- `Repository guardrails`
  - 维持仓库级文档、模块 owner 和验证路径，避免后续 UI 开发把边界打散

## 最近进展

- `.NET 8` 解决方案骨架、`Core` / `Cli` / `Desktop` / `Core.Tests` 项目已建立
- `Core` 已具备任务模型、预设模型、`ffprobe` 解析、`ffmpeg` 命令构建、进程执行和作业执行能力
- `Cli` 已提供 `presets`、`cut`、`concat`、`extract-audio`、`render`、`probe`、`plan`、`run` 八个命令，用于最小可运行验证
- `Cli` 已提供 `templates` 与 `init-plan`，用于列出模板并生成可编辑 `edit.json` skeleton
- 内置模板目录已扩展到 `short-form`、`commentary`、`explainer`、`montage` 四类常见套路，补齐 transcript-first 与 beat-first 模板入口
- `Cli` 已提供 `scaffold-template`，用于一次性生成模板工作目录、示例文件与初始 `edit.json`，并可选在生成后立即校验
- `templates <id>` 已支持返回单模板指南，包含 artifact skeleton、template-params skeleton 与示例命令
- `templates <id>` 已支持返回推荐 seed 模式和对应 seedCommands
- `templates <id>` 已支持返回按推荐 seed 模式生成的 previewPlans
- `templates <id> --write-examples <dir>` 已支持直接写出 `guide.json`、skeleton 和 preview plan 文件
- `templates <id> --write-examples <dir>` 已支持直接写出命令脚本文件，减少外部 AI 或脚本再拼下一步命令
- `templates` 已支持按 `category` / `seed-mode` / `output-container` / `artifact-kind` / `has-artifacts` / `has-subtitles` 过滤列表，并可通过 `--summary` 返回机器友好摘要、通过 `--json-out` 直接写出结果
- `init-plan` 已支持 `--artifacts`，把模板声明的 artifact slot 绑定写入顶层 `artifacts`
- `init-plan` 已支持 `--template-params`，把模板默认参数覆盖写入 `edit.json.template.parameters`
- `init-plan` 已支持读取 `transcript.json` 作为顶层 `transcript` 引用，并可选按 segment 确定性生成初始 clips
- `Cli` 已提供 `beat-track`，用于把媒体输入转换成 `beats.json`
- `init-plan` 已支持读取 `beats.json` 作为顶层 `beats` 引用，并可选按节拍组确定性生成初始 clips
- `Cli` 已提供 `subtitle`，用于把外部 `transcript.json` 渲染为 `srt` / `ass`
- `Cli` 已提供 `validate-plan`，用于在执行前校验 `edit.json` 语义并可选检查引用文件存在性
- `Cli` 已提供 `mix-audio --preview` 与 `render --preview`，用于在执行前输出稳定的 `executionPreview`
- `Cli` 已提供 `mix-audio`，用于从 `edit.json` 单独导出音频混合结果
- 仓库级 bootstrap 文档已补齐，包含项目画像、架构护栏、模块 `MODULE.md`、plan 和 ADR 入口
- 产品方向已收敛为“给外部 AI 代理调用的 CLI 工具箱”，不内置 AI provider
- `edit.json schema v1` 已有核心模型、模板/扩展位与序列化测试，owner 落在 `Core.Editing`
- `edit.json schema v1` 已显式包含顶层 `artifacts` 字段，用于模板 slot 绑定与后续扩展
- `edit.json schema v1` 已显式包含顶层 `transcript` 字段，用于 transcript 引用与按 segment seed 输入
- `edit.json schema v1` 已显式包含顶层 `beats` 字段，用于节奏引用与模板种子输入
- `render --plan` 已落在 `Core.Execution`，打通 `edit.json` 到最终导出的执行闭环
- `mix-audio --plan` 已落在 `Core.Execution`，并复用 `render` 的音频图构建逻辑
- `transcript.json` 与字幕导出 owner 已落在 `Core.Subtitles`
- `beats.json` 与波形分析 owner 已落在 `Core.Beats`
- 内置模板目录已通过代码目录实现首版契约，便于外部 AI 先拿模板再填 plan

## 待验证项

- 结构化输出约定还需要继续稳定，便于外部 AI 代理消费
- Desktop 是否保留为长期目标仍需后续确认

## 已验证

- 当前开发机已通过真实 `ffprobe` / `ffmpeg` smoke：
  - `ffprobe` 媒体探测
  - `render` 导出与 sidecar 字幕复制
  - `mix-audio` 音频混合导出
  - `cut` 单段裁切
  - `concat` 片段拼接

## 下一步

1. 继续丰富模板契约，让 `init-plan` 能覆盖更多常见视频套路。
2. 在现有 transcript / beat 种子生成之上，补更多可解释的编辑辅助策略。
3. 评估哪些能力直接整合现有工具，哪些继续由 ffmpeg 链承接。
