# V2-P5 验收清单

最后更新：2026-04-25

## 说明

本清单对应当前 `V2-P5` 已落地范围：

- 首个 built-in v2 模板
- `template.planModel`
- `templates -> init-plan -> render --preview` 的真实 v2 模板闭环
- `templates -> init-plan --seed-from-transcript/--seed-from-beats -> render --preview` 的 v2 seed 闭环
- `auto-cut-silence -> render --preview` 的首条信号驱动 v2 plan 闭环
- `export --plan <edit.json> --format edl --output <path>` 的 v1/v2 统一导出闭环

本清单仍作为阶段验收材料保留，不要求在每张内部任务卡后强制停顿。

## Step 1：查看模板发现结果

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  templates `
  timeline-effects-starter
```

通过标准：

- `template.id = "timeline-effects-starter"`
- `template.planModel = "v2Timeline"`
- `template.recommendedSeedModes` 包含：
  - `manual`
  - `transcript`
  - `beats`
- `examples.previewPlans` 包含：
  - `manual`
  - `transcript`
  - `beats`
- 所有 `examples.previewPlans[*].editPlan.schemaVersion = 2`

## Step 2：生成真实 v2 plan

```powershell
$root = Join-Path $env:TEMP ("ovt-v2-p5-check-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $root | Out-Null

@'
{
  "bgm": "audio\\bed.wav"
}
'@ | Set-Content -LiteralPath (Join-Path $root "artifacts.json") -Encoding UTF8

dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  init-plan `
  input.mp4 `
  --template timeline-effects-starter `
  --output (Join-Path $root "edit.v2.json") `
  --render-output final.mp4 `
  --artifacts (Join-Path $root "artifacts.json")
```

通过标准：

- 返回 `editPlan.schemaVersion = 2`
- `editPlan.timeline` 存在
- `editPlan.clips` 为空数组
- `editPlan.artifacts[0].slotId = "bgm"`
- `editPlan.timeline.tracks` 至少包含：
  - `main`
  - `bgm`

## Step 3：检查 transcript seed 也会产出真实 v2 timeline clips

```powershell
@'
{
  "schemaVersion": 1,
  "language": "en",
  "segments": [
    { "id": "seg-001", "start": "00:00:00", "end": "00:00:01.2000000", "text": "Hello" },
    { "id": "seg-002", "start": "00:00:01.2000000", "end": "00:00:02.4000000", "text": "World" }
  ]
}
'@ | Set-Content -LiteralPath (Join-Path $root "transcript.json") -Encoding UTF8

dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  init-plan `
  input.mp4 `
  --template timeline-effects-starter `
  --output (Join-Path $root "edit.v2.transcript.json") `
  --render-output final.mp4 `
  --transcript (Join-Path $root "transcript.json") `
  --seed-from-transcript
```

通过标准：

- 返回 `editPlan.schemaVersion = 2`
- `editPlan.clips` 仍为空数组
- `editPlan.timeline.tracks[0].clips` 数量为 `2`
- `editPlan.timeline.tracks[0].clips[0].effects[0].type = "brightness_contrast"`

## Step 4：检查 timeline/effects 真实形状

直接打开上一步输出的 `edit.v2.json`，确认至少有这些字段：

- `timeline.tracks[0].effects[0].type = "scale"`
- `timeline.tracks[0].clips[0].effects[0].type = "brightness_contrast"`
- `timeline.tracks[0].clips[0].transitions.out.type = "fade"`
- `timeline.tracks[1].clips[0].effects[0].type = "volume"`

通过标准：

- 这些 effect / transition 出现在真实输出 plan 中，而不是只存在于说明文档

## Step 5：检查 render preview 可消费模板产物

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  render `
  --plan (Join-Path $root "edit.v2.json") `
  --preview
```

通过标准：

- 返回 `command = "render"`
- `preview = true`
- `payload.render.schemaVersion = 2`
- `payload.executionPreview.commandPlan.schemaVersion = 2`
- `payload.executionPreview.commandPlan.arguments` 包含：
  - `-filter_complex`
  - `[v_out]`

