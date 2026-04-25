# V2-P6-C10 timeline gap / placeholder video 计划稿

最后更新：2026-04-25

## 卡片信息

```text
卡片编号：V2-P6-C10
所属阶段：V2-P6
卡片类型：计划
目标：为 narrated-slides 后续 visual slot 预先定义 timeline gap / placeholder video 的 canonical owner、最小 contract 与实施边界
输入：docs/plans/v2/2026-04-25-v2-p6-c9-narrated-slides-visual-slot-feasibility.md、当前 FfmpegTimelineRenderCommandBuilder 代码事实
输出：本计划稿
完成标准：明确 placeholder video 该由谁实现、最小 contract 长什么样、当前不纳入什么
阻塞条件：如果 placeholder video 需要同时引入新的页面渲染后端、batch、图表后端或磁盘中间产物链，则本卡失效
完成后下一张卡：V2-P6-C11
```

## 结论

如果 `V2-P6` 还要继续往 visual slot 走，**下一步必须先做 timeline placeholder / gap strategy**。

更具体地说：

1. visual slot 的前置能力不是 narrated manifest 小改
2. 它是 `Core.Execution` 对“无素材视频段”的正式消费能力
3. 因此下一张真正可实施的卡，应是：
   - `V2-P6-C11`：placeholder video 最小实现

换句话说，当前 `V2-P6` 的推进顺序应是：

```text
${var}
  ↓
bgm.slot
  ↓
visual slot feasibility
  ↓
timeline gap / placeholder plan
  ↓
placeholder video implementation
  ↓
再决定 visual slot 是否 ready
```

## 为什么这是 visual slot 的前置卡

visual slot 现在卡住，不是因为 manifest 不会表达 optional，而是因为：

- 当前 render builder 只消费已有 video clip
- 空 track 会被跳过
- track 内 clip 默认按连续序列拼接
- 没有“这里需要保留时长，但没有真实视频素材”的执行语义

所以如果缺视觉时还想保留 voice：

- 必须有正式 placeholder video

否则任何 visual slot 都只能退化成：

- 删 section
- 或错误地把前后 clip 直接拼紧

这两种都不是当前想要的能力。

## 建议的最小 contract

## 1. 不建议用“删 clip”表达 gap

当前不建议把 gap 语义设计成：

- 直接让某个 section 的 main clip 消失

因为这会把“空时间段”的含义隐藏起来，而 render builder 当前并不会自动补空白段。

## 2. 建议引入显式 placeholder video 语义

推荐的方向是：在 `Core.Editing` 的 timeline 模型里显式表达“这是一段占位视频”，而不是靠空路径、特殊字符串或 CLI 约定猜测。

计划层面的最小示意可以是：

```jsonc
{
  "id": "section-02-placeholder",
  "start": "00:00:03",
  "duration": "00:00:04",
  "placeholder": {
    "kind": "color",
    "color": "black"
  }
}
```

这里的关键不是字段名最终一定长这样，而是下面三点必须成立：

1. placeholder 是显式模型
2. 不依赖磁盘上的临时视频文件
3. 不把执行细节泄漏成 CLI 占位符或伪路径协议

## 3. 第一版只支持 `color` placeholder

第一版建议只支持：

- `kind = color`
- 单色背景，默认黑色

明确不纳入：

- 标题卡
- 文本排版
- 图片占位
- 模糊背景
- 自动截图 / 自动取帧

原因很简单：

- 这些都已经会把问题升级成页面渲染能力
- 当前这张卡只想先解决“voice 在，但 video 缺素材时，如何保留时长”

## owner 边界

## Core.Editing

owner：

- placeholder 的计划模型
- narrated builder 未来如何把“缺视觉但保留 section”投影成 placeholder clip
- validator 对 placeholder 字段的结构约束

不应负责：

- 真正生成黑底视频流
- 拼接 `ffmpeg color` 参数

## Core.Execution

owner：

- placeholder clip 到 FFmpeg graph / input / filter 的真实消费
- “没有真实视频素材时如何生成对应时长视频流”的唯一执行语义

不应负责：

- narrated manifest 的业务层决定
- slot 是否 missing 的判断

## Cli

CLI 只继续负责：

- plan 装载
- 命令 envelope

CLI 不应负责：

- placeholder clip 合成
- 把缺视觉 section 转成临时视频文件
- 手动补 `ffmpeg lavfi color`

## 为什么不建议用磁盘中间产物

一个看起来简单但实际上不适合当前边界的方案是：

- narrated builder 先输出临时黑色视频文件
- timeline 再把它当普通 `src`

当前不建议这样做，因为它会引入：

1. 新的写盘时机
2. 新的清理责任
3. 新的路径生命周期
4. `Core.Editing` 越权介入执行产物

这会明显破坏当前 owner 边界。

因此 placeholder video 更适合是：

- `Core.Editing` 持有声明
- `Core.Execution` 持有运行时生成

## 对 validator / render 的最低要求

## Validator

后续如果进入实现，至少要能校验：

- placeholder 与 `src` 不能同时滥用成两套互相冲突的来源
- placeholder `kind` 当前只允许 `color`
- placeholder `color` 非空
- placeholder clip 仍必须有合法 `start` / `duration`

## Render

后续如果进入实现，至少要满足：

1. 单个 placeholder video clip 可被消费
2. placeholder video 可与普通 voice track 同时存在
3. 多个 placeholder / normal clip 混合时，总时长保持稳定
4. 整条 video 轨只剩 placeholder 时，仍输出标准视频流而不是音频-only

## 与 visual slot 的关系

这张卡完成后，visual slot 才能重新判断是否 ready。

那时再决定的事情是：

- 某 section 缺 visual 时，是否自动投影为 placeholder clip
- 是否仍保留 section voice

而不是在这张卡里提前决定。

## 不纳入项

本卡明确不纳入：

1. visual slot 真正落地
2. section 条件删除
3. title-card / quote-card / image placeholder
4. 数据驱动 batch
5. 图表后端
6. `.pptx` / Markdown 页面生成
7. 任何磁盘临时视频生成链

## 回滚路径

本卡仍然只是计划稿。

如果后续判断 placeholder strategy 本身不值得做，回滚范围应局限在：

1. 本计划稿
2. `docs/roadmap.md`

不影响：

- narrated-slides 主路径
- `${var}` foundation
- `bgm.slot` 最小实现
- visual slot 的 feasibility 结论
