# OpenVideoToolbox.Core.Subtitles

> 最后更新：2026-04-16

## 职责

本模块是 `transcript.json` 与确定性字幕导出的 canonical owner，负责承接外部 AI、`whisper.cpp` 适配层或其他工具生成的 transcript，并渲染为 `srt` / `ass`。

## Owns

- `TranscriptDocument`
- `TranscriptSegment`
- `SubtitleRenderRequest`
- `SubtitleRenderResult`
- `SubtitleRenderer`

## Must Not Own

- CLI 参数解析
- `ffmpeg` 烧录执行
- GUI 样式编辑状态
- 转录推理或任何供应商集成
- 外部转写工具 JSON 到 transcript 的映射规则

## 关键依赖

- `OpenVideoToolbox.Core.Serialization`

## 不变量

- `transcript.json` 必须保持足够简单，方便外部 AI 直接生成
- 字幕导出必须是确定性的，不依赖机器本地状态
- 烧录仍由 `render` / `ffmpeg` 链承接，本模块只负责生成 sidecar 文本

## 常见坑

- 把复杂时间线或样式编辑状态塞进 transcript schema
- 让 CLI 或 UI 复制字幕格式化逻辑

## 文档同步触发条件

- `transcript.json` schema 变化
- 新增字幕格式或文本规范化规则变化
