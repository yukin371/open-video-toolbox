# V2-P2 阶段检查：schema v2 合约层

最后更新：2026-04-24

## 目的

这份文档只回答一个问题：

> `V2-P2` 现在到底算不算可以进入阶段验收？

它不新增功能范围，也不替代设计稿。它只把当前已经落地的 `schema v2` 合约层，与 [2026-04-24-v1-v2-boundary-and-phased-delivery-plan.md](./2026-04-24-v1-v2-boundary-and-phased-delivery-plan.md) 中定义的 `V2-P2` 目标逐条对照，避免把“类型骨架存在”误判成“阶段完成”。

## 阶段定义回顾

`V2-P2` 的目标是：

- 只落 `EditPlan` 的 v2 合约层和 validator 双轨支持
- 让 `schemaVersion = 2` 的 `edit.json` 可以被稳定解析、序列化和校验
- 不进入 render 主路径，不把 timeline 正式接到执行链

本阶段当前纳入范围：

- `SchemaVersions.V2`
- `EditPlan.Timeline`
- `EditPlanTimeline`、`TimelineTrack`、`TimelineClip`、`TimelineResolution`、`Transition` 等类型
- `EditPlanValidator` 的 v2 timeline 校验
- CLI `validate-plan` 对 v2 plan 的装载与校验输出

本阶段当前不纳入范围：

- v2 render builder 正式接线
- effect discovery / list / describe
- 插件 effect registry 发现流程
- `render` / `mix-audio` 的 v2 分发

## 当前已落地能力

### 合约层

当前已落地：

- `SchemaVersions.V2 = 2`
- `EditPlan` 新增可选 `Timeline`
- `EditPlanTimeline`
- `TimelineTrack`
- `TimelineClip`
- `TimelineResolution`
- `ClipTransitions`
- `Transition`
- `TimelineEffect`

当前已守住的边界：

- v1 顶层字段仍保留
- `timeline` 仍只是结构化编辑意图，不是 UI 状态
- 本阶段没有把 `timeline` 自动接进 render 主路径

### validator

当前已落地：

- `schemaVersion` 受支持版本检查
- `schemaVersion = 2` 时 `timeline` 必填
- `timeline` 存在但 `schemaVersion != 2` 时报错
- timeline 分辨率 / 帧率校验
- track id 唯一性校验
- clip id 全局唯一性校验
- video clip 的 `in/out` 必填与范围校验
- clip `start` / `duration` 校验
- transition duration 校验
- `auto_ducking.reference` 指向已存在 track id 的校验
- 可选 effect registry 下的未知 effect warning

### CLI 校验入口

当前已落地：

- `validate-plan` 现已接受 `schemaVersion = 2` 的 `edit.json`
- 对 v2 plan 不再在装载阶段报 `Unsupported edit plan schema version '2'`
- 结构化输出继续沿用现有 envelope

## 与阶段验收条件对照

### 条件 1

> v2 合约层必须能被解析和序列化

当前判断：**满足**

证据：

- `V2TimelineSkeletonTests` 已覆盖 timeline round-trip
- `schemaVersion = 2` 与 `timeline` 已能稳定出入 JSON

### 条件 2

> v2 validator 必须落地，且不影响现有 v1 校验链

当前判断：**满足**

证据：

- `EditPlanValidator` 已新增 v2 timeline 规则
- 现有 `EditPlanValidatorTests` 与全量测试继续通过
- v1 plan 未被强制要求 `timeline`

### 条件 3

> CLI 至少要有一条可手测的 v2 合约验证路径

当前判断：**满足**

证据：

- `validate-plan` 已能直接消费 v2 plan
- 新增 CLI 集成测试已覆盖：
  - 合法 v2 plan
  - 非法 v2 timeline 结构错误

### 条件 4

> 本阶段不能顺手进入 render / effects 的正式实现

当前判断：**满足**

证据：

- 当前阶段验收只围绕合约、validator 与 `validate-plan`
- 未把 v2 render builder 接到 `render` / `mix-audio`
- effect registry 仍未变成正式 CLI 能力

## 当前不纳入本阶段验收的内容

以下内容当前明确不属于 `V2-P2` 本轮阶段验收：

- `FfmpegTimelineRenderCommandBuilder` 的正式接线
- `IEffectDefinition` / `EffectRegistry` 的对外发现命令
- v2 render parity
- v2 模板正式输出

这样做的原因是：

1. `V2-P2` 只负责合约层与 validator
2. 如果现在把 render/effects 一并拉进来，会再次跨到 `V2-P3/P4`

## 当前验证结果

当前已完成：

- `dotnet build OpenVideoToolbox.sln`
- `dotnet test OpenVideoToolbox.sln`

当前最新全量结果为：

- `OpenVideoToolbox.Core.Tests`：150 通过
- `OpenVideoToolbox.Cli.Tests`：163 通过
- 总计：313 通过

## 当前结论

`V2-P2` 当前判断为：**本阶段已达到阶段验收输入条件，可进入人工验收**

更准确地说：

- `schema v2` 现在已经不是只存在于设计文档中的结构
- `validate-plan` 已提供最小可手测入口
- 当前代码、测试与文档边界仍保持在“合约层，不进 render”

因此下一步不应继续在 `V2-P2` 里无边界扩更多执行能力，而应先做阶段验收决定。

## 手动验收入口

本阶段已补可直接执行的人工验收清单：

- [2026-04-24-v2-p2-acceptance-checklist.md](./2026-04-24-v2-p2-acceptance-checklist.md)

该清单当前覆盖：

1. 合法 v2 plan 的 `validate-plan`
2. 非法 v2 timeline 结构错误的 `validate-plan`

## 本轮阶段检查输出

```text
阶段：V2-P2
阶段目标是否完成：已完成当前合约层范围，达到阶段验收输入条件
本阶段范围是否清楚：是，仅包含 schema v2 类型、validator 与 validate-plan 装载兼容
当前 owner 是否保持单一：是，仍由 Core.Editing 持有
是否出现第二套 render 或执行语义：否
当前验证是否充分：是，已完成全量 build/test
是否应继续在本阶段追加实现：否，应先进入阶段验收
如果现在停止，仓库是否仍处于一致状态：是
```
