# OpenVideoToolbox.Cli

> 最后更新：2026-04-15

## 职责

CLI 是脚本化和调试入口，负责把命令行参数映射成对 `Core` 的调用，并把结果输出为 JSON 或退出码。它不拥有核心业务规则。

## Owns

- 命令分发：`presets`、`probe`、`plan`、`run`
- 参数解析、默认值选择、帮助输出
- CLI 级错误提示和退出码语义

## Must Not Own

- `ffmpeg` / `ffprobe` 命令拼接规则
- 外部进程执行、超时、取消和输出采集
- 媒体探测解析逻辑
- 预设语义和内置预设定义

## 关键依赖

- `OpenVideoToolbox.Core.Media`
- `OpenVideoToolbox.Core.Presets`
- `OpenVideoToolbox.Core.Execution`
- `OpenVideoToolbox.Core.Serialization`

## 不变量

- CLI 只组合 `Core` 能力，不复制核心逻辑
- `plan` 只能生成 `JobDefinition` 和 `CommandPlan` 预览，不直接执行任务
- `run` 先探测再执行，并把结果序列化输出

## 常见坑

- 新增 CLI 选项时，容易把默认值、路径规则或命令拼接塞回入口层
- `Program.cs` 当前是手写参数解析，改命令面时要同时验证帮助输出和错误消息

## 文档同步触发条件

- 新增或删除 CLI 命令
- CLI 参数约定、退出码语义或帮助输出结构变化
- CLI 不再只是 `Core` 的薄入口时
