# Roadmap

最后更新：2026-04-17

本文件只保留当前版本目标、实施顺序与活跃工作面，不记录完整历史流水账。

## 产品定位

- 产品不是专业 NLE，也不是缩小版 PR。
- 当前目标是交付一个“轻量剪映式”的 CLI 媒体内核：
  - 常见视频场景优先
  - 默认路径简单
  - 对外结构化、可编排、可审计
  - 通过模板降低人工操作和 AI 编排难度
- 软件内不内置 AI provider；AI 只通过 CLI、结构化文件和模板工作流与仓库交互。

## 实施原则

- 新功能默认先找已有开源实现，再决定是否补自研。
- 优先复用成熟工具、库或现有 CLI，而不是在仓库里重复开发同类能力。
- 只有在以下情况同时成立时，才考虑自研：
  - 现有开源实现无法满足确定性、可审计或可测试要求
  - 无法通过适配层或 CLI 集成方式复用
  - 自研后的 canonical owner 明确，不会把边界打散
- 能通过“封装现有开源能力 + 稳定结构化输出”解决的问题，不应直接升级为新的复杂内核实现。

## 当前版本目标

- 交付可供外部 AI 代理稳定调用的 CLI 媒体工具链：
  - 文件探测
  - 任务规划
  - 剪辑计划
  - 执行
  - 结构化 JSON 输出
- 把项目收敛为“基础能力层 + 模板层 + 插件扩展层”的结构，而不是继续堆离散命令。
- 保持 `Core` 作为唯一业务 owner，避免在 `Cli` 或未来 Desktop 重复实现命令构建和外部工具调用。
- 当前实施计划：`docs/plans/2026-04-15-cli-mvp-implementation.md`
- 相关专项计划：`docs/plans/2026-04-16-audio-speech-foundation.md`
- 最近一次功能/里程碑盘点：`docs/plans/2026-04-17-feature-design-milestone-check.md`

## 总体路线

### 1. Foundation

- 稳定媒体探测、命令构建、执行、`edit.json schema v1` 与结构化输出。
- 保持 `Core.Media`、`Core.Execution`、`Core.Editing`、`Core.Subtitles`、`Core.Beats` 的清晰 owner。
- 这一层只提供确定性原语，不直接承载大量场景逻辑。

### 2. Audio / Speech Base

- 为模板能力补地基，而不是直接为每个模板硬编码特例。
- 这层能力优先评估可复用的开源实现，例如现有音频分析、分离、语音识别和字幕工具链。
- 第一波 CLI 基础命令已落地，当前重点从“补入口”转向“收敛契约、错误路径和真实工具验证”。
- 优先补齐：
  - 音量检测与分贝识别
  - 分贝调整 / 增益控制
  - 静音 / 停顿检测
  - 人声与背景音分离
  - 音频转 transcript
  - 外挂字幕 / 烧录字幕的稳定工作流

### 3. Template Platform

- 模板不再只是内置样板，而是可扩展的场景编排单元。
- 模板平台优先复用基础能力与外部工具，不为单个模板发明新的私有处理链。
- 模板层负责：
  - 场景抽象
  - 参数默认值
  - artifact slot 声明
  - 推荐 seed 模式
  - 推荐 transcript / beat 辅助策略
- 先做模板插件，再评估能力插件；避免一开始就在仓库里引入不可控的扩展面。

### 4. User-facing Simplicity

- 面向用户与 AI 的目标不是“功能最多”，而是“高频任务最短路径”。
- 优先围绕以下体验收敛：
  - 导入素材
  - 生成初始计划
  - 字幕
  - BGM / 音频混合
  - 常见比例输出
  - 模板一键起步
- Desktop 只在 CLI / 模板工作流稳定后再评估，且应定位为简单操作壳，而不是完整时间线系统。

## 分阶段实施

### Phase A: 稳定基础能力

- 目标：
  - 固化 `edit.json schema v1`
  - 稳定 `probe` / `plan` / `run` / `render` / `mix-audio` / `validate-plan`
  - 稳定 transcript / beats / artifacts 的结构化边界
- 完成判定：
  - CLI 输出契约稳定
  - `edit.json` 能支撑后续模板与外部 AI 二次编辑

### Phase B: 夯实模板地基

- 目标：
  - 音量检测
  - 分贝调整
  - 静音 / 停顿检测
  - 音频转 transcript
  - 外挂字幕 / 烧录字幕双路径收敛
  - 人声 / 背景音分离
- 说明：
  - 这些能力属于模板平台地基，不应被塞进某个模板专属逻辑里。
  - 这阶段优先级高于继续无节制扩模板数量。
- 当前状态：
  - `audio-analyze`、`audio-gain`、`transcribe`、`detect-silence`、`separate-audio` 命令面已进入仓库
  - 当前剩余重点是稳定 JSON 契约、补真实工具 smoke，并把这些基础信号继续收敛进模板工作流

### Phase C: 模板平台与插件扩展

- 目标：
  - 模板 schema 继续完善
  - 模板 guide / preview / commands 输出继续稳定
  - 模板级推荐策略可配置
  - 为外部模板 / 插件预留稳定入口
- 约束：
  - 先支持“模板插件”
  - 再评估“能力插件”
  - 插件仍应以 deterministic CLI 能力为主，不引入仓库内 AI 黑盒

