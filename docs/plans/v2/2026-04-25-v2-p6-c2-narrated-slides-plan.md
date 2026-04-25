# V2-P6-C2 narrated-slides 计划稿

最后更新：2026-04-25

> 当前状态补记：本计划稿对应的首轮 `V2-P6-C3 ~ C5` 已完成落地；当前验收入口见：
>
> - `docs/plans/v2/2026-04-25-v2-p6-phase-check.md`
> - `docs/plans/v2/2026-04-25-v2-p6-acceptance-checklist.md`

## 卡片信息

```text
卡片编号：V2-P6-C2
所属阶段：V2-P6
卡片类型：计划
目标：拆分讲解型 / PPT 风格视频能力的实现顺序、owner、依赖、测试面、基线与回滚点
输入：docs/plans/v2/2026-04-25-narrated-slides-video-spec.md、当前 schema v2 / timeline render / batch contract / voice bind 代码事实
输出：本计划稿
完成标准：明确为何它不应进入当前 E2-F*，并明确 V2-P6 内的最小实施切口、owner、测试与风险收敛路径
阻塞条件：如果实现需要把 AI/TTS/Remotion runtime 直接引入仓库，或 canonical owner 无法确认，则本卡失效
完成后下一张卡：V2-P6-C3
```

## 阶段结论

这项能力不应在当前 `E2-F*` 实施，应在：

- `E2-F4` 正式关闭之后
- `V2-P5` 已接受的前提下
- 作为 `V2-P6` 的单独主题启动

更具体地说：

1. 当前阶段只能做到 `V2-P6-C2` 计划收口
2. 真正进入代码实施时，对应卡片应是 `V2-P6-C3`
3. 在进入 `V2-P6-C3` 前，需要人工明确批准“本轮只做 narrated-slides，不并行拉上 resolve-assets / ${var} / 图表后端”

因此答案不是“现在就做”，而是：

- **当前阶段归属：`V2-P6-C2`**
- **真正实施阶段：`V2-P6-C3`**

## 为什么不是当前 E2-F*

## 1. 它不是现有高频工作流的小延长

当前 `E2-F2 ~ E2-F4` 的主线仍是：

- 计划内素材替换 / 挂载 / voice bind
- supporting signal 闭环
- batch / workdir contract 收口

这些都建立在：

- 已有 `edit.json`
- 已有单主素材或现有模板骨架
- 复用现有单项命令 owner

而 narrated-slides 的核心是：

- 从 section manifest 直接生成新的 v2 plan
- 重新定义讲解型模板的输入面
- 为“页面素材 + 旁白 + 字幕 + BGM”建立新的 plan 投影规则

这已经不是 `E2-F*` 的局部补丁，而是新的 v2 用户能力。

## 2. 它天然靠近 V2-P6 的高风险区

它和以下后续能力天然相邻：

- `${var}` 数据注入
- slot 条件裁剪
- 数据驱动 batch
- 图表 / 页面组件

如果在 `E2-F*` 或 `V2-P5` 里强行推进，范围几乎一定失控，最后会和现有 batch contract、render owner、template owner 打架。

## 3. 它会产生新的长期 contract

一旦正式进入实现，就至少会新增：

- 讲解型 manifest contract
- 新 built-in template
- section 到 timeline 的投影规则
- 对 v2 timeline render 的新输入假设

这属于长期 contract，不适合混入当前活跃工作面。

## 为什么不能直接复用现有 batch/render owner

`V2-P6-C2` 的完成标准之一，是回答“为何不能复用现有 batch/render owner”。结论如下。

## 1. 不能复用现有 batch contract

现有 batch contract 的共同前提是：

- `scaffold-template-batch`
  - 批量生成已有模板工作目录
- `render-batch`
  - 批量消费既有 `edit.json`
- `replace-plan-material-batch`
  - 批量替换既有素材绑定
- `attach-plan-material-batch`
  - 批量挂载既有素材绑定
- `bind-voice-track-batch`
  - 批量接回既有 voice track

它们都不是：

- “从章节清单直接生成一条全新的讲解型 v2 plan”

如果直接复用现有 batch owner，会出现两个问题：

1. 让 batch 命令从“消费既有 plan / workdir”漂移为“定义新计划模型”
2. 让 `E2-F4` 刚收口的 batch manifest 再长出第二套高层语义

因此 narrated-slides 第一阶段不应做成 batch 命令，而应先是单视频显式命令。

## 2. 可以复用现有 render owner，但不能只靠现有 render owner

`Core.Execution` 仍应保持：

- v2 timeline render 的唯一执行 owner
- `render --preview` / `render` 的唯一执行入口

