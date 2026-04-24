# E2-F4 Batch Command Contract

最后更新：2026-04-24

## 目的

`E2-F4` 已经连续落地了多条 batch 命令，但它们的公共约定目前主要体现在代码和零散命令文档里。

这会带来两个问题：

- 维护者很难快速判断“哪些字段是跨命令公共 contract，哪些只是某个命令的私有参数”
- future Desktop 或外部脚本在消费 batch 结果时，容易再次从多个命令里反推规则

因此这份文档的目标是把当前**已经稳定落地**的 batch 公共 contract 单独冻结出来，作为 `E2-F4-W1` 的阶段性产物。

## 适用范围

当前适用的 batch 命令：

- `scaffold-template-batch`
- `render-batch`
- `replace-plan-material-batch`
- `attach-plan-material-batch`
- `bind-voice-track-batch`

这份文档只定义这些命令之间已经共享的 contract，不替代各命令的用户文档，也不扩展新的 batch 语义。

## 设计原则

### 1. batch 只做 manifest / 路径 / 汇总

batch 命令只负责：

- 读取 manifest
- 按 manifest 所在目录解析相对路径
- 调用对应单项语义
- 汇总 summary 与任务结果文件

单项 mutation / validation / render / scaffold 语义仍由原有 owner 持有。

### 2. summary 与 task result 是 batch 公共产物

所有 batch 命令都应把同一批次的结果固定落到：

```text
<manifest-dir>/
  batch.json
  summary.json
  results/
    <id>.json
```

这套结构当前既服务脚本，也作为 future Desktop 的最小批处理消费边界。

### 3. item 级 `id` 应稳定

新 batch manifest 默认应显式提供 `items[].id`。

原因：

- `id` 是 `results/<id>.json` 的文件名来源
- `id` 也是顶层 `results[]` 中最稳定的任务索引
- future Desktop 不应再靠数组顺序推导任务 identity

当前为了兼容旧 manifest，`bind-voice-track-batch` 仍允许未显式提供 `id`，这时会自动补一个稳定默认值：

```text
item-001
item-002
...
```

但这只是兼容路径，不是推荐新 contract。

## 顶层 manifest 公共约定

所有 batch manifest 共享的最小顶层结构：

```json
{
  "schemaVersion": 1,
  "items": []
}
```

稳定约定：

- `schemaVersion`
  - 当前固定为 `1`
- `items`
  - 至少包含一个条目
- manifest 内所有相对路径
  - 统一按 manifest 所在目录解析

## item 级公共字段

当前已经稳定复用的 item 级公共字段如下：

| 字段 | 作用 | 当前适用命令 |
| --- | --- | --- |
| `id` | 稳定任务标识、结果文件命名 | 全部 batch；`bind-voice-track-batch` 当前兼容缺失 |
| `writeTo` | 写出新的 plan 路径，而不是原地覆盖 | `replace-plan-material-batch`、`attach-plan-material-batch`、`bind-voice-track-batch` |
| `checkFiles` | 是否做文件存在性校验 | 素材类 batch、`scaffold-template-batch` |
| `requireValid` | 更新后的 plan 若校验失败则阻止写盘 | 素材类 batch |
| `pathStyle` | 路径写回风格 | 素材类 batch |

说明：

- “公共字段”表示字段名和语义已经在多个命令间收敛，不代表每个 batch 命令都必须支持它
- 命令私有字段仍由各自命令面定义，例如 `template`、`output`、`overwrite`、`trackId`

## 当前已落地命令的 item 字段

### `scaffold-template-batch`

```json
{
  "id": "job-a",
  "input": "inputs/a.mp4",
  "template": "shorts-captioned",
  "workdir": "tasks/job-a",
  "validate": true,
  "checkFiles": true
}
```

### `render-batch`

```json
{
  "id": "job-a",
  "plan": "tasks/job-a/edit.json",
  "output": "exports/job-a.mp4",
  "overwrite": true
}
```

### `replace-plan-material-batch`

```json
{
  "id": "job-a",
  "plan": "tasks/job-a/edit.json",
  "path": "audio/updated.wav",
  "audioTrackId": "voice-main",
  "writeTo": "outputs/job-a.edit.json",
  "pathStyle": "relative",
  "checkFiles": true,
  "requireValid": true
}
```