## Step 6：确认 v1 模板仍未被打坏

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  templates `
  shorts-captioned
```

通过标准：

- `template.planModel = "v1"`
- `examples.previewPlans` 仍包含：
  - `manual`
  - `transcript`
  - `beats`
- `examples.previewPlans[*].editPlan.schemaVersion` 仍为 `1`

## Step 7：确认 auto-cut-silence 可接到 v2 模板

```powershell
$root = Join-Path $env:TEMP ("ovt-v2-p5-autocut-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $root | Out-Null

@'
{
  "schemaVersion": 1,
  "inputPath": "input.mp4",
  "segments": [
    { "start": "00:00:02", "end": "00:00:03", "duration": "00:00:01" }
  ]
}
'@ | Set-Content -LiteralPath (Join-Path $root "silence.json") -Encoding UTF8

dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  auto-cut-silence `
  --silence (Join-Path $root "silence.json") `
  --source-duration-ms 5000 `
  --template timeline-effects-starter `
  --render-output final.mp4 `
  --output (Join-Path $root "edit.autocut.v2.json")
```

通过标准：

- 返回 `payload.autoCutSilence.mode = "plan"`
- 返回 `payload.result.plan.schemaVersion = 2`
- 输出文件 `edit.autocut.v2.json` 中：
  - `timeline.tracks[0].effects[0].type = "scale"`
  - `timeline.tracks[0].clips` 数量为 `2`
  - `timeline.tracks[0].clips[0].effects[0].type = "brightness_contrast"`

## Step 8：确认 render preview 可消费 auto-cut-silence 生成的 v2 plan

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  render `
  --plan (Join-Path $root "edit.autocut.v2.json") `
  --preview
```

通过标准：

- 返回 `command = "render"`
- `payload.executionPreview.commandPlan.schemaVersion = 2`
- `payload.executionPreview.commandPlan.arguments` 包含 `-filter_complex`

## Step 9：确认 v1 plan 可导出为 EDL

```powershell
$root = Join-Path $env:TEMP ("ovt-v2-p5-export-v1-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $root | Out-Null

@'
{
  "schemaVersion": 1,
  "source": { "inputPath": "input.mp4" },
  "clips": [
    { "id": "clip-001", "in": "00:00:00", "out": "00:00:02", "label": "intro" },
    { "id": "clip-002", "in": "00:00:05", "out": "00:00:06" }
  ],
  "output": { "path": "final.mp4", "container": "mp4" }
}
'@ | Set-Content -LiteralPath (Join-Path $root "edit.v1.json") -Encoding UTF8

dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  export `
  --plan (Join-Path $root "edit.v1.json") `
  --format edl `
  --output (Join-Path $root "out" "v1.edl")
```

通过标准：

- 返回 `command = "export"`
- `payload.export.format = "edl"`
- `payload.export.fidelityLevel = "L1"`
- `payload.export.eventCount = 2`
- `payload.export.warnings[*].code` 包含：
  - `export.plan.v1Wrapped`
  - `export.frameRate.defaulted`
- 输出文件 `out\v1.edl` 存在

## Step 10：确认 v2 plan 可导出主视频轨并显式返回 warning

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  export `
  --plan (Join-Path $root "edit.autocut.v2.json") `
  --format edl `
  --output (Join-Path $root "out" "v2.edl")
```

通过标准：

- 返回 `command = "export"`
- `payload.export.format = "edl"`
- `payload.export.fidelityLevel = "L1"`
- `payload.export.eventCount` 等于 `edit.autocut.v2.json` 中主视频轨 clips 数量
- `payload.export.warnings[*].code` 至少包含：
  - `export.timeline.effectsIgnored`
- 输出文件 `out\v2.edl` 存在

## Step 11：确认 export 的 `--json-out` / overwrite failure 契约

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  export `
  --plan (Join-Path $root "edit.autocut.v2.json") `
  --format edl `
  --output (Join-Path $root "out" "v2.edl") `
  --json-out (Join-Path $root "out" "export-result.json") `
  --overwrite
