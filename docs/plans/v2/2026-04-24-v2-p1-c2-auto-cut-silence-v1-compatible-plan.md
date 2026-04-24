# V2-P1-C2 auto-cut-silence v1-compatible 计划稿

最后更新：2026-04-24

## 卡片信息

```text
卡片编号：V2-P1-C2
所属阶段：V2-P1
卡片类型：计划
目标：拆分 auto-cut-silence 的实现顺序、owner、文件范围、测试面与回滚点
输入：docs/plans/v2/2026-04-24-v2-p1-c1-auto-cut-silence-v1-compatible-spec.md、当前 detect-silence / probe / EditPlan 代码事实
输出：本计划稿
完成标准：明确 Core / Cli 分工、优先实现路径、测试清单与失败回滚方式
阻塞条件：如果命令必须依赖 schema v2、timeline 或自动接入 render 主路径，则本卡失效并需升级到 V2-P2+
完成后下一张卡：V2-P1-C3
```

## 计划结论

`auto-cut-silence` 的最小可执行实现应拆成三层：

1. `Core.Editing`
   - 持有非静音区间反推算法
   - 持有 `EditClip[]` 与完整 `EditPlan` 的生成语义
2. `Core.Media`
   - 继续持有源素材时长探测
3. `Cli`
   - 只持有参数解析、模式选择、JSON envelope 与退出码

当前最小落地目标不是“自动剪辑器”，而是：

- 给定 `silence.json`
- 明确总时长来源
- 产出稳定的 `EditClip[]` 或 `schemaVersion = 1` 的 plan

## owner 与文件范围

### Core.Editing

本轮建议新增：

- `src/OpenVideoToolbox.Core/Editing/AutoCutSilencePlanner.cs`
  - `AutoCutSilenceRequest`
  - `AutoCutSilenceResult`
  - `AutoCutSilencePlanner`

本轮可能需要轻量补充：

- `src/OpenVideoToolbox.Core/Editing/MODULE.md`
  - 新增命令背后的 canonical owner 说明

职责边界：

- 输入：
  - `SilenceDetectionDocument`
  - 已解析的 `sourcePath`
  - 已解析的 `sourceDuration`
  - padding / merge gap / min clip duration
  - 可选 template id / output path
- 输出：
  - 稳定的 `EditClip[]`
  - 可选完整 `EditPlan`
  - 统计字段，例如：
    - `generatedClipCount`
    - `removedSilenceCount`
    - `retainedDuration`

本轮不建议把算法放到：

- `Core.Audio`
  - 因为它不应拥有 plan 生成语义
- `Cli`
  - 因为这会复制 clip / plan 业务规则

### Core.Media

继续复用现有：

- `src/OpenVideoToolbox.Core/Media/FfprobeMediaProbeService.cs`
- `src/OpenVideoToolbox.Core/Media/MediaProbeResult.cs`

本轮不建议新增新的媒体探测 owner。

实现策略：

- 优先使用 `--source-duration-ms`
- 未提供时才通过 `Core.Media` 探测 `silence.inputPath`

### Cli

本轮建议先把命令入口放在：

- `src/OpenVideoToolbox.Cli/AudioCommandHandlers.cs`

原因：

1. 输入直接消费 `silence.json`
2. 与 `detect-silence` 同属 signal 驱动链路
3. 当前只新增一个命令，不值得立刻再拆新的命令族文件

需要改动的入口文件：

- `src/OpenVideoToolbox.Cli/AudioCommandHandlers.cs`
  - 新增 `RunAutoCutSilenceAsync`
- `src/OpenVideoToolbox.Cli/Program.cs`
  - 新增命令路由与 usage
- `src/OpenVideoToolbox.Cli/MODULE.md`
  - 同步命令 owner 与边界

如果后续这条线继续增长为：

- `auto-cut-silence`
- `auto-cut-beats`
- `auto-cut-transcript`

再考虑抽成新的 planning command owner；本轮先不做结构预支。

## 建议的实现顺序

### Step 1. Core 算法与输出模型

先在 `Core.Editing` 落：

1. 请求模型
2. 结果模型
3. 非静音区间生成算法
4. `clips-only` 输出
5. 完整 `EditPlan` 输出

算法顺序建议固定为：

1. 对 `segments[]` 按起止时间排序
2. 反推非静音区间
3. 对每段应用 padding
4. 做边界 clamp
5. 对 gap 小于阈值的片段做合并
6. 丢弃低于最小时长的片段
7. 生成稳定 clip id

clip id 规则建议先固定为：

```text
clip-001
clip-002
clip-003
```

理由：

- 稳定
- 与输入内容解耦
- 快照友好

### Step 2. CLI 参数与时长来源接线

在 `Cli` 落：

- `--silence`
- `--clips-only`
- `--padding-ms`
- `--merge-gap-ms`
- `--min-clip-duration-ms`
- `--source-duration-ms`
- `--template`
- `--output`
- `--render-output`
- `--ffprobe`
- `--json-out`

默认策略建议：

- `clips-only = false`
- `padding-ms = 200`
- `merge-gap-ms = 500`
- `min-clip-duration-ms = 1000`

时长来源顺序建议：

1. `--source-duration-ms`
2. `ffprobe` 探测 `silence.inputPath`
3. 失败则命令返回结构化 error

### Step 3. 输出 envelope 固定

建议统一采用 command envelope，而不是裸写数组到 stdout。

建议形态：

