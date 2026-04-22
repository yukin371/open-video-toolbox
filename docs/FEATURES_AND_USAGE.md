# 功能与使用总览

最后更新：2026-04-22

## 文档定位

本文档面向实际使用者，聚合当前仓库已经落地的功能、依赖、典型工作流与命令入口。

它回答的是：

- 现在到底能做什么
- 应该先装什么
- 常见工作流怎么串
- 每类命令各自负责什么

它不负责：

- 当前阶段决策与路线图
- 架构 owner 规则
- MVP 设计历史

这些内容分别以 `docs/roadmap.md`、`docs/ARCHITECTURE_GUARDRAILS.md`、`docs/CLI_MVP.md` 为准。

与本文档配套的文档栈：

- 最短上手路径：`docs/QUICK_START.md`
- 精确命令签名速查：`docs/COMMAND_REFERENCE.md`
- 设计边界与中间产物草案：`docs/CLI_MVP.md`

## 当前已交付能力

当前 CLI 已完成 `H1 -> H2+T1 -> T2 -> P1 -> E1`，可直接使用的命令共 21 条：

- 基础媒体：`probe`、`plan`、`run`
- 模板与工作流：`templates`、`doctor`、`init-plan`、`scaffold-template`、`validate-plan`
- 音频 / speech / signals：`beat-track`、`audio-analyze`、`audio-gain`、`transcribe`、`detect-silence`、`separate-audio`
- 编辑基元：`cut`、`concat`、`extract-audio`、`subtitle`
- 执行导出：`mix-audio`、`render`
- 查询：`presets`

这些命令现在都满足：

- 成功与失败路径都优先返回结构化 JSON
- 支持稳定的 command envelope
- 关键命令已进入契约快照保护

## 命令前缀约定

下文示例统一使用 `<ovt>` 作为命令前缀占位符。你可以按自己的运行方式替换：

```powershell
# 从源码运行
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj --

# Windows Release 二进制
.\ovt-win-x64.exe
```

如果你直接使用 GitHub Release 上的其他平台二进制，也可以把 `<ovt>` 替换成对应可执行文件路径。

## 安装与依赖

### 运行方式

当前最稳妥的两种入口：

- 源码运行
- 直接使用 GitHub Release single-file 二进制

当前发布链已支持：

- `win-x64`
- `linux-x64`
- `osx-x64`

Windows 发布资产当前同时包含：

- `ovt-win-x64.zip`
- `ovt-win-x64.exe`

说明：

- `winget portable` 提交草稿已准备，但正式提交仍受许可证元数据 blocker 约束。
- 当前最稳妥的安装方式仍是直接使用 GitHub Release 资产或源码运行。

### 必需依赖

所有媒体处理命令都依赖：

- `ffmpeg`
- `ffprobe`

详见 [external-dependencies.md](external-dependencies.md)。

### 可选依赖

以下命令需要额外依赖：

- `transcribe`
  - `whisper-cli`
  - `whisper model`
- `separate-audio`
  - `demucs`

### 常用环境变量

```powershell
$env:OVT_WHISPER_CLI_PATH = "C:\tools\whisper.cpp\build\bin\Release\whisper-cli.exe"
$env:OVT_WHISPER_MODEL_PATH = "C:\models\whisper\ggml-base.bin"
$env:OVT_DEMUCS_PATH = "C:\Users\<you>\AppData\Local\Programs\Python\Python311\Scripts\demucs.exe"
```

### 当前开发机验证样本

基于本轮实际执行的 `doctor`：

- `ffmpeg`
  - 已可用
- `ffprobe`
  - 已可用
- `whisper-cli`
  - 当前机器未解析到
- `demucs`
  - 当前机器未解析到
- `whisper-model`
  - 当前未配置

这意味着当前机器可以直接跑：

- `presets`
- `probe`
- `plan`
- `run`
- `templates`
- `doctor`
- `init-plan`
- `scaffold-template`
- `validate-plan`
- `beat-track`
- `audio-analyze`
- `audio-gain`
- `cut`
- `concat`
- `extract-audio`
- `subtitle`
- `mix-audio`
- `render`

而以下命令仍需先补依赖：

- `transcribe`
- `separate-audio`

这只是当前开发机样本，不是所有机器的保证；新环境仍应先跑一次 `doctor`。

## 按任务选入口

