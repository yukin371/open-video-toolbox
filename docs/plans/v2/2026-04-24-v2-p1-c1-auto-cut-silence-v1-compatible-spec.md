# V2-P1-C1 auto-cut-silence v1-compatible 规格稿

最后更新：2026-04-24

## 卡片信息

```text
卡片编号：V2-P1-C1
所属阶段：V2-P1
卡片类型：规格
目标：为 auto-cut-silence 固定 v1-compatible 范围、输入约束、输出形态与 owner 边界
输入：docs/plans/v2/2026-04-24-ai-intelligent-workflows-design.md、当前 detect-silence / MediaProbe / EditPlan 能力
输出：本规格稿
完成标准：明确本轮不依赖 schema v2、不进入 timeline、不改 render 主路径
阻塞条件：如果需求要求输出 timeline、track/effect 或自动接入 render 主路径，则本卡失效并需升级到 V2-P2+
完成后下一张卡：V2-P1-C2
```

## 背景

当前仓库已经具备两段可复用能力：

- `detect-silence`
  - 由 `OpenVideoToolbox.Core.Audio` 持有 `silence.json` 结构与解析语义
- `EditPlan` / `EditClip`
  - 由 `OpenVideoToolbox.Core.Editing` 持有 `schemaVersion = 1` 的正式计划模型

因此 `auto-cut-silence` 可以被拆成一个新的显式 CLI 辅助命令：

```text
silence.json + 源素材总时长
  ↓
生成 v1 clips 或 v1 edit plan
```

这条链路当前适合作为 `V2-P1` 孵化项，因为它可以直接服务现有高频工作流，但前提是必须守住：

1. 不把输出升级成 `schemaVersion = 2`
2. 不把生成结果偷偷接成 `timeline`
3. 不让 `render` 主路径自动长出新的隐式前处理

## 当前能力基线

当前仓库已经具备：

- `SilenceDetectionDocument`
  - 含 `inputPath` 与 `segments[]`
- `MediaProbeResult`
  - 可通过 `Core.Media` 获取源素材总时长
- `EditPlan`
  - 可表达顶层 `source / clips / output / template`
- `validate-plan`
  - 可校验生成后的 v1 plan 是否仍合法

当前明确还没有的能力包括：

- 从 `silence.json` 直接反推非静音 clips 的核心算法
- `auto-cut-silence` 命令
- clips-only 输出模式
- 与 `detect-silence` 配套的可审计建议输出

## 本轮目标

本轮 `V2-P1-C1` 只负责把 `auto-cut-silence` 收敛成一个**仍然属于 v1 的显式辅助命令**。

本轮目标是：

1. 让外部 AI 或人工可以基于 `silence.json` 快速生成首版去静音 clips
2. 输出仍然是现有 `EditClip[]` 或标准 `schemaVersion = 1` plan
3. 复用既有 owner，而不是新开 timeline / render-v2 实现线

## 本轮范围

本轮只允许后续 `V2-P1-C2/C3` 讨论以下能力。

### 1. 输入约束

命令至少消费：

- `--silence <silence.json>`

源素材路径默认来自：

- `silence.json.inputPath`

源素材总时长允许两种来源：

1. 显式传入时长覆盖
2. 通过 `Core.Media` 对 `silence.json.inputPath` 做探测

要求：

- 总时长来源必须显式、可解释、可测试
- 不允许在 CLI 层自己发明媒体探测逻辑
- 若 `silence.json.inputPath` 为空且未提供显式时长来源，本命令必须失败，而不是猜测

### 2. clips 生成算法

允许的算法范围只限于：

1. 以静音段反推出非静音区间
2. 对每段应用 padding
3. 对小间隔片段应用 merge gap
4. 丢弃低于最小时长的片段
5. 生成稳定、确定性的 `EditClip[]`

要求：

- 算法只基于时间信息，不引入内容理解或 AI 判定
- clip id 生成必须稳定、可重复
- 片段边界必须做 clamp，不允许超出 `[0, sourceDuration]`

### 3. 输出模式

允许两种输出模式：

1. `clips-only`
   - 输出 `EditClip[]`
2. `plan`
   - 输出标准 `schemaVersion = 1` 的 `EditPlan`

生成完整 plan 时允许带：

- `source`
- `clips`
- `output`
- 可选 `template`

要求：

- 不写 `timeline`
- 不写 `tracks / effects / transitions`
- 不把输出默认为 `schemaVersion = 2`

## 本轮不做

以下内容明确不属于 `V2-P1-C1`：

