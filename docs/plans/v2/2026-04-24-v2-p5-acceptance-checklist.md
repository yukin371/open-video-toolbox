# V2-P5 验收清单

最后更新：2026-04-24

## 说明

本清单对应当前 `V2-P5` 已落地范围：

- 首个 built-in v2 模板
- `template.planModel`
- `templates -> init-plan -> render --preview` 的真实 v2 模板闭环
- `templates -> init-plan --seed-from-transcript/--seed-from-beats -> render --preview` 的 v2 seed 闭环
- `auto-cut-silence -> render --preview` 的首条信号驱动 v2 plan 闭环

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

## 当前不在本清单内

- 插件 v2 模板
- 新 effect 类型扩面
- 数据驱动 batch
