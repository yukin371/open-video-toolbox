# V2-P5 阶段检查：首个真正消费 timeline/effects 的模板示例

最后更新：2026-04-25

## 目的

这份文档只回答一个问题：

> `V2-P5` 当前是否已经交付了首个真正可用、可回归、可追踪的 v2 模板工作流？

这里说的“模板工作流”不是指文档里举一个 v2 JSON 例子，而是指同一个 built-in 模板已经能稳定贯通：

- `templates <id>`
- `init-plan --template <id>`
- `render --plan ... --preview`

并且这些入口都复用真实模板工厂与真实 v2 plan，而不是在 CLI 层再维护一份平行 skeleton。

当前阶段已继续补进第二个正式消费入口：

- `auto-cut-silence --template timeline-effects-starter`
- `render --plan ... --preview`

也就是首条“信号驱动生成 v2 plan”的正式路径。

当前阶段已继续补进同一模板下的第三类正式入口：

- `init-plan --template timeline-effects-starter --seed-from-transcript`
- `init-plan --template timeline-effects-starter --seed-from-beats`

也就是首条“模板 guide -> signal seed -> v2 timeline plan”的正式路径。

当前阶段已继续补进同阶段的第四类正式入口：

- `export --plan <edit.json> --format edl --output <path>`

也就是首条“v1/v2 统一导出到外部 NLE 互操作格式”的正式路径。

## 阶段定义回顾

`V2-P5` 当前选择的最小目标是：

- 增加首个显式 `v2` built-in 模板
- 为模板定义增加显式 `planModel`
- 让模板工厂对该模板真实产出 `schemaVersion = 2`
- 让模板 guide / preview / init-plan 使用同一份真实产物
- 保持 v1 模板链路不破坏

本阶段当前纳入范围：

- `EditPlanTemplateDefinition.PlanModel`
- `EditPlanTemplateSummary.PlanModel`
- `timeline-effects-starter` built-in 模板
- `EditPlanTemplateFactory` 的 v1 / v2 模板分流
- CLI `templates` / `init-plan` 对该模板的真实输出
- `auto-cut-silence` 对 built-in v2 模板的首个 planner 复用接线
- `export L1` 的 v1/v2 统一导出接线

本阶段当前不纳入范围：

- 插件模板的 v2 正式支持
- 新的 effect 类型扩面
- `V2-P6` 的数据驱动 batch 与 resolve-assets
- `premiere-xml` / `fcpxml` / effect 映射级导出

## 当前已落地能力

### Core.Editing

当前已落地：

- `EditPlanTemplatePlanModel`
  - `V1`
  - `V2Timeline`
- `EditPlanTemplateSummary` 已输出 `planModel`
- `BuiltInEditPlanTemplateCatalog` 已新增：
  - `timeline-effects-starter`
- `EditPlanTemplateFactory` 已按 `template.planModel` 分流：
  - v1 模板继续产出既有 v1 `EditPlan`
  - `timeline-effects-starter` 真实产出 v2 `EditPlan`
  - transcript / beats seed 规则现已可同时复用于 v1 `clips` 与 v2 `timeline` 路径
- `AutoCutSilencePlanner` 已按模板 planModel 分流：
  - 默认仍生成既有 v1 plan
  - built-in 且 `planModel = V2Timeline` 时，先生成 v2 baseline plan，再替换主视频轨 clips

### 首个 v2 模板当前产物

`timeline-effects-starter` 当前真实产出：

- `schemaVersion = 2`
- 顶层 `timeline`
- 主视频轨 `main`
  - track effect：`scale`
  - clip effect：`brightness_contrast`
  - manual 模式保留 starter transition：`fade`
  - transcript / beats seed 模式会把 seed 结果转换为真实 timeline clips，并保留基础 clip look
- 可选音频轨 `bgm`
  - clip effect：`volume`

当前已守住的边界：

- v1 模板默认仍为 `planModel = v1`
- v1 `clips/audioTracks/subtitles` 语义没有被强制改成 v2
- v2 模板没有反向要求 CLI 额外造一份假 preview

### CLI 可见结果

当前已落地：

- `templates timeline-effects-starter`
  - `template.planModel = "v2Timeline"`
  - `template.recommendedSeedModes = ["manual", "transcript", "beats"]`
  - `examples.previewPlans[].editPlan.schemaVersion = 2`
- `init-plan --template timeline-effects-starter`
  - 输出真实 v2 `edit.json`
  - `--seed-from-transcript` / `--seed-from-beats` 也会输出真实 v2 `timeline` clips
- `render --plan <v2-plan> --preview`
  - 可直接消费上一步生成的模板产物
- `auto-cut-silence --template timeline-effects-starter`
  - 可直接写出可被 `render --preview` 消费的 v2 plan
- `export --plan ... --format edl --output ...`
  - 可直接消费 v1 plan
  - 也可直接消费 v2 plan
  - warning / failure envelope 已落地

## 与阶段目标对照

### 条件 1

> 必须存在显式的模板级 v1 / v2 区分字段

当前判断：**满足**

说明：

- 已落 `template.planModel`
- 该字段由 `Core.Editing` 持有
- CLI 只透出，不猜测

### 条件 2

> `templates <id>` 的 preview 必须来自真实模板工厂

当前判断：**满足**

说明：

- `EditPlanTemplateExampleBuilder.BuildPreviewPlans(...)` 继续走 `EditPlanTemplateFactory.Create(...)`
- 没有新增 CLI 特判 skeleton

### 条件 3

