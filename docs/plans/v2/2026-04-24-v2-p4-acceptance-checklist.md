# V2-P4 回归清单

最后更新：2026-04-24

## 说明

按当前工作方式，这份清单不再作为强制人工停顿点，而是作为回归材料保留。

本清单只覆盖 `V2-P4` 当前已落地范围：

- `render --preview` 的 schema v2 timeline 路径
- v2 render failure envelope
- v1 / v2 双轨并存

## Step 1：准备最小 v2 plan

```powershell
$root = Join-Path $env:TEMP ("ovt-v2-p4-check-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $root | Out-Null

@'
{
  "schemaVersion": 2,
  "source": { "inputPath": "input.mp4" },
  "timeline": {
    "duration": "00:00:03",
    "resolution": { "w": 1920, "h": 1080 },
    "frameRate": 30,
    "tracks": [
      {
        "id": "main",
        "kind": "video",
        "clips": [
          { "id": "clip-001", "start": "00:00:00", "in": "00:00:00", "out": "00:00:03" }
        ]
      }
    ]
  },
  "output": { "path": "final-v2.mp4", "container": "mp4" }
}
'@ | Set-Content -LiteralPath (Join-Path $root "edit.v2.json") -Encoding UTF8
```

## Step 2：检查 v2 render preview

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

## Step 3：检查 v2 render failure envelope

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  render `
  --plan (Join-Path $root "edit.v2.json") `
  --ffmpeg missing-ffmpeg
```

通过标准：

- 返回非零退出码
- stdout 仍是结构化 envelope
- `payload.executionPreview` 仍存在
- `payload.error.message` 中包含 `missing-ffmpeg`

## Step 4：确认 v1 仍可 preview

```powershell
@'
{
  "schemaVersion": 1,
  "source": { "inputPath": "input.mp4" },
  "clips": [
    { "id": "clip-001", "in": "00:00:00", "out": "00:00:02" }
  ],
  "output": { "path": "final-v1.mp4", "container": "mp4" }
}
'@ | Set-Content -LiteralPath (Join-Path $root "edit.v1.json") -Encoding UTF8

dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  render `
  --plan (Join-Path $root "edit.v1.json") `
  --preview
```

通过标准：

- v1 preview 继续成功
- 未出现 schema v2 强制字段要求

## 当前不在本清单内

- 插件 effect 执行
- 复杂 executor effect
- v2 template 正式示例