1. 不引入 `schemaVersion = 2`
2. 不输出 `timeline` 或多轨结构
3. 不自动把 `auto-cut-silence` 嵌进 `render` 或 `run` 主路径
4. 不按 silence 结果自动生成音轨、字幕或 artifact 绑定
5. 不引入“内容理解式”剪辑，例如自动判断句子完整性、镜头价值或语义优先级
6. 不处理 `resolve-assets`、数据驱动批量或图表渲染

## 推荐的命令形态

本轮推荐维持一个新的显式命令，而不是改写 `detect-silence` 的输出语义。

建议形态：

```text
auto-cut-silence --silence <silence.json>
                 [--output <path>]
                 [--clips-only]
                 [--padding-ms <n>]
                 [--merge-gap-ms <n>]
                 [--min-clip-duration-ms <n>]
                 [--source-duration-ms <n>]
                 [--ffprobe <path>]
                 [--template <id>]
                 [--render-output <path>]
                 [--json-out <path>]
```

说明：

- `--clips-only`
  - 输出 `EditClip[]`
- 不带 `--clips-only`
  - 输出完整 `schemaVersion = 1` plan
- `--source-duration-ms`
  - 作为显式覆盖，优先级高于探测
- `--ffprobe`
  - 只作为时长探测 override，不改变 owner

## 推荐的输出形态

### clips-only

```jsonc
{
  "sourcePath": "raw/interview.mp4",
  "sourceDuration": "00:00:15",
  "clips": [
    { "id": "clip-001", "in": "00:00:00.200", "out": "00:00:03.300" },
    { "id": "clip-002", "in": "00:00:05.000", "out": "00:00:07.800" }
  ],
  "stats": {
    "generatedClipCount": 2,
    "removedSilenceCount": 2
  }
}
```

### plan

```jsonc
{
  "schemaVersion": 1,
  "source": {
    "inputPath": "raw/interview.mp4"
  },
  "template": {
    "id": "shorts-basic"
  },
  "clips": [
    { "id": "clip-001", "in": "00:00:00.200", "out": "00:00:03.300" },
    { "id": "clip-002", "in": "00:00:05.000", "out": "00:00:07.800" }
  ],
  "output": {
    "path": "output/interview-autocut.mp4",
    "container": "mp4"
  }
}
```

要求：

- 完整模式输出必须能继续被现有 `validate-plan` 和 `render` 消费
- clips-only 模式输出必须与现有 `EditClip` 结构一致，而不是发明第二套 clip schema

## owner 约束

### Core.Audio 必须拥有

- `silence.json` 的结构语义
- 静音区间解析结果

### Core.Media 必须拥有

- 源素材总时长的探测语义
- `ffprobe` 调用与错误上下文

### Core.Editing 必须拥有

- 非静音 clips 生成算法
- `EditClip[]` / `EditPlan` 产出语义
- 生成结果的稳定字段约定

### Cli 只能拥有

- 参数解析
- 输出模式选择
- envelope / `--json-out` 写出
- 退出码映射

如果后续计划卡发现需要在 CLI 里单独维护：

- clip 边界算法
- clip id 规则
- source duration fallback 逻辑

则说明范围已经越界，应停下重判。

## 后续计划卡必须回答的问题

进入 `V2-P1-C2` 时，必须回答以下问题：

1. 总时长缺失时，默认走 `ffprobe` 还是直接要求显式传参？
2. clips-only 模式是只输出 `clips[]`，还是输出带 `stats` 的结构化 envelope payload？
3. 完整 plan 模式下，`output.path` 的默认值如何确定，还是要求显式传入？
4. 该命令入口更适合放在 `AudioCommandHandlers` 还是拆成新的命令族文件？
5. 需要补哪些最小回归测试与契约快照？

## 测试与验收边界

本轮规格要求后续至少准备以下测试面：

1. 多段静音反转后的 clips 生成
2. padding / merge gap / min clip duration 规则
3. 未提供总时长且无法探测时的失败路径
4. clips-only 输出结构
5. 完整 plan 输出可继续通过 `validate-plan`

## 必须停下重判的情况

出现以下任一情况时，不应继续把这张卡推进到实现：

1. 需求要求输出 `timeline` 或多轨结构
2. 需求要求把命令自动接入 `render` / `run`
3. 需求要求引入“语义理解式”自动剪辑
4. 需求要求连带实现 `resolve-assets` 或数据驱动批量逻辑

## 当前结论

`auto-cut-silence` 适合作为 `V2-P1` 的下一张规格卡，前提是严格收敛为：

- 一个新的显式 CLI 辅助命令
- 复用 `Core.Audio` + `Core.Media` + `Core.Editing`
- 输出仍然是 `EditClip[]` 或 `schemaVersion = 1` 的标准 plan

必须守住的一点是：

> 这是一张 `v1-compatible` 规格卡，不是 `timeline` 或 `schema v2` 的前置伪装。