| 你要做什么 | 先看什么 | 第一条建议命令 |
|------|------|------|
| 第一次在新机器上跑 | `QUICK_START.md` | `<ovt> doctor --json-out doctor.json` |
| 想快速找模板 | 本文档 + `COMMAND_REFERENCE.md` | `<ovt> templates --summary` |
| 想直接做传统转码 | 本文档“传统转码”工作流 | `<ovt> probe input.mp4 --json-out probe.json` |
| 想生成 `edit.json` 再导出 | 本文档“模板生成到最终导出”工作流 | `<ovt> init-plan input.mp4 --template shorts-captioned --output edit.json --render-output final.mp4` |
| 想查某条命令的精确签名 | `COMMAND_REFERENCE.md` | 按命令名查对应条目 |

## 命令矩阵

下表按当前 CLI 实际帮助输出整理，便于快速定位“做某件事应该用哪条命令”。

| 分组 | 命令 | 主要用途 | 常见输出 |
|------|------|----------|----------|
| 基础媒体 | `presets` | 查看内置转码预设 | stdout / `--json-out` |
| 基础媒体 | `probe` | 媒体探测 | `probe.json` |
| 基础媒体 | `plan` | 传统转码计划预览 | `plan.json` |
| 基础媒体 | `run` | 传统转码执行 | `run.json` |
| 模板工作流 | `templates` | 模板发现、单模板指南、脚手架示例 | stdout / `guide.json` / `commands.*` |
| 模板工作流 | `doctor` | 外部依赖预检 | `doctor.json` |
| 模板工作流 | `init-plan` | 生成初始 `edit.json` | `edit.json` |
| 模板工作流 | `scaffold-template` | 一次落出模板工作目录 | 工作目录文件集 |
| 模板工作流 | `validate-plan` | 校验 `edit.json` | stdout / `--json-out` |
| supporting signals | `beat-track` | 节拍分析 | `beats.json` |
| supporting signals | `audio-analyze` | 响度分析 | `audio.json` |
| supporting signals | `audio-gain` | 显式音量增益 | 输出音频文件 |
| supporting signals | `transcribe` | 语音转写 | `transcript.json` |
| supporting signals | `detect-silence` | 停顿段检测 | `silence.json` |
| supporting signals | `separate-audio` | stem 分离 | `stems/` |
| 编辑基元 | `cut` | 单段裁切 | 输出片段 |
| 编辑基元 | `concat` | 片段拼接 | 输出成片 |
| 编辑基元 | `extract-audio` | 音轨提取 | 输出音频文件 |
| 编辑基元 | `subtitle` | 渲染 `srt` / `ass` | 字幕文件 |
| 导出 | `mix-audio` | 音频混合预览 / 执行 | `mixed.wav` / preview JSON |
| 导出 | `render` | 最终视频导出预览 / 执行 | `final.mp4` / preview JSON |

## 标准输出约定

所有命令都已统一到同一套 command envelope：

```json
{
  "command": "render",
  "preview": false,
  "payload": {
  }
}
```

使用时应默认假设：

- stdout 是主要结构化输出
- `--json-out <path>` 会把同一份结果原样写到文件
- 失败时仍尽量返回结构化 failure envelope，而不是退回 usage 文本

## 推荐上手顺序

如果第一次使用，建议按下面顺序：

1. `doctor`
2. `presets` / `templates`
3. `probe`
4. 选择直接执行流或模板流
5. `validate-plan` / `render` / `mix-audio`

最小检查命令：

```powershell
<ovt> doctor --json-out doctor.json
<ovt> presets
<ovt> templates --summary
```

## 基础媒体命令

需要精确参数签名时，直接对照 `docs/COMMAND_REFERENCE.md`。

### `presets`

用途：

- 列出内置转码预设

示例：

```powershell
<ovt> presets
```

### `probe`

用途：

- 调用真实 `ffprobe`
- 输出规范化媒体信息

示例：

```powershell
<ovt> probe input.mp4 --json-out probe.json
```

### `plan`

用途：

- 基于输入和 preset 生成任务计划
- 用于查看后续执行语义，而不是直接渲染

示例：

```powershell
<ovt> plan input.mp4 --preset h264-aac-mp4 --json-out plan.json
```

### `run`

用途：

- 执行传统 `probe -> transcode` 路径

示例：

```powershell
<ovt> run input.mp4 --preset h264-aac-mp4 --json-out run.json
```

## 模板与计划工作流

### `doctor`

用途：

