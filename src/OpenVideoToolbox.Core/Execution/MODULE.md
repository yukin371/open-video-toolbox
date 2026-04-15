# OpenVideoToolbox.Core.Execution

> 最后更新：2026-04-15

## 职责

本模块是外部命令计划与执行链路的 canonical owner，负责把 `JobDefinition + PresetDefinition` 转成 `CommandPlan`，执行外部进程，并沉淀 `ExecutionResult` 与原始输出。

## Owns

- `ffmpeg` 命令构建
- 输出路径规范化
- 进程执行、超时、取消和输出采集
- 转码任务执行编排
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
- `DefaultProcessRunner` 必须保留标准输出、标准错误、超时和取消上下文
- `ExecutionResult` 是执行链路的统一结果模型，状态和原始输出都不能丢
- 输出文件路径由计划阶段显式计算，不在执行后靠模糊扫描推断

## 常见坑

- 修改 quoting 或参数拼接顺序时，容易打破命令快照测试
- 在入口层直接调用 `Process` 会绕开超时、取消和日志采集
- 额外参数和 `faststart` 规则容易产生重复参数，需要保持单一映射点

## 文档同步触发条件

- `CommandPlan` 或 `ExecutionResult` schema 变化
- 新增新的外部命令执行器或路径规则
- 执行状态、日志采集或取消/超时语义变化
