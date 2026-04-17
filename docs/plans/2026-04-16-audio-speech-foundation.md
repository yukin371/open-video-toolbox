# Audio / Speech Foundation Plan

日期：2026-04-16

## 目标

把模板平台最缺的基础能力收敛成一批可实施、可验证、可复用的 CLI 命令，为后续模板扩展和插件化奠定稳定地基。

本计划优先解决：

- 音量检测与分贝识别
- 分贝调整 / 增益控制
- 音频转 transcript
- 静音 / 停顿检测
- 人声 / 背景音分离

## 适用范围

本计划只覆盖基础能力层，不直接定义新的复杂模板，也不引入 GUI 或仓库内 AI provider。

## 复用原则

- 新功能默认先找已有开源实现，再决定是否补自研。
- 优先复用现有工具、库或成熟 CLI，而不是在仓库里重复开发音频算法或语音模型。
- 首选“外部工具适配层 + 稳定结构化 JSON 输出”的方式接入能力。
- 只有在开源实现无法满足确定性、可审计或可测试要求时，才考虑自研补充。

## 能力拆分

### 1. `audio-analyze`

目标：

- 输出可被模板和 AI 直接消费的音量/响度 JSON。

推荐实现：

- 首选：`FFmpeg`
  - `volumedetect`
  - `loudnorm`
  - 可选补充：`ebur128`

原因：

- 当前仓库已依赖 `ffmpeg` / `ffprobe` 生态。
- 集成成本最低。
- 输出确定性强，便于做快照测试和日志采集。

命令草案：

```text
ovt audio-analyze <input> --output audio.json
```

建议输出：

```json
{
  "schemaVersion": 1,
  "inputPath": "input.mp4",
  "analysis": {
    "meanVolumeDb": -18.2,
    "maxVolumeDb": -1.1,
    "integratedLoudness": -16.4,
    "loudnessRange": 5.8,
    "truePeakDb": -0.9
  }
}
```

owner：

- 检测模型与 JSON：`OpenVideoToolbox.Core/Audio`
- 外部执行与日志：`OpenVideoToolbox.Core/Execution`
- 参数解析与输出：`OpenVideoToolbox.Cli`

验证：

- 参数解析测试
- 日志解析单测
- JSON 契约测试

### 2. `audio-gain`

目标：

- 提供最简单、最可解释的增益控制能力。

推荐实现：

- 第一阶段：`FFmpeg volume`
- 第二阶段：`FFmpeg loudnorm`

原因：

- `volume` 适合作为显式增益原语。
- `loudnorm` 适合作为后续归一化模式，但不应和显式增益混成一个模糊命令。

命令草案：

```text
ovt audio-gain <input> --gain-db <n> --output <path>
```

后续扩展：

```text
ovt audio-gain <input> --normalize-lufs -16 --output <path>
```

owner：

- 请求模型与命令构建：`OpenVideoToolbox.Core/Audio`
- 执行：`OpenVideoToolbox.Core/Execution`
- CLI：`OpenVideoToolbox.Cli`

验证：

- 命令计划测试
- 参数解析测试
- 输出文件路径与 overwrite 行为测试

### 3. `transcribe`

目标：

- 把媒体或音频输入转换成仓库标准 `transcript.json`。

推荐实现：

- 主推荐：`whisper.cpp`
- 备选验证：`openai/whisper`
- 暂不作为首选：`faster-whisper`

原因：

- `whisper.cpp` 本地 CLI 集成友好，更符合当前仓库定位。
- 运行时边界清晰，适合做确定性外部工具适配。
- `openai/whisper` 可用作精度对比或早期验证，但 Python/PyTorch 依赖较重。

命令草案：

```text
ovt transcribe <input> --output transcript.json [--model <path>] [--language <id>] [--vad [true|false]]
```

建议输出：

```json
{
  "schemaVersion": 1,
  "language": "zh-CN",
  "segments": [
    {
      "id": "seg-001",
      "start": "00:00:00.5000000",
      "end": "00:00:02.0000000",
      "text": "示例字幕文本"
    }
  ]
}
```

owner：

- transcript schema：`OpenVideoToolbox.Core/Subtitles`
- 外部工具适配：建议新增 `OpenVideoToolbox.Core/Speech`
- CLI：`OpenVideoToolbox.Cli`

验证：

- transcript schema 测试
- CLI 输出契约测试
- 外部工具缺失 / 模型缺失错误路径测试

### 4. `detect-silence`

目标：

- 输出停顿段 JSON，供模板和 AI 做辅助切分。