- 统一检查 `ffmpeg`、`ffprobe`、`whisper-cli`、`demucs`、`whisper model`
- 适合作为所有复杂工作流的第一步

示例：

```powershell
<ovt> doctor --json-out doctor.json
```

### `templates`

用途：

- 列出内置模板
- 按模板 id 查看单模板指南
- 输出 artifact skeleton、template params、supporting signals、preview plan、命令脚本

常见用法：

```powershell
<ovt> templates
<ovt> templates --summary
<ovt> templates --category short-form
<ovt> templates shorts-captioned
<ovt> templates shorts-captioned --write-examples .template-guide
```

### `init-plan`

用途：

- 从模板生成 `edit.json`
- 可接入 transcript、beats、artifact、template params
- 可按 transcript 或 beats 直接生成确定性初始 clips

常见用法：

```powershell
<ovt> init-plan input.mp4 --template shorts-captioned --output edit.json --render-output final.mp4
<ovt> init-plan input.mp4 --template shorts-basic --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript
<ovt> init-plan input.mp4 --template shorts-captioned --output edit.json --render-output final.mp4 --beats beats.json --seed-from-beats --beat-group-size 2
```

### `scaffold-template`

用途：

- 一次性写出模板工作目录
- 适合外部 AI 直接在目录里继续加工

输出通常包含：

- `guide.json`
- `template.json`
- `artifacts.json`
- `template-params.json`
- `preview-*.edit.json`
- `commands.json`
- `commands.ps1`
- `commands.cmd`
- `commands.sh`
- 初始 `edit.json`

示例：

```powershell
<ovt> scaffold-template input.mp4 --template shorts-captioned --dir .workspace --validate
```

### `validate-plan`

用途：

- 对 `edit.json` 做结构与语义校验
- 可选检查引用文件是否存在
- 插件模板场景下可接回 `--plugin-dir`

示例：

```powershell
<ovt> validate-plan --plan edit.json --check-files
```

## 音频 / speech / supporting signal 命令

### `beat-track`

用途：

- 生成 `beats.json`
- 为节奏型模板和 beat seed 提供输入

示例：

```powershell
<ovt> beat-track input.mp4 --output beats.json --json-out beat-track.json
```

### `audio-analyze`

用途：

- 输出响度分析结果 `audio.json`

示例：

```powershell
<ovt> audio-analyze input.mp4 --output audio.json --json-out audio-analyze.json
```

### `audio-gain`

用途：

- 做显式分贝增益

示例：

```powershell
<ovt> audio-gain input.wav --gain-db -6 --output leveled.wav --json-out audio-gain.json
```

### `transcribe`

用途：

- 通过 `whisper-cli` 生成 `transcript.json`

示例：

```powershell
<ovt> transcribe input.mp4 --model ggml-base.bin --output transcript.json --json-out transcribe.json
```

### `detect-silence`

用途：

- 输出停顿段 `silence.json`

示例：

```powershell
<ovt> detect-silence input.mp4 --output silence.json --json-out detect-silence.json
```

### `separate-audio`

用途：

- 通过 `demucs` 生成人声 / 伴奏等 stems

示例：

```powershell
<ovt> separate-audio input.mp4 --output-dir stems --json-out separate-audio.json
```

## 编辑基元

### `cut`

用途：

- 最小单段裁切

示例：

```powershell
<ovt> cut input.mp4 --from 00:00:12.000 --to 00:00:27.500 --output clip-01.mp4
```

### `concat`

用途：

- 合并片段列表

示例：

```powershell
<ovt> concat --input-list clips.txt --output merged.mp4
```

### `extract-audio`

用途：

- 提取指定音轨

示例：

```powershell
<ovt> extract-audio input.mp4 --track 0 --output voice.m4a
```

### `subtitle`

用途：

- 把 `transcript.json` 渲染为 `srt` / `ass`

示例：

```powershell
<ovt> subtitle input.mp4 --transcript transcript.json --format srt --output subtitles.srt --json-out subtitle.json
```

## 导出与预览

### `mix-audio`

用途：

- 从 `edit.json` 导出混音结果

预览：

```powershell
<ovt> mix-audio --plan edit.json --output mixed.wav --preview --json-out mix-preview.json
```

实际执行：

```powershell
<ovt> mix-audio --plan edit.json --output mixed.wav
```

### `render`

用途：

- 从 `edit.json` 完成最终导出
- 支持字幕 sidecar 或 burn-in、混音和片段拼接

