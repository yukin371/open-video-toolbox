# CLI MVP

最后更新：2026-04-15

## 目标

定义第一版最常用、最适合被外部 AI 代理调用的 CLI 能力范围，并明确 `edit.json` 作为 AI 生成与人工二次修正之间的共同边界。

## 设计原则

- 命令必须确定性，可重复执行。
- 默认输出机器可读 JSON；必要时再补人类可读文本。
- 软件内不做 AI 推理；AI 只在仓库外部做决策。
- 二次手动剪辑先通过手改 `edit.json` 完成，不先做完整 GUI 时间线。

## MVP 功能清单

### 已有基础

- `probe`
  - 输出标准化媒体信息
- `plan`
  - 输出任务定义与命令计划
- `run`
  - 执行转码任务并输出执行结果
- `templates`
  - 输出内置模板列表与 artifact slot
  - 支持按模板 id 输出单模板指南，便于外部 AI 直接生成 `artifacts.json`、`template-params.json` 并选择合适的 seed 方式
- `init-plan`
  - 已实现生成可编辑的 `edit.json` skeleton
  - 已支持 `--artifacts <artifacts.json>` 绑定模板 artifact slot
  - 已支持 `--template-params <template-params.json>` 覆盖模板默认参数
  - 已支持 `--transcript <transcript.json>`、`--seed-from-transcript`
  - 已支持 `--beats`、`--seed-from-beats`、`--beat-group-size`
- `beat-track`
  - 已实现输出 `beats.json`
- `cut`
  - 已实现最小单段裁切命令
- `concat`
  - 已实现基于片段列表的最小拼接命令
- `extract-audio`
  - 已实现按轨提取音频命令
- `subtitle`
  - 已实现从外部 `transcript.json` 生成 `srt` / `ass`
- `mix-audio`
  - 已实现从 `edit.json` 导出独立音频混合结果

### 第一批编辑基元

- `cut`
  - 按时间段裁切单段或多段
- `concat`
  - 合并多个片段或文件
- `extract-audio`
  - 提取音频轨
- `render`
  - 根据 `edit.json` 执行最终导出

说明：
- `templates`、`init-plan`、`beat-track`、`cut`、`concat`、`extract-audio`、`subtitle`、`mix-audio`、`render` 已进入当前实现。

### 第二批常用增强

- `separate-audio`
  - 输出人声 / 背景 / 原音轨等分离结果
- `subtitle`
  - 生成 `srt` / `ass`，并支持烧录或外挂导出
- `beat-track`
  - 输出 BPM、节拍点、鼓点时间标记
- `mix-audio`
  - 处理 BGM、多轨混音、音量包络和 ducking

说明：
- `subtitle`、`beat-track`、`mix-audio` 已进入当前实现。
- `separate-audio` 仍是后续能力，不代表当前仓库已实现。

## 最小中间产物

- `media.json`
  - 来自 `probe`
- `transcript.json`
  - 来自外部 AI 或外部转录工具，不要求仓库内生成
- `beats.json`
  - 来自 `beat-track`
- `edit.json`
  - AI 生成的剪辑计划与人工二次修正结果
  - 保留模板标识与扩展字段，便于外部 AI 复用固定套路

## 二次手动剪辑策略

### 要支持什么

- 改片段入点 / 出点
- 删除片段
- 调整片段顺序
- 指定 BGM 轨道、起止区间和音量
- 修字幕文本或时间轴

### 暂不支持什么

- 完整非线性时间线 GUI
- 复杂关键帧动画
- 多人协作编辑系统

## 计划中的命令草案