但 narrated-slides 不能“只靠 render owner”完成，因为问题不在执行，而在：

- 如何把讲解型 manifest 投影成合法的 `EditPlan`
- 如何定义章节、页面素材、旁白、字幕、BGM 的结构边界
- 如何避免 CLI 自己偷偷手搓 plan

这些必须仍由 `Core.Editing` 持有。

因此结论是：

- **render 执行可复用**
- **plan 生成语义不能复用当前 batch owner，也不能回流到 CLI**

## 3. 不能把它做成 CLI 脚本拼装层

如果只在 `Cli` 里直接：

- 读 manifest
- 拼 timeline JSON
- 再交给 render

会直接违反当前模块边界：

- `Cli` 不应拥有第二套 plan 语义
- `Core.Editing` 才是 `EditPlan` 的 canonical owner

所以这条能力必须建立在新的 `Core.Editing` 入口之上，而不是 CLI 快速拼装。

## 计划结论

第一阶段应拆成四层：

1. `Core.Editing`
   - narrated manifest 模型
   - narrated manifest -> v2 `EditPlan` 投影
   - narrated built-in template 定义
2. `Core.Execution`
   - 尽量复用现有 v2 timeline render
   - 仅在必须时补最小输入适配
3. `Cli`
   - 新命令入口、manifest 装载、路径解析、envelope 输出
4. `Tests + Docs`
   - 合约、plan 生成、preview、失败路径与文档验收

第一阶段的目标不是“讲解视频平台”，而是：

- 把外部准备好的讲解素材
- 稳定变成可渲染的 `edit.json v2`

## owner 与文件范围

## Core.Editing

建议 owner：

- `src/OpenVideoToolbox.Core/Editing`

建议新增：

- `NarratedSlidesManifest.cs`
  - 顶层 manifest 模型
- `NarratedSlidesPlanBuilder.cs`
  - manifest -> `EditPlan` 投影规则
- `NarratedSlidesPlanBuilderResult.cs`
  - 结构化构建结果与统计

建议扩展：

- `BuiltInEditPlanTemplateCatalog.cs`
  - 新增 `narrated-slides-starter`
- `MODULE.md`
  - 同步 canonical owner

职责边界：

- 持有讲解型 manifest 的长期语义
- 持有 section 到 timeline track / clip 的生成规则
- 持有默认轨道结构：
  - `main`
  - `voice`
  - `bgm`（可选）

明确不应放在：

- `Cli`
- `Core.Execution`
- 任何未来 Desktop 层

## Core.Execution

建议 owner：

- `src/OpenVideoToolbox.Core/Execution`

第一阶段建议尽量不新增新的正式 command builder，而是：

- 继续复用现有 `FfmpegTimelineRenderCommandBuilder`
- 用验收数据确认它能稳定消费 narrated-slides 生成的 v2 plan

仅当第一阶段样例显示现有 builder 无法稳定承接时，才允许补最小适配，例如：

- 对 section voice / bgm 的默认混音约定做轻量增强
- 对未来静态图片页支持预留输入适配点

第一阶段明确不做：

- 新增 Node / Remotion runtime
- 新增页面组件 renderer
- 新增图表后端

## Cli

建议新增显式入口：

- `init-narrated-plan --manifest <narrated.json> --template <id> --output <edit.json> --render-output <final.mp4>`

建议改动文件：

- `src/OpenVideoToolbox.Cli/Program.cs`
- 新的 handler 文件，或 `EditPlanCommandHandlers.cs`
- `src/OpenVideoToolbox.Cli/MODULE.md`

CLI 只负责：

- 参数解析
- manifest 路径解析
- 调用 `Core.Editing`
- 写出 `edit.json`
- 返回结构化 envelope

不负责：

- 手搓 timeline
- 推导章节时长算法
- 管理讲解型模板语义

## 第一阶段建议范围

## Step 1. 固定 manifest 最小 contract

只支持：

- `sections[].visual.kind = "video"`
- `sections[].voice.path`
- 顶层 `subtitles`
- 顶层 `bgm`

先不支持：

- 图片页
- title-card
- quote-card
- 图表
- `${var}`
- slot
- 章节级字幕

原因：

- 先把“预渲染页面视频 + 旁白音频”的闭环跑通
- 避免一上来把能力推进到页面渲染后端

## Step 2. 固定 plan 投影

把 narrated manifest 投影为：

- `schemaVersion = 2`
- `template.id = narrated-slides-starter`
- `timeline.tracks`
  - `main` video track
  - `voice` audio track
  - `bgm` audio track（可选）

每个 section 的视觉和语音分别成为对应轨道 clip。

第一阶段的推荐时长规则：

