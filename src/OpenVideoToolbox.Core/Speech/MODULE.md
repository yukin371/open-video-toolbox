# OpenVideoToolbox.Core.Speech

> 最后更新：2026-04-16

## 职责

本模块是语音转写工具适配与结构化 transcript 映射的 canonical owner，负责把外部转写工具的原始输出转换成仓库标准 `transcript.json` 语义。

## Owns

- 转写请求模型
- `whisper.cpp` JSON 到 `TranscriptDocument` 的映射规则
- 转写流程编排

## Must Not Own

- CLI 参数解析
- transcript schema 本身
- `ffmpeg` / `whisper-cli` 命令拼接细节
- UI 交互或模型下载管理

## 关键依赖

- `OpenVideoToolbox.Core.Subtitles`
- `OpenVideoToolbox.Core.Execution`

## 不变量

- `transcript.json` 仍由 `Core.Subtitles` 定义，本模块只做外部结果映射
- 外部工具调用与日志采集仍由 `Core.Execution` 负责
- 转写流程必须保持确定性、可测试，并明确依赖缺失时的错误路径
