# V2-P1-C1 validate-plan 增强规格稿

最后更新：2026-04-24

## 卡片信息

```text
卡片编号：V2-P1-C1
所属阶段：V2-P1
卡片类型：规格
目标：为 validate-plan 增强固定 v1-compatible 范围、非目标和准入约束
输入：docs/plans/v2/2026-04-24-ai-intelligent-workflows-design.md、当前 EditPlanValidator / validate-plan 行为
输出：本规格稿
完成标准：明确本轮增强不能越过 v1 边界，也不能把校验 owner 从 Core.Editing 漂移到 Cli
阻塞条件：如果需求要求同时引入 timeline / effects / render-v2 校验，则本卡失效并需升级到 V2-P2+
完成后下一张卡：V2-P1-C2
```

## 背景

当前 `validate-plan` 已经是 `edit.json schema v1` 的正式护栏能力：

- Core 侧由 `OpenVideoToolbox.Core.Editing.EditPlanValidator` 持有校验语义
- CLI 侧由 `validate-plan` 命令负责加载 plan、解析相对路径、接入插件模板上下文和输出 envelope
- 当前 payload 重点字段只有：
  - `planPath`
  - `resolvedBaseDirectory`
  - `checkFiles`
  - `isValid`
  - `issues`

这条链路已经稳定、已被文档和测试使用，因此任何增强都必须满足：

1. 不破坏现有 envelope 的可消费性
2. 不引入第二套校验语义
3. 不借机把 `validate-plan` 升级成 `schema v2` 入口

## 当前能力基线

当前 `EditPlanValidator` 已覆盖的能力包括：

- `source` 必填与文件存在性检查
- `output` 路径和 container 一致性
- `clips` 基础结构与重复 id 检查
- `audioTracks` 基础结构与文件存在性检查
- `artifacts` 基础结构、模板 slot 匹配与必填 slot 检查
- `transcript / beats / subtitles` 基础结构与文件存在性检查
- 插件模板来源解析与 `--plugin-dir` 上下文接入

当前明确还没有的能力包括：

- 统计摘要
- issue 的建议字段
- 校验层级标记
- 基于媒体探测的深度时长校验
- 针对 v2 `timeline/effects/transitions` 的任何规则

## 本轮目标

本轮 `V2-P1-C1` 只负责把 `validate-plan` 增强定义为一个**仍然属于 v1 的能力收口项**。

本轮增强的目标是：

1. 让 `validate-plan` 更容易被人和外部 AI 直接消费
2. 让失败信息更可操作，而不是只返回原始 issue 列表
3. 为后续是否值得进入更深的校验能力建立边界

## 本轮范围

本轮规格允许进入后续 `V2-P1-C2/C3` 计划与实现讨论的内容只有以下三类。

### 1. 输出摘要增强

允许新增非破坏性字段，用于减少调用方自己统计 `issues[]` 的成本。

允许的摘要方向包括：

- `stats.totalIssues`
- `stats.errorCount`
- `stats.warningCount`
- `stats.bySeverity`
- `stats.byCode`

要求：

- 现有 `isValid` 与 `issues` 保持原样可用
- 新字段只能是附加字段，不能改变现有字段语义
- 统计逻辑必须来源于 `Core.Editing` 的校验结果，而不是 CLI 重新发明规则

### 2. issue 可操作性增强

允许为每个 issue 增加更多消费友好的附加信息。

允许讨论的字段包括：

- `suggestion`
- `category`
- `checkStage`

要求：

- `path / code / message / severity` 继续保留
- 新字段缺失时不影响现有调用方
- issue 的分类和建议必须由 `Core.Editing` 生成或持有，不在 CLI 单独拼装第二套含义

### 3. 深度校验 feasibility 边界

允许在本轮计划阶段继续评估是否值得引入一个额外的深度校验模式，但本规格不承诺它一定落地。

允许评估的 `deep` 范围只限于：

- 基于 `ffprobe` 检查 `source.inputPath` 是否可探测
- 基于源素材总时长检查 v1 `clips[].in/out` 是否越界

本轮明确不允许把 `deep` 扩展为：

- v2 `timeline` 规则
- `effect` 参数规则
- `transition` 规则
- 多素材 clip `src` 解析与查询式素材选择

## 本轮不做

以下内容明确不属于 `V2-P1-C1`：

