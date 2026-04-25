# V2-P6 narrated-slides 验收清单

最后更新：2026-04-25

## 说明

本清单覆盖 narrated-slides 当前实现：

- `init-narrated-plan`
- narrated manifest -> v2 `edit.json`
- `visual.kind = "image"`
- `video.progressBar`
- narrated `${var}`
- narrated `bgm.slot.required = false`
- narrated `sections[].visual.slot.required = false`
- `render --preview` 对该结果的消费

本清单不验收 section 删除、batch、图表或 `.pptx`。

## Step 1：准备最小样例目录

```powershell
$root = Join-Path $env:TEMP ("ovt-v2-p6-narrated-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $root | Out-Null
New-Item -ItemType Directory -Path (Join-Path $root "slides") | Out-Null
New-Item -ItemType Directory -Path (Join-Path $root "audio") | Out-Null
New-Item -ItemType Directory -Path (Join-Path $root "subtitles") | Out-Null

Set-Content -LiteralPath (Join-Path $root "slides\\intro.mp4") "video-intro"
Set-Content -LiteralPath (Join-Path $root "slides\\deep-dive.mp4") "video-deep-dive"
Set-Content -LiteralPath (Join-Path $root "audio\\intro.wav") "voice-intro"
Set-Content -LiteralPath (Join-Path $root "audio\\deep-dive.wav") "voice-deep-dive"
Set-Content -LiteralPath (Join-Path $root "audio\\bgm.mp3") "bgm"
@'
1
00:00:00,000 --> 00:00:01,000
Hello
'@ | Set-Content -LiteralPath (Join-Path $root "subtitles\\podcast.srt") -Encoding UTF8
```

通过标准：

- 目录下存在 `slides`、`audio`、`subtitles`
- 所有样例文件已写出

## Step 2：写入 narrated manifest

```powershell
@'
{
  "schemaVersion": 1,
  "video": {
    "id": "episode-01",
    "output": "exports/final.mp4"
  },
  "subtitles": {
    "path": "subtitles/podcast.srt",
    "mode": "sidecar"
  },
  "bgm": {
    "path": "audio/bgm.mp3",
    "gainDb": -16
  },
  "sections": [
    {
      "id": "intro",
      "title": "Intro",
      "visual": {
        "kind": "video",
        "path": "slides/intro.mp4",
        "durationMs": 5000
      },
      "voice": {
        "path": "audio/intro.wav",
        "durationMs": 3000
      }
    },
    {
      "id": "deep-dive",
      "title": "Deep Dive",
      "visual": {
        "kind": "video",
        "path": "slides/deep-dive.mp4",
        "durationMs": 7000
      },
      "voice": {
        "path": "audio/deep-dive.wav",
        "durationMs": 4000
      }
    }
  ]
}
'@ | Set-Content -LiteralPath (Join-Path $root "narrated.json") -Encoding UTF8
```

通过标准：

- `narrated.json` 成功写出
- 两个 section 都显式声明 `visual.kind = "video"` 和 `voice.path`

## Step 3：生成 narrated v2 plan

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  init-narrated-plan `
  --manifest (Join-Path $root "narrated.json") `
  --output (Join-Path $root "edit.v2.json") `
  --json-out (Join-Path $root "init-narrated-plan.json")
```

通过标准：

- 返回 `command = "init-narrated-plan"`
- 返回 `preview = false`
- `payload.template.id = "narrated-slides-starter"`
- `payload.planPath` 指向 `edit.v2.json`
- `payload.stats.sectionCount = 2`
- `payload.stats.hasSubtitles = true`
- `payload.stats.hasBgm = true`
- 输出文件 `edit.v2.json` 存在

## Step 4：检查输出 plan 形状

直接打开 `edit.v2.json`，至少确认：

- `schemaVersion = 2`
- `template.id = "narrated-slides-starter"`
- `template.source.kind = "builtIn"`
- `timeline.tracks` 包含：
  - `main`
  - `voice`
  - `bgm`
- `timeline.duration = "00:00:07"`
- `timeline.tracks[0].effects[0].type = "scale"`
- `timeline.tracks[2].clips[0].effects[0].type = "volume"`

通过标准：

- section 总时长按两段 voice 求和
- BGM 被投影为单独音轨，而不是回写到旧版 `audioTracks`

## Step 5：确认 render preview 可消费 narrated plan

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
- `payload.executionPreview.commandPlan.arguments` 包含 `-filter_complex`

## Step 6：确认 image section 也可稳定生成并进入 preview