1. section 的主时间轴长度默认以 `voice` 音频时长为准
2. `visual` 素材时长不足时直接校验失败，不隐式 loop / freeze
3. `visual` 素材时长更长时，只取与语音对齐的前段

这样更确定，也更容易测试。

## Step 3. CLI 命令与 envelope

建议 payload 至少包含：

- `manifestPath`
- `template`
- `plan`
- `stats`
  - `sectionCount`
  - `videoTrackCount`
  - `audioTrackCount`
  - `hasBgm`
  - `hasSubtitles`

如果命令失败，仍应返回结构化 failure envelope，而不是 usage 文本。

## Step 4. preview / render 验收

第一阶段的正式闭环必须至少验证：

1. `init-narrated-plan`
2. `validate-plan --check-files`
3. `render --preview`

如 execute 验收成本可控，再补一次真实 `render`。

## 第二阶段以后再看的能力

以下全部后置，不进入本轮：

1. `visual.kind = image`
2. 图片自动延时
3. 章节进度条
4. `${var}` 深度注入
5. slot 条件裁剪
6. 数据驱动 `run-batch`
7. 图表后端
8. `.pptx` / Markdown / structured script 直接转页面

## 测试面

## 1. Core 单测

至少覆盖：

- manifest 合法输入可稳定产出 `EditPlan`
- section 顺序稳定映射为 clip 顺序
- 缺失 `voice.path` / `visual.path` 的失败路径
- visual 时长不足的失败路径
- 带 / 不带 `bgm` 的 plan 投影
- 带 / 不带 `subtitles` 的 plan 投影

## 2. CLI 契约测试

至少覆盖：

- 成功输出 envelope
- `--json-out`
- manifest schema version 错误
- 缺失文件路径错误
- `render-output` 写入 plan.output 的稳定语义

## 3. Render preview 集成测试

至少覆盖：

- 讲解型 v2 plan 可被 `render --preview` 接受
- `payload.executionPreview.commandPlan.schemaVersion = 2`
- `ProducedPaths` 与输出路径声明稳定

## 4. 可选 execute smoke

如果仓库已有最小媒体样本和可控成本，可补：

- 两段 section video + 两段 voice audio + 一段 bgm
- 真实渲染一次最小 narrated-slides 输出

## 性能与安全基线

## 1. 性能

第一阶段不新增单独 benchmark 系统，但至少要纳入：

- `init-narrated-plan` 的轻量运行时样本
- narrated plan 的 `render --preview` 样本

建议只有在功能闭环稳定后，才考虑把它接入现有 runtime baseline。

## 2. 安全

必须继续遵守现有外部工具基线：

1. 不新增绕过 `Core.Execution` 的外部进程调用
2. 不在 CLI 拼 shell 命令
3. 所有写盘路径显式声明
4. overwrite 语义继续走统一规则
5. 失败时保留 stdout / stderr / 结构化上下文

## 新依赖判断

第一阶段结论：

- **不应引入新的正式运行时依赖**

也就是说，本轮不能以“需要 Remotion / Cairo / Magick.NET 才能成立”为前提。

如果后续需求升级到：

- 静态图片页转视频
- 图表渲染
- 页面组件库

则必须重新立项，不得偷偷塞进 narrated-slides 第一阶段。

## 回滚路径

如果执行阶段发现 narrated-slides 主题不成立，回滚应尽量局限在：

1. narrated manifest 模型
2. narrated plan builder
3. `narrated-slides-starter` 模板
4. `init-narrated-plan` CLI 入口
5. 相关文档与测试

不应牵连回滚：

- 现有 `render`
- 现有 batch contract
- 现有 `init-plan`
- 现有 `bind-voice-track`

这也是为什么第一阶段不建议复用并污染既有高频入口。

## 推荐实施顺序

```text
V2-P6-C3 第一轮执行：
1. narrated manifest 模型
2. narrated plan builder
3. narrated built-in template
4. init-narrated-plan CLI
5. validate-plan / render --preview 验收

若上述闭环通过：
  ↓
下一轮再评估图片页 / progress bar / ${var} / slot
```

## 当前结论

这条能力**应该在 `V2-P6` 实施，而不是当前 `E2-F*`**。

更精确地说：

- 现在可以做的是 `V2-P6-C2` 计划收口
- 真正开始代码实现时，应进入 `V2-P6-C3`
- 实施范围必须严格限制为：
  - 单视频
  - 预渲染页面视频
  - 外部已生成的旁白 / 字幕 / BGM
  - 复用现有 v2 timeline render

只要范围开始逼近：

- 内置 TTS
- 页面组件引擎
- 数据驱动 batch
- 图表后端
- `.pptx` 解析

就不再属于本计划的第一阶段，应重新拆卡。
