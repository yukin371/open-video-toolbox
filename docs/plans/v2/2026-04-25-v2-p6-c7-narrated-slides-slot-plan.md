# V2-P6-C7 narrated-slides slot 条件裁剪计划稿

最后更新：2026-04-25

> 当前状态补记：本计划稿对应的最小实现 `V2-P6-C8` 已落地到“可选 BGM 轨裁剪”；当前仍未扩到 visual slot、section 删除或 batch。

## 卡片信息

```text
卡片编号：V2-P6-C7
所属阶段：V2-P6
卡片类型：计划
目标：定义 narrated-slides 在 `${var}` 之后的最小 slot 条件裁剪实施边界，并明确它应在哪个阶段实施
输入：docs/plans/v2/2026-04-25-narrated-slides-video-spec.md、docs/plans/v2/2026-04-25-v2-p6-c2-narrated-slides-plan.md、当前 narrated-slides / ${var} 代码事实
输出：本计划稿
完成标准：明确 slot 条件裁剪是否继续留在 `V2-P6`、最小范围、owner、测试、回滚路径与不纳入项
阻塞条件：如果 slot 语义无法在 `Core.Editing` 内收敛，或需要同时引入 batch / resolve-assets / 图表后端，则本卡失效
完成后下一张卡：V2-P6-C8
```

## 阶段结论

`slot` 条件裁剪如果继续推进，**应继续放在 `V2-P6` 内实施**，但不是并入 `${var}` 这张卡，而是作为：

- `V2-P6-C7`：计划收口
- `V2-P6-C8`：最小实现

更具体地说：

1. 它不应回到 `E2-F*`
2. 它也不应提前升级成数据驱动 `run-batch`
3. 它应该作为 narrated-slides 单主题下的 **post-acceptance 最小增量**

因此答案是：

- **阶段归属：仍是 `V2-P6`**
- **真正实施阶段：`V2-P6-C8`**

## 为什么仍留在 V2-P6

## 1. 它仍然是 narrated manifest 到 v2 plan 的投影问题

当前 `${var}` 已经证明 narrated-slides 后续增量仍然围绕同一件事：

- manifest 如何表达可选输入
- plan 如何稳定省略对应 clip / track

这仍然是 `Core.Editing` 的长期 contract，而不是 CLI 的参数胶水，也不是执行层的渲染问题。

## 2. 它与 batch 有关，但不等于 batch

`slot` 条件裁剪常被拿来和数据行缺省一起讨论，但当前最小价值并不需要数据源：

- 单视频 narrated manifest 也会出现“某章节没有可选 BGM / subtitle / visual 扩展资源”的情况
- 在 manifest 内明确声明 optional slot，再由 `Core.Editing` 稳定裁掉对应 plan 片段，本身就是独立能力

因此这一步应该先在单视频 narrated 场景里跑通，而不是直接绑定到 `run-batch`。

## 3. 它不应进入 E2 正式交付线

`E2-F*` 当前仍以 v1 正式能力、batch/workdir contract、CLI 稳定输出为主。

slot 条件裁剪会引入新的 v2 合约：

- manifest 的 optional slot 表达
- timeline 级 clip/track 裁剪规则
- validator / preview 对裁剪后 plan 的稳定消费语义

这已经超出 `E2-F*` 的边界，仍应留在 `V2-P6` 孵化线。

## 本轮建议的最小范围

`V2-P6-C8` 如果进入实现，只做下面这一个最小切口：

1. narrated manifest 允许声明 **可选 BGM slot**
2. 当 slot 未绑定时：
   - 不生成 `bgm` 轨
3. 当 slot 已绑定时：
   - 继续生成当前稳定的 `bgm` 轨
4. 不处理：
   - section 级 visual slot 裁剪
   - 整章 section 删除
   - artifacts 查询式绑定
   - batch 数据行驱动 slot

原因很直接：

- `bgm` 轨已经是当前 plan builder 中唯一天然 optional 的轨道
- 它的裁剪不需要引入新的 timeline 对齐算法
- 它能先验证 slot contract 是否真的需要进入 narrated manifest

## 建议 contract

第一版只建议加一个极小模型，不扩成通用模板语言：

```jsonc
{
  "bgm": {
    "slot": {
      "name": "bgm",
      "required": false
    },
    "path": "${bgmPath:-}",
    "gainDb": -18
  }
}
```

对应语义：

- `slot.name`
  - 只是稳定标识，不承担查询逻辑
- `required = false`
  - 缺失时允许不产出 `bgm` 轨
- 当前不支持 `required = true`
  - 因为 narrated-slides 已经可以直接用普通 `path` 表达“明确必须存在的 BGM”

这能把“可选资源”与“普通直接路径”区分开，但不把整套 slot language 一次性拉进来。

## owner 边界

## Core.Editing

owner 仍应固定在：

- `NarratedSlidesManifest`
- `NarratedSlidesPlanBuilder`
- 必要时新增的 narrated slot 解析 helper

职责：

- 持有 slot 的 manifest 语义
- 决定何时省略 `bgm` 轨
- 决定 `required = false` 的稳定默认行为

## Cli

CLI 只应继续负责：

- manifest 装载
- `${var}` overlay 装载
- 相对路径解析
- failure envelope

CLI 不应负责：

- 判断 slot 是否应裁剪
- 在 CLI 层手动删轨

## Core.Execution

Execution 不应因为这一步新增第二套逻辑。

它只继续消费裁剪后的稳定 plan：

- 有 `bgm` 轨就渲染
- 没有 `bgm` 轨就按既有 timeline 执行

## 测试建议

## Core 单测

至少覆盖：

- `bgm.slot.required = false` 且无绑定时，不生成 `bgm` 轨
- `bgm.slot.required = false` 且有绑定时，仍生成 `bgm` 轨
- slot 与直接 `path` 同时存在时的优先级
- 不合法 slot 定义的失败路径

## CLI 集成测试

至少覆盖：

- 不传可选 BGM 绑定时，`init-narrated-plan` 仍成功
- 传入绑定后，输出 plan 重新出现 `bgm` 轨
- 失败 envelope 仍保留 manifest 路径和 output 路径

## 文档验收

至少同步：

- `docs/roadmap.md`
- `docs/PROJECT_PROFILE.md`
- `docs/COMMAND_REFERENCE.md`
- `src/OpenVideoToolbox.Core/Editing/MODULE.md`
- `src/OpenVideoToolbox.Cli/MODULE.md`

## 不纳入项

本卡明确不纳入：

1. visual slot
2. section 级条件删除
3. artifact slot 查询式解析
4. `resolve-assets`
5. 数据驱动 `run-batch`
6. 图表后端
7. `.pptx` / Markdown 页面生成

只要实现开始触碰上述任一项，就应停止，重新立项，而不是偷偷塞进 `V2-P6-C8`。

## 回滚路径

如果 `slot` 这一方向在实现中证伪，回滚应局限在：

1. narrated manifest 的 slot 字段
2. narrated plan builder 的 optional `bgm` 轨裁剪逻辑
3. 对应 Core / CLI 测试
4. 本计划稿与 roadmap 状态

不应影响：

- 已落地的 narrated-slides 主路径
- `image` section
- `progressBar`
- `${var}` foundation

## 当前结论

下一步如果继续 narrated-slides，最合理的顺序是：

1. 先停在 `V2-P6-C7`，把 slot 条件裁剪范围缩到“可选 BGM 轨”
2. 若继续实现，再进入 `V2-P6-C8`
3. `V2-P6-C8` 完成后再决定是否值得继续扩到 visual slot 或 batch，而不是现在提前打包