```powershell
@'
{
  "schemaVersion": 1,
  "video": {
    "id": "episode-image"
  },
  "sections": [
    {
      "id": "cover",
      "title": "Cover",
      "visual": {
        "kind": "image",
        "path": "slides/cover.png"
      },
      "voice": {
        "path": "audio/intro.wav",
        "durationMs": 2500
      }
    }
  ]
}
'@ | Set-Content -LiteralPath (Join-Path $root "narrated.image.json") -Encoding UTF8

Set-Content -LiteralPath (Join-Path $root "slides\\cover.png") "image"

dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  init-narrated-plan `
  --manifest (Join-Path $root "narrated.image.json") `
  --output (Join-Path $root "edit.image.v2.json")

dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  render `
  --plan (Join-Path $root "edit.image.v2.json") `
  --preview
```

通过标准：

- `edit.image.v2.json` 成功写出
- `timeline.tracks[0].clips[0].src` 指向 `cover.png`
- `timeline.tracks[0].clips[0].duration = "00:00:02.5000000"`
- `render --preview` 返回的 `payload.executionPreview.commandPlan.arguments` 包含：
  - `-loop`
  - `1`
  - `-framerate`

## Step 7：确认缺失素材会返回结构化 failure envelope

```powershell
@'
{
  "schemaVersion": 1,
  "video": {},
  "sections": [
    {
      "id": "intro",
      "visual": {
        "kind": "video",
        "path": "slides/missing.mp4",
        "durationMs": 3000
      },
      "voice": {
        "path": "audio/missing.wav",
        "durationMs": 3000
      }
    }
  ]
}
'@ | Set-Content -LiteralPath (Join-Path $root "narrated.missing.json") -Encoding UTF8

dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  init-narrated-plan `
  --manifest (Join-Path $root "narrated.missing.json") `
  --output (Join-Path $root "edit.missing.v2.json")
```

通过标准：

- 进程返回非零退出码
- stdout 仍是结构化 JSON
- `command = "init-narrated-plan"`
- `payload.manifest.path` 指向失败 manifest
- stderr 含 `points to a missing file`

## Step 8：确认 progress bar 可投影到轨道 effect 并进入 preview

```powershell
@'
{
  "schemaVersion": 1,
  "video": {
    "id": "episode-progress",
    "progressBar": {
      "enabled": true,
      "height": 10,
      "margin": 24,
      "color": "yellow@0.9",
      "backgroundColor": "black@0.2"
    }
  },
  "sections": [
    {
      "id": "cover",
      "visual": {
        "kind": "image",
        "path": "slides/cover.png"
      },
      "voice": {
        "path": "audio/intro.wav",
        "durationMs": 2500
      }
    }
  ]
}
'@ | Set-Content -LiteralPath (Join-Path $root "narrated.progress.json") -Encoding UTF8

dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  init-narrated-plan `
  --manifest (Join-Path $root "narrated.progress.json") `
  --output (Join-Path $root "edit.progress.v2.json")

dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  render `
  --plan (Join-Path $root "edit.progress.v2.json") `
  --preview
```

通过标准：

- `edit.progress.v2.json` 成功写出
- `timeline.tracks[0].effects[*].type` 包含：
  - `scale`
  - `progress_bar`
- `render --preview` 返回的 `payload.executionPreview.commandPlan.arguments` 包含：
  - `drawbox=x=0`
  - `yellow@0.9`

## Step 9：确认 optional visual slot 可投影 placeholder 并进入 preview

```powershell
@'
{
  "schemaVersion": 1,
  "video": {
    "id": "episode-visual-slot",
    "output": "exports/final.mp4"
  },
  "sections": [
    {
      "id": "cover",
      "visual": {
        "kind": "image",
        "slot": {
          "name": "cover-visual",
          "required": false
        }
      },
      "voice": {
        "path": "audio/intro.wav",
        "durationMs": 2500
      }
    }
  ]
}
'@ | Set-Content -LiteralPath (Join-Path $root "narrated.visual-slot.json") -Encoding UTF8

dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  init-narrated-plan `
  --manifest (Join-Path $root "narrated.visual-slot.json") `
  --output (Join-Path $root "edit.visual-slot.v2.json")

dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  render `
  --plan (Join-Path $root "edit.visual-slot.v2.json") `
  --preview
```

通过标准：

- `edit.visual-slot.v2.json` 成功写出
- `source.inputPath` 回退到 `audio/intro.wav`
- `timeline.tracks[0].clips[0]` 不含 `src`
- `timeline.tracks[0].clips[0].placeholder.kind = "color"`
- `timeline.tracks[0].clips[0].placeholder.color = "black"`
- `timeline.tracks[0].clips[0].duration = "00:00:02.5000000"`
- `render --preview` 返回的 `payload.executionPreview.commandPlan.arguments` 包含：
  - `lavfi`
  - `color=c=black`
  - `[v_out]`
- `render --preview` 不返回 `-an`

## 结论

以上步骤全部通过，才可把当前 narrated-slides 当前实现视为：

- 已完成当前一轮实现
- 可进入 `V2-P6-C6` 人工反馈
- 仍保持在受控范围内
