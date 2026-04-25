# OpenVideoToolbox.Core.Execution

> 最后更新：2026-04-24

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
- `demucs` 音频分离命令构建与执行编排
- `ffmpeg silencedetect` 命令构建与执行编排
- 转码任务执行编排
- `mix-audio --plan` 音频导出编排
- `audio-gain` 显式分贝增益命令构建与执行编排
- `render --plan` 最终导出编排
- `export --format edl` 的导出投影归一化、v1 包装与 EDL 文本生成
- `FfmpegTimelineRenderCommandBuilder` 与后续 v2 timeline render 语义
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
- `export` 的 v1/v2 兼容规则必须集中在本模块，不能在 CLI 维护第二套包装或主轨选择逻辑
- v2 timeline render builder 必须与 v1 render builder 并存，直到显式完成 parity / deprecation 决策，不能隐式替换现有 v1 路径
- render preview / runner 对 v1/v2 的 builder 分发必须留在本模块，不能把 timeline 分支判断回流到 CLI
- 当前 v2 render baseline 只保证 timeline builder、preview/execute dispatch 和基础 template effect filter graph；复杂 executor effect 仍不得伪装成“已正式支持”
- v2 timeline 的 still-image 输入循环、帧率接线和 built-in layout/filter effect 映射必须集中在本模块，不能回流到 CLI 或模板 builder
- v2 timeline 的 placeholder video 必须由本模块直接映射到运行时 FFmpeg 输入；首版只允许 `color` placeholder，不能回退成 CLI 预生成临时视频文件
- `export L1` 当前只承诺粗粒度 `EDL` cut list，不得在未完成设计收口前把 XML / fcpxml / effect 映射一起混入
- preview 语义必须复用和执行阶段相同的 `CommandPlan`、`ProducedPaths` 与 side effect 规则，不能在 CLI 维护平行逻辑
- `DefaultProcessRunner` 必须保留标准输出、标准错误、超时和取消上下文
- `ExecutionResult` 是执行链路的统一结果模型，状态和原始输出都不能丢
- `doctor` 的依赖探测必须复用本模块的进程执行器与文件存在性检查，不能在 CLI 再维护一套平行探测逻辑
- 输出文件路径由计划阶段显式计算，不在执行后靠模糊扫描推断
- 外部命令调用必须通过 `ProcessStartInfo.ArgumentList` / `UseShellExecute = false` 进入统一执行器，不能回退到 shell 字符串拼接
- timeout / cancellation 必须复用统一进程终止与状态映射逻辑，不能在单命令 runner 里各自实现
- overwrite 行为必须显式传入请求并映射到单一命令构建点，不能让命令默认静默覆盖已有文件
- sidecar copy、preview 与 execute 必须复用同一份 `ProducedPaths` / side effect 规则，避免安全与审计语义漂移
- 原始 stdout / stderr 日志必须保留到 `ExecutionResult`，不能只留下摘要错误消息

## 常见坑

- 修改 quoting 或参数拼接顺序时，容易打破命令快照测试
- 在入口层直接调用 `Process` 会绕开超时、取消和日志采集
- 额外参数和 `faststart` 规则容易产生重复参数，需要保持单一映射点

## 文档同步触发条件

- `CommandPlan` 或 `ExecutionResult` schema 变化
- 新增新的外部命令执行器或路径规则
- 执行状态、日志采集或取消/超时语义变化
- v1/v2 render 分发规则或 timeline builder 覆盖范围变化
- still-image 输入适配或 built-in effect 到 FFmpeg filter 的映射语义变化
- timeline placeholder video 的输入建模或 FFmpeg 映射语义变化
- `export` 的格式范围、warning 契约或 v1/v2 导出兼容策略变化
