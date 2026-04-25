# V2-P6 narrated-slides 当前停顿点建议

最后更新：2026-04-25

## 目的

这份文档只回答一个问题：

> `V2-P6 narrated-slides` 在当前这轮增量之后，是否还应该继续扩 scope？

## 当前结论

当前建议：**先停在这里，不继续扩张功能面。**

更具体地说：

- 当前 narrated-slides 已经具备一条自洽、可回归、可验收的最小正式链路
- 它已经覆盖了当前这条主题最值得验证的几个关键点：
  - 独立 `init-narrated-plan`
  - manifest -> v2 timeline plan
  - `video` / `image` 两类 visual
  - `video.progressBar`
  - `${var}` foundation
  - `bgm.slot.required = false`
  - `visual.slot.required = false`
  - placeholder video render baseline
  - `render --preview` 对上述结果的直接消费

在这个点上继续往下做，边际收益已经明显下降，而 scope 漂移风险开始上升。

## 为什么建议停在这里

## 1. 当前主问题已经被回答

`V2-P6` 最初想验证的是：

- 仓库能不能稳定承接“讲解型 / 幻灯片型视频装配”
- 这件事能不能在现有 `v2 timeline + render` 路径里完成
- 是否能不引入第二套模板 / 执行 owner

当前答案已经足够明确：**可以。**

## 2. 再往前走会迅速进入高耦合区

接下来最自然会诱导继续做的内容是：

1. section 删除
2. 可配置 placeholder 样式
3. title-card / quote-card
4. 数据驱动 batch
5. 图表或页面渲染后端

这些都已经不再是同一层复杂度的问题。

它们会把当前主题从：

- “讲解型 plan 装配”

拉向：

- “页面系统 / 内容生成系统 / 第二套模板平台”

这不是当前 roadmap 的最小下一步。

## 3. 当前正式主线仍是 E2

按照当前 `roadmap`，正式交付主线仍然是 `E2`。

`V2-P6` 当前更适合被视为：

- 已经验证过最小讲解型装配路径
- 可以先保留成果
- 等后续真有明确需求，再决定是否重开下一张更大的卡

## 当前建议的停顿点

建议把 `V2-P6` 当前主题的 stop point 固定为：

- narrated-slides 最小链路已闭环
- slot 能力只停在：
  - `bgm.slot.required = false`
  - `visual.slot.required = false`
- placeholder 只停在 black color
- 不进入：
  - section 删除
  - 可配置 placeholder 样式
  - batch / resolve-assets
  - chart / `.pptx` / Markdown 页面后端

## 对 roadmap 的建议解释

因此 roadmap 更合理的口径应是：

1. `V2-P6` 当前主题已有完整验收包
2. 当前不继续扩 narrated-slides scope
3. 回到正式主线 `E2`
4. 未来若要重开 narrated 线，应以新的明确卡片进入，而不是口头“继续补一点”

## 未来如果重开，推荐顺序

如果以后确实要继续 narrated-slides，建议只从下面三类里选其一：

1. section 删除 / 条件裁剪
2. richer placeholder / title card
3. 数据驱动 batch narrated

不要把三类一起打包。

## 后续补记

在这份 stop-point 文档之后，第三类路线已经被按“新明确卡片”的方式单独重开并执行：

- `V2-P6-C15 数据驱动 batch narrated`

这次重开仍然遵守了本文件的原始边界判断：

- 只选三类中的一个继续
- 只做 `Cli` batch wrapper
- 不把 narrated 主题扩成 `.pptx` / 页面 / 图表系统
- 在补齐实现、测试、phase-check 与验收清单后再次回到 stop-point

因此当前更准确的口径应是：

- `V2-P6` 默认仍然不继续横向扩 narrated scope
- `C15` 已作为一次受控重开完成，当前状态为 `ready_for_acceptance`
- 若还要继续 narrated，必须再开下一张明确新卡，而不是沿着 `C15` 继续加能力

## 当前判定

```text
阶段：V2-P6
当前主题：narrated-slides
当前状态：ready_to_hold
建议动作：停止扩 scope，保留当前实现，回到 E2 主线
```
