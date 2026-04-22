# D1 Desktop MVP 启动前检查清单

最后更新：2026-04-22

## 目的

在真正启动 `D1 Desktop MVP` 之前，先用一份可执行的清单回答两个问题：

1. 当前是否已经满足启动门槛？
2. 如果还不能启动，最小剩余阻塞是什么？

本文档只判断“能不能启动 D1”，不替代 `D1` 的正式实施计划。

## 当前结论

**结论：截至 2026-04-22，暂不建议正式启动 `D1 Desktop MVP`。**

原因不是 CLI 或模板链路未完成，而是最关键的启动门槛之一尚未满足：

- `edit.json schema v1` 仍未证明进入低频变更窗口。
- 按当前文档口径，最早也要到 **2026-05-22** 之后，且期间没有 breaking change，才能满足“连续 1 个月无 breaking change”的判断条件。

其余启动门槛大体已经满足或接近满足，当前真正缺的不是能力面，而是阶段纪律与时间验证。

## 启动门槛检查

| 检查项 | 门槛来源 | 当前判断 | 证据 | 备注 |
|--------|----------|----------|------|------|
| `edit.json schema v1` 进入低频变更阶段 | `ARCHITECTURE_GUARDRAILS` / 长期路线 | `未满足` | 2026-04-22 刚完成 H2+T1 收口；尚未形成连续 1 个月观察窗口 | 最早观察完成日：`2026-05-22` |
| 模板工作流稳定 | `ARCHITECTURE_GUARDRAILS` / 长期路线 | `基本满足` | `templates -> init-plan / scaffold-template -> validate-plan -> render` 已在当前阶段文档中认定完成 | 后续仍应避免重新改写契约 |
| 外部 AI 已能通过 CLI 完成多数基础剪辑任务 | `ARCHITECTURE_GUARDRAILS` / 长期路线 | `满足` | roadmap 已认定 CLI 契约冻结、模板链路稳定、插件边界与发布链完成 | 当前不缺 CLI 主能力 |
| Desktop 明确定位为交互壳 | `ARCHITECTURE_GUARDRAILS` / 长期路线 | `满足` | 现有文档已明确禁止 Desktop 成为新业务 owner | 启动后仍需用 `MODULE.md` 锁边界 |

## 阻塞清单

### B1. `edit.json schema v1` 低频变更窗口未完成

- 这是当前唯一明确的硬阻塞。
- 如果在 **2026-04-22 到 2026-05-22** 之间继续发生 breaking change，就应顺延 D1 启动时间。
- 观察口径建议固定为：
  - 顶层字段语义不变
  - 模板工作流相关 schema 不变
  - `render` / `mix-audio` / `validate-plan` 对 `edit.json` 的消费语义不变

### B2. 交互壳需求尚未被显式确认

- 当前文档承认 `D1` 是候选阶段，但还没有把“为什么现在值得做 Desktop”写成明确决策。
- 这不是架构阻塞，但会直接影响是否应优先做 `D1` 还是先做 `E2`。

## 非阻塞但启动前建议完成

### R1. Desktop owner 落文档

- 在第一行 Desktop 代码之前，先补：
  - `src/OpenVideoToolbox.Desktop/MODULE.md`
  - UI framework owner
  - 状态管理 owner
  - 允许依赖与禁止依赖

### R2. Desktop 消费模型固定

- 启动前先明确 Desktop 只消费这些稳定对象：
  - 模板目录与插件发现结果
  - `edit.json`
  - `render` / `mix-audio` 执行入口
  - 执行日志与原始输出

### R3. Desktop MVP 只做一个闭环

- 第一轮只允许承诺这一条主流程：
  - 导入素材
  - 选择模板
  - 编辑少量参数
  - 执行
  - 查看结果与日志

如果一开始就尝试时间线、批处理、设置持久化、多入口快捷操作，极易把 D1 做成 D2。

## 启动判定规则

只有当以下四项都为 `是` 时，才把 `D1` 从“候选阶段”改成“正式启动”：

```text
1. 从 2026-04-22 起连续 1 个月无 `edit.json schema v1` breaking change？
2. 当前仍确认 Desktop 比 E2 更优先？
3. Desktop 的角色仍被限定为交互壳，而不是新业务 owner？
4. Desktop 的首轮闭环是否已被压缩到最小可交付范围？
```

若任一答案为“否”，继续停留在候选阶段。

## 满足门槛后立即执行的动作

1. 在 `docs/roadmap.md` 中把当前候选阶段改成正式启动 `D1`。
2. 新建 `src/OpenVideoToolbox.Desktop/MODULE.md`，锁定边界。
3. 新建 `D1` 的正式实施计划，按模块拆成最小工作包。
4. 先交付一个无时间线的表单/卡片式 MVP，不允许直接跳到 D2 范围。

## 关联文档

- `docs/roadmap.md`
- `docs/ARCHITECTURE_GUARDRAILS.md`
- `docs/plans/2026-04-21-long-term-evolution-roadmap.md`
- `docs/PROJECT_PROFILE.md`
