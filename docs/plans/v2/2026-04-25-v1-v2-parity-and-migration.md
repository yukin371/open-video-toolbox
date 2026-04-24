# V1 / V2 Parity 与迁移说明

最后更新：2026-04-25

## 目的

这份文档只回答三个问题：

1. `V2-P5` 验收通过后，当前 `v1` 与 `v2` 各自已经能正式做什么
2. 当前哪些能力还没有达到 `v1 / v2 parity`
3. 现在是否应该触发 `V1 Runtime Sunset`

本文档不新增 owner，也不改变 `Core.Editing` / `Core.Execution` / `Cli` 的职责划分；它只收口当前阶段后的对外口径。

## 当前总判断

- `v1` 仍然是当前默认正式运行路径
- `v2` 已不再只是设计稿，已经具备首条真实可跑的正式工作流
- 当前已补 parity / migration 文档，但这不等于 `v1` 已可退场
- 当前**不触发** `V1 Runtime Sunset`

原因不是文档没补，而是当前 `v2` 只完成了首条正式工作流，并未覆盖当前高频 `v1` 工作流面。

## 当前正式可用路径

### v1

当前继续正式支持：

- `schemaVersion = 1` 的 `edit.json`
- 所有既有 built-in v1 模板
- `templates -> init-plan / scaffold-template -> validate-plan -> render`
- `replace-plan-material` / `attach-plan-material` / `bind-voice-track`
- 已冻结的 batch contract
- 默认 `auto-cut-silence` v1 路径
- `export --format edl` 对 v1 plan 的统一导出

### v2

当前已正式可跑通的范围只包含：

- `schemaVersion = 2` 的 timeline 合约层
- `validate-plan` 对 v2 timeline plan 的结构校验
- `effects list / describe` 的 built-in effect discovery
- `render --plan <v2-plan>` 的 timeline render baseline
- 首个 built-in v2 模板 `timeline-effects-starter`
- `templates timeline-effects-starter`
- `init-plan --template timeline-effects-starter`
- `init-plan --template timeline-effects-starter --seed-from-transcript`
- `init-plan --template timeline-effects-starter --seed-from-beats`
- `auto-cut-silence --template timeline-effects-starter`
- `export --plan <v2-plan> --format edl`

## 当前 parity 判断

### 已达到“有对应替代路径”的部分

| 工作流 | v1 当前路径 | v2 当前路径 | 当前判断 |
| --- | --- | --- | --- |
| 模板发现 | `templates <v1-template>` | `templates timeline-effects-starter` | 已具备最小 parity |
| 草稿生成 | `init-plan --template <v1-template>` | `init-plan --template timeline-effects-starter` | 已具备最小 parity |
| signal seed | `--seed-from-transcript` / `--seed-from-beats` 生成 v1 clips | 同命令生成 v2 timeline clips | 已具备最小 parity |
| preview / render | `render --plan <v1-plan>` | `render --plan <v2-plan>` | 已具备 baseline parity |
| silence-driven draft | `auto-cut-silence` 默认生成 v1 clips / plan | `auto-cut-silence --template timeline-effects-starter` 生成 v2 plan | 已具备最小 parity |
| 粗粒度导出 | `export --format edl` | `export --format edl` | 已具备 L1 parity |

### 仍未达到 parity 的部分

当前仍明确缺失：

- `v2` 还没有覆盖现有 v1 built-in 模板族，只落了 `timeline-effects-starter`
- 插件模板当前没有正式 v2 支持
- 没有从既有 v1 `edit.json` 自动迁移到 v2 `timeline` 的正式工具
- `export` 当前只有 `edl`，没有 `premiere-xml` / `fcpxml`
- `export L1` 对 v2 仍只导出主视频轨 cut list，不保留 audio / effect / transition / extra video track 语义
- `v2` 还没有数据驱动 batch、`resolve-assets`、`${var}` 注入等高风险能力
- 当前 v2 render 仍只是 timeline render baseline，不应被表述成“复杂 effect 已全面正式支持”

## 当前迁移建议

### 什么时候继续用 v1

优先继续使用 `v1` 的场景：

- 你依赖现有 built-in v1 模板
- 你当前工作流依赖 batch contract
- 你需要稳定复用现有 `edit.json schema v1`
- 你不需要 timeline / track / clip-level effect 语义
- 你需要最小风险地继续现有生产流程

### 什么时候可以开始用 v2

当前适合切到 `v2` 的场景：

- 你明确要测试或使用 `timeline-effects-starter`
- 你需要真实 `timeline / tracks / effects / transitions` 的最小正式样例
- 你希望让 transcript / beats seed 直接生成 v2 timeline clips
- 你希望让 `auto-cut-silence` 直接接到 v2 timeline plan
- 你接受当前 `v2` 仍是“首条正式工作流”，不是全面替代 `v1`

### 当前推荐的迁移方式

当前推荐的是“新建 v2 工作流”，不是“批量升级旧 v1 plan”：

1. 从 `templates timeline-effects-starter` 先看 guide / preview
2. 用 `init-plan --template timeline-effects-starter` 或 `auto-cut-silence --template timeline-effects-starter` 生成新的 v2 plan
3. 用 `validate-plan --plan <v2-plan>` 与 `render --plan <v2-plan> --preview` 检查
4. 如需外部互操作，先用 `export --format edl`

当前不推荐：

- 把所有既有 v1 `edit.json` 批量改写成 v2
- 把“已有一条 v2 工作流”理解成“所有 v1 模板都应立即迁移”
- 把 `export L1` 误解成完整的 NLE parity

## 当前不触发 `V1 Runtime Sunset` 的原因

虽然当前已经满足以下事实：

- `V2-P5` 已通过人工阶段验收
- 已经存在首条正式可跑的 v2 工作流
- parity / migration 说明已文档化

但当前仍**不建议**把 `v1` 标记为 sunset，原因是：

1. 当前 v2 只覆盖首个 built-in 模板，不覆盖现有高频 v1 模板族
2. 当前 batch、插件模板、导出互操作都还没有形成足够广的 v2 替代面
3. 当前没有正式的 v1 -> v2 迁移工具或迁移 contract
4. 当前更准确的结论是“v2 已进入正式可用的首条工作流阶段”，而不是“v1 已失去正式运行意义”

因此当前状态应表达为：

- `v1`：继续正式支持
- `v2`：已有首条正式工作流，可继续扩面
- `V1 Runtime Sunset`：未触发，需后续单独决策

## 后续建议顺序

当前更合理的后续动作顺序是：

1. 保持 `V2-P5` 已关闭，不再往该阶段混入新能力
2. 如果继续走 v2，明确开启下一阶段，而不是回头继续扩写 `V2-P5`
3. 在未来再次讨论 `V1 Runtime Sunset` 前，至少先补：
   - 更多正式 v2 模板覆盖
   - 更完整的互操作格式
   - 更明确的 v1 -> v2 迁移 contract 或迁移工具

## 与其他文档的关系

- 阶段验收事实：
  - [2026-04-24-v2-p5-phase-check.md](./2026-04-24-v2-p5-phase-check.md)
  - [2026-04-24-v2-p5-acceptance-checklist.md](./2026-04-24-v2-p5-acceptance-checklist.md)
- 阶段总入口与边界：
  - [2026-04-24-v1-v2-boundary-and-phased-delivery-plan.md](./2026-04-24-v1-v2-boundary-and-phased-delivery-plan.md)
- 当前活跃工作面：
  - [../../roadmap.md](../../roadmap.md)
