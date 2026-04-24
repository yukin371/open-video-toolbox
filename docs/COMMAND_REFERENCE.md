# 命令速查

最后更新：2026-04-24

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

- 以下签名按 2026-04-24 实际 CLI 顶层帮助输出整理。
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

### `scaffold-template-batch`

```text
scaffold-template-batch --manifest <batch.json> [--plugin-dir <path>] [--json-out <path>]
```

用途：

- 从 manifest 批量生成模板工作目录，并在 manifest 同目录写出 `summary.json`
- 每个条目还会额外写出 `results/<id>.json`
- 未显式提供 `workdir` 的条目默认落到 `tasks/<id>`
- 退出码约定：全部成功返回 `0`，部分或全部条目失败返回 `2`，manifest 解析或装载失败返回 `1`

### `validate-plan`

```text
validate-plan --plan <edit.json> [--check-files [true|false]] [--plugin-dir <path>] [--json-out <path>]
```

用途：

- 校验 `edit.json`

### `inspect-plan`

```text
inspect-plan --plan <edit.json> [--check-files [true|false]] [--plugin-dir <path>] [--json-out <path>]
```

用途：

- 输出 `edit.json` 的素材概览、可替换目标、缺失绑定、transcript / beats / subtitles signal 状态与校验摘要
- `signals[].status` 会额外给出总状态：`attachedPresent`、`attachedMissing`、`attachedNotChecked`、`expectedUnbound`、`optionalUnbound`

### `replace-plan-material`

```text
replace-plan-material --plan <edit.json> [--write-to <path>] [--in-place [true|false]] --path <new-file> (--source-input | --audio-track-id <id> | --artifact-slot <slotId> | --transcript | --beats | --subtitles) [--subtitle-mode <sidecar|burnIn>] [--path-style <auto|relative|absolute>] [--check-files [true|false]] [--plugin-dir <path>] [--require-valid [true|false]] [--json-out <path>]
```

用途：

- 对 plan 中已存在的 source、audio track、artifact、transcript、beats 或 subtitles 做受控替换

### `replace-plan-material-batch`

```text
replace-plan-material-batch --manifest <batch.json> [--plugin-dir <path>] [--json-out <path>]
```

用途：

- 从批量 manifest 读取多条素材替换任务，逐项复用 `replace-plan-material` 流程
- 支持批量替换 `sourceInput`、`transcript`、`beats`、`subtitles`、`audioTrackId`、`artifactSlot`
- 在 manifest 同目录固定写出 `summary.json` 与 `results/<id>.json`
- 退出码约定：全部成功返回 `0`，部分或全部条目失败返回 `2`，manifest 解析或装载失败返回 `1`

### `attach-plan-material`

```text
attach-plan-material --plan <edit.json> [--write-to <path>] [--in-place [true|false]] --path <new-file> (--transcript | --beats | --subtitles | --audio-track-id <id> [--audio-track-role <original|voice|bgm|effects>] | --artifact-slot <slotId>) [--subtitle-mode <sidecar|burnIn>] [--path-style <auto|relative|absolute>] [--check-files [true|false]] [--plugin-dir <path>] [--require-valid [true|false]] [--json-out <path>]
```

用途：

- 对当前缺失的 `transcript`、`beats`、`subtitles`、`audioTracks` 做显式挂载，或对模板已声明的 artifact slot 做 upsert

### `attach-plan-material-batch`

```text
attach-plan-material-batch --manifest <batch.json> [--plugin-dir <path>] [--json-out <path>]
```

用途：

- 从批量 manifest 读取多条素材挂载任务，逐项复用 `attach-plan-material` 流程
- 支持批量接回 `transcript`、`beats`、`subtitles`、`audioTrackId`、`artifactSlot`
- 在 manifest 同目录固定写出 `summary.json` 与 `results/<id>.json`
- 退出码约定：全部成功返回 `0`，部分或全部条目失败返回 `2`，manifest 解析或装载失败返回 `1`

### `bind-voice-track`

```text
bind-voice-track --plan <edit.json> --path <audio-file> [--track-id <id>] [--role <original|voice|bgm|effects>] [--write-to <path>] [--in-place [true|false]] [--path-style <auto|relative|absolute>] [--check-files [true|false]] [--plugin-dir <path>] [--require-valid [true|false]] [--json-out <path>]
```

用途：

- 用默认 `voice-main` / `voice` 约定，把外部 dubbing、TTS 或 voice conversion 产物接回 `edit.json`

### `bind-voice-track-batch`

```text
bind-voice-track-batch --manifest <batch.json> [--plugin-dir <path>] [--json-out <path>]
```

用途：

- 从批量 manifest 读取多条 plan / 音频绑定任务，逐项复用 `bind-voice-track` 流程，并返回部分成功摘要
- 退出码约定：全部成功返回 `0`，部分或全部条目失败返回 `2`，manifest 解析或装载失败返回 `1`

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

### `audio-normalize`

```text
audio-normalize <input> --output <path> [--target-lufs <n>] [--lra <n>] [--true-peak-db <n>] [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>] [--overwrite]
```

用途：

- 做独立响度归一化导出

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

### `render-batch`

```text
render-batch --manifest <batch.json> [--preview [true|false]] [--ffmpeg <path>] [--timeout-seconds <n>] [--json-out <path>]
```

用途：

- 从批量 manifest 读取多份 `edit.json`，逐项复用 `render` 语义
- 支持先用 `--preview` 汇总 execution preview，再决定是否真正执行
- item 级当前支持：`id`、`plan`、可选 `output`、可选 `overwrite`
- 每个条目会额外写出 `results/<id>.json`
- 退出码约定：全部成功返回 `0`，部分或全部条目失败返回 `2`，manifest 解析或装载失败返回 `1`

## 建议阅读顺序

- 第一次上手：
  - [QUICK_START.md](QUICK_START.md)
- 看完整功能、工作流与排障：
  - [FEATURES_AND_USAGE.md](FEATURES_AND_USAGE.md)
- 看命令签名速查：
  - 本文档
- 看中间产物和设计边界：
  - [CLI_MVP.md](CLI_MVP.md)
