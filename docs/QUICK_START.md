# 快速开始

最后更新：2026-04-22

本文档只保留最短上手路径。

如果你想看完整功能、命令分组、工作流和排障，请直接看 [FEATURES_AND_USAGE.md](FEATURES_AND_USAGE.md)。

## 0. 选择运行方式

下文统一用 `<ovt>` 代表 CLI 可执行入口。你可以替换成：

```powershell
# 从源码运行
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj --

# Windows Release 二进制
.\ovt-win-x64.exe
```

如果你直接使用 GitHub Release 上的其他平台二进制，也把 `<ovt>` 替换成对应可执行文件路径即可。

## 1. 安装必需依赖

先确保机器上能找到：

- `ffmpeg`
- `ffprobe`

安装细节见 [external-dependencies.md](external-dependencies.md)。

## 2. 跑依赖预检

```powershell
<ovt> doctor --json-out doctor.json
```

如果 `ffmpeg` / `ffprobe` 缺失，先修环境，再继续。

## 3. 看看有哪些模板

```powershell
<ovt> templates --summary
```

如果你已经知道模板 id，也可以直接看单模板指南：

```powershell
<ovt> templates shorts-captioned
```

## 4. 生成初始 `edit.json`

```powershell
<ovt> init-plan input.mp4 --template shorts-captioned --output edit.json --render-output final.mp4
```

## 5. 校验计划

```powershell
<ovt> validate-plan --plan edit.json --check-files
```

## 6. 先看预览，再执行

预览：

```powershell
<ovt> render --plan edit.json --output final.mp4 --preview --json-out render-preview.json
```

执行：

```powershell
<ovt> render --plan edit.json --output final.mp4
```

## 常见扩展

### 字幕链路

```powershell
<ovt> transcribe input.mp4 --model ggml-base.bin --output transcript.json --json-out transcribe.json
<ovt> subtitle input.mp4 --transcript transcript.json --format srt --output subtitles.srt --json-out subtitle.json
```

### 节拍辅助

```powershell
<ovt> beat-track input.mp4 --output beats.json --json-out beat-track.json
<ovt> init-plan input.mp4 --template shorts-captioned --output edit.json --render-output final.mp4 --beats beats.json --seed-from-beats --beat-group-size 2
```

### 插件模板

```powershell
<ovt> templates --plugin-dir .plugins\community-pack
<ovt> init-plan input.mp4 --template plugin-captioned --plugin-dir .plugins\community-pack --output edit.json --render-output final.mp4
```

## 下一步读什么

- 完整功能与使用：[FEATURES_AND_USAGE.md](FEATURES_AND_USAGE.md)
- 命令边界与中间产物设计：[CLI_MVP.md](CLI_MVP.md)
- 外部依赖安装：[external-dependencies.md](external-dependencies.md)
