# E2-A4 依赖 / 性能 / 安全最小基线

最后更新：2026-04-23

## 背景

- `E2-A4` 的目标不是一次建成完整 benchmark 或安全审计系统。
- 当前更现实的目标是先把长期最容易漂移的三类问题收敛成最小基线：
  - 外部依赖怎么验证才算“可用”
  - 性能至少该怎么观察，才不至于完全无感
  - 外部工具调用需要守住哪些确定性与安全边界

## 本轮目标

1. 给外部依赖建立一份最小验证约定
2. 给代表性命令建立一份轻量性能观察样本
3. 给 `Core.Execution` 外部调用边界建立一份最小安全检查清单

## 本轮不做

- 不引入新的 benchmark 基础设施
- 不要求 CI 立即跑所有 real smoke
- 不把本机观测样本误写成正式 SLA
- 不引入新的安全运行时、沙箱或权限系统

## 依赖兼容性基线

### 依赖分层

| 依赖 | 类型 | 当前角色 | 最低验证方式 |
|------|------|----------|--------------|
| `ffmpeg` | required | 核心媒体执行 | `doctor` + real smoke |
| `ffprobe` | required | 媒体探测 | `doctor` + real smoke |
| `whisper-cli` | optional | 转写执行 | `doctor` + optional real smoke |
| `whisper-model` | optional | 转写模型文件 | `doctor` + optional real smoke |
| `demucs` | optional | stem 分离 | `doctor` + optional real smoke |

### 当前验证约定

所有机器至少先跑：

```powershell
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- doctor --json-out doctor.json
```

如果想把 `doctor + Core real smoke + Cli real smoke` 收成一条维护入口，可直接运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify-DependencyBaseline.ps1
```

如果想通过 GitHub Actions 跑同一组检查，可直接触发 `runtime-baseline` workflow。

如果想把两份 JSON 进一步整理成 maintainer 友好的 markdown 摘要，可运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Write-RuntimeBaselineSummary.ps1 -RuntimeBaselinePath .artifacts\runtime-baseline.json -DependencyBaselinePath .artifacts\dependency-baseline.json -SummaryPath .artifacts\runtime-baseline-summary.md
```

如需把依赖摘要直接写到文件，可加：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify-DependencyBaseline.ps1 -OutputJsonPath .artifacts\dependency-baseline.json
```

然后按依赖层级继续：

1. `ffmpeg` / `ffprobe`
   - 期望：`doctor` 中 `required: true` 且 `isAvailable: true`
   - 再跑已存在的 real smoke 或整仓测试
2. `whisper-cli` / `whisper-model`
   - 期望：仅在要用 `transcribe` 时要求可用
   - 未配置时允许跳过，不应让默认 CI 变红
3. `demucs`
   - 期望：仅在要用 `separate-audio` 时要求可用
   - 未配置时允许跳过，不应让默认 CI 变红

### 当前开发机样本

基于 2026-04-22 实际 `doctor`：

- `ffmpeg`
  - 可用
- `ffprobe`
  - 可用
- `whisper-cli`
  - 当前未解析到
- `demucs`
  - 当前未解析到
- `whisper-model`
  - 当前未配置

说明：

- 这是一份当前开发机样本，不是平台支持矩阵。
- 真正的长期约定是：required 依赖必须能通过 `doctor` 与最小 real smoke；optional 依赖至少要能被 `doctor` 清晰表达缺失原因。

## 性能观察基线

### 观察原则

- 当前只做“回归观察样本”，不做性能承诺。
- 先关注高频、低噪音、易重复的命令：
  - `doctor`
  - `probe`
  - `render --preview`
- 命令统一用 `dotnet run --no-build`，避免把编译时间混进样本。

### 本轮观测方法

当前仓库已补一个固定入口：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Measure-RuntimeBaseline.ps1
```

如需把观测结果和仓库内阈值做一次显式比对，可继续运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Test-RuntimeBaselineThresholds.ps1 -RuntimeBaselinePath .artifacts\runtime-baseline.json -OutputJsonPath .artifacts\runtime-threshold-check.json
```

仓库内也已补独立维护 workflow：

- `.github/workflows/runtime-baseline.yml`
  - 当前运行在 `windows-latest`
  - 当前脚本步骤使用 `powershell`，与本地验证宿主保持一致
  - 支持 `workflow_dispatch`
  - 每周定时跑一次
  - 直接写出 `.artifacts/runtime-baseline-summary.md`，便于后续下载或离线比较
  - 当前也会把关键结果写进 GitHub Actions job summary
  - 当前会用仓库内 `Test-RuntimeBaselineThresholds.ps1 -FailOnExceeded` 做阈值判定，不再在 workflow 内重复维护一套失败逻辑
  - 若 `doctor` / `probe` / `render --preview` 超出仓库阈值，workflow 会显式失败
  - 上传 `runtime-baseline.json`、`runtime-threshold-check.json`、`dependency-baseline.json` 与 `runtime-baseline-summary.md` 产物

如已安装 PowerShell 7，也可执行：

```powershell
pwsh ./scripts/Measure-RuntimeBaseline.ps1
```

如需保留本轮样例输入、`edit.json` 和 `--json-out` 文件，可加：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Measure-RuntimeBaseline.ps1 -KeepArtifacts
```

