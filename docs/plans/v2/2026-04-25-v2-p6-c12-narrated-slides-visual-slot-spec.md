# V2-P6-C12 narrated-slides visual slot 最小规格稿

最后更新：2026-04-25

## 卡片信息

```text
卡片编号：V2-P6-C12
所属阶段：V2-P6
卡片类型：规格
目标：在 C11 placeholder video 基线已落地后，为 narrated-slides 定义 visual slot 的最小 manifest contract 与投影语义
输入：docs/plans/v2/2026-04-25-v2-p6-c9-narrated-slides-visual-slot-feasibility.md、docs/plans/v2/2026-04-25-v2-p6-c10-timeline-gap-placeholder-plan.md、docs/plans/v2/2026-04-25-v2-p6-c11-placeholder-video-implementation.md
输出：本规格稿
完成标准：明确 visual slot 的 manifest 字段、缺视觉时的最小行为、owner 和不纳入项
下一张卡：V2-P6-C13
```

## 结论

`C11` 之后，visual slot 已从 `not ready` 进入 **可定义最小 contract，但仍未进入代码实现** 的状态。

当前推荐的最小 visual slot 只解决一件事：

- 当某个 narrated section 没有绑定 visual 素材时
- 仍保留该 section 的 voice 时长
- 并由 `Core.Editing` 投影成显式 placeholder video clip

本卡不解决：

- section 删除
- 复杂 title card / quote card
- 数据驱动图表页
- `.pptx` / Markdown 页面后端

## 为什么现在可以继续

此前 visual slot 卡住，不是 manifest 不会表达 optional，而是执行层没有“无真实素材视频段”的稳定消费能力。

`C11` 已经补齐了这个前置条件：

1. timeline clip 现在可以显式表达 `placeholder`
2. validator 已锁住 placeholder 的合法组合
3. render builder 已能直接消费 `color` placeholder

因此 `V2-P6` 现在可以安全讨论：

- narrated builder 在什么条件下投影 placeholder
- visual 缺失时，最小 manifest 应该长什么样

## 推荐的最小 contract

## 1. 只在 section.visual 层引入 slot

推荐在 `sections[].visual` 下新增：

```json
{
  "kind": "video",
  "slot": {
    "name": "intro-visual",
    "required": false
  }
}
```

最小原则：

- `slot` 仍复用已经存在的 `NarratedSlidesSlotManifest`
- `required = false` 表示“当前 section 允许缺视觉”
- 当 `path` 缺失且 `required = false` 时，不报错删除 section，而是进入 placeholder 投影

## 2. 第一版仍保持 visual.kind 范围不扩张

第一版 visual slot 不新增新的 visual kind。

也就是说：

- 有真实素材时，仍只支持现有 `video` / `image`
- 缺素材时，也不新增 `title-card` / `quote-card` / `chart`
- fallback 始终投影为 timeline 层的 `placeholder.kind = color`

## 3. placeholder 行为先固定为黑底

第一版建议固定：

- placeholder `kind = color`
- `color = black`

原因：

- 当前 execution 只正式支持 `color`
- 如果现在就暴露 per-section placeholder 颜色、背景图、文本样式，会把范围重新拉进页面系统

所以第一版 visual slot 的目标不是“可配置占位页”，而是：

- `voice` 在
- 视频时长在
- 没有真实 visual 时仍能稳定渲染

## 推荐的投影规则

对每个 section：

1. 如果 `visual.path` 存在：
   - 走当前既有投影
2. 如果 `visual.path` 缺失，且 `visual.slot.required = false`：
   - `Core.Editing` 生成一段 video placeholder clip
   - `clip.duration = section.voiceDuration`
   - `clip.placeholder = { kind: "color", color: "black" }`
3. 如果 `visual.path` 缺失，且未声明 optional slot：
   - 继续报错，不隐式降级

## manifest 建议示例

```json
{
  "id": "chapter-02",
  "title": "Tradeoffs",
  "visual": {
    "kind": "video",
    "slot": {
      "name": "chapter-02-visual",
      "required": false
    }
  },
  "voice": {
    "path": "audio/chapter-02.wav"
  }
}
```

对应的最小 timeline 投影示意：

```json
{
  "id": "chapter-02-video",
  "start": "00:00:12",
  "duration": "00:00:05",
  "placeholder": {
    "kind": "color",
    "color": "black"
  }
}
```

## owner

## Core.Editing

owner：

- `sections[].visual.slot` 的 manifest contract
- “缺视觉但允许 optional” 时，何时投影 placeholder
- narrated visual slot 的验证与失败条件

不应负责：

- 生成临时黑底视频文件
- CLI 层降级判断

## Core.Execution

owner：

- placeholder video 的运行时消费

本卡不再新增 execution 新语义，只复用 `C11`。

## Cli

CLI 只负责：

- manifest 反序列化
- 相对路径解析
- envelope 输出

CLI 不负责：

- 缺视觉 section 的 fallback 策略

## 明确不纳入

本卡明确不纳入：

1. `visual.slot.required = true`
2. 自动删除 section
3. section 级标题卡 / 文本卡
4. 可配置 placeholder 样式
5. batch / resolve-assets
6. 图表后端
7. `.pptx` / Markdown 页面渲染

## 验收方向

后续如果进入实现，最小验收应至少覆盖：

1. `visual.path` 正常存在时，输出与当前实现保持兼容
2. `visual.slot.required = false` 且未绑定素材时，main 轨生成 placeholder clip
3. placeholder clip 的时长与 voice 时长一致
4. 不声明 optional slot 时，缺视觉仍显式失败
5. 最终 `render --preview` 可直接消费写出的 plan
