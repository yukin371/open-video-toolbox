# E2-G1 Desktop 启动重判（2026-04-24）

最后更新：2026-04-24

## 目的

本文件用于正式执行一次 `E2-G1` 阶段门判断，回答两个问题：

1. 截至 **2026-04-24**，是否应正式启动 `D1 Desktop MVP`
2. 如果当前仍不启动，下一次重判应以什么条件为准

本文档只负责给出阶段门结果，不替代未来 `D1` 的正式实施计划。

## 输入依据

本次重判基于以下已落地输入：

- `docs/ARCHITECTURE_GUARDRAILS.md`
- `docs/plans/2026-04-22-d1-desktop-mvp-start-checklist.md`
- `docs/plans/2026-04-24-e2-feature-delivery-staged-plan.md`
- `docs/plans/2026-04-24-e2-f4-phase-check.md`
- `docs/plans/2026-04-24-cli-foundation-and-desktop-reservation.md`

## 当前结论

**结论：截至 2026-04-24，执行 `E2-G1` 后仍不正式启动 `D1 Desktop MVP`。**

本次结果采用 `E2-G1` 允许的两种结果中的：

```text
结果 A：继续留在 E2，不启动 D1
```

## 判定理由

### 1. 硬门槛仍未满足

`ARCHITECTURE_GUARDRAILS.md` 对 Desktop MVP 的第一条启动门槛是：

- `edit.json schema v1` 已进入低频变更阶段

截至 **2026-04-24**，这一条仍不能判定为满足。

原因不是 CLI 能力不够，而是观察窗口仍未结束：

- `docs/plans/2026-04-22-d1-desktop-mvp-start-checklist.md` 已明确：最早观察完成日为 **2026-05-22**
- 在此之前，无法把“schema 已进入低频变更窗口”判定为成立

### 2. 其他门槛已基本满足，但不足以覆盖第一条硬门槛

当前已经基本成立的条件包括：

- 模板工作流稳定性已接近可直接被 UI 消费
- 外部 AI 已能通过 CLI 完成多数基础剪辑任务
- Desktop 的角色已被文档明确限制为交互壳，而不是新业务 owner
- `E2-F4` 的 batch / workdir / owner 收口已基本完成，可进入阶段门判断

这意味着当前不缺“启动 Desktop 的前置能力”，而是缺“满足启动纪律的时间与稳定性证据”。

### 3. 当前更高价值的动作仍是继续留在 E2

即使暂不考虑第一条硬门槛，当前也还没有足够证据说明：

- 立即启动 Desktop
  比
- 继续保持 CLI / Core 收敛与观察窗口

更有价值。

因此在阶段纪律上，更合理的选择仍是：

- 不提前启动 `D1`
- 继续把 Desktop 保持为候选阶段
- 直到观察窗口满足后再正式重判

## 本轮重判输出

```text
阶段：E2-G1
执行日期：2026-04-24
是否启动 D1：否
结果类型：结果 A（继续留在 E2，不启动 D1）
主要阻塞：edit.json schema v1 低频变更窗口尚未完成
当前 CLI 是否已具备 Desktop 前置能力：基本具备
当前是否允许提前启动：不允许
下一次最早重判日期：2026-05-22
```

## 对 roadmap 的影响

本次 `E2-G1` 执行后，roadmap 应同步为：

- `E2-F4` 可视为已达到阶段门输入要求
- `E2-G1` 已完成首轮正式判断
- 当前判断结果仍为“继续留在 E2，不启动 D1”
- `D1` 最早仍在 **2026-05-22** 后重判

## 下一步

下一步不再是“是否现在开始写 Desktop”，而是：

1. 继续保持 `edit.json schema v1` 的观察窗口
2. 避免在观察窗口内引入会重新拉长窗口的 breaking change
3. 在 **2026-05-22** 或之后，再执行下一轮 Desktop 启动重判

## 关联文档

- `docs/ARCHITECTURE_GUARDRAILS.md`
- `docs/roadmap.md`
- `docs/plans/2026-04-22-d1-desktop-mvp-start-checklist.md`
- `docs/plans/2026-04-24-e2-feature-delivery-staged-plan.md`
- `docs/plans/2026-04-24-e2-f4-phase-check.md`
