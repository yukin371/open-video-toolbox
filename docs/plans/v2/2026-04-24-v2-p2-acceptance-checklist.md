# V2-P2 阶段验收清单

最后更新：2026-04-24

## 目的

这份清单只服务一件事：

> 让人工可以直接手动验证 `V2-P2` 当前阶段产物，而不是只看阶段总结。

本清单只覆盖 `V2-P2` 当前已落地范围：

- `schemaVersion = 2` 的 `edit.json` 合约层
- `validate-plan` 对 v2 plan 的装载与结构校验

不覆盖：

- `render` 的 v2 执行路径
- effect discovery / registry CLI
- v2 模板输出

## 验收原则

本轮验收采用最小可重复样例：

1. 不依赖真实媒体文件
2. 只验证当前用户可见的 CLI 行为
3. 重点确认：v2 plan 不再在 parse 阶段失败，而是进入结构化校验

建议在仓库根目录执行以下命令。

## 准备临时工作目录

```powershell
$root = Join-Path $env:TEMP ("ovt-v2-p2-acceptance-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $root | Out-Null
$root
```

记录输出目录，后续命令都复用它。

## Step 1：准备合法 v2 plan

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
        "clips": [
          {
            "id": "clip-001",
            "start": "00:00:00",
            "in": "00:00:01",
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
'@ | Set-Content -LiteralPath (Join-Path $root "valid-v2.edit.json") -Encoding UTF8
```

通过标准：

- `$root\valid-v2.edit.json` 已生成

## Step 2：手测合法 v2 plan 的 `validate-plan`

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  validate-plan `
  --plan (Join-Path $root "valid-v2.edit.json") `
  --json-out (Join-Path $root "valid-envelope.json")
```

手动检查：

```powershell
Get-Content -Raw (Join-Path $root "valid-envelope.json")
```

通过标准：

- 命令退出码为 `0`
- envelope 中：
  - `command = "validate-plan"`
  - `payload.checkMode = "basic"`
  - `payload.isValid = true`
  - `payload.stats.totalIssues = 0`
  - `payload.issues` 为空数组
- 结果不能再出现：
  - `plan.parse.failed`
  - `Unsupported edit plan schema version '2'`

## Step 3：准备非法 v2 plan

```powershell
@'
{
  "schemaVersion": 2,
  "source": {
    "inputPath": "input.mp4"
  },
  "timeline": {
    "resolution": { "w": 0, "h": 1080 },
    "frameRate": 0,
    "tracks": [
      {
        "id": "main",
        "kind": "video",
        "clips": [
          {
            "id": "clip-001",
            "start": "-00:00:01"
          },
          {
            "id": "clip-001",
            "start": "00:00:01"
          }
        ]
      },
      {
        "id": "main",
        "kind": "audio",
        "clips": []
      }
    ]
  },
  "output": {
    "path": "final.mp4",
    "container": "mp4"
  }
}
'@ | Set-Content -LiteralPath (Join-Path $root "invalid-v2.edit.json") -Encoding UTF8
```

通过标准：

- `$root\invalid-v2.edit.json` 已生成

## Step 4：手测非法 v2 plan 的 `validate-plan`

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  validate-plan `
  --plan (Join-Path $root "invalid-v2.edit.json") `
  --json-out (Join-Path $root "invalid-envelope.json")
```

手动检查：

```powershell
Get-Content -Raw (Join-Path $root "invalid-envelope.json")
```

通过标准：

- 命令退出码为 `0`
- envelope 中：
  - `payload.isValid = false`
  - `payload.checkMode = "basic"`
  - `payload.stats.totalIssues >= 6`
- `payload.issues[].code` 至少包含：
  - `timeline.resolution.invalid`
  - `timeline.frameRate.invalid`
  - `timeline.track.id.duplicate`
  - `timeline.clip.id.duplicate`
  - `timeline.clip.start.invalid`
  - `timeline.clip.video.range.required`
- 错误应来自结构校验，而不是 parse 阶段失败

## 本阶段暂不要求手测的点

以下点当前由自动化测试覆盖，但不要求放进本轮手测验收：

- 可选 effect registry 下的未知 effect warning
- 未来 render builder 对 timeline 的消费

原因：

1. effect registry discovery 属于 `V2-P3`
2. timeline render builder 属于 `V2-P4`

## 阶段验收通过条件

只有以下条件同时满足，才应把本轮 `V2-P2` 判为通过：

1. 合法 v2 plan 可被 `validate-plan` 正常接受
2. 非法 v2 plan 会返回 timeline 结构错误，而不是 parse failed
3. 以上结果都不要求把 v2 接进 render 主路径

## 验收输出建议

如果以上步骤都通过，建议阶段验收输出为：

```text
V2-P2: accepted
```

如果命令能跑，但结果仍停在 parse failed，或 timeline 错误字段与预期不一致，建议输出为：

```text
V2-P2: continue-P2
```

并明确指出失败的是哪一步、哪个字段。