推荐实现：

- `FFmpeg silencedetect`

原因：

- 最简单、最稳定、无额外模型依赖。
- 适合先做成确定性辅助信号，而不是“自动剪辑”。

命令草案：

```text
ovt detect-silence <input> --output silence.json [--noise-db <n>] [--min-duration-ms <n>]
```

建议输出：

```json
{
  "schemaVersion": 1,
  "inputPath": "input.mp4",
  "segments": [
    {
      "start": "00:00:04.2000000",
      "end": "00:00:05.1000000",
      "duration": "00:00:00.9000000"
    }
  ]
}
```

owner：

- 检测模型：建议新增 `OpenVideoToolbox.Core/Audio`
- 执行：`OpenVideoToolbox.Core/Execution`
- CLI：`OpenVideoToolbox.Cli`

验证：

- 日志解析测试
- JSON 契约测试

### 5. `separate-audio`

目标：

- 为模板和用户提供人声 / 伴奏分离结果。

推荐实现：

- 主推荐：`Demucs`
- 备选：`Spleeter`

原因：

- `Demucs` 质量路线更值得优先验证。
- `Spleeter` 可作为轻量备选，但不应抢在 `Demucs` 前成为默认方案。

命令草案：

```text
ovt separate-audio <input> --output-dir stems/ [--model <id>]
```

建议输出：

```json
{
  "inputPath": "input.mp4",
  "stems": {
    "vocals": "stems/vocals.wav",
    "accompaniment": "stems/accompaniment.wav"
  }
}
```

owner：

- 外部能力适配：建议新增 `OpenVideoToolbox.Core/AudioSeparation`
- 执行：`OpenVideoToolbox.Core/Execution`
- CLI：`OpenVideoToolbox.Cli`

验证：

- 先做工具存在性 / 参数路径测试
- 后续再补真实素材 smoke

## 推荐实施顺序

### Wave A

1. `audio-analyze`
2. `audio-gain`

原因：

- 依赖最少
- 能快速产出模板可复用基础信号
- 最容易稳定 JSON 契约

### Wave B

1. `transcribe`
2. 收敛 `transcribe -> subtitle -> render`

原因：

- 这是字幕模板和口播模板的核心地基
- 比继续扩模板数量更有价值

### Wave C

1. `detect-silence`
2. transcript / silence 联动的编辑辅助

原因：

- 静音检测适合作为辅助信号，不应抢在转写前

### Wave D

1. `separate-audio`

原因：

- 运行时重、依赖复杂、分发成本高
- 更适合在前几批基础能力稳定后接入

## 模块落点建议

- `OpenVideoToolbox.Core/Audio`
  - 音量分析、增益控制、静音检测模型与解析
- `OpenVideoToolbox.Core/Speech`
  - 转写工具适配、transcript 生成协调
- `OpenVideoToolbox.Core/AudioSeparation`
  - 分离工具适配
- `OpenVideoToolbox.Core/Execution`
  - 外部进程执行、日志采集、命令计划
- `OpenVideoToolbox.Cli`
  - 参数解析、错误输出、JSON 输出契约

说明：

- 这些模块名是建议 owner，不代表必须一次性全部建立。
- 如短期内只落 `audio-analyze` / `audio-gain`，可先建 `Core/Audio`，其余在真正接入时再创建。

## 外部依赖管理原则

- 必须记录：
  - 工具名
  - 推荐安装方式
  - 最低版本
  - CLI 调用方式
  - 缺失依赖时的错误输出
- 不在当前仓库内捆绑不可替代的闭源运行时。
- 外部工具集成必须保留完整 stderr / stdout 日志，便于调试和 AI 审计。

## 测试策略

每个新增命令至少补：

- 1 个参数解析测试
- 1 个输出契约测试
- 1 个错误路径测试

优先补：

- 命令计划 / 日志解析测试
- JSON 快照测试

后续补：

- 真实工具 smoke
- 与模板 / `edit.json` 的联动测试

## 验收标准

- `audio-analyze` / `audio-gain` / `transcribe` 至少 3 个能力先落地
- 每个命令都有稳定 JSON 输出
- 至少一条闭环成立：
  - `transcribe -> subtitle -> render`
  - `audio-analyze -> template parameter / gain adjustment`
- 新能力能被模板系统复用，而不是只在 CLI 层各自独立存在

## 不在本计划内

- 内置 AI provider
- 复杂 DAW 级音频后期
- 完整时间线编辑器
- 高级调色、关键帧、专业特效系统
