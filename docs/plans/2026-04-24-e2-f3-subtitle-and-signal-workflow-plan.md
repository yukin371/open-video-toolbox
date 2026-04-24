# E2-F3 字幕与 Supporting Signal 工作流计划

最后更新：2026-04-24

## 目的

在 `E2-F2` 收口后，下一阶段不应马上跳去更多 batch 命令，而是先把字幕与 supporting signal 工作流补成稳定闭环。

当前仓库的问题不是“没有转写或字幕命令”，而是：

- 有 `transcribe`
- 有 `subtitle`
- 有 `attach-plan-material`
- 有 `render`

但这条链路还没有被明确整理成统一阶段目标、统一检查口径和统一用户文档。

因此这份文档的目标是：

1. 定义 `E2-F3` 的明确范围
2. 固定本阶段的工作包顺序
3. 明确什么算完成，什么不属于本阶段

## 当前问题

虽然相关命令已经存在，但实际高频工作流仍有三类断点：

1. 用户不容易快速看清：
   - 当前 plan 缺的是 transcript、subtitles 还是 beats
   - 哪些 signal 已经存在但还没接回 plan
2. `transcribe -> subtitle -> attach -> render` 还没有被当成一个完整闭环来说明和校验
3. 模板 supporting signal guidance、plan inspection、最终执行之间，还缺少更明确的一致性说明

这意味着 `E2-F3` 不该只是“再加一个字幕命令”，而应该做工作流收口。

## 阶段目标

把字幕、转写、节拍等 supporting signals 从“已有命令可用”，推进到“可稳定编排、可检查、可解释的高频工作流”。

## 范围

### 本阶段应覆盖

- `transcribe -> subtitle -> attach-plan-material -> validate-plan -> render`
- `inspect-plan` 对 transcript / subtitles / beats 缺失状态的摘要增强
- “已有 signal 文件，但还没接回 plan”的状态提示
- 模板 signal consumption 与当前 plan 状态之间的一致性说明
- 用户向文档中的字幕 / signal 工作流统一表达

### 本阶段不做

- 不新增远程 AI provider
- 不做复杂字幕编辑器
- 不引入新的 UI 私有状态
- 不重写模板 schema
- 不直接扩展为任务队列或批量调度系统

## 设计原则

### 1. 继续坚持单一 owner

- transcript / beats / subtitles 的 plan 语义仍由 `Core.Editing` 持有
- CLI 只做参数解析、工作流编排、输出和错误映射
- `render` / `mix-audio` 仍只消费现有结构化 plan

### 2. 先补状态可见性，再补新入口

本阶段优先级应是：

1. 让当前状态更清楚
2. 让既有链路更短更稳
3. 最后才判断是否需要新命令

如果状态可见性不够，继续加命令只会放大混乱。

### 3. 不把 signal 变成第二套计划系统

不允许出现：

- 独立于 `edit.json` 的 signal 工作流 JSON
- CLI 私有 signal state
- UI 私有 signal metadata

signal 最终仍应回接到 `edit.json` 或模板既有声明边界。

## 建议工作包顺序

### E2-F3-W1：状态摘要补齐

目标：

- 把 transcript / subtitles / beats 在 `inspect-plan` 里的状态表达补完整

建议关注点：

- 缺失 signal
- 已存在但未接回 plan 的 signal
- 已接回但路径失效的 signal
- 模板声明了 signal / artifact 需求但当前 plan 还未满足

完成判定：

- 使用者能从 inspect 结果直接判断“下一步该跑 transcribe、subtitle，还是 attach”

### E2-F3-W2：字幕链路文档收口

目标：

- 把 `transcribe -> subtitle -> attach -> render` 写成统一工作流，而不是分散在多个文档和示例里

建议同步文档：

- `docs/FEATURES_AND_USAGE.md`
- `README.md`
- `docs/COMMAND_REFERENCE.md` 只补必要签名，不承担工作流说明

完成判定：

- 使用者不需要再跨多个文档自己拼这条链

### E2-F3-W3：模板 signal consumption 对齐

目标：

- 确认模板 supporting signal guidance、guide 示例、plan inspection、实际 attach / render 行为一致

建议关注点：

- transcript signal 的示例与实际 attach 路径
- subtitle artifact / subtitle top-level metadata 的区分
- beats signal 在 inspect / validate / render 中的可见性

完成判定：

- 不会出现 guide 说得通，但实际 CLI 行为对不上

### E2-F3-W4：决定是否需要 batch signal 入口

目标：

- 判断是否真的需要 batch transcript / batch subtitle / batch signal manifest

注意：

- 这一步是判断，不是默认实现
- 只有当前三步稳定后，才允许进入这一步

进入条件：

- 单项 signal 工作流已经足够清晰
- batch 不会长出第二套语义

## 验收标准

只有同时满足以下条件，才算 `E2-F3` 完成：

1. `transcribe -> subtitle -> plan attach -> render` 已形成稳定闭环
2. `inspect-plan` 能明确提示缺失的 transcript / subtitles / beats
3. 模板 signal consumption 说明与实际 CLI 行为一致
4. 用户不需要再靠多份零散 README 或命令帮助去猜 signal 怎么接回

## 必须停下重判的情况

- 发现 signal 接回需要新的 plan 顶层模型
- 发现字幕工作流开始要求 UI 私有状态
- 发现 signal 语义正在从 `Core.Editing` 漂移到 `Cli`
- 发现模板 signal guidance 与实际执行语义无法复用同一套 owner

## 与后续阶段的关系

`E2-F3` 完成后，才适合进入 `E2-F4`。

原因很简单：

- 如果单项字幕 / signal 工作流还没收口，批量工作流只会把当前不一致放大
- 如果 signal 工作流的状态可见性不够，future Desktop 也无法稳定消费

因此 `E2-F4` 必须建立在 `E2-F3` 已经把单项 signal 工作流理顺的前提下。

## 当前建议下一步

如果按这份计划继续推进，当前最合理的下一步应是：

1. 先完成 `E2-F3` 阶段检查，确认 `W1/W2/W3` 是否已满足验收标准
2. 对 `E2-F3-W4` 做“是否需要 batch signal 入口”的判断，而不是默认进入实现
3. 如果没有明确硬缺口，就把 batch signal 统一留给 `E2-F4` 处理，避免提前长出第二套 batch 约定
