# V2-P6 阶段检查：narrated-slides 首轮实现

最后更新：2026-04-25

## 目的

这份文档只回答一个问题：

> `V2-P6` 当前选定的 narrated-slides 主题，是否已经完成第一轮可回归、可验收、边界清晰的实现？

这里说的“第一轮实现”不是把讲解型视频能力整个做完，而是确认首版已稳定交付：

- 独立 manifest contract
- `Core.Editing` 持有的 narrated plan 投影
- 独立 CLI 入口 `init-narrated-plan`
- 可直接被现有 `render --preview` 消费的 v2 `edit.json`

## 当前纳入范围

本轮只纳入：

- narrated manifest 模型
- `NarratedSlidesPlanBuilder`
- `init-narrated-plan`
- `render --preview` 对首版 narrated v2 plan 的复用验收
- 对应 Core / CLI 测试与文档同步

本轮明确不纳入：

- `templates` catalog 集成
- `init-plan <input>` 复用
- 图片页、标题卡、图表或 `.pptx`
- `${var}`、slot 条件裁剪、数据驱动 batch
- AI provider、TTS provider、Remotion runtime

## 当前已落地能力

### Core.Editing

当前已新增：

- `NarratedSlidesManifest` 及相关子模型
- `NarratedSlidesPlanBuilder`
- `NarratedSlidesPlanBuildRequest / Result / Stats`

当前已固定的投影规则：

- 输出 `schemaVersion = 2`
- 主视频轨固定为 `main`
- 旁白轨固定为 `voice`
- 可选 BGM 轨固定为 `bgm`
- section 时长以 `voice` 为准
- 当 `visual` 时长短于 `voice` 时，构建直接失败

### Cli

当前已新增：

- `init-narrated-plan --manifest <narrated.json> --output <edit.json> ...`
- narrated manifest 相对路径解析
- manifest 缺省时长的 `ffprobe` fallback
- 统一 success / failure envelope

当前已守住的边界：

- CLI 不手搓 timeline
- narrated-slides 没有混入现有 `templates` / `init-plan <input>` 单素材模板入口
- `template.id` 目前只作为稳定输出字段，不代表已进入 built-in template catalog

## 与阶段目标对照

### 条件 1

> 必须先有独立显式入口，而不是把 section manifest 混入现有模板命令面

当前判断：**满足**

### 条件 2

> narrated manifest -> v2 plan 的规则必须由 `Core.Editing` 持有

当前判断：**满足**

### 条件 3

> 首版结果必须能被现有 v2 render 路径消费，而不引入第二套执行 owner

当前判断：**满足**

### 条件 4

> 第一版范围必须受控，不把图片页、`${var}`、slot logic、batch 一起混入

当前判断：**满足**

### 条件 5

> 至少要有稳定测试，覆盖 plan 生成与 CLI 失败路径

当前判断：**满足**

## 当前验证结果

本轮已执行：

- `dotnet test OpenVideoToolbox.sln`

当前最新结果：

- `OpenVideoToolbox.Core.Tests`：166 通过
- `OpenVideoToolbox.Cli.Tests`：181 通过
- 总计：347 通过

本轮新增覆盖：

- `NarratedSlidesPlanBuilderTests`
- `CommandArtifactsIntegrationTests.InitNarratedPlanCommands`

## 当前结论

`V2-P6 narrated-slides` 当前判断为：**已完成首轮 `C1 ~ C5`，达到 `ready_for_acceptance`**

更准确地说：

- 首版讲解型 manifest contract 已落地
- 首版 narrated v2 plan 投影已落地
- 独立 CLI 入口和失败 envelope 已落地
- 首版结果已能复用现有 v2 render 路径
- 仍然守住了“不把它伪装成当前模板 catalog 项”的范围控制

因此当前不应继续无边界追加图片页、变量注入或 batch；应先进入人工反馈。

## 手动验收入口

当前阶段验收清单见：

- `docs/plans/v2/2026-04-25-v2-p6-acceptance-checklist.md`