> `init-plan --template <id>` 必须真正写出 v2 plan

当前判断：**满足**

说明：

- 新模板生成结果为 `schemaVersion = 2`
- 顶层 `clips` 保持空集合
- `timeline` 承载主语义

### 条件 4

> 新能力不能破坏现有 v1 模板与现有 v1 attach/bgm 语义

当前判断：**满足**

说明：

- v1 路径仍按既有分支生成
- `request.BgmPath` 在 v1 模板中仍保留直接接回 `AudioTracks` 的行为

### 条件 5

> 至少有一条非 `init-plan` 的正式入口，能真实消费 v2 模板并把结果交给 render preview

当前判断：**满足**

说明：

- `auto-cut-silence --template timeline-effects-starter` 已真实产出 `schemaVersion = 2`
- planner 没有在 CLI 层另造 timeline skeleton
- `render --plan ... --preview` 已能直接消费该结果

### 条件 6

> `init-plan` 现有 transcript / beats seed 规则必须能复用到 v2 模板，而不是只剩 manual demo skeleton

当前判断：**满足**

说明：

- `timeline-effects-starter` 现已公开 `manual / transcript / beats` 三种 seed 模式
- seed clip 生成仍由 `Core.Editing` 统一拥有
- v2 路径只把同一批 seed 结果转换成 `timeline` clips，没有在 CLI 层复制一套规则

### 条件 7

> 至少要有一条不依赖 render 的正式能力，证明 v2 plan 已可被第二个长期 owner 消费

当前判断：**满足**

说明：

- `export L1` 已落地到 `Core.Execution`
- `Cli` 只新增 `export` 命令入口，不持有第二套导出骨架
- `v1` 与 `v2` 已能通过同一条 `Core.Execution` 路径统一导出为 `EDL`

## 当前验证结果

本阶段当前已完成：

- 定向 Core tests
- 定向 CLI tests
- `dotnet build OpenVideoToolbox.sln`
- `dotnet test OpenVideoToolbox.sln`

当前最新全量结果为：

- `OpenVideoToolbox.Core.Tests`：164 通过
- `OpenVideoToolbox.Cli.Tests`：179 通过
- 总计：343 通过

本阶段已于 `2026-04-25` 继续完成一次按清单逐项执行的人工阶段验收，当前结果为：

- `templates timeline-effects-starter`
  - 返回真实 `planModel = "v2Timeline"`，且 preview plans 全为 `schemaVersion = 2`
- `init-plan --template timeline-effects-starter`
  - 成功写出真实 v2 plan，并保留 `main / bgm` 两条轨道
- `init-plan --seed-from-transcript`
  - 成功写出真实 v2 timeline clips，首个 clip effect 为 `brightness_contrast`
- `render --plan ... --preview`
  - 能直接消费模板生成的 v2 plan 与 signal-driven v2 plan
- `auto-cut-silence --template timeline-effects-starter`
  - 成功产出可继续 preview / export 的 v2 plan
- `export --format edl`
  - 已确认可同时消费 v1 / v2 plan
  - warning / overwrite failure 契约已在真实 CLI 输出中验证

## 当前结论

`V2-P5` 当前判断为：**已完成当前最小模板工作流交付、首个信号驱动 v2 planner 入口和 `export L1`，并已通过人工阶段验收**

更准确地说：

- v2 模板不再只是设计概念
- 首个 built-in v2 模板已能从发现、生成到 preview 闭环跑通
- transcript / beats 的既有 seed 规则已能在同一模板路径下直接产出 v2 timeline clips
- `auto-cut-silence` 已能在显式 v2 模板下复用同一条真实 plan 生成路径
- `export L1` 已能以同一条 `Core.Execution` 路径同时消费 v1 / v2 `edit.json`
- `template.planModel` 已把 v1 / v2 模板边界固定成显式契约
- 当前仍保持了“只做一个最小正式样例 + 一个最小互操作出口”的范围控制，没有把更多 v2 能力一起混进来

因此下一步不应继续在 `V2-P5` 内无边界追加新能力；`V2-P5` 应视为已完成并关闭，后续如要继续推进，应单独决定是补 parity / 迁移文档，还是进入下一阶段。

## 手动验收入口

本阶段已补可直接执行的人工验收清单：

- [2026-04-24-v2-p5-acceptance-checklist.md](./2026-04-24-v2-p5-acceptance-checklist.md)

该清单当前覆盖：

1. `templates -> init-plan -> render --preview` 的首个 built-in v2 模板闭环
2. `init-plan --seed-from-transcript/--seed-from-beats` 的 v2 seed 闭环
3. `auto-cut-silence --template timeline-effects-starter` 的信号驱动 v2 plan 闭环
4. `export L1` 对 v1 / v2 plan 的统一导出闭环
5. `export` 的 `--json-out` / overwrite failure 契约

## 本轮阶段检查输出

```text
阶段：V2-P5
阶段目标是否完成：已完成当前最小 v2 模板工作流、signal-driven planner 与 export L1，并已通过人工阶段验收
本阶段范围是否清楚：是，仅包含首个 built-in v2 模板、其 seed/planner 接线和 export L1
当前 owner 是否保持单一：是，模板语义仍由 Core.Editing 持有，render / export 仍由 Core.Execution 持有
是否出现第二套 CLI 骨架：否
当前验证是否充分：是，已完成全量 build/test，且已完成手动验收清单逐项执行
是否应继续在本阶段追加实现：否，本阶段应关闭并等待下一阶段决策
如果现在停止，仓库是否仍处于一致状态：是
```