如需把摘要直接写到文件，可加：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Measure-RuntimeBaseline.ps1 -OutputJsonPath .artifacts\runtime-baseline.json
```

脚本会：

1. 临时生成 2 秒样例媒体
2. 写出最小 `edit.json`
3. 跑 `doctor`
4. 跑 `probe`
5. 跑 `render --preview`
6. 输出一份 JSON 摘要

脚本内部观测的命令等价于：

```powershell
dotnet run --no-build --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- doctor --json-out doctor.json
dotnet run --no-build --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- probe <sample.mp4> --ffprobe ffprobe --json-out probe.json
dotnet run --no-build --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- render --plan <edit.json> --output final.mp4 --preview --json-out render-preview.json
```

### 当前开发机观测样本

| 命令 | 输入 | 观测值 |
|------|------|--------|
| `doctor` | 无 | 约 `1031 ms` |
| `probe` | 2 秒样例 mp4 | 约 `928 ms` |
| `render --preview` | 1 clip 的最小 `edit.json` | 约 `929 ms` |

解释：

- 这些数字只用于后续回归对照，不是发布承诺。
- 同一台机器多次运行也会有波动；更适合关注是否出现数量级退化，而不是追求毫秒级完全一致。
- 当前仓库已补第一层阈值检查，但阈值仍只用于维护告警，不代表正式 SLA。
- 如果后续同机型、同命令、同输入下持续出现数量级退化，再考虑把阈值收紧或升级成更正式的基线。

## 外部工具调用安全清单

当前安全基线只覆盖仓库已经拥有的外部调用边界，不扩展到系统级沙箱。

### 1. 进程启动必须走 `Core.Execution`

- 不允许在 CLI 或未来 Desktop 里直接拼 shell 命令字符串再启动
- 当前实现依赖：
  - `ProcessStartInfo.UseShellExecute = false`
  - `ProcessStartInfo.ArgumentList`
  - `RedirectStandardOutput = true`
  - `RedirectStandardError = true`

### 2. 输出路径必须显式声明

- `ProcessExecutionRequest` 要带 `ProducedPaths`
- preview 和 execute 都应复用同一套 `ProducedPaths`
- 不允许靠执行后扫目录来猜测产物

### 3. 覆盖行为必须显式

- `ffmpeg` 类写盘命令默认走 `-n`
- 只有显式 `--overwrite` 或 preset 已声明允许覆盖时，才走 `-y`
- sidecar 字幕复制也必须复用同一 overwrite 语义

### 4. 超时 / 取消 / 杀进程必须统一

- 所有外部命令应通过 `ProcessExecutionRequest.Timeout`
- timeout 或 cancellation 时，统一由 `DefaultProcessRunner` 尝试 `Kill(entireProcessTree: true)`
- 不允许在命令实现里各自发明超时与清理逻辑

### 5. 日志与错误上下文不能丢

- 必须保留 stdout / stderr
- 失败、超时、取消都要带回结构化上下文
- CLI 只能做 envelope 映射，不能吞掉原始执行线索

### 6. 插件边界不能绕过执行安全

- 模板插件只能提供静态模板与辅助文档
- 不允许脚本、hook、二进制下载或私有执行入口
- 如果社区模板需要新能力，先新增 deterministic CLI 命令，再回到模板层声明使用它

### 7. Review 流程要能显式检查

- `.github/PULL_REQUEST_TEMPLATE.md` 应对外部工具边界改动给出显式检查项
- 至少覆盖：
  - overwrite 语义
  - timeout / cancellation 语义
  - stdout / stderr / 错误上下文保留
  - `ProducedPaths` / side effect 声明

## 完成判定

本轮满足以下条件后，可认为 `E2-A4` 已形成第一层价值：

1. 新环境知道先用什么命令判断依赖是否可用
2. maintainer 至少有一组可复跑的性能观察样本
3. 轻量性能样本已能接到仓库内阈值判定，而不只是纯观察
4. 外部工具调用的 overwrite / timeout / logging / produced-paths 边界已写成清单，而不是只藏在代码里