```text
ovt probe <input> --json-out media.json
ovt templates
ovt templates --category short-form
ovt templates --seed-mode beats --json-out templates.json
ovt templates --artifact-kind subtitle --has-subtitles --summary
ovt templates shorts-captioned
ovt templates shorts-captioned --write-examples .template-guide
ovt init-plan <input> --template shorts-captioned --output edit.json --render-output final.mp4
ovt scaffold-template <input> --template shorts-captioned --dir .workspace --validate
ovt init-plan <input> --template shorts-captioned --output edit.json --render-output final.mp4 --artifacts artifacts.json
ovt init-plan <input> --template shorts-captioned --output edit.json --render-output final.mp4 --template-params template-params.json
ovt init-plan <input> --template shorts-basic --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript
ovt init-plan <input> --template shorts-captioned --output edit.json --render-output final.mp4 --beats beats.json --seed-from-beats --beat-group-size 2
ovt cut <input> --from 00:00:12.000 --to 00:00:27.500 --output clip-01.mp4
ovt concat --input-list clips.txt --output merged.mp4
ovt extract-audio <input> --track 0 --output voice.wav
ovt separate-audio <input> --output-dir stems/
ovt subtitle <input> --transcript transcript.json --format srt --output subtitles.srt
ovt beat-track <input> --output beats.json
ovt validate-plan --plan edit.json --check-files
ovt mix-audio --plan edit.json --output mixed.wav --preview
ovt render --plan edit.json --output final.mp4 --preview
ovt mix-audio --plan edit.json --output mixed.wav
ovt render --plan edit.json --output final.mp4
```

说明：
- `templates`、`init-plan`、`scaffold-template`、`beat-track`、`cut`、`concat`、`extract-audio`、`subtitle`、`validate-plan`、`mix-audio`、`render` 已实现。
- 其余命令仍是 MVP 草案，不代表当前仓库已经实现。
- `templates <id>` 会返回单模板详情，以及适合直接保存的 `artifacts.json` / `template-params.json` skeleton。
- `templates <id>` 还会返回 `recommendedSeedModes` 与对应 `seedCommands`，明确这个模板更适合手工 plan、transcript seed 还是 beat seed。
- `templates <id>` 还会返回 `previewPlans`，直接展示按这些 seed 模式生成后的最小 `edit.json` 形状。
- `templates <id> --write-examples <dir>` 会直接写出 `guide.json`、`template.json`、`artifacts.json`、`template-params.json`、`preview-*.edit.json`，以及 `commands.json` / `commands.ps1` / `commands.cmd` / `commands.sh`，适合外部 AI 或脚本直接复用。
- `templates --category <id>`、`--seed-mode <mode>`、`--output-container <ext>`、`--artifact-kind <kind>`、`--has-artifacts`、`--has-subtitles` 可以先缩小模板集合，再决定是否查询单模板详情。
- `templates --summary` 会返回稳定的机器友好摘要，避免外部 AI 先读取完整模板定义再自行裁剪字段。
- `templates --json-out <path>` 会把当前返回值原样写到文件，适合把模板列表或筛选结果直接交给后续脚本。
- `scaffold-template` 会把 `guide.json`、`template.json`、`artifacts.json`、`template-params.json`、`preview-*.edit.json`、命令脚本文件和初始 `edit.json` 一次落到工作目录，适合外部 AI 在目录内继续编辑。
- `scaffold-template --validate` 会在生成后立即附带一份 plan 校验结果；加 `--check-files` 时，缺失输入或引用文件会让命令以非零退出码返回。
- `validate-plan --check-files` 会额外检查 `source`、`audioTracks`、`artifacts`、`transcript`、`beats`、`subtitles` 引用文件是否存在；命令无论成功失败都输出 JSON，失败时返回非零退出码。
- `mix-audio --preview` 与 `render --preview` 会输出统一的 `executionPreview`，不会创建目录、不会启动 `ffmpeg`；适合外部 AI 先审查路径、参数、滤镜图与预期产物。
- `init-plan --artifacts` 读取一个简单 JSON object，例如 `{ "subtitles": "subs/captions.srt", "bgm": "audio/theme.wav" }`，把模板 slot 绑定写进 `edit.json.artifacts`。
- `init-plan --template-params` 读取一个简单 JSON object，例如 `{ "hookStyle": "match-cut", "captionStyle": "clean-sidecar" }`，把模板默认参数覆盖后写进 `edit.json.template.parameters`。
- `init-plan --transcript` 会把 `transcript.json` 写入顶层 `transcript`，仅作引用。
- `init-plan` 只有在额外传入 `--seed-from-transcript` 时，才会按 transcript segment 确定性生成初始 clips；若同时传 `--seed-from-transcript` 和 `--seed-from-beats`，CLI 会直接报错。
- `init-plan` 在传入 `--beats` 时会把 `beats.json` 写入顶层 `beats`，仅作引用。
- `init-plan` 只有在额外传入 `--seed-from-beats` 时，才会按节拍组自动生成初始 clips；`--beat-group-size` 默认为 `4`。

