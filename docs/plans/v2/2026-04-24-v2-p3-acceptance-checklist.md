# V2-P3 阶段验收清单

最后更新：2026-04-24

## 目的

这份清单只服务一件事：

> 让人工可以直接手动验证 `V2-P3` 当前阶段产物，而不是只看阶段总结。

本清单只覆盖 `V2-P3` 当前已落地范围：

- built-in effect catalog
- `effects list/describe`
- `validate-plan` 对 built-in / unknown effect type 的识别

不覆盖：

- 插件 effect 加载
- render builder 的 effect 执行
- v2 timeline render parity

## 验收原则

本轮验收采用最小可重复样例：

1. 不依赖真实媒体文件
2. 只验证当前用户可见的 CLI 行为
3. 重点确认：effect discovery 已可直接使用，且 validator 已能识别 built-in effect

建议在仓库根目录执行以下命令。

## 准备临时工作目录

```powershell
$root = Join-Path $env:TEMP ("ovt-v2-p3-acceptance-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $root | Out-Null
$root
```

记录输出目录，后续命令都复用它。

## Step 1：手测 `effects list --category audio`

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  effects list `
  --category audio `
  --json-out (Join-Path $root "effects-audio.json")
```

手动检查：

```powershell
Get-Content -Raw (Join-Path $root "effects-audio.json")
```

通过标准：

- 命令退出码为 `0`
- envelope 中：
  - `command = "effects"`
  - `payload.mode = "list"`
  - `payload.filters.category = "audio"`
  - `payload.count >= 3`
- `payload.effects[].type` 至少包含：
  - `auto_ducking`
  - `fade_audio`
  - `volume`
- `auto_ducking.templateMode = "executor"`

## Step 2：手测 `effects describe fade`

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  effects describe fade `
  --json-out (Join-Path $root "effect-fade.json")
```

手动检查：

```powershell
Get-Content -Raw (Join-Path $root "effect-fade.json")
```

通过标准：

- 命令退出码为 `0`
- envelope 中：
  - `payload.mode = "describe"`
  - `payload.effect.type = "fade"`
  - `payload.effect.category = "transition"`
  - `payload.effect.templateMode = "transitionTemplate"`
- `payload.effect.parameters.duration.type = "float"`
- `payload.effect.ffmpegTemplates.transitions.in = "fade=t=in:d={duration}:alpha=1"`
- `payload.effect.ffmpegTemplates.transitions.out = "fade=t=out:d={duration}:alpha=1"`

## Step 3：准备包含 built-in effect 的合法 v2 plan

```powershell
@'
{
  "schemaVersion": 2,
  "source": {
    "inputPath": "input.mp4"
  },
  "timeline": {
    "duration": "00:00:05",
    "resolution": { "w": 1920, "h": 1080 },
    "frameRate": 30,
    "tracks": [
      {
        "id": "main",
        "kind": "video",
        "effects": [
          {
            "type": "fade"
          }
        ],
        "clips": [
          {
            "id": "clip-001",
            "start": "00:00:00",
            "in": "00:00:00",
            "out": "00:00:03"
          }
        ]
      }
    ]
  },
  "output": {
    "path": "final.mp4",
    "container": "mp4"
  }
}
'@ | Set-Content -LiteralPath (Join-Path $root "known-effect.edit.json") -Encoding UTF8
```

通过标准：

- `$root\known-effect.edit.json` 已生成

## Step 4：手测 built-in effect 的 `validate-plan`

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  validate-plan `
  --plan (Join-Path $root "known-effect.edit.json") `
  --json-out (Join-Path $root "known-effect-envelope.json")
```

手动检查：

```powershell
Get-Content -Raw (Join-Path $root "known-effect-envelope.json")
```

通过标准：

- 命令退出码为 `0`
- envelope 中：
  - `payload.isValid = true`
  - `payload.checkMode = "basic"`
- `payload.issues[].code` 中不能出现：
  - `timeline.effect.type.unknown`

## Step 5：准备包含 unknown effect 的 v2 plan

```powershell
@'
{
  "schemaVersion": 2,
  "source": {
    "inputPath": "input.mp4"
  },
  "timeline": {
    "duration": "00:00:05",
    "resolution": { "w": 1920, "h": 1080 },
    "frameRate": 30,
    "tracks": [
      {
        "id": "main",
        "kind": "video",
        "effects": [
          {
            "type": "unknown_effect"
          }
        ],
        "clips": [
          {
            "id": "clip-001",
            "start": "00:00:00",
            "in": "00:00:00",
            "out": "00:00:03"
          }
        ]
      }
    ]
  },
  "output": {
    "path": "final.mp4",
    "container": "mp4"
  }
}
'@ | Set-Content -LiteralPath (Join-Path $root "unknown-effect.edit.json") -Encoding UTF8
```

通过标准：

- `$root\unknown-effect.edit.json` 已生成

## Step 6：手测 unknown effect warning 仍保留

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  validate-plan `
  --plan (Join-Path $root "unknown-effect.edit.json") `
  --json-out (Join-Path $root "unknown-effect-envelope.json")
```

手动检查：

```powershell
Get-Content -Raw (Join-Path $root "unknown-effect-envelope.json")
```

通过标准：

- 命令退出码为 `0`
- envelope 中：
  - `payload.checkMode = "basic"`
- `payload.issues[].code` 至少包含：
  - `timeline.effect.type.unknown`
- 该问题的 `severity = "warning"`
- 返回结果应来自结构化校验，而不是 parse failed

## 本阶段暂不要求手测的点

以下点当前由自动化测试或阶段边界约束覆盖，但不要求放进本轮手测验收：

- 插件 effect JSON 加载
- effect executor 运行时接线
- `render` / `mix-audio` 的 effect 执行路径

原因：

1. 插件 effect 与执行器接线都不在 `V2-P3` 范围内
2. `V2-P3` 只负责 descriptor / discovery，不负责 render parity

## 阶段验收通过条件

只有以下条件同时满足，才应把本轮 `V2-P3` 判为通过：

1. `effects list` 能稳定列出 built-in catalog
2. `effects describe` 能稳定返回单 effect 描述符
3. `validate-plan` 能识别 built-in effect，不再误报 unknown
4. `validate-plan` 仍会对 truly unknown effect 保留 warning
5. 以上结果都不要求把 effect 接进 render 主路径

## 验收输出建议

如果以上步骤都通过，建议阶段验收输出为：

```text
V2-P3: accepted
```

如果 discovery 能跑，但字段缺失、内置 effect 仍被判 unknown，或结果开始混入 render 语义，建议输出为：

```text
V2-P3: continue-P3
```

并明确指出失败的是哪一步、哪个字段。
