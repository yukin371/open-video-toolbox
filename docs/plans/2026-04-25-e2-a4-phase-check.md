# E2-A4 阶段检查：依赖 / 性能 / 安全最小基线

最后更新：2026-04-25

## 目的

这份文档只回答一个问题：

> `E2-A4` 当前到底算不算已经形成第一层阶段价值？

它不新增新的 runtime baseline 范围，也不替代专项方案文档。它只把当前已经落地的脚本、workflow、阈值判定和安全检查项，与 `docs/plans/2026-04-22-e2-ecosystem-sustainability-plan.md` 中定义的 `E2-A4` 目标逐条对照，避免继续凭感觉推进。

## 阶段定义回顾

`E2-A4` 的目标是先建立三类最小护栏：

1. 外部依赖怎么验证才算可用
2. 代表性命令至少怎么观察，才不至于对性能回归完全无感
3. 外部工具调用需要守住哪些确定性与安全边界

本阶段并不要求：

- 完整 benchmark 基础设施
- 全平台性能矩阵
- 系统级安全沙箱
- 默认 CI 跑所有重依赖 smoke

## 当前已落地能力

### 依赖基线

当前仓库已补固定入口：

- `scripts/Verify-DependencyBaseline.ps1`
- `docs/external-dependencies.md`
- `doctor` 命令与 optional/required 依赖区分

当前已固定的最小约定：

- `ffmpeg` / `ffprobe`
  - required
  - 至少通过 `doctor` + real smoke 判断可用性
- `whisper-cli` / `whisper-model` / `demucs`
  - optional
  - 缺失时允许跳过，但必须被 `doctor` 清晰表达

### 性能基线

当前仓库已补固定入口：

- `scripts/Measure-RuntimeBaseline.ps1`
- `scripts/Test-RuntimeBaselineThresholds.ps1`
- `scripts/runtime-baseline.thresholds.json`
- `scripts/Write-RuntimeBaselineSummary.ps1`
- `.github/workflows/runtime-baseline.yml`

当前样本已经覆盖：

- `doctor`
- `probe`
- `scaffold-template-batch`
- `render-batch --preview`
- `render --preview`

当前 workflow 已具备：

- 手动触发
- 每周定时执行
- 超阈值显式失败
- summary markdown artifact 上传

### 安全基线

当前已落地：

- `docs/plans/2026-04-22-e2-a4-runtime-baseline.md` 中的外部工具安全清单
- `.github/PULL_REQUEST_TEMPLATE.md` 中的 external tool check
- `docs/ARCHITECTURE_GUARDRAILS.md` 中的外部工具安全基线

当前已经被明确固定的检查点包括：

- overwrite 显式语义
- timeout / cancellation 统一处理
- stdout / stderr / 错误上下文保留
- `ProducedPaths` / side effect 声明
- CLI / Desktop 不绕过 `Core.Execution`

## 与阶段目标对照

### 条件 1

> 至少有一份依赖兼容性矩阵或验证约定

当前判断：**满足**

证据：

- `doctor` 已明确 required / optional 依赖
- `Verify-DependencyBaseline.ps1` 已把 `doctor + real smoke` 收成固定入口
- `docs/external-dependencies.md` 已形成用户 / maintainer 视角的依赖说明

### 条件 2

> 至少有一份最小性能观察基线

当前判断：**满足**

证据：

- `Measure-RuntimeBaseline.ps1` 已固定代表性命令样本
- 样本已不只覆盖单命令，还覆盖 batch/workdir 的生产端与消费端
- 当前已有仓库内阈值配置，而不是只保留一份临时观察日志

### 条件 3

> 至少有一份安全检查清单，覆盖高风险外部调用边界

当前判断：**满足**

证据：

- `runtime baseline` 专项文档已单独列出安全清单
- `ARCHITECTURE_GUARDRAILS.md` 已固化长期基线
- PR 模板已把 external tool review 变成显式检查项

## 当前结论

`E2-A4` 当前判断为：**已形成第一层阶段价值，可进入收口/持有状态**

更准确地说：

- 依赖验证不再只靠口头说明
- 性能样本不再只是一次性观察，而已接入仓库阈值与 workflow
- 安全边界不再只藏在代码与 reviewer 经验里，而已有显式文档与 PR 检查项

因此当前不再适合继续把 `E2-A4` 往“再补一个脚本”方向无限扩。

## 当前不判为更高阶段的原因

当前仍不应把 `E2-A4` 误判成“完整平台基线系统”，原因很明确：

- 样本仍以单机 / 单 workflow 为主
- 阈值仍更适合做维护告警，不是 SLA
- optional 重依赖 smoke 仍主要依赖本机条件满足时的可选验证

这些都属于下一轮是否继续深化的问题，不影响当前第一层价值已经成立。

## 更合理的下一步

当前更合理的动作不是继续扩 `E2-A4` scope，而是：

1. 在 roadmap 中把 `E2-A4` 视为已完成首轮基线
2. 保留现有脚本 / workflow / 文档
3. 后续只有在真实 CI 样本或维护问题暴露缺口时，再开二轮卡片

## 本轮阶段检查输出

```text
阶段：E2-A4
阶段目标是否完成：已形成第一层阶段价值
是否已建立固定验证入口：是
是否已有仓库内阈值判定：是
是否已有外部工具安全清单：是
当前是否还适合继续扩实现：否，先进入收口/持有状态
如果现在停止，仓库是否仍处于一致状态：是
```
