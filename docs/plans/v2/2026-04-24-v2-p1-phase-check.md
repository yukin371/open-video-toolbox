# V2-P1 阶段检查：v1-compatible 孵化项

最后更新：2026-04-24

## 目的

这份文档只回答一个问题：

> `V2-P1` 现在到底算不算可以进入阶段验收？

它不是新的长期计划，也不新增功能范围。它只把当前已经完成的 `v1-compatible` 孵化项，与 [2026-04-24-v1-v2-boundary-and-phased-delivery-plan.md](./2026-04-24-v1-v2-boundary-and-phased-delivery-plan.md) 中定义的 `V2-P1` 目标逐条对照，避免继续把“单项做完”误判成“阶段完成”。

## 阶段定义回顾

`V2-P1` 的目标是：

- 从 v2 设计池里挑出少量高价值、但仍保持 `v1-compatible` 的孵化项
- 在不改 `schemaVersion = 1`、不改 `render` 主路径、不引入第二套 batch contract 的前提下完成落地和验证
- 用阶段性结果验证 owner、边界和测试成本，而不是整包推进 v2

本阶段当前实际选中的子项为：

- `validate-plan` 增强
- `auto-cut-silence` 的 `v1-compatible` 落地

本阶段当前未选中的候选为：

- `export L1` feasibility / 预研

说明：

- 未选中的候选不属于本轮阶段验收范围
- 后续如果要做 `export L1`，应作为下一轮 `V2-P1` 子项单独进入，而不是补挂到本轮验收里

## 当前已落地能力

### 子项 1：`validate-plan` 增强

当前已落地：

- `checkMode`
- `stats`
- issue 级：
  - `category`
  - `checkStage`
  - `suggestion`

当前已守住的边界：

- owner 仍在 `Core.Editing`
- CLI 只做透传和 envelope 输出
- 没有借机把 `timeline` / `effects` / `schema v2` 语义塞回当前 v1 校验链

### 子项 2：`auto-cut-silence`

当前已落地：

- 新增显式 CLI 命令 `auto-cut-silence`
- 支持从 `silence.json` 生成：
  - `clips-only`
  - 完整 v1 `EditPlan`
- 总时长来源顺序已固定：
  - `--source-duration-ms`
  - 否则探测 `silence.json.inputPath`

当前已守住的边界：

- 非静音区间算法 owner 保持在 `Core.Editing.AutoCutSilencePlanner`
- CLI 只负责参数解析、时长来源接线、输出 envelope 与失败映射
- 不引入 `schemaVersion = 2`
- 不输出 `timeline`
- 不把结果自动接进 `render` / `run` 主路径

## 与阶段验收条件对照

### 条件 1

> 本阶段每个已选子项都必须能在现有 v1 contract 下解释清楚

当前判断：**满足**

证据：

- `validate-plan` 增强仍然是 v1 validator 的细化，不改变长期 plan 结构
- `auto-cut-silence` 输出仍然是现有 `EditClip[]` 或标准 v1 `EditPlan`

### 条件 2

> 本阶段不能改写现有模板 / plugin / batch / render 主语义

当前判断：**满足**

证据：

- `validate-plan` 只增强 issue 与统计信息
- `auto-cut-silence` 是新的显式辅助命令，不改 `render`、`run`、模板 guide 主链路，也不引入第二套 batch contract

### 条件 3

> owner 必须保持单一，不能把核心规则复制到 CLI

当前判断：**满足**

证据：

- `validate-plan` 的增强仍由 `Core.Editing` 持有
- `auto-cut-silence` 的 clips / plan 生成语义由 `Core.Editing` 持有，CLI 没有复制算法

### 条件 4

> 本阶段结果必须有测试和回归结论，而不是只停留在设计稿

当前判断：**满足**

证据：

- `validate-plan` 与 `auto-cut-silence` 都已完成实现与测试
- 当前已完成全量验证：
  - `dotnet build OpenVideoToolbox.sln`
  - `dotnet test OpenVideoToolbox.sln`

当前最新全量结果为：

- `OpenVideoToolbox.Core.Tests`：148 通过
- `OpenVideoToolbox.Cli.Tests`：161 通过
- 总计：309 通过

## 当前不纳入本阶段验收的内容

以下内容当前明确不属于 `V2-P1` 本轮阶段验收：

- `export L1`
- `schema v2`
- `timeline / effects / transitions`
- v2 render builder
- `resolve-assets`
- 数据驱动 batch / `${var}` / 图表能力

这样做的原因是：

1. 当前阶段目标是验证“少量 v1-compatible 孵化项”是否能稳定落地
2. 如果把未选候选临时塞进来，会再次把“阶段完成”边界打散

## 当前结论

`V2-P1` 当前判断为：**本阶段已达到阶段验收输入条件，可进入人工验收**

更准确地说：

- 本阶段已选中的两个子项都已完成 `规格 -> 计划 -> 执行 -> 测试 -> 修复`
- 当前代码、文档与 owner 边界保持一致
- 当前没有必须继续补实现才能解释清楚的硬缺口

因此下一步不应继续在 `V2-P1` 里无边界滚动追加新子项，而应先做阶段验收决定。

## 手动验收入口

本阶段已补可直接执行的人工验收清单：

- [2026-04-24-v2-p1-acceptance-checklist.md](./2026-04-24-v2-p1-acceptance-checklist.md)

该清单当前覆盖：

1. `auto-cut-silence --clips-only`
2. `auto-cut-silence` 输出 v1 plan
3. `validate-plan` 成功路径
4. `validate-plan --check-files` 失败路径中的新增字段

因此本轮阶段验收不再依赖“我口头说已经完成”，而是可以直接按清单逐步手测。

## 阶段验收建议

当前建议的阶段验收输出为：

- `accepted`
  - 接受当前 `V2-P1` 阶段结果
  - 下一步再决定：
    - 开新一轮 `V2-P1`
    - 还是进入 `V2-P2`
- `continue-P1`
  - 不关闭 `V2-P1`
  - 明确追加新的单独子项，例如 `export L1`
- `escalate-to-P2`
  - 认为当前 `v1-compatible` 孵化已足够，转入 schema v2 合约层
- `pause`
  - 暂停整个 v2 线

## 本轮阶段检查输出

```text
阶段：V2-P1
阶段目标是否完成：已完成当前已选子项，达到阶段验收输入条件
本阶段已选范围是否清楚：是，仅包含 validate-plan 增强 与 auto-cut-silence
当前 owner 是否保持单一：是
是否出现第二套模型或第二套语义：否
当前验证是否充分：是，已完成全量 build/test
是否应继续在本阶段追加实现：否，应先进入阶段验收
如果现在停止，仓库是否仍处于一致状态：是
```
