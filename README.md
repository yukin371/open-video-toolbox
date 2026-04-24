# Open Video Toolbox

`Open Video Toolbox` 是一个面向个人创作者、脚本工作流和外部 AI 代理的 CLI 视频工具箱。

如果你想把常见的视频处理工作做成可复用、可审计、可批量运行的命令流程，而不是反复手工点 GUI，这个仓库就是为这类场景准备的。

它不打算取代专业 NLE，也不会把 AI SDK 直接塞进软件里。它更适合做这些事：

- 快速探测媒体信息
- 生成可修改的 `edit.json` 草稿
- 用模板批量产出固定风格的视频
- 把转写、字幕、节拍、静音、音频分离接进统一流程
- 让 Claude、Codex 或你自己的脚本稳定调用同一套 CLI

## 适合谁

- 想把视频处理流程脚本化的个人创作者
- 想把 `ffmpeg` 工作流变得更稳定、可维护的人
- 想让外部 AI 通过确定性命令而不是黑盒逻辑来协作的人
- 想先产出草稿，再把精修留给外部编辑器的人

## 功能矩阵

| 场景 | 命令 | 你会得到什么 | 当前状态 |
| --- | --- | --- | --- |
| 媒体探测 | `probe` | 规范化媒体信息 JSON | 已可用 |
| 快速计划 / 执行 | `plan` / `run` | 命令预览或直接执行 | 已可用 |
| 模板筛选 | `templates` | 模板列表、过滤结果、单模板指南 | 已可用 |
| 草稿生成 | `init-plan` / `scaffold-template` | 可编辑的 `edit.json` 与工作目录 | 已可用 |
| 计划校验 | `validate-plan` | 对 AI 或手改后的计划做结构化校验 | 已可用 |
| 成片渲染 | `render` | 最终视频或预览执行计划 | 已可用 |
| 独立混音 | `mix-audio` | 单独导出混音结果 | 已可用 |
| 裁切 / 拼接 / 提音轨 | `cut` / `concat` / `extract-audio` | 基础媒体处理产物 | 已可用 |
| 响度处理 | `audio-analyze` / `audio-gain` / `audio-normalize` | 分析、增益、归一化 | 已可用 |
| 节拍 / 静音信号 | `beat-track` / `detect-silence` | `beats.json` / `silence.json` | 已可用 |
| 转写 / 字幕 | `transcribe` / `subtitle` | `transcript.json`、`srt`、`ass` | 已可用 |
| 音频分离 | `separate-audio` | 人声 / 伴奏 stem 输出 | 已可用 |
| 插件模板 | `validate-plugin`、`templates --plugin-dir` | 静态模板插件发现与校验 | 已可用 |
| Desktop 交互壳 | `OpenVideoToolbox.Desktop` | 图形界面入口 | 规划中 |

## 快速开始

### 1. 准备环境

至少需要这些依赖：

- `.NET 8 SDK`
- `ffmpeg`
- `ffprobe`

可选依赖：

- `whisper-cli` 和 Whisper 模型，用于 `transcribe`
- `demucs`，用于 `separate-audio`

先跑一次依赖检查最省事：

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- doctor
```

如果你已经下载了 release 里的单文件程序，也可以直接这样用：

```powershell
ovt doctor
```

### 2. 看看现在有哪些模板

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- templates --summary
```

### 3. 生成一个可编辑草稿

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- scaffold-template .\input.mp4 --template shorts-captioned --dir .\.workspace --validate
```

这条命令会把模板指南、示例文件和初始 `edit.json` 一起写进 `.\.workspace`，你可以继续手改，也可以交给外部 AI 继续编排。

### 4. 渲染输出

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- render --plan .\.workspace\edit.json --output .\final.mp4
```

## 常见工作流

### 先探测，再决定怎么做

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- probe .\input.mp4 --json-out .\probe.json
```

适合想先看轨道、时长、编码参数，再决定走模板还是直接处理的人。

### 用模板生成短视频草稿

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- init-plan .\input.mp4 --template shorts-captioned --output .\edit.json --render-output .\final.mp4
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- validate-plan --plan .\edit.json --check-files
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- render --plan .\edit.json --output .\final.mp4
```

适合先拿到一个能改的草稿，再逐步细修的场景。

### 给现有素材补转写和字幕

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- transcribe .\input.mp4 --model .\ggml-base.bin --output .\transcript.json
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- subtitle .\input.mp4 --transcript .\transcript.json --format srt --output .\subtitles.srt
```

适合先拿到 `transcript.json` 和字幕文件，再接模板、外挂字幕或烧录流程。

### 只做基础媒体处理

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- cut .\input.mp4 --from 00:00:12.000 --to 00:00:27.500 --output .\clip-01.mp4
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- extract-audio .\input.mp4 --track 0 --output .\voice.m4a
```

适合只想把高频处理动作做成稳定命令的人。

## 输出风格

这个仓库的 CLI 不是只给人眼看。很多命令都支持：

- 稳定的结构化 JSON 输出
- `--json-out` 文件写出
- 失败路径也尽量返回结构化结果，而不是只吐 usage 文本

这让它更适合作为脚本、CI 或外部 AI 的工作流节点。

## 推荐阅读顺序

- 想最快跑通：`docs/QUICK_START.md`
- 想了解功能和排障：`docs/FEATURES_AND_USAGE.md`
- 想查精确命令签名：`docs/COMMAND_REFERENCE.md`
- 想了解插件模板：`docs/plugin-development-guide.md`
- 想了解外部依赖：`docs/external-dependencies.md`

## 仓库协作

如果你要在本仓库里提交代码，先启用本地 Git hooks：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Enable-GitHooks.ps1
```

验证命令：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Enable-GitHooks.ps1 -VerifyOnly
```

说明：

- 仓库内 hook 脚本位于 `.githooks/`
- 这一步会把本地仓库的 `core.hooksPath` 指向 `.githooks`
- 如果不做这一步，`pre-commit`、`commit-msg` 与 `pre-push` 检查不会生效
- `pre-push` 会校验待推送 commit；如果本机已安装 `gh` 且当前分支已有打开中的 PR，还会额外校验 PR 标题与描述是否满足模板要求

## 当前定位

当前仓库的重心仍然是 CLI 能力，而不是桌面 GUI。你可以把它理解成：

- 一个适合外部 AI 和脚本调用的视频生产内核
- 一个更关注“草稿生成、模板化产出、可审计流程”的工具箱
- 一个愿意把复杂精修继续留给外部编辑器的工作流组件