说明：

- replacement selector 当前与单项命令保持一致：
  - `sourceInput`
  - `transcript`
  - `beats`
  - `subtitles`
  - `audioTrackId`
  - `artifactSlot`

### `attach-plan-material-batch`

```json
{
  "id": "job-a",
  "plan": "tasks/job-a/edit.json",
  "path": "signals/transcript.json",
  "transcript": true,
  "writeTo": "outputs/job-a.edit.json",
  "pathStyle": "relative",
  "checkFiles": true,
  "requireValid": true
}
```

说明：

- attachment selector 当前与单项命令保持一致：
  - `transcript`
  - `beats`
  - `subtitles`
  - `audioTrackId`
  - `artifactSlot`
- `subtitleMode`
  - 仅适用于 `subtitles`
- `audioTrackRole`
  - 仅适用于 `audioTrackId`

### `bind-voice-track-batch`

```json
{
  "id": "job-a",
  "plan": "tasks/job-a/edit.json",
  "path": "audio/dub.wav",
  "trackId": "voice-main",
  "role": "voice",
  "writeTo": "outputs/job-a.edit.json",
  "pathStyle": "relative",
  "checkFiles": true,
  "requireValid": true
}
```

## summary.json 公共字段

所有 batch 命令的顶层 summary 当前都应包含以下公共字段：

- `manifestPath`
- `manifestBaseDirectory`
- `summaryPath`
- `itemCount`
- `succeededCount`
- `failedCount`
- `results`

部分命令还会额外带自己的顶层字段，例如：

- `render-batch`
  - `preview`

## results[] 公共字段

所有 batch 命令的 `results[]` 当前都应至少包含：

- `index`
- `id`
- `resultPath`
- `status`

成功时：

- `status = "succeeded"`
- `result`

失败时：

- `status = "failed"`
- `error`
- 如果单项语义已经产出结构化失败 payload，则还会保留 `result`

当前常见的补充字段包括：

- `planPath`
- `outputPlanPath`
- `inputPath`
- `templateId`
- `workdir`
- `outputPath`

这些字段当前仍允许按命令保留，不要求强行裁成完全同一份 schema。

## results/<id>.json 约定

任务级结果文件路径固定为：

```text
results/<id>.json
```

稳定约定：

- 成功条目：
  - 写入该条目的成功 payload
- 失败条目：
  - 至少写入结构化失败信息
- 顶层 `results[]` 中的 `resultPath`
  - 必须能直接指向这一文件

## 退出码公共约定

所有 batch 命令当前统一采用：

- `0`
  - 全部成功
- `2`
  - 只要有条目失败
- `1`
  - manifest 解析 / 装载失败，或命令级前置失败

## 当前明确不统一的部分

以下部分当前仍然保留命令差异，不要求在本阶段强行统一：

- item 私有业务字段
  - 例如 `template`、`output`、`overwrite`、`trackId`
- result 内部 payload 结构
  - 它们仍由各自单项命令 owner 决定
- 是否必须显式提供 `id`
  - 长期推荐全部显式提供
  - 当前只对 `bind-voice-track-batch` 保留兼容性兜底

## 与后续工作的关系

这份文档解决的是“batch 公共 contract 已经是什么”。

它不直接回答以下问题：

- 是否需要继续新增更多 batch 命令
- 是否应把 batch command handler 再拆分为更细 owner
- future Desktop 要不要直接读取这些文件，还是再包一层 adapter

这些问题进入后续 `E2-F4` 收尾或 `E2-G1` 重判时再处理。

## 当前结论

到当前为止，`E2-F4-W1` 至少已经达到“公共 contract 可文档化、可回归验证”的阶段。

后续如果再新增 batch 命令，默认先检查：

1. 是否继续复用 `schemaVersion + items[]`
2. 是否继续复用 manifest 相对路径解析
3. 是否继续写 `summary.json`
4. 是否继续写 `results/<id>.json`
5. 是否继续使用 `0 / 2 / 1` 退出码

只要任一答案是否定的，就不应直接实现，而应先更新这份文档并重新过一轮边界评估。
