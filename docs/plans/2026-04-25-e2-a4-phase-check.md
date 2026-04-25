# E2-A4 阶段检查：依赖 / 性能 / 安全最小基线

最后更新：2026-04-26

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

当前仓库维护入口还额外补了：

- `.github/workflows/ci.yml` 支持 `workflow_dispatch`
- `.github/workflows/pr-validation.yml` 支持 `workflow_dispatch + pr_number`
- 当自动 `pull_request` 事件未正常挂到最新 head 时，maintainer 仍可手动补跑 `ci` 与 `pr-validation`，不需要绕开现有 review gate

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

## 从 Clean Code 角度复核

这轮补进来的 `workflow_dispatch` fallback，不应只按“能补跑 CI”来判断，还要看它是否继续符合仓库当前的 clean code / 可维护性原则。

### 1. 职责是否仍然单一

当前判断：**是**

原因：

- `ci.yml` 仍只负责 build + test
- `pr-validation.yml` 仍只负责 PR 标题 / 正文与 review gate 校验
- 手动触发只补了触发入口，没有把额外业务判断塞进 workflow
- `pr-validation.yml` 手动模式下新增的 PR 元数据解析，也只负责把真实 PR title/body 接回既有校验路径，不额外拥有第二套规则

### 2. 是否引入重复实现

当前判断：**否**

原因：

- 手动补跑没有复制第二份 CI / PR 校验 workflow，而是继续复用原文件
- 手动触发的 `pr_number` 只是把事件上下文补齐，不是重新实现一份本地 PR 规则解析器
- 当前 fallback 仍产出同一类 GitHub Actions run、check suite 与日志入口，没有长出“临时脚本通过即可”这类旁路

### 3. 命名与入口是否足够显式

当前判断：**是**

原因：

- `workflow_dispatch` 是 GitHub Actions 原生入口，不是隐藏开关
- `pr_number` 直接表达手动补跑时唯一必需的外部输入
- maintainer 看到 workflow 名称与输入字段，就能理解这条 fallback 的用途，不需要额外猜测隐含语义

### 4. 失败语义与可观察性是否保持一致

当前判断：**是**

原因：

- 手动补跑后的结果仍进入 GitHub Actions run、job log 与 check suite
- `ci` 与 `pr-validation` 不需要切换到仓库外的脚本或人工口头确认
- 当自动 `pull_request` 事件缺失时，维护者仍能用同一套日志面定位问题，而不是丢失可追踪性

### 5. 当前仍保留的技术债务

当前判断：**已显式识别，但不阻塞 `E2-A4` 首轮结论**

当前仍存在：

- 截至 **2026-04-26**，PR #5 的最新 head 已有成功的手动 `workflow_dispatch` runs 与对应 check suites
- 但 `gh pr view 5 --json statusCheckRollup` 仍返回空数组，说明自动 `pull_request` 检查面板链路尚未完全恢复
- 这属于“自动事件接线异常仍待继续定位”，不属于“仓库已失去 review gate”

因此这轮更合理的判断是：

- 当前 fallback 已满足 clean code 语义下的职责清晰、复用优先、入口显式与可观察性要求
- 但不能把它夸大成“自动 PR 检查链路已彻底修复”

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
- PR 校验 workflow 已支持手动反查真实 PR 元数据，避免因为事件源缺失而退化成“无 gate 直接放行”

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
维护 fallback 是否仍保持单一路径 / 单一职责：是
自动 PR 检查链路是否已完全恢复：否，当前仅确认手动补跑链路稳定可用
当前是否还适合继续扩实现：否，先进入收口/持有状态
如果现在停止，仓库是否仍处于一致状态：是
```