```jsonc
{
  "command": "auto-cut-silence",
  "preview": false,
  "payload": {
    "mode": "clipsOnly",
    "sourcePath": "...",
    "sourceDuration": "...",
    "clips": [...],
    "stats": {...}
  }
}
```

如果是完整 plan 模式：

```jsonc
{
  "command": "auto-cut-silence",
  "preview": false,
  "payload": {
    "mode": "plan",
    "sourcePath": "...",
    "sourceDuration": "...",
    "plan": {...},
    "stats": {...}
  }
}
```

说明：

- stdout / `--json-out` 继续复用统一 envelope helper
- `--output` 仅用于写盘 clips JSON 或 plan JSON
- command payload 内仍返回结构化结果，便于脚本与外部 AI 消费

### Step 4. 文档与帮助同步

需要同步：

- `README.md`
  - 只补最小使用方式，不展开成大段设计文
- `docs/PROJECT_PROFILE.md`
  - 若命令正式落地，补 CLI 命令面
- `src/OpenVideoToolbox.Cli/MODULE.md`
  - 新增命令 owner
- `src/OpenVideoToolbox.Core/Editing/MODULE.md`
  - 新增 planner owner

## 参数与行为决策

### 1. 总时长来源

当前结论：

- 默认不强制用户自己提供时长
- 但必须保持来源可解释

因此计划采用：

1. `--source-duration-ms` 优先
2. 否则通过 `Core.Media` 探测 `silence.inputPath`
3. 探测失败时返回结构化错误，不退回 usage

### 2. 完整 plan 模式输出

当前结论：

- 允许生成完整 `schemaVersion = 1` plan
- 但不强制引入 template

建议规则：

- 未传 `--template`
  - 生成无 template 的标准 plan
- 传了 `--template`
  - 仅填顶层 `template.id`
- 未传 `--output`
  - stdout 仍给 envelope，磁盘不落计划文件
- 若要写 plan 文件
  - 显式要求 `--output`

### 3. output path / render output

当前结论：

- `--output`
  - 表示写盘目标
- `--render-output`
  - 仅在完整 plan 模式下用于填 `plan.output.path`

若完整 plan 模式未给 `--render-output`，建议默认：

- `output/final.mp4`

但这项默认值仍应在执行卡开始前再确认；若担心默认路径过重，也可改成：

- 完整 plan 模式要求显式 `--render-output`

本轮计划建议保守一些，优先采用：

- 完整 plan 模式要求显式 `--render-output`

理由：

- 更少隐式路径策略
- 更符合现有仓库“输出路径显式”的习惯

### 4. 失败路径

建议固定以下结构化失败场景：

1. `silence.json` 解析失败
2. `silence.inputPath` 缺失且未给显式覆盖
3. 总时长无法解析 / 无法探测
4. 输出模式与参数组合冲突
5. 生成后 clips 为空

是否把“空结果”视为 error，当前建议：

- 不直接视为执行失败
- 但 payload 应显式返回：
  - `generatedClipCount = 0`
  - warning 或 summary message

## 测试计划

### Core.Tests

建议新增：

- `src/OpenVideoToolbox.Core.Tests/AutoCutSilencePlannerTests.cs`

最小测试面：

1. 静音段反转为非静音 clips
2. padding 后会被 clamp 到合法范围
3. merge gap 会合并相邻片段
4. min clip duration 会过滤过短片段
5. clips id 稳定
6. 可生成完整 `schemaVersion = 1` plan

### Cli.Tests

建议新增：

- `src/OpenVideoToolbox.Cli.Tests/CommandArtifactsIntegrationTests.AutoCutSilenceCommands.cs`
- 如输出结构需要冻结，再补 snapshot

最小测试面：

1. 缺少 `--silence` 的错误路径
2. `--clips-only` 模式成功输出
3. 完整 plan 模式成功输出
4. `--source-duration-ms` 覆盖路径
5. `ffprobe` 失败时的结构化错误路径

### 契约快照

建议只冻结一组 machine-independent 结构，不冻结路径。

理由：

- 这个命令会天然涉及本地路径
- 延续现有 `validate-plan` / `inspect-plan` 的快照思路更稳

## 文档同步点

进入执行卡后，至少需要同步：

1. `README.md`
2. `docs/PROJECT_PROFILE.md`
3. `docs/roadmap.md`
4. `src/OpenVideoToolbox.Cli/MODULE.md`
5. `src/OpenVideoToolbox.Core/Editing/MODULE.md`

如果最终决定把命令继续留在 `AudioCommandHandlers.cs`，则不需要新增新的 CLI 命令族文档。

## 回滚路径

如果执行中发现问题，优先按以下顺序回滚：

1. 回滚 CLI 新命令入口
2. 保留 `Core.Editing` 中纯算法与模型
3. 若算法模型也证明不稳定，再整体回退本命令

不建议的回滚方式：

- 让命令继续存在，但偷偷改成输出 `timeline`
- 让 CLI 直接实现临时算法绕过 `Core`
- 让 `render` 自动隐式消费 `silence.json`

## 当前结论

`V2-P1-C3` 的执行应以“先 Core、后 CLI、最后文档与快照”的顺序推进。

本轮计划已经明确：

- canonical owner
- 目标文件范围
- 默认参数
- 总时长来源策略
- 最小测试与回滚路径

因此下一步可以进入 `V2-P1-C3`，但仍需守住一句话：

> `auto-cut-silence` 本轮只是显式的 v1 plan 生成辅助命令，不是 v2 自动剪辑入口。