预览：

```powershell
<ovt> render --plan edit.json --output final.mp4 --preview --json-out render-preview.json
```

实际执行：

```powershell
<ovt> render --plan edit.json --output final.mp4
```

## 典型工作流

### 工作流 1：传统转码

适合不需要模板和 `edit.json` 的最小场景。

```powershell
<ovt> probe input.mp4 --json-out probe.json
<ovt> plan input.mp4 --preset h264-aac-mp4 --json-out plan.json
<ovt> run input.mp4 --preset h264-aac-mp4 --json-out run.json
```

### 工作流 2：模板生成到最终导出

适合大多数外部 AI / 脚本编排场景。

```powershell
<ovt> doctor --json-out doctor.json
<ovt> templates --summary
<ovt> init-plan input.mp4 --template shorts-captioned --output edit.json --render-output final.mp4
<ovt> validate-plan --plan edit.json --check-files
<ovt> render --plan edit.json --output final.mp4
```

### 工作流 3：字幕链路

```powershell
<ovt> transcribe input.mp4 --model ggml-base.bin --output transcript.json --json-out transcribe.json
<ovt> subtitle input.mp4 --transcript transcript.json --format srt --output subtitles.srt --json-out subtitle.json
<ovt> init-plan input.mp4 --template shorts-captioned --output edit.json --render-output final.mp4 --subtitle subtitles.srt
<ovt> render --plan edit.json --output final.mp4
```

### 工作流 4：节拍辅助模板

```powershell
<ovt> beat-track input.mp4 --output beats.json --json-out beat-track.json
<ovt> init-plan input.mp4 --template shorts-captioned --output edit.json --render-output final.mp4 --beats beats.json --seed-from-beats --beat-group-size 2
<ovt> render --plan edit.json --output final.mp4
```

### 工作流 5：插件模板

```powershell
<ovt> templates --plugin-dir .plugins\community-pack
<ovt> init-plan input.mp4 --template plugin-captioned --plugin-dir .plugins\community-pack --output edit.json --render-output final.mp4
<ovt> validate-plan --plan edit.json --plugin-dir .plugins\community-pack
```

## 关键文件与中间产物

常见文件包括：

- `probe.json`
  - 媒体探测结果
- `plan.json`
  - 传统转码计划
- `doctor.json`
  - 依赖状态
- `transcript.json`
  - 语音转写结果
- `beats.json`
  - 节拍信息
- `audio.json`
  - 响度分析
- `silence.json`
  - 停顿段
- `artifacts.json`
  - 模板 artifact slot 绑定
- `template-params.json`
  - 模板参数覆盖
- `edit.json`
  - 核心剪辑计划

## 什么时候用 preview

以下场景优先用 preview：

- 想先检查命令计划
- 想让外部 AI 先审查输出路径和 side effects
- 想先验证模板来源和执行语义是否正确

当前主要 preview 入口：

- `mix-audio --preview`
- `render --preview`

## 什么时候先跑 `doctor`

以下场景都建议先跑：

- 第一次在新机器使用
- `transcribe` 不工作
- `separate-audio` 不工作
- 不确定 `ffmpeg` / `ffprobe` / `demucs` / `whisper-cli` 路径是否已解析

## 常见排障

### `doctor` 里 required 依赖缺失

先安装或显式指定：

- `--ffmpeg <path>`
- `--ffprobe <path>`

### `transcribe` 失败

检查：

- `whisper-cli` 是否可执行
- `OVT_WHISPER_MODEL_PATH` 是否存在
- 模型路径是否与 CLI 参数一致

### `separate-audio` 失败

检查：

- `demucs` 是否可执行
- 输出目录是否可写

### `validate-plan` 报文件不存在

检查：

- `source.inputPath`
- `artifacts`
- `transcript.path`
- `beats.path`
- `subtitles.path`

## 补充说明

- 插件边界当前仍限定为：
  - 显式目录发现
  - 静态 manifest
  - 复用现有 template schema
- 仓库当前没有内置 AI provider。
- Desktop 仍未进入正式实施阶段；当前正式入口仍是 CLI。

## 相关文档

- [README.md](../README.md)
- [CLI_MVP.md](CLI_MVP.md)
- [external-dependencies.md](external-dependencies.md)
- [plugin-development-guide.md](plugin-development-guide.md)
- [roadmap.md](roadmap.md)
