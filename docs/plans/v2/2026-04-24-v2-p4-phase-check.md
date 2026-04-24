# V2-P4 阶段检查：timeline render baseline / v1 parity

最后更新：2026-04-24

## 目的

这份文档只回答一个问题：

> `V2-P4` 现在到底算不算已经完成当前最小 render baseline？

它不替代设计稿，也不把复杂 effect / plugin effect 提前塞进本阶段。它只把当前已经落地的 timeline render baseline 与 `V2-P4` 的最小目标逐条对照。

## 阶段定义回顾

`V2-P4` 的最小目标是：

- 在 `Core.Execution` 内部完成 v1 / v2 render 分发
- 让 `render --plan` 对 `schemaVersion = 2` 至少支持 preview 与执行失败路径
- 让 timeline builder 具备最小可运行的 filter graph 生成能力
- 守住 v1 parity，不把 v1 路径隐式替换掉

本阶段当前纳入范围：

- `FfmpegTimelineRenderCommandBuilder`
- `EditPlanExecutionPreviewBuilder` 的 v1/v2 builder 分发
- `EditPlanRenderRunner` 的 v1/v2 builder 分发
- CLI `render` 对 `schemaVersion = 2` 的装载与结构化 envelope

本阶段当前不纳入范围：

- 插件 effect 加载
- 复杂 executor effect 的正式运行保证
- 高级 timeline gap/layout 语义
- v2 template 正式输出

## 当前已落地能力

### Core.Execution

当前已落地：

- `FfmpegTimelineRenderCommandBuilder.Build(...)`
- timeline 输入收集与稳定 `-i` 顺序
- 单 / 多轨 `filter_complex` 基线：
  - trim / atrim
  - setpts / asetpts
  - built-in template effect filter chain
  - xfade / acrossfade
  - overlay
  - amix
- v1 / v2 builder 分发留在 `EditPlanExecutionPreviewBuilder` 与 `EditPlanRenderRunner`

当前已守住的边界：

- v1 builder 仍保留且继续使用
- CLI 只负责 plan load，不持有 render 分支规则
- 复杂 executor effect 当前不会被冒充成“已正式执行”

### CLI render

当前已落地：

- `render --plan` 已接受 `schemaVersion = 2`
- `render --preview` 已能返回 v2 `executionPreview.commandPlan`
- v2 render 的失败路径继续输出既有结构化 failure envelope

### parity / 回归

当前已落地：

- v1 render 现有测试继续通过
- 新增 v2 core tests：
  - timeline builder command plan
  - preview builder dispatch
  - render runner dispatch
- 新增 v2 CLI tests：
  - render preview v2
  - render failure envelope v2

## 与阶段目标对照

### 条件 1

> v1 / v2 render builder 必须并存，且分发留在 Core.Execution

当前判断：**满足**

### 条件 2

> render --preview 必须可直接消费 schema v2 plan

当前判断：**满足**

### 条件 3

> v2 render 失败路径不能破坏现有 failure envelope

当前判断：**满足**

### 条件 4

> 本阶段不能把插件 effect / 复杂 executor effect 一并宣称完成

当前判断：**满足**

## 当前验证结果

当前已完成：

- `dotnet build OpenVideoToolbox.sln`
- `dotnet test OpenVideoToolbox.sln`
- 手工运行一次 `render --preview` 的 v2 plan

当前最新全量结果为：

- `OpenVideoToolbox.Core.Tests`：157 通过
- `OpenVideoToolbox.Cli.Tests`：170 通过
- 总计：327 通过

## 当前结论

`V2-P4` 当前判断为：**已完成当前最小 render baseline，可进入下一阶段选择**

更准确地说：

- v2 render 已不是只有设计稿和骨架
- `render` 命令已经能在不破坏 v1 的前提下消费最小 schema v2 timeline
- 当前实现仍有明确边界，不把复杂 effect 执行假装成已经完成

因此下一步应进入 `V2-P5` 的用户可见能力选择，而不是继续在 `V2-P4` 内无限扩 builder 范围。
