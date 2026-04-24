# V2-P1 阶段验收清单

最后更新：2026-04-24

## 目的

这份清单只服务一件事：

> 让人工可以直接手动验证 `V2-P1` 当前阶段产物，而不是只看阶段总结。

本清单只覆盖 `V2-P1` 当前已选范围：

- `validate-plan` 增强
- `auto-cut-silence` 的 `v1-compatible` 落地

不覆盖：

- `export L1`
- `schema v2`
- `timeline / effects / transitions`

## 验收原则

本轮验收采用最小可重复样例：

1. 不依赖真实媒体文件
2. 尽量避免外部依赖
3. 直接验证用户可见 CLI 行为与输出字段

建议在仓库根目录执行以下命令。

## 准备临时工作目录

```powershell
$root = Join-Path $env:TEMP ("ovt-v2-p1-acceptance-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $root | Out-Null
$root
```

记录输出目录，后续命令都复用它。

## Step 1：准备 `auto-cut-silence` 样例输入

```powershell
@'
{
  "schemaVersion": 1,
  "inputPath": "input.mp4",
  "segments": [
    { "start": "00:00:02", "end": "00:00:03", "duration": "00:00:01" }
  ]
}
'@ | Set-Content -LiteralPath (Join-Path $root "silence.json") -Encoding UTF8
```

通过标准：

- `$root\silence.json` 已生成

## Step 2：手测 `auto-cut-silence --clips-only`

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  auto-cut-silence `
  --silence (Join-Path $root "silence.json") `
  --clips-only `
  --source-duration-ms 5000 `
  --padding-ms 0 `
  --merge-gap-ms 0 `
  --min-clip-duration-ms 0 `
  --output (Join-Path $root "clips.json") `
  --json-out (Join-Path $root "auto-cut-clips-envelope.json")
```

手动检查：

```powershell
Get-Content -Raw (Join-Path $root "auto-cut-clips-envelope.json")
Get-Content -Raw (Join-Path $root "clips.json")
```

通过标准：

- 命令退出码为 `0`
- envelope 中：
  - `command = "auto-cut-silence"`
  - `payload.autoCutSilence.mode = "clipsOnly"`
  - `payload.autoCutSilence.usedExplicitSourceDuration = true`
  - `payload.result.stats.generatedClipCount = 2`
- `clips.json` 中应有两个 clip：
  - `clip-001`：`in = "00:00:00"`，`out = "00:00:02"`
  - `clip-002`：`in = "00:00:03"`，`out = "00:00:05"`

## Step 3：手测 `auto-cut-silence` 输出 v1 plan

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  auto-cut-silence `
  --silence (Join-Path $root "silence.json") `
  --source-duration-ms 5000 `
  --padding-ms 0 `
  --merge-gap-ms 0 `
  --min-clip-duration-ms 0 `
  --template shorts-basic `
  --render-output final.mp4 `
  --output (Join-Path $root "edit.autocut.json") `
  --json-out (Join-Path $root "auto-cut-plan-envelope.json")
```

手动检查：

```powershell
Get-Content -Raw (Join-Path $root "auto-cut-plan-envelope.json")
Get-Content -Raw (Join-Path $root "edit.autocut.json")
```

通过标准：

- 命令退出码为 `0`
- envelope 中：
  - `payload.autoCutSilence.mode = "plan"`
  - `payload.autoCutSilence.usedExplicitSourceDuration = true`
- `edit.autocut.json` 中：
  - `schemaVersion = 1`
  - `template.id = "shorts-basic"`
  - `output.path = "final.mp4"`
  - `output.container = "mp4"`
  - `source.inputPath` 为解析后的绝对路径，且应以 `input.mp4` 结尾

## Step 4：手测增强后的 `validate-plan` 成功路径

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  validate-plan `
  --plan (Join-Path $root "edit.autocut.json") `
  --json-out (Join-Path $root "validate-valid-envelope.json")
```

手动检查：

```powershell
Get-Content -Raw (Join-Path $root "validate-valid-envelope.json")
```

通过标准：

- 命令退出码为 `0`
- envelope 中：
  - `command = "validate-plan"`
  - `payload.isValid = true`
  - `payload.checkMode = "basic"`
  - `payload.stats.totalIssues = 0`
  - `payload.issues` 为空数组

## Step 5：手测增强后的 `validate-plan` 失败路径字段

```powershell
@'
{
  "schemaVersion": 1,
  "source": {
    "inputPath": "missing.mp4"
  },
  "clips": [],
  "audioTracks": [],
  "artifacts": [],
  "output": {
    "path": "final.mp4",
    "container": "mp4"
  }
}
'@ | Set-Content -LiteralPath (Join-Path $root "invalid.edit.json") -Encoding UTF8

dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  validate-plan `
  --plan (Join-Path $root "invalid.edit.json") `
  --check-files `
  --json-out (Join-Path $root "validate-invalid-envelope.json")
```

手动检查：

```powershell
Get-Content -Raw (Join-Path $root "validate-invalid-envelope.json")
```

通过标准：

- 命令退出码为 `0`
- envelope 中：
  - `payload.isValid = false`
  - `payload.checkFiles = true`
  - `payload.checkMode = "basic"`
  - `payload.stats.totalIssues = 1`
  - `payload.stats.errorCount = 1`
  - `payload.stats.byCode["source.inputPath.missing"] = 1`
- 第一条 issue 中：
  - `severity = "error"`
  - `path = "source.inputPath"`
  - `code = "source.inputPath.missing"`
  - `category = "source"`
  - `checkStage = "files"`
  - `suggestion = "Fix source.inputPath so it points to an existing media file, or rerun without --check-files."`

## 阶段验收通过条件

只有以下条件同时满足，才应把本轮 `V2-P1` 判为通过：

1. `auto-cut-silence` 的 clips-only 模式可手动跑通
2. `auto-cut-silence` 的 v1 plan 模式可手动跑通
3. 增强后的 `validate-plan` 成功路径字段符合预期
4. 增强后的 `validate-plan` 失败路径字段符合预期
5. 以上结果均不要求引入 `schema v2`、`timeline` 或修改 `render` 主路径

## 验收输出建议

如果以上步骤都通过，建议阶段验收输出为：

```text
V2-P1: accepted
```

如果命令能跑，但字段与预期不一致，建议输出为：

```text
V2-P1: continue-P1
```

并明确指出失败的是哪一步、哪个字段。
