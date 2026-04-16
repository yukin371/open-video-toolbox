# OpenVideoToolbox.Core.Beats

> 最后更新：2026-04-15

## 职责

本模块是 `beats.json` 节拍分析结果的 canonical owner，负责把统一 PCM 波形分析成可复现的 BPM 估计和节拍时间点。

## Owns

- `BeatTrackDocument`
- `BeatMarker`
- `WavePcmReader`
- `BeatTrackAnalyzer`

## Must Not Own

- CLI 参数解析
- `ffmpeg` 解码调用
- GUI 节奏标注状态
- AI provider 或在线分析服务

## 关键依赖

- `OpenVideoToolbox.Core.Serialization`

## 不变量

- `beats.json` 必须保持最小且机器可消费
- 音频解码仍由 `Core.Execution` 负责，本模块只分析统一 PCM 输入
- 同一输入波形必须得到稳定输出

## 常见坑

- 把原始 ffmpeg 调用散到本模块
- 为了“智能”引入不可复现的启发式随机性

## 文档同步触发条件

- `beats.json` schema 变化
- 节拍检测或 BPM 估计规则有实质变化
