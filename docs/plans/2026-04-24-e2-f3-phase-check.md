# E2-F3 阶段检查：字幕与 Supporting Signal 工作流

最后更新：2026-04-24

## 目的

这份文档只回答一个问题：

> `E2-F3` 现在到底算不算完成？

它不是新的长期计划，也不新增功能范围。它只把当前已经落地的字幕 / transcript / beats / supporting signal 工作流，与 `docs/plans/2026-04-24-e2-feature-delivery-staged-plan.md` 中定义的 `E2-F3` 验收条件逐条对照，并给出 `W4` 的决策结论。

## 阶段定义回顾

`E2-F3` 的目标是：

- 把字幕、转写、节拍等 supporting signals 从“已有命令”推进到“高频工作流闭环”
- 让用户和外部 AI 不再自己猜 signal 该怎么接回 `edit.json`

本阶段覆盖的能力为：

- `transcribe`
- `subtitle`
- `attach-plan-material`
- `inspect-plan`
- `validate-plan`
- 模板 `guide.json` / `commands.json` / `commands.*`

## 当前已落地能力

### E2-F3-W1：状态摘要补齐

当前已落地：

- `inspect-plan` 已输出 `signals[]`
- `signals[]` 已补稳定 `status`
- 当前稳定状态包括：
  - `attachedPresent`
  - `attachedMissing`
  - `attachedNotChecked`
  - `expectedUnbound`
  - `optionalUnbound`

这意味着当前使用者已经能直接从 inspection 结果判断下一步更像是：

- 先跑 `transcribe` / `subtitle`
- 还是先 `attach-plan-material`
- 还是先修路径

### E2-F3-W2：字幕链路文档收口

当前已落地：

- `README.md` 已补用户向闭环说明
- `docs/FEATURES_AND_USAGE.md` 已补统一的字幕链路说明
- `docs/COMMAND_REFERENCE.md` 已补 `inspect-plan` 的新增状态字段说明

当前用户已经可以在文档中看到这条稳定链路：

1. `transcribe`
2. `subtitle`
3. `attach-plan-material`
4. `inspect-plan --check-files`
5. `validate-plan --check-files`
6. `render`

### E2-F3-W3：模板 signal consumption 对齐

当前已落地：

- supporting signal guidance 现已同时覆盖：
  - `init-plan` 前接入
  - 已有 `edit.json` 后 attach
- 字幕模板的 `commands.json` / `commands.*` 现已补：
  - `subtitle`
  - `attach-plan-material --transcript`
  - `attach-plan-material --subtitles`
- 模板命令示例现已把下游闭环一并写出：
  - `inspect-plan --check-files`
  - `validate-plan --check-files`

这意味着模板 guide 已不再只停留在“把 signal 文件生成出来”，而是明确说明“如何接回当前 plan，并继续检查和导出”。

## 与阶段验收条件对照

### 条件 1

> `transcribe -> subtitle -> plan attach -> render` 已形成稳定闭环

当前判断：**满足**

证据：

- 用户向文档已把这条链路写成统一工作流
- 模板 guide / scripts 已能直接给出闭环命令序列
- `attach-plan-material` 已提供 transcript / subtitles 的显式接回入口

### 条件 2

> `inspect-plan` 能明确提示缺失的 transcript / subtitles / beats

当前判断：**满足**

证据：

- `signals[]` 已覆盖 transcript / beats / subtitles
- `status` 已能直接区分已绑定、缺失、未校验、模板期望但未绑定、可选未绑定

### 条件 3

> 模板 signal consumption 说明与实际 CLI 行为一致

当前判断：**本轮补齐后满足**

证据：

- transcript / beats 的 consumption 已与 `attach-plan-material` 语义对齐
- 字幕模板的 artifact commands 已补 attach 步骤
- workflow commands 已把 `inspect-plan --check-files`、`validate-plan --check-files` 纳入稳定序列
- 相关 Core / CLI 测试、快照和脚本输出均已更新

### 条件 4

> 用户不需要再靠多份零散 README 或命令帮助去猜 signal 怎么接回

当前判断：**基本满足**

证据：

- `README.md`、`docs/FEATURES_AND_USAGE.md`、模板 guide 已形成同一条叙述主线
- `COMMAND_REFERENCE.md` 只保留签名与必要状态说明，没有再承载另一套工作流说法

剩余风险：

- 如果未来继续扩更多 supporting signal 类型，仍需保持同一套 owner 与 guide 输出规则
- 但这已经属于后续扩面风险，不构成当前 `E2-F3` 的阻塞

## E2-F3-W4 决策

### 当前问题

`W4` 要回答的不是“能不能做 batch signal”，而是“现在是否值得做、以及是否会与 `E2-F4` 冲突”。

### 当前判断

**当前不进入新的 batch transcript / batch subtitle / batch signal manifest 实现。**

### 原因

1. 当前单项 signal 工作流已经清晰闭环，`W4` 的进入条件已满足“可以判断”，但还没有出现必须立刻补 batch 的硬缺口。
2. 当前仓库里唯一已经稳定的 batch 样板是 `bind-voice-track-batch`，它成立的前提是语义非常单一：
   - 一个 manifest
   - 一类材料
   - 单项逻辑可直接复用
3. transcript / subtitle / beats 如果现在各自长出独立 batch 入口，很容易提前把 `E2-F4` 的统一 manifest / 输出目录 / partial success 约定打散。
4. `E2-F4` 本来就负责：
   - batch manifest 公共约定
   - partial success 规则
   - 工作目录组织
   - 多类 batch 入口的统一排序

### 当前结论

- `E2-F3-W4` 当前结论是：**完成判断，但不进入实现**
- 如果后续真要做 batch signal，优先方向也不应是：
  - `batch-transcript`
  - `batch-subtitle`
  - 各自独立的 signal manifest
- 更合理的方向应是留到 `E2-F4`，再统一评估是否需要一个能复用单项 attach 语义的批量入口

## 当前结论

`E2-F3` 当前判断为：**可收口，进入完成前状态**

更准确地说：

- 代码、测试、模板 guide 与用户文档层面，`E2-F3` 的主体工作已经完成
- 当前不应为了“顺手再补一个 batch 命令”继续把 `E2-F3` 膨胀成 `E2-F4`

## 对下一阶段的影响

基于当前检查，下一步更适合转入 `E2-F4` 的设计与入口统一，而不是继续在 `E2-F3` 里横向扩 batch signal。

进入 `E2-F4` 前，当前已经明确具备的前提是：

- 单项 signal 工作流已稳定
- 模板 guide 已能表达 attach / inspect / validate / render
- 当前没有新增顶层 signal 模型需求

## 本轮阶段检查输出

```text
阶段：E2-F3
阶段目标是否完成：基本完成，当前进入判断与文档收尾
当前 owner 是否保持单一：是
是否出现第二套模型或第二套语义：否
当前 CLI 是否已能形成稳定闭环：是，覆盖 transcribe / subtitle / attach / inspect / validate / render
下一阶段是否已满足进入条件：是，适合转入 E2-F4 的 batch 与工作目录统一设计
如果现在停止，仓库是否仍处于一致状态：是
```
