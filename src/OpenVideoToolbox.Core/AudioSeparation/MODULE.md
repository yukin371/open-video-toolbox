# OpenVideoToolbox.Core.AudioSeparation

> 最后更新：2026-04-16

## 职责

本模块是外部分离工具适配与稳定 stem 结果映射的 canonical owner，负责把 Demucs 等外部工具的目录产物映射成仓库标准的音频分离结果语义。

## Owns

- 音频分离请求模型
- stem 结果模型
- Demucs 目录产物到结构化 JSON 的映射规则

## Must Not Own

- CLI 参数解析
- `demucs` 命令拼接细节
- 模板专属编排逻辑
- GUI 文件浏览或试听逻辑

## 关键依赖

- `OpenVideoToolbox.Core.Execution`

## 不变量

- 分离结果必须保持最小、结构化且机器可消费
- 外部工具调用仍由 `Core.Execution` 负责，本模块只持有结果语义与目录映射规则
- 第一版先覆盖双 stem 高频场景，不把 4-stem/6-stem 复杂度提前扩散到 CLI
