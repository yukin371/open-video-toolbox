# V2-P6-C9 narrated-slides visual slot 可行性评估稿

最后更新：2026-04-25

## 卡片信息

```text
卡片编号：V2-P6-C9
所属阶段：V2-P6
卡片类型：评估 / 计划
目标：判断 narrated-slides 在 `bgm.slot` 之后，是否适合继续推进 visual slot，并明确真正实施前还缺什么
输入：当前 narrated-slides / `${var}` / `bgm.slot` 代码事实，及 `FfmpegTimelineRenderCommandBuilder` 当前能力
输出：本评估稿
完成标准：明确 visual slot 是否 ready、若不 ready 卡在什么 owner、应先补哪一层能力
阻塞条件：若 visual slot 必须同时引入 batch / 图表后端 / 新页面渲染后端，则本卡直接判定为 not ready
完成后下一张卡：待定
```

## 结论

当前 **不建议直接实现 narrated-slides visual slot**。

更准确地说：

- 它仍然属于 `V2-P6`
- 但 **还没有到可以直接写 `C10` 代码的状态**
- 在它之前，必须先补一张“timeline gap / 空视频段 / 背景填充”层面的 owner 计划卡

因此当前判断是：

- **阶段归属：仍是 `V2-P6`**
- **实施状态：not ready**
- **下一步：先补 render / timeline 侧的能力计划，而不是先改 narrated manifest**

## 为什么 `bgm.slot` 可以先做，而 visual slot 不行

`bgm.slot` 的裁剪只会影响：

- 有没有 `bgm` 轨

这不会改变：

- 主视频轨是否存在
- section 的时间分段是否还能成立
- `render` 是否还能产出视频流

但 visual slot 一旦进入，就会立刻碰到下面这些问题：

1. 某个 section 没有 visual 时，当前 voice 是否继续保留
2. 若保留 voice，该时间段的视频层要显示什么
3. 若直接删掉 main clip，timeline 中间的空洞由谁表达
4. 如果整条 main 轨被删空，render 是否还应输出视频而不是音频-only

这些都不是单纯的 manifest 字段问题，而是 `Core.Execution` 当前 timeline 消费能力的问题。

## 当前代码事实

根据当前实现，`FfmpegTimelineRenderCommandBuilder` 有两个关键限制：

## 1. Track 只处理 `track.Clips.Count > 0`

当前 render builder 在构图时会直接跳过：

- `Muted = true` 的轨
- `Clips.Count == 0` 的轨

这意味着如果 visual slot 导致：

- 某段没有视频 clip
- 或整条 `main` 轨被裁空

当前执行层不会自动补任何“空白底板”或 placeholder video。

## 2. Track 内 clip 当前按连续序列拼接，不表达显式时间空洞

当前 builder 对同一轨的 clip 处理方式，本质上是：

- 逐段拼接 / transition / concat

它没有建立一个“中间空白时间段”的显式表示。

也就是说：

- `clip.Start` 现在不足以表达“前面空 3 秒，再显示这段视觉”
- 如果删掉某个中间 visual clip，当前 builder 并不会自动补出黑底视频段

因此 visual slot 一旦实现，不只是“删一个 clip”这么简单。

## visual slot 真正依赖的前置能力

如果后续真的要做 visual slot，至少要先选定下面三种语义中的一种：

## 方案 A：缺视觉时自动补黑底 / 纯色底视频段

优点：

- voice 可继续存在
- section 总时长不变
- narrated 语义最完整

代价：

- `Core.Execution` 需要新增稳定的“生成空白视频段”能力
- `Core.Editing` 需要稳定表达“该 section 没有素材，但有占位视觉”

## 方案 B：缺视觉时删除该 section 的 main clip，但保留 voice

优点：

- manifest 层最简单

问题：

- 当前 render builder 不能稳定表达视频空洞
- 很容易得到意料之外的“前后 clip 紧贴拼接”

因此当前不建议选这个方案。

## 方案 C：缺视觉时整章 section 一起删除

优点：

- 当前时间线更容易保持连续

问题：

- 它已经不是 visual slot，而是 section 条件裁剪
- 风险明显高于当前想要的最小增量

因此也不适合直接作为下一步。

## 当前推荐路径

如果还要继续 narrated-slides，推荐顺序应改成：

1. 先停在 `V2-P6-C9`
2. 新开一张“timeline gap / placeholder video strategy”计划卡
3. 先明确缺视觉时的视频占位 owner 在哪里
4. 明确之后，才决定是否值得继续做 visual slot

也就是说，下一张真正该写的不是：

- `visual slot implementation`

而应该是：

- `visual gap / placeholder video owner plan`

## owner 判断

## Core.Editing

如果未来有 visual slot：

- 只应持有 manifest 到 plan 的投影规则
- 不应自己发明“黑底视频怎么渲染”

## Core.Execution

如果未来要支持“缺视觉但保留 voice”：

- placeholder video / gap fill 必须由这里统一承接
- 不能在 CLI 或 narrated builder 里偷偷拼 `ffmpeg color`

## Cli

CLI 不应参与：

- visual 缺失时的降级策略判断
- 轨道空洞补齐
- placeholder 片段生成

## 当前结论对应的 backlog 调整

当前不建议把下一张卡直接定成：

- `V2-P6 visual slot implementation`

更合理的是：

- `V2-P6-C10 timeline placeholder / gap strategy 计划稿`

只有这张卡收口之后，visual slot 才算 ready。

## 不纳入项

本卡明确不纳入：

1. visual slot 代码实现
2. section 条件删除
3. black frame / color source 真正执行实现
4. batch / resolve-assets
5. 图表后端
6. `.pptx` / Markdown 页面生成

## 回滚路径

本轮只改文档：

1. 本评估稿
2. `docs/roadmap.md`

如方向不合适，直接回滚文档即可。
