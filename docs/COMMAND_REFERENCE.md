# 命令速查

最后更新：2026-04-22

## 文档定位

本文档只保留当前 CLI 的精确命令签名与最短说明。

它的职责是：

- 快速查某条命令有哪些参数
- 对照 CLI 实际帮助输出
- 作为 `Quick Start` 和完整使用手册之间的“速查层”

它不负责：

- 工作流说明
- 中间产物设计
- 架构边界

这些内容分别以 `FEATURES_AND_USAGE.md`、`CLI_MVP.md`、`ARCHITECTURE_GUARDRAILS.md` 为准。

## 说明

- 以下签名按 2026-04-22 实际 CLI 顶层帮助输出整理。
- 命令默认优先输出结构化 JSON。
- 传 `--json-out <path>` 时，会把 stdout 的同一份结果原样写到文件。
- 本页只写命令尾部签名；如何选择源码运行或 release 二进制，见 `QUICK_START.md`。

## 基础媒体

### `presets`

```text
presets [--json-out <path>]
```

用途：

- 列出内置转码预设

### `probe`

```text
probe <input> [--ffprobe <path>] [--json-out <path>]
```

用途：

- 输出规范化媒体探测结果

### `plan`

```text
plan <input> [--preset <id>] [--output-dir <dir>] [--output-name <name>] [--ffmpeg <path>] [--json-out <path>] [--overwrite]
```

用途：

- 生成传统转码计划

### `run`

```text
run <input> [--preset <id>] [--output-dir <dir>] [--output-name <name>] [--ffprobe <path>] [--ffmpeg <path>] [--timeout-seconds <n>] [--json-out <path>] [--overwrite]
```

用途：

- 执行传统转码任务

## 模板与计划工作流

### `templates`

```text
templates [<template-id>] [--template <id>] [--category <id>] [--seed-mode <manual|transcript|beats>] [--output-container <ext>] [--artifact-kind <kind>] [--has-artifacts [true|false]] [--has-subtitles [true|false]] [--summary [true|false]] [--plugin-dir <path>] [--json-out <path>] [--write-examples <dir>]
```

用途：

- 模板发现、单模板指南、工作目录示例输出

### `doctor`

```text
doctor [--ffmpeg <path>] [--ffprobe <path>] [--whisper-cli <path>] [--whisper-model <path>] [--demucs <path>] [--json-out <path>] [--timeout-seconds <n>]
```

用途：

- 统一检查外部依赖状态

### `validate-plugin`

```text
validate-plugin --plugin-dir <path> [--json-out <path>]
```

用途：

- 显式校验模板插件目录、manifest 与模板定义是否合规

### `init-plan`

```text
init-plan <input> --template <id> --output <edit.json> [--render-output <path>] [--probe] [--ffprobe <path>] [--transcript <transcript.json>] [--seed-from-transcript] [--transcript-segment-group-size <n>] [--min-transcript-segment-duration-ms <n>] [--max-transcript-gap-ms <n>] [--beats <beats.json>] [--seed-from-beats] [--beat-group-size <n>] [--artifacts <artifacts.json>] [--template-params <template-params.json>] [--subtitle <path>] [--subtitle-mode <sidecar|burnIn|none>] [--bgm <path>] [--plugin-dir <path>] [--timeout-seconds <n>]
```

用途：

- 从模板生成初始 `edit.json`

### `scaffold-template`

```text
scaffold-template <input> --template <id> --dir <workdir> [--validate [true|false]] [--check-files [true|false]] [--render-output <path>] [--probe] [--ffprobe <path>] [--transcript <transcript.json>] [--seed-from-transcript] [--transcript-segment-group-size <n>] [--min-transcript-segment-duration-ms <n>] [--max-transcript-gap-ms <n>] [--beats <beats.json>] [--seed-from-beats] [--beat-group-size <n>] [--artifacts <artifacts.json>] [--template-params <template-params.json>] [--subtitle <path>] [--subtitle-mode <sidecar|burnIn|none>] [--bgm <path>] [--plugin-dir <path>] [--timeout-seconds <n>]
```

用途：

- 一次落出模板工作目录

### `validate-plan`

```text
validate-plan --plan <edit.json> [--check-files [true|false]] [--plugin-dir <path>] [--json-out <path>]
```

用途：

- 校验 `edit.json`

## 音频 / speech / supporting signal

### `beat-track`

```text
beat-track <input> --output <beats.json> [--ffmpeg <path>] [--sample-rate <hz>] [--json-out <path>] [--timeout-seconds <n>]
```

用途：

- 生成 `beats.json`

### `audio-analyze`

```text
audio-analyze <input> --output <audio.json> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>]
```

用途：

- 生成 `audio.json`

### `audio-gain`

```text
audio-gain <input> --gain-db <n> --output <path> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>] [--overwrite]
```

用途：

- 做显式增益导出

### `transcribe`

```text
transcribe <input> --model <path> --output <transcript.json> [--language <id>] [--translate [true|false]] [--whisper-cli <path>] [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>]
```

用途：

- 生成 `transcript.json`

### `detect-silence`

```text
detect-silence <input> --output <silence.json> [--noise-db <n>] [--min-duration-ms <n>] [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>]
```

用途：

- 生成 `silence.json`

### `separate-audio`

```text
separate-audio <input> --output-dir <path> [--model <id>] [--demucs <path>] [--json-out <path>] [--timeout-seconds <n>]
```

用途：

- 生成 stem 分离结果

## 编辑基元

### `cut`

```text
cut <input> --from <hh:mm:ss.fff> --to <hh:mm:ss.fff> --output <path> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>] [--overwrite]
```

用途：

- 单段裁切

### `concat`

```text
concat --input-list <path> --output <path> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>] [--overwrite]
```

用途：

- 片段拼接

### `extract-audio`

```text
extract-audio <input> --track <n> --output <path> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>] [--overwrite]
```

用途：

- 音轨提取

### `subtitle`

```text
subtitle <input> --transcript <transcript.json> --format <srt|ass> --output <path> [--max-line-length <n>] [--json-out <path>]
```

用途：

- 渲染 sidecar 字幕

## 导出

### `mix-audio`

```text
mix-audio --plan <edit.json> --output <path> [--preview [true|false]] [--json-out <path>] [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]
```

用途：

- 混音预览与执行

### `render`

```text
render --plan <path> [--output <path>] [--preview [true|false]] [--json-out <path>] [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]
```

用途：

- 最终导出预览与执行

## 建议阅读顺序

- 第一次上手：
  - [QUICK_START.md](QUICK_START.md)
- 看完整功能、工作流与排障：
  - [FEATURES_AND_USAGE.md](FEATURES_AND_USAGE.md)
- 看命令签名速查：
  - 本文档
- 看中间产物和设计边界：
  - [CLI_MVP.md](CLI_MVP.md)