## `beats.json` 最小结构草案

```json
{
  "schemaVersion": 1,
  "sourcePath": "input.mp4",
  "sampleRateHz": 16000,
  "frameDuration": "00:00:00.0500000",
  "estimatedBpm": 120,
  "beats": [
    {
      "index": 0,
      "time": "00:00:00.5000000",
      "strength": 0.82
    }
  ]
}
```

说明：
- `beat-track` 当前是确定性波形分析，不依赖内置 AI 或在线服务。
- 输出先服务于节奏参考、模板填充和外部脚本编排，不先追求 DAW 级精度。

## `transcript.json` 最小结构草案

```json
{
  "schemaVersion": 1,
  "language": "zh-CN",
  "segments": [
    {
      "id": "seg-001",
      "start": "00:00:00.500",
      "end": "00:00:02.000",
      "text": "hello from toolbox subtitle command"
    }
  ]
}
```

说明：
- `transcript.json` 只保留最小字段，方便外部 AI 或其他工具直接生成。
- `subtitle` 默认会做简单换行，输出机器可复现的 `srt` / `ass`。

## `edit.json` 最小结构草案

说明：
- 对应的 `schema v1` 核心模型已经落在 `src/OpenVideoToolbox.Core/Editing`。

```json
{
  "schemaVersion": 1,
  "source": {
    "inputPath": "input.mp4"
  },
  "template": {
    "id": "shorts-captioned",
    "version": "1.0.0",
    "parameters": {
      "hookStyle": "hard-cut"
    }
  },
  "clips": [
    {
      "id": "clip-001",
      "in": "00:00:12.000",
      "out": "00:00:27.500",
      "label": "intro-hook"
    }
  ],
  "audioTracks": [
    {
      "id": "bgm-01",
      "role": "bgm",
      "path": "bgm.wav",
      "start": "00:00:00.000",
      "gainDb": -8.0
    }
  ],
  "artifacts": [
    {
      "slotId": "subtitles",
      "kind": "subtitle",
      "path": "subtitles.srt"
    }
  ],
  "transcript": {
    "path": "transcript.json",
    "language": "zh-CN",
    "segmentCount": 24
  },
  "subtitles": {
    "path": "subtitles.srt",
    "mode": "sidecar"
  },
  "output": {
    "path": "final.mp4",
    "container": "mp4"
  },
  "beats": {
    "path": "beats.json",
    "estimatedBpm": 120
  }
}
```

说明：
- `template` 是稳定入口，用来给外部 AI 或脚本复用固定剪辑套路。
- `artifacts` 是模板 slot 到具体文件路径的稳定绑定层，用来给未来模板扩展、外部 AI 和轻量 UI 共享同一份 artifact 语义。
- `transcript` 是显式顶层字段，用来表达 transcript 引用和基础元数据，便于外部 AI、字幕流程和后续编辑辅助共享同一份来源。
- `beats` 是显式顶层字段，不再藏在扩展区，便于 CLI、外部 AI 和未来 UI 共享相同语义。
- 如果只传 `--transcript`，它只表达 transcript 来源，不强制改变现有 clips。
- 如果同时传 `--seed-from-transcript`，`init-plan` 会按 transcript segment 的 `start/end` 确定性生成初始 clips。
- 如果只传 `--beats`，它只表达节奏参考来源，不强制改变现有 clips。
- 如果同时传 `--seed-from-beats`，`init-plan` 会按 `beat[i]` 到 `beat[i+N]` 的规则，以 `N = --beat-group-size` 确定性生成初始 clips。

## 收敛规则

- `edit.json` 一旦确定字段语义，就不要在多个命令里各自发明变体。
- 新增编辑命令前，先明确它是生成 `edit.json`、消费 `edit.json`，还是两者都做。
- 若未来需要 GUI，GUI 也应复用同一份 `edit.json` 边界，而不是重造计划模型。

## 实施追踪

- 具体实施顺序、波次划分和验收标准见 `docs/plans/2026-04-15-cli-mvp-implementation.md`。