1. 不引入 `schemaVersion = 2`
2. 不新增 `timeline / tracks / effects / transitions`
3. 不把 `validate-plan` 变成 `render` 前的自动隐式探测流程
4. 不修改 `inspect-plan`、`replace-plan-material`、`attach-plan-material` 的 owner 边界
5. 不在 CLI 层复制 `EditPlanValidator` 的校验语义
6. 不承诺一次把 `--deep` 和所有统计增强全部做完

## 兼容性要求

本轮增强必须满足以下兼容性要求：

### CLI 兼容性

- 现有命令签名 `validate-plan --plan <edit.json> [--check-files] [--plugin-dir] [--json-out]` 继续可用
- 如果新增 `--deep`，必须作为可选 flag，而不是改变默认行为
- 当前 stdout / `--json-out` 的 envelope 外层结构不变

### 文档兼容性

- README、FEATURES、COMMAND_REFERENCE 中当前 `validate-plan` 的基本用法不能失效
- 模板工作流里已经引用 `validate-plan --check-files` 的地方不需要全部跟着改

### 测试兼容性

- 现有 `validate-plan` 相关集成测试、契约快照和模板工作流测试必须继续通过
- 若输出新增字段导致快照变化，应以“附加字段”方式更新，而不是重排已有结构

## 建议的输出形态

本轮推荐采用“保留现有字段 + 追加摘要字段”的形态，而不是重做 payload。

建议形态：

```jsonc
{
  "planPath": "E:/work/edit.json",
  "resolvedBaseDirectory": "E:/work",
  "checkFiles": true,
  "checkMode": "basic",
  "isValid": false,
  "issues": [
    {
      "severity": "error",
      "path": "clips[1]",
      "code": "clips.range.invalid",
      "message": "Clip 'c2' must end after it starts.",
      "suggestion": "Adjust clip out so it is greater than clip in."
    }
  ],
  "stats": {
    "totalIssues": 1,
    "errorCount": 1,
    "warningCount": 0
  }
}
```

说明：

- `checkMode` 是可选候选字段，用于区分 `basic` / `deep`
- `stats` 是追加摘要，不替代 `issues`
- `suggestion` 是追加字段，不替代 `message`

## owner 约束

### Core.Editing 必须拥有

- issue 的正式结构
- 统计口径
- `isValid` 判定
- 深度校验规则本身

### Cli 只能拥有

- 参数解析
- `--plugin-dir` 上下文接入
- envelope 输出
- 退出码映射

如果后续计划卡发现需要在 CLI 里单独维护：

- issue 分类
- stats 统计
- suggestion 生成

则说明本轮范围已经越界，应停下重判。

## 后续计划卡需要回答的问题

进入 `V2-P1-C2` 时，必须回答以下问题：

1. `stats` 是由 `EditPlanValidationResult` 持有，还是由新的 Core 辅助结果模型持有？
2. `suggestion` 是每条 issue 静态映射，还是只对部分高频 code 提供？
3. `--deep` 是否值得在本轮进入实现，还是只做 feasibility 结论？
4. 如果引入 `ffprobe`，它的 owner 是否仍然只通过 `Core.Media` / `Core.Execution` 进入，而不是 CLI 直连？
5. 现有快照与文档最小需要同步哪些地方？

## 测试与验收边界

本轮规格要求后续至少准备以下测试面：

1. 合法 plan 的 `stats` / `issues` 输出
2. 非法 plan 的 `stats` / `issues` 输出
3. 插件模板场景下的 `validate-plan --plugin-dir`
4. `--check-files` 与默认模式的差异
5. 如果 `--deep` 进入实现，新增一组基于真实或 fake probe 结果的越界 case

## 必须停下重判的情况

出现以下任一情况时，不应继续把这张卡往实现推进：

1. 需要引入 `timeline/effects/transitions` 才能完成需求
2. 需要在 CLI 层复制 Core 的校验规则
3. 需要把 `validate-plan` 自动嵌进 `render` 主路径
4. `--deep` 的真实需求已经变成“v2 校验入口”

## 当前结论

`validate-plan` 增强适合作为 `V2-P1` 的首张规格卡，原因是：

- 它已经是现有正式工作流的一部分
- 当前 owner 清晰
- 可以通过附加字段和可选模式增强，而不必立即升级到 v2

但本轮必须守住一点：

> 这是一张 `v1-compatible` 规格卡，不是 `schema v2` 的隐形入口。
