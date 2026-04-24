# E2-F4 批量工作流与工作目录编排计划

最后更新：2026-04-24

补充：

- 当前 batch 公共 contract 已补到 `docs/plans/2026-04-24-e2-f4-batch-command-contract.md`
- 本文档继续负责阶段目标、顺序与判断；公共字段和结果目录约定不再散写在这里

## 目的

`E2-F3` 收口后，下一阶段不应继续零散补新的 batch 命令，而应该先把两件事统一下来：

1. batch manifest 的公共约定
2. 工作目录的组织方式

当前仓库已经有一些“像 batch”或“像工作目录”的能力：

- `bind-voice-track-batch`
- `scaffold-template`
- `templates <id> --write-examples`

但它们还没有被抽成一个明确阶段，也没有固定成未来所有 batch 命令都要复用的统一约定。

因此这份文档的目标是：

1. 定义 `E2-F4` 的明确范围
2. 固定本阶段的工作包顺序
3. 明确哪类 batch 入口应优先，哪类应延后

## 当前输入

### 已有 batch 样板

当前仓库里最早稳定落地的 batch 样板是：

- `bind-voice-track-batch`

它已经证明了三件事：

1. manifest 驱动是可行的
2. 相对路径按 manifest 所在目录解析是合理的
3. `results[] + succeededCount + failedCount + 稳定退出码` 这套部分成功语义是可复用的

但它的语义仍然偏单一：

- 只处理一种材料
- 只复用一种单项命令
- 不涉及多层工作目录组织

因此它更适合作为 batch contract 的样板，而不是直接扩成“所有 batch 命令的隐式标准”。

### 已有工作目录样板

当前仓库里已经能稳定落工作目录的主要入口是：

- `scaffold-template`

它已经能写出：

- `edit.json`
- `guide.json`
- `template.json`
- `artifacts.json`
- `template-params.json`
- `preview-*.edit.json`
- `commands.json`
- `commands.ps1`
- `commands.cmd`
- `commands.sh`

这说明仓库已经有“单任务工作目录”的雏形，但还没有回答：

- 多任务 batch 时目录层级怎么组织
- 公共输入、任务级产物、失败摘要该放哪里
- future Desktop 要消费哪个层级的文件

## 阶段目标

把现有单项高频工作流整理成一套稳定、可批量、可审计、可供 future Desktop 复用的 batch 与工作目录约定。

## 范围

### 本阶段应覆盖

- batch manifest 公共字段和路径解析规则
- 部分成功 / 全部失败 / manifest 失败的统一退出码策略
- 工作目录的顶层结构约定
- 首个高价值 batch 入口的选择与实现顺序
- 对 future Desktop 可复用的 batch summary / task summary 输出

### 本阶段不做

- 不做任务队列系统
- 不做后台 worker / scheduler
- 不做历史数据库
- 不做跨进程状态同步
- 不在本阶段同时铺开多个 batch 命令

## 设计原则

### 1. 先统一 contract，再补具体 batch 命令

本阶段优先级应是：

1. 固定 manifest 形状
2. 固定工作目录结构
3. 只选择一个最有复用价值的 batch 入口先落地

如果先各自实现 `batch-render`、`batch-scaffold`、`batch-attach`，很容易长出互不兼容的 manifest 与输出目录。

### 2. batch 必须复用单项 owner

不允许出现：

- batch 私有的第二套 plan 模型
- batch 私有的第二套 material selector
- batch 私有的第二套 signal 语义

batch 只负责：

- 读取 manifest
- 解析路径
- 调用单项语义
- 汇总结果

单项 mutation / validation / render / scaffold 语义仍必须留在既有 owner。

### 3. 工作目录要同时服务人、脚本和 future Desktop

工作目录结构不能只为 CLI 方便，也不能只为 future GUI 方便。

至少要同时满足：

- 人能快速定位单任务产物
- 脚本能稳定读取 summary 和任务结果
- future Desktop 能消费任务级状态和写盘结果，而不需要再发明另一套目录解释规则

### 4. 当前优先先做“批量建工作目录”，再做“批量消费工作目录”

本阶段首个实现候选更适合是：

- `scaffold-template-batch`

原因：

1. 它最容易先固定目录结构
2. 它最容易复用现有 `scaffold-template`
3. 它能为后续 `render-batch`、素材 batch、future Desktop 建立统一输入底座

相比之下，直接先做 `render-batch` 会更快碰到：

- 输出命名冲突
- 失败重试粒度
- 中间产物是否保留
- 工作目录解释规则尚未固定

## 建议工作包顺序

### E2-F4-W1：batch contract 固定

目标：

- 固定 batch manifest 的公共字段和公共约定

