# OpenVideoToolbox.Core.Execution

> 最后更新：2026-04-17

## 职责

本模块是外部命令计划与执行链路的 canonical owner，负责把 `JobDefinition + PresetDefinition` 或 `EditPlan` 转成 `CommandPlan`，执行外部进程，并沉淀 `ExecutionResult` 与原始输出。

## Owns

- `ffmpeg` 命令构建
- 输出路径规范化
- 进程执行、超时、取消和输出采集
- 统一音频波形提取
- 统一音频增益导出编排
- 外部依赖探测与 `doctor` 结果模型
- `whisper-cli` 转写命令构建与执行编排
- `ffmpeg silencedetect` 命令构建与执行编排
- 转码任务执行编排
- `mix-audio --plan` 音频导出编排
- `audio-gain` 显式分贝增益命令构建与执行编排
- `render --plan` 最终导出编排
- 执行预览模型与 side effect 预览
- 执行结果模型与原始日志模型

## Must Not Own

- 预设目录管理
- `ffprobe` JSON 解析
- UI 展示模型、任务列表状态和交互逻辑
- 对磁盘产物做隐式扫描来替代显式 `ProducedPaths`

## 关键依赖

- `OpenVideoToolbox.Core.Jobs`
- `OpenVideoToolbox.Core.Presets`
- `System.Diagnostics.Process`

## 不变量

- `FfmpegCommandBuilder` 必须保持可测试、可预览，不依赖 UI 状态
- `mix-audio` 与 `render` 的音频图逻辑必须复用同一映射点，避免双份 filter 规则漂移
- `render` 的 `edit.json` 消费逻辑必须集中在本模块，不能回流到 `Cli` 或未来 Desktop
- preview 语义必须复用和执行阶段相同的 `CommandPlan`、`ProducedPaths` 与 side effect 规则，不能在 CLI 维护平行逻辑
- `DefaultProcessRunner` 必须保留标准输出、标准错误、超时和取消上下文
- `ExecutionResult` 是执行链路的统一结果模型，状态和原始输出都不能丢
- `doctor` 的依赖探测必须复用本模块的进程执行器与文件存在性检查，不能在 CLI 再维护一套平行探测逻辑
- 输出文件路径由计划阶段显式计算，不在执行后靠模糊扫描推断

## 常见坑

- 修改 quoting 或参数拼接顺序时，容易打破命令快照测试
- 在入口层直接调用 `Process` 会绕开超时、取消和日志采集
- 额外参数和 `faststart` 规则容易产生重复参数，需要保持单一映射点

## 文档同步触发条件

- `CommandPlan` 或 `ExecutionResult` schema 变化
- 新增新的外部命令执行器或路径规则
- 执行状态、日志采集或取消/超时语义变化