### Phase D: 轻量用户层

- 目标：
  - 用更少参数覆盖更多高频场景
  - 用模板把常见短视频工作流收敛成简单入口
  - 视 CLI 成熟度决定是否接入 Desktop MVP

## Active Tracks

- `Foundation hardening`
  - 稳定 `edit.json schema`、CLI 输出契约与执行预览
  - 收敛 `doctor` 依赖预检与外部工具可用性语义
- `Audio / speech base`
  - 收敛 `audio-analyze` / `audio-gain` / `transcribe` / `detect-silence` / `separate-audio` 的输出契约、错误路径和外部依赖边界
- `Template platform`
  - 继续把模板从“内置样板”收敛为“可扩展场景单元”
- `Template guidance output`
  - 保持 `templates <id>` guide、preview、commands 与 supporting signal guidance 的稳定性
- `Transcript-assisted planning`
  - 继续扩展可解释的 transcript 辅助策略，但必须保持显式参数和 deterministic 行为
- `Beat-assisted planning`
  - 保持 `beats.json -> init-plan` 的稳定语义，不引入不可解释自动剪辑
- `Plugin extension space`
  - 为模板插件预留入口，但避免过早引入复杂运行时机制
- `Repository guardrails`
  - 维持仓库级文档、模块 owner 和验证路径，避免后续 UI 开发把边界打散

## 已实现基础

- `.NET 8` 解决方案骨架、`Core` / `Cli` / `Desktop` / `Core.Tests` 项目已建立
- `Core` 已具备任务模型、预设模型、`ffprobe` 解析、`ffmpeg` 命令构建、进程执行和作业执行能力
- `Cli` 已提供：
  - `presets`
  - `doctor`
  - `probe`
  - `plan`
  - `run`
  - `templates`
  - `init-plan`
  - `scaffold-template`
  - `beat-track`
  - `audio-analyze`
  - `audio-gain`
  - `transcribe`
  - `detect-silence`
  - `separate-audio`
  - `cut`
  - `concat`
  - `extract-audio`
  - `subtitle`
  - `validate-plan`
  - `mix-audio`
  - `render`
- `edit.json schema v1` 已有核心模型、模板/扩展位与序列化测试
- `transcript.json` 与字幕导出 owner 已落在 `Core.Subtitles`
- `beats.json` 与波形分析 owner 已落在 `Core.Beats`
- `audio.json` 与响度分析 owner 已落在 `Core.Audio`
- `silence.json` 与静音检测 owner 已落在 `Core.Audio`
- 音频分离结果与 stem 映射 owner 已落在 `Core.AudioSeparation`

## 已实现的模板与辅助能力

- 内置模板目录已覆盖：
  - `short-form`
  - `commentary`
  - `explainer`
  - `montage`
- 已支持：
  - artifact slot 绑定
  - 模板参数覆盖
  - transcript 引用与 segment seed
  - transcript 固定 segment 分组
  - transcript 最小时长过滤
  - transcript 最大 gap 断点
  - beat seed 与固定节拍组
  - 模板 guide / preview / commands 输出
  - transcript-first 模板的推荐 transcript 策略组合

## 近期优先级

### P0

- 收敛 `audio-analyze` / `audio-gain` / `transcribe` / `detect-silence` / `separate-audio` 的结构化输出契约
- 补齐 `ffmpeg` / `whisper.cpp` / `demucs` 真实工具 smoke 与失败路径验证
- 用 `doctor` 收敛 `ffmpeg` / `ffprobe` / `whisper-cli` / `demucs` / `whisper model` 的依赖预检
- 继续把 transcript / silence / stems 等基础信号接入模板 guide、preview 和脚手架工作流
- 保持 supporting signal guidance 由模板 owner 显式声明，避免 CLI 侧临时拼接
- 明确重型外部依赖的安装前提、日志保留和错误输出约束

### P1

- 字幕工作流继续收敛
- 评估 `audio-gain` 是否需要保持显式增益模式之外的独立归一化入口
- 模板插件入口设计

### P2

- 继续扩模板类型
- 评估哪些增强适合做成单独 deterministic CLI 子命令
- Desktop MVP 是否值得继续推进

## 已规划但未实现的方向

- 模板插件机制
- Desktop 实际 UI 框架接入
- CI / workflow
- 打包 / 发布流程

## 待验证项

- 结构化输出约定还需要继续稳定，便于外部 AI 代理消费
- `whisper.cpp` 与 `demucs` 依赖较重，真实机器上的可用性、目录约定和错误路径还需要继续验证
- 模板插件机制需要在不破坏 `Core` owner 的前提下设计
- Desktop 是否保留为长期目标仍需后续确认

## 已验证

- 当前开发机已通过真实 `ffprobe` / `ffmpeg` smoke：
  - `ffprobe` 媒体探测
  - `render` 导出与 sidecar 字幕复制
  - `mix-audio` 音频混合导出
  - `cut` 单段裁切
  - `concat` 片段拼接

## 下一步

1. 优先补音量检测、分贝调整、音频转 transcript 这类模板地基能力。
2. 把已落地的音频 / 语音命令收敛到稳定的契约、真实工具 smoke 和模板接入。
3. 继续稳住模板 guide / preview / commands 的机器友好契约，并基于现有模板能力设计“模板插件优先”的扩展路径。
