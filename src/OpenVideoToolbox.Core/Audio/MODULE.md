# OpenVideoToolbox.Core.Audio

> 最后更新：2026-04-16

## 职责

本模块是音频基础分析结果模型与解析规则的 canonical owner，负责把外部工具日志转换成稳定的结构化 JSON 语义，供模板、CLI 和后续插件复用。

## Owns

- `audio.json` 相关结果模型
- 响度 / 分贝 / 阈值等音频分析字段语义
- 音频分析日志解析规则
- `silence.json` 相关结果模型
- 静音 / 停顿检测日志解析规则

## Must Not Own

- CLI 参数解析
- `ffmpeg` 命令拼接
- 外部进程执行与超时控制
- 模板专属启发式

## 关键依赖

- `OpenVideoToolbox.Core.Execution`
- `OpenVideoToolbox.Core.Serialization`

## 不变量

- 音频分析结果必须保持最小、结构化且机器可消费
- 外部工具调用仍由 `Core.Execution` 负责，本模块只持有解析与结果语义
- 解析规则必须可测试，不能依赖 UI 或人工读取 stderr
