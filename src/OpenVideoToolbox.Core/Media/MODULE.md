# OpenVideoToolbox.Core.Media

> 最后更新：2026-04-15

## 职责

本模块是媒体探测链路的 canonical owner，负责调用 `ffprobe`、解析其 JSON 输出，并生成仓库内部统一使用的 `MediaProbeResult`。

## Owns

- `MediaProbeResult` 及相关流信息模型
- `ffprobe` 调用参数约定
- 探测失败时的错误上下文拼装
- `ffprobe` JSON 到领域模型的映射

## Must Not Own

- 转码命令构建
- 预设选择与输出路径策略
- CLI / Desktop 的展示格式和交互行为
- 直接决定任务是否执行

## 关键依赖

- `OpenVideoToolbox.Core.Execution`
- `FfprobeJsonParser`
- 外部 `ffprobe` 二进制

## 不变量

- 探测成功时只解析标准输出中的 JSON，不把标准错误混入结果
- 探测失败时要尽量保留标准错误上下文，不能只返回空泛失败消息
- `MediaProbeResult` 作为快照可挂到 `JobDefinition` 上，供后续规划或执行使用
- 默认超时由模块内部提供，避免入口层遗漏

## 常见坑

- 在 `Cli` 或未来 `Desktop` 中重复拼接 `ffprobe` 参数，会造成行为分叉
- 直接消费外部工具原始 JSON 会让上层耦合到第三方输出格式
- 不同媒体文件的字段缺失很常见，解析器必须允许空值而不是假定完整字段

## 文档同步触发条件

- `MediaProbeResult` schema 变化
- `ffprobe` 调用参数、默认超时或错误映射规则变化
- 新增额外媒体探测后端或缓存策略