建议关注点：

- `schemaVersion`
- `items[]`
- item 级 `id`
- manifest 相对路径解析基准
- item 级 `writeTo` / `workdir` / `checkFiles` / `requireValid` 一类通用字段是否允许复用
- 统一 summary 字段：
  - `itemCount`
  - `succeededCount`
  - `failedCount`
  - `results[]`

完成判定：

- 新 batch 命令不需要再各自发明 manifest 顶层结构

### E2-F4-W2：工作目录结构固定

目标：

- 固定 batch 工作目录的层级组织

建议基础形状：

```text
<batch-root>/
  batch.json
  summary.json
  tasks/
    <task-id>/
      edit.json
      guide.json
      commands.json
      ...
```

建议关注点：

- 顶层 summary 放什么
- 单任务目录命名来自哪里
- 单任务失败时是否保留已写文件
- 与现有 `scaffold-template` 产物如何对齐

完成判定：

- future Desktop 与脚本都能基于同一套目录解释规则消费 batch 输出

### E2-F4-W3：首个 batch 入口优先级确定

目标：

- 只选一个最值得先实现的 batch 入口

当前建议顺序：

1. `scaffold-template-batch`
2. `render-batch`
3. 其他素材类 batch（如 attach / replace）

原因：

- `scaffold-template-batch` 更适合先固定工作目录
- `render-batch` 应建立在目录和 summary 已稳定之上
- 素材类 batch 更适合等公共 manifest 约定稳定后再复用

完成判定：

- 当前阶段不会再陷入“到底先做哪个 batch 命令”的摇摆

### E2-F4-W4：首个 batch 入口实现

目标：

- 基于前面三步，落第一个 batch 命令

当前推荐：

- `scaffold-template-batch`

最小要求：

- manifest 驱动
- 复用 `scaffold-template`
- 输出顶层 summary
- 输出任务级 `results[]`
- 统一退出码

## 当前建议的 batch manifest 草案

以下只是当前建议方向，不代表已实现：

```json
{
  "schemaVersion": 1,
  "items": [
    {
      "id": "job-a",
      "input": "inputs/a.mp4",
      "template": "shorts-captioned",
      "workdir": "tasks/job-a",
      "validate": true,
      "checkFiles": true
    }
  ]
}
```

说明：

- `id` 用于稳定任务级目录名和结果索引
- 相对路径统一以 manifest 所在目录为基准解析
- 如果某个 batch 入口不需要某些字段，允许在该命令中限制，但不应改顶层 contract 风格

## 验收标准

只有同时满足以下条件，才算 `E2-F4` 完成：

1. 至少一组高频工作流已有稳定 batch 入口
2. batch manifest 公共约定已固定
3. partial success / failed summary / 退出码语义已固定
4. 工作目录组织对人、脚本和 future Desktop 都可读
5. batch 仍明确复用单项 owner，不存在第二套语义

## 必须停下重判的情况

- 发现不同 batch 入口需要完全不同的 manifest 顶层结构
- 发现工作目录组织已经逼近持久化数据库或任务队列
- 发现 future Desktop 需要的状态无法通过现有文件 / summary 表达
- 发现 batch 命令开始直接拥有 `Core.Editing` 或 `Core.Execution` 的业务规则

## 与后续阶段的关系

`E2-F4` 完成后，才适合进入 `E2-G1`，重新判断是否启动 `D1 Desktop MVP`。

原因是：

- 如果 batch 和工作目录还没稳定，future Desktop 就没有明确的批处理消费边界
- 如果 manifest 与 summary 还在漂移，Desktop 很容易被迫长出自己的私有解释层

因此 `E2-G1` 必须建立在 `E2-F4` 已经先把 batch 与工作目录 contract 固定的前提下。

## 当前状态补充（2026-04-24）

当前已落地的 batch 命令：

- `scaffold-template-batch`
- `render-batch`
- `replace-plan-material-batch`
- `attach-plan-material-batch`
- `bind-voice-track-batch`

当前已固定的共识：

- manifest 相对路径统一按 manifest 所在目录解析
- 顶层统一写 `summary.json`
- 任务级统一写 `results/<id>.json`
- 退出码统一为 `0 / 2 / 1`

当前仍保留的差异：

- item 私有业务字段
- result 内部 payload 结构
- `bind-voice-track-batch` 对缺失 `id` 的兼容性兜底

## 当前建议下一步

如果按这份计划继续推进，当前最合理的下一步应是：

1. 先把新增的 batch contract 文档同步进 `roadmap` 和分阶段总表
2. 再判断是否需要把 batch handler 进一步拆出更明确的 owner
3. 最后再决定 `E2-F4` 是否已达到阶段检查条件
