# E2-F4 阶段检查：批量工作流与工作目录编排

最后更新：2026-04-24

## 目的

这份文档只回答一个问题：

> `E2-F4` 现在到底算不算完成？

它不是新的长期计划，也不新增功能范围。它只把当前已经落地的 batch 命令、batch 公共 contract 和工作目录约定，与 `docs/plans/2026-04-24-e2-feature-delivery-staged-plan.md` 中定义的 `E2-F4` 验收条件逐条对照，避免继续凭感觉推进。

## 阶段定义回顾

`E2-F4` 的目标是：

- 把已经存在的单项高频工作流整理成稳定 batch 入口
- 固定 batch manifest、summary、任务结果文件和退出码约定
- 让人、脚本和 future Desktop 都能基于同一套目录解释规则消费结果

本阶段覆盖的能力为：

- `scaffold-template-batch`
- `render-batch`
- `replace-plan-material-batch`
- `attach-plan-material-batch`
- `bind-voice-track-batch`

同时覆盖的配套约定为：

- `summary.json`
- `results/<id>.json`
- `0 / 2 / 1` 退出码
- manifest 相对路径解析规则

## 当前已落地能力

### 已实现 batch 命令

以下 batch 命令都已实现，并已有集成测试或契约快照保护：

- `scaffold-template-batch`
- `render-batch`
- `replace-plan-material-batch`
- `attach-plan-material-batch`
- `bind-voice-track-batch`

### 已固定的 batch 公共 contract

当前已单独落文档：

- `docs/plans/2026-04-24-e2-f4-batch-command-contract.md`

其中已明确固定：

1. 顶层 batch manifest 继续采用 `schemaVersion + items[]`
2. manifest 内相对路径统一按 manifest 所在目录解析
3. 顶层固定写 `summary.json`
4. 任务级固定写 `results/<id>.json`
5. 退出码固定为：
   - 全部成功 `0`
   - 只要有条目失败 `2`
   - manifest 解析 / 装载失败 `1`

### 已统一的内部实现辅助

当前 CLI 内部也已把 batch 结果写盘辅助收口到：

- `src/OpenVideoToolbox.Cli/BatchCommandArtifacts.cs`

当前已经复用的内部规则包括：

- `summary.json` 路径解析
- `results/<id>.json` 路径解析
- summary 写盘
- result 写盘
- `bind-voice-track-batch` 的兼容性默认 item id 生成

这意味着当前 batch 结果 contract 已不再只是“多个命令恰好长得差不多”，而是开始有单一 helper 承接共享输出规则。

## 与阶段验收条件对照

### 条件 1

> 至少一组高频工作流已具备稳定 batch 入口

当前判断：**满足**

证据：

- 模板建目录已有 `scaffold-template-batch`
- 执行导出已有 `render-batch`
- 素材替换 / 挂载已有：
  - `replace-plan-material-batch`
  - `attach-plan-material-batch`
  - `bind-voice-track-batch`

当前已不只是“一组”高频工作流，而是已经覆盖：

- 批量建目录
- 批量消费目录
- 批量素材回写

### 条件 2

> batch 命令的 manifest 约定和部分成功语义已稳定

当前判断：**基本满足**

证据：

- 顶层 manifest contract 已收敛为 `schemaVersion + items[]`
- 各命令都已采用：
  - `itemCount`
  - `succeededCount`
  - `failedCount`
  - `results[]`
- 顶层 summary / 任务级 result 文件约定已固定
- 退出码 `0 / 2 / 1` 已固定

当前仍保留的差异：

- 各命令 item 私有字段仍不同
- 各命令 result 内部 payload 结构仍不同
- `bind-voice-track-batch` 仍保留缺失 `id` 的兼容兜底

这些差异当前判断是**可接受差异**，不构成“第二套公共 contract”，因为它们都已被 batch contract 文档显式标记为“当前不强制统一”的部分。

### 条件 3

> 工作目录组织对人、脚本和 future Desktop 都可读

当前判断：**满足**

证据：

- `scaffold-template-batch` 已固定：
  - 顶层 `summary.json`
  - 默认 `tasks/<id>`
  - 每个条目 `results/<id>.json`
- 其他 batch 命令虽然不一定都生成 `tasks/<id>`，但已统一采用同一层级的：
  - manifest 所在目录
  - `summary.json`
  - `results/<id>.json`

这意味着 future Desktop 至少已经有稳定的最小消费边界：

- manifest 顶层状态
- 任务级结果文件
- 可选的任务工作目录

### 条件 4

> 批量能力仍复用单项 owner，不存在第二套语义

当前判断：**满足**

证据：

- `render-batch` 仍复用单项 `render`
- `replace-plan-material-batch` 仍复用单项 `replace-plan-material`
- `attach-plan-material-batch` 仍复用单项 `attach-plan-material`
- `bind-voice-track-batch` 仍复用单项 `bind-voice-track`
- `scaffold-template-batch` 仍复用单项 `scaffold-template`

当前没有出现：

- batch 私有 plan 模型
- batch 私有 material selector
- batch 私有 signal 语义

## 当前结论

`E2-F4` 当前判断为：**基本完成，已进入阶段收尾判断**

更准确地说：

- 代码层面，batch 入口已经不再只是一个样板，而是已经覆盖高频闭环
- contract 层面，公共字段、summary / result 目录和退出码已经显式文档化
- 内部实现层面，也已经开始有共享 helper 承接 batch 公共规则

因此当前不再适合继续用“随手补一个 batch 命令”的方式推进 `E2-F4`。

如果后续还继续把 `E2-F4` 往前推，应该优先回答的不是“还差哪个 batch 命令”，而是：

1. 是否还需要把 batch handler 的 owner 再拆清楚
2. 当前这套 contract 是否已经足够支撑 `E2-G1` 的阶段门判断

## 当前不判为完全完成的原因

当前仍有一个收尾问题没有完全判断完：

- batch command handler 目前仍分散在：
  - `TemplateCommandHandlers.cs`
  - `RenderCommandHandlers.cs`
  - `FoundationCommandHandlers.cs`

这不影响当前 contract 的稳定性，但会影响后续维护者对“batch handler 的代码 owner 是否足够清晰”的判断。

因此当前更合理的结论不是“完全结束”，而是：

- `E2-F4` 已经达到**可做阶段门判断**的状态
- 是否还需要进一步做 owner 收口，应作为进入 `E2-G1` 前的最后一个实现 / 设计判断点

## 对下一阶段的影响

基于当前检查，下一步已经不适合继续横向扩更多 batch 命令。

更合理的下一步是二选一：

1. 先补 batch handler owner 收口判断，再正式关闭 `E2-F4`
2. 直接进入 `E2-G1`，判断当前 contract 是否已经足够支撑 Desktop 启动重判

这意味着当前功能交付线已经不再缺“明显没补的 batch 高频入口”。

## 本轮阶段检查输出

```text
阶段：E2-F4
阶段目标是否完成：基本完成，当前进入阶段收尾判断
当前 owner 是否保持单一：单项 owner 保持单一；batch handler 代码 owner 仍有进一步收口空间
是否出现第二套模型或第二套语义：否
当前 CLI 是否已能形成稳定闭环：是，已覆盖 batch scaffold / render / replace / attach / voice bind
下一阶段是否已满足进入条件：基本满足，可进入 E2-G1 前的最终判断
如果现在停止，仓库是否仍处于一致状态：是
```
