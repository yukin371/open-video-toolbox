# V2-P6 阶段检查：narrated-slides 当前实现

最后更新：2026-04-25

> 后续补记：该首轮实现已获人工接受；当前已在同一主题下继续补“单视频 `${var}` foundation`、可选 `bgm.slot`、timeline placeholder video 与首个 optional visual slot”，但仍不直接进入 section 删除 / 图表后端。其后第三类重开路线 `V2-P6-C15 数据驱动 batch narrated` 已作为单独卡片另行收口，见 `docs/plans/v2/2026-04-25-v2-p6-c15-batch-narrated-phase-check.md`。

## 目的

这份文档只回答一个问题：

> `V2-P6` 当前选定的 narrated-slides 主题，是否已经完成当前这一轮可回归、可验收、边界清晰的实现？

这里说的“当前实现”不是把讲解型视频能力整个做完，而是确认当前已稳定交付：

- 独立 manifest contract
- `Core.Editing` 持有的 narrated plan 投影
- 独立 CLI 入口 `init-narrated-plan`
- 可直接被现有 `render --preview` 消费的 v2 `edit.json`
- `visual.kind = "image"` 的最小静态图输入支持
- 可选 `video.progressBar` 的最小轨道效果支持
- 可选 `bgm.slot` 的首个轨道裁剪
- 可选 `visual.slot` 的首个 placeholder 投影

## 当前纳入范围

本轮只纳入：

- narrated manifest 模型
- `NarratedSlidesPlanBuilder`
- `init-narrated-plan`
- `visual.kind = "image"` 章节页
- 可选 `video.progressBar`
- narrated `${var}` foundation
- 可选 `bgm.slot`
- 可选 `visual.slot`
- still-image 输入的最小 render 适配
- timeline placeholder video 的最小 render 适配
- `render --preview` 对首版 narrated v2 plan 的复用验收
- 对应 Core / CLI 测试与文档同步

本轮明确不纳入：

- `templates` catalog 集成
- `init-plan <input>` 复用
- title-card、图表或 `.pptx`
- section 删除、数据驱动 batch
- AI provider、TTS provider、Remotion runtime

## 当前已落地能力

### Core.Editing

当前已新增：

- `NarratedSlidesManifest` 及相关子模型
- `NarratedSlidesPlanBuilder`
- `NarratedSlidesPlanBuildRequest / Result / Stats`
- narrated `${var}` 解析与 overlay 语义
- narrated `bgm.slot.required = false`
- narrated `sections[].visual.slot.required = false`

当前已固定的投影规则：

- 输出 `schemaVersion = 2`
- 主视频轨固定为 `main`
- 旁白轨固定为 `voice`
- 可选 BGM 轨固定为 `bgm`
- section 时长以 `voice` 为准
- `visual.kind = "video"` 时，素材时长短于 `voice` 会直接失败
- `visual.kind = "image"` 时，静态图章节默认按 `voice` 时长持有
- `video.progressBar` 开启时，会稳定投影为 `main` 轨的 `progress_bar` effect
- `bgm.slot.required = false` 且未绑定素材时，会省略 `bgm` 轨
- `visual.slot.required = false` 且未绑定素材时，会保留 `voice`，并把 `main` clip 投影为 black color placeholder

### Cli

当前已新增：

- `init-narrated-plan --manifest <narrated.json> --output <edit.json> ...`
- narrated manifest 相对路径解析
- manifest 缺省时长的 `ffprobe` fallback
- narrated `${var}` 变量 overlay
- narrated optional visual 缺失时的路径 / 时长探测回退
- 统一 success / failure envelope

### Core.Execution

当前已补：

- still-image 视频输入识别
- 对静态图输入的 `-loop 1` / `-framerate` 最小 FFmpeg 接线
- timeline placeholder video 的 `lavfi color` 最小 FFmpeg 接线
- `progress_bar` built-in effect 到 `drawbox` filter 的最小映射

当前已守住的边界：

- 图片输入适配继续留在 `Core.Execution`
- progress bar 继续走 built-in effect catalog + timeline render，不走 CLI 特判
- CLI 不手搓 timeline
- CLI 不拼 still-image / progress-bar 执行参数
- CLI 不新增独立的 visual slot fallback；缺视觉时的 placeholder 投影继续留在 `Core.Editing`
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

> 当前范围必须受控，不把 `${var}`、slot logic、batch 一起混入

当前判断：**满足**

### 条件 5

> 至少要有稳定测试，覆盖 plan 生成与 CLI 失败路径

当前判断：**满足**

## 当前验证结果

本轮已执行：

- `dotnet test OpenVideoToolbox.sln`

当前最新结果：

- `OpenVideoToolbox.Core.Tests`：180 通过
- `OpenVideoToolbox.Cli.Tests`：187 通过
- 总计：367 通过

本轮新增覆盖：

- `NarratedSlidesPlanBuilderTests`
- `CommandArtifactsIntegrationTests.InitNarratedPlanCommands`
- optional visual slot -> placeholder -> `render --preview` narrated 闭环

## 当前结论

`V2-P6 narrated-slides` 当前判断为：**首轮 `C1 ~ C5` 已通过人工阶段验收**

更准确地说：

- 首版讲解型 manifest contract 已落地
- 首版 narrated v2 plan 投影已落地
- 独立 CLI 入口和失败 envelope 已落地
- 首版结果已能复用现有 v2 render 路径
- 静态图片页已可通过最小 still-image 输入适配进入同一条 render 路径
- 可选 progress bar 已可通过 built-in effect + 同一条 v2 render 路径消费
- 可选 `bgm.slot` 已能做最小轨道裁剪
- 可选 `visual.slot` 已能保留 voice 并投影 black color placeholder，且 `render --preview` 可直接消费
- 仍然守住了“不把它伪装成当前模板 catalog 项”的范围控制

因此后续继续推进时，仍必须保持增量受控：当前最小 slot 能力已落到可选 `bgm` 与可选 `visual`，后续不应直接滑向 section 删除、batch 或图表后端。

## 手动验收入口

当前阶段验收清单见：

- `docs/plans/v2/2026-04-25-v2-p6-acceptance-checklist.md`