```

然后重复执行一次，但去掉 `--overwrite`。

通过标准：

- 首次执行：
  - `export-result.json` 成功写出
  - `export-result.json` 与 stdout envelope 结构一致
- 第二次执行：
  - 返回非零退出码
  - 仍返回结构化 envelope
  - `payload.error.message` 明确说明输出文件已存在

## 阶段验收通过条件

只有同时满足以下条件，才建议把 `V2-P5` 标为人工验收通过：

1. v2 模板发现、生成、preview 三条正式路径全部可重复跑通
2. `auto-cut-silence --template timeline-effects-starter` 的信号驱动 v2 plan 路径可重复跑通
3. `export L1` 能同时消费：
   - v1 plan
   - v2 plan
4. `export` 的 warning / failure 契约不是口头说明，而是能在真实 CLI 输出中看到
5. 现有 v1 模板仍未被打坏

## 本次验收结果（2026-04-25）

本清单已于 `2026-04-25` 在本地逐条执行，结果为：**通过**

- Step 1 通过
  - `timeline-effects-starter` 返回 `template.planModel = "v2Timeline"`
  - `recommendedSeedModes` 与 `previewPlans` 均包含 `manual / transcript / beats`
  - `previewPlans[*].editPlan.schemaVersion = 2`
- Step 2 通过
  - `init-plan --template timeline-effects-starter` 成功写出真实 `schemaVersion = 2` 的 `edit.v2.json`
  - 输出 plan 保持 `clips = []`，并包含 `timeline.tracks = ["main", "bgm"]`
- Step 3 通过
  - `--seed-from-transcript` 成功写出真实 v2 timeline clips
  - 主视频轨 clip 数量为 `2`，首个 clip effect 为 `brightness_contrast`
- Step 4 通过
  - 真实输出 plan 中确认存在 `scale / brightness_contrast / fade / volume`
- Step 5 通过
  - `render --plan edit.v2.json --preview` 成功返回 `schemaVersion = 2` 的 execution preview
  - `commandPlan.arguments` 包含 `-filter_complex` 与 `[v_out]`
- Step 6 通过
  - `shorts-captioned` 仍返回 `planModel = "v1"`
  - `previewPlans[*].editPlan.schemaVersion` 仍为 `1`
- Step 7 通过
  - `auto-cut-silence --template timeline-effects-starter` 成功写出 `schemaVersion = 2` 的 plan
  - 主视频轨 clip 数量为 `2`，并保留模板的 `scale` / `brightness_contrast`
- Step 8 通过
  - `render --plan edit.autocut.v2.json --preview` 成功返回 v2 command preview
- Step 9 通过
  - v1 plan 成功导出 `EDL`
  - 返回 `fidelityLevel = "L1"`、`eventCount = 2`
  - warning 包含 `export.plan.v1Wrapped`、`export.frameRate.defaulted`
- Step 10 通过
  - v2 plan 成功导出 `EDL`
  - 返回 `fidelityLevel = "L1"`、`eventCount = 2`
  - warning 包含 `export.timeline.effectsIgnored`
- Step 11 通过
  - `--json-out` 成功写出，与 stdout envelope 保持同一结构
  - 重复执行且不带 `--overwrite` 时返回退出码 `1`
  - failure stdout 仍返回结构化 envelope，且 `payload.error.message` 明确说明输出文件已存在

## 验收输出建议

如果以上步骤都通过，建议阶段验收输出为：

```text
阶段：V2-P5
阶段验收结果：通过
人工确认结论：
- 首个 built-in v2 模板工作流已可正式手测
- signal-driven v2 planner 路径已可正式手测
- export L1 已具备 v1 / v2 统一导出能力
后续动作：
- 关闭 V2-P5
- 再决定是否进入下一阶段或补 parity / 迁移文档
```

## 当前不在本清单内

- 插件 v2 模板
- 新 effect 类型扩面
- 数据驱动 batch
