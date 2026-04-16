# CLI MVP Implementation Plan

日期：2026-04-15

## 目标

把当前产品想法收敛成第一批真正可实施、可验证、可追踪的命令与模型范围。

配套图：
- `docs/wave1-cli-flow.drawio`
- `docs/code-module-map.drawio`

## 基本原则

- 软件内不实现 AI provider。
- 外部 AI 代理只通过 CLI 命令和结构化 JSON 输出编排工作流。
- 二次手动剪辑先通过 `edit.json` 完成，不先做完整时间线编辑器。
- 优先复用现有 `Core.Media`、`Core.Execution`、`Core.Presets`。

## Wave 0：现有基线

- `presets`
- `probe`
- `plan`
- `run`

说明：
- 这批命令是当前已实现基线。
- 新命令的 JSON 输出风格、错误输出风格应与这批命令保持一致。

## Wave 1：最小剪辑闭环

状态：
- 已完成

目标：
- 让外部 AI 先生成片段方案，再通过 CLI 完成最小剪辑闭环。

范围：
- `cut`
  - 单段裁切
  - 多段裁切
- `concat`
  - 合并多个文件或片段
- `extract-audio`
  - 按轨提取音频
- `render`
  - 消费 `edit.json` 并导出最终视频

依赖：
- `edit.json` schema v1
- 命令统一 JSON 输出契约

验收标准：
- 上述 4 个命令都能运行
- 每个命令都有稳定 JSON 输出
- 外部 AI 可以只靠 CLI 输出组织一次最小剪辑闭环

## Wave 2：计划文件边界固化

状态：
- 已完成

目标：
- 固化 AI 生成与人工二次修正之间的共同边界。

范围：
- `edit.json` schema v1
  - `source`
  - `clips`
  - `audioTracks`
  - `beats`
  - `subtitles`
  - `output`
- `render --plan edit.json`
  - 支持消费 schema v1
- 输出契约文档
  - 时间格式
  - 路径字段
  - 错误输出结构

验收标准：
- 人工可以直接修改 `edit.json`
- 修正后的 `edit.json` 可以被 `render` 消费
- 字段语义固定，不在多个命令里各自发明变体

## Wave 3：常用增强能力

状态：
- 已完成第一批

目标：
- 覆盖短视频、解说视频和常见后处理场景。

范围：
- `subtitle`
  - 消费外部 `transcript.json`
  - 生成 `srt` / `ass`
- `beat-track`
  - 输出 BPM、beat marker、时间点
- `mix-audio`
  - 处理 BGM、多轨混音、ducking

验收标准：
- 至少有一条真实闭环
- 对应产物能进入 `edit.json` 或与其配套使用

当前结果：
- `subtitle` 已可从外部 `transcript.json` 输出 `srt` / `ass`
- `beat-track` 已可输出 `beats.json`
- `mix-audio` 已可独立消费 `edit.json`
- `init-plan` 已可在传入 `--beats` 时写入顶层 `beats`，并在 `--seed-from-beats` 下按节拍组生成初始 clips

## Wave 4：后置能力

- `separate-audio`
  - 依赖外部工具或后续评估
- 轻量可视化编辑器
  - 仅在 `edit.json` 和 CLI 闭环稳定后评估

## 具体实施顺序

1. 定义 `cut` 命令契约和 JSON 输出。
2. 定义 `edit.json` schema v1。
3. 实现 `cut`。
4. 实现 `concat`。
5. 实现 `extract-audio`。
6. 实现 `render --plan edit.json`。
7. 为 Wave 1 命令补参数解析测试和输出契约测试。
8. 继续补真实素材 smoke 与更丰富模板，而不是再重新定义计划边界。

## 模块落点

- `Core.Media`
  - 输入媒体信息与轨道元数据
- `Core.Execution`
  - 命令计划、进程执行、日志采集、输出产物
- `Core`
  - 新增 `edit.json` 相关模型时，落在最贴近任务 / 执行语义的位置
- `Cli`
  - 参数解析、JSON 输出、错误码、帮助文本

## 验证方式

- `dotnet build E:\Github\open-video-toolbox\OpenVideoToolbox.sln`
- `dotnet test E:\Github\open-video-toolbox\OpenVideoToolbox.sln`
- 每个新增命令至少补：
  - 1 个参数解析 / 输出契约测试
  - 1 个命令计划或执行计划测试

## 不在本计划内

- 内置 AI 推理
- 供应商 SDK / LLM API
- 完整多轨时间线 GUI
- 复杂动效、关键帧和协作编辑
