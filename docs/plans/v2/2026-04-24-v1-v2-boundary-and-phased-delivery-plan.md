# V1 / V2 边界与分阶段实施清单

最后更新：2026-04-25

## 目的

`docs/plans/v2/` 下已经存在一组完整的 v2 设计，但当前仓库的主实施线仍然是：

- `E2-A*`：生态 / 分发 / 社区 / 基线
- `E2-F*`：功能交付 / 工作流收口 / Desktop 预留

这意味着 v2 设计当前不能被当成“已经立项、可以并行全面开工”的实现线。

如果不先把 `v1` 与 `v2` 的边界冻结清楚，后续很容易出现三类问题：

1. 还在 `schema v1` 稳定窗口内，就提前把 `timeline/effects/render-v2` 混进现有代码
2. 现有 batch / render / template contract 还没正式关门，就又长出第二套批量和执行 owner
3. v2 文档里的能力池被当成当前活跃需求，导致 roadmap、代码与文档互相打架

因此这份文档只做三件事：

1. 固定 `v1` 与 `v2` 的当前边界
2. 把 `docs/plans/v2/*.md` 拆成可追踪的阶段清单
3. 明确哪些项可以先按 `v1-compatible` 方式落地，哪些必须等 v2 正式立项

## 与现有文档的关系

- `docs/roadmap.md`
  - 声明当前活跃工作面与阶段门
- `docs/plans/2026-04-24-e2-feature-delivery-staged-plan.md`
  - 负责当前功能交付线
- 本文档
  - 负责 `docs/plans/v2/*.md` 的边界冻结、准入门和后续阶段拆分
- `docs/plans/v2/*.md`
  - 保留各专项设计细节，但不单独定义“当前是否开工”

本文档不改变 canonical owner；owner 仍以 `docs/ARCHITECTURE_GUARDRAILS.md` 和各模块 `MODULE.md` 为准。

## 当前总判断

- 当前仓库仍以 `v1` 为唯一正式交付契约。
- `v2` 当前只处于：
  - 边界冻结
  - 依赖关系梳理
  - 候选 backlog 排序
- 在满足正式准入门之前，`v2` 不作为当前活跃实现线。

## V1 边界

`v1` 指当前已经落地并受现有测试、快照和用户文档保护的正式能力边界。

### v1 当前正式 owner 的能力

- `schemaVersion = 1` 的 `edit.json`
- 顶层 `source / clips / audioTracks / artifacts / transcript / beats / subtitles / output`
- 现有模板发现、plan 生成、signal attach、素材替换、voice bind、render / mix-audio
- 已冻结的 batch contract：
  - `scaffold-template-batch`
  - `render-batch`
  - `replace-plan-material-batch`
  - `attach-plan-material-batch`
  - `bind-voice-track-batch`

### v1 结束的两个时间点

为避免继续把“v1 不再扩功能”和“v1 仍需兼容运行”混成一件事，后续统一拆成两个节点：

#### 1. V1 Feature Freeze

满足以下条件时，`v1` 停止新增功能，只接受：

- bug 修复
- 文档同步
- 契约不破坏的兼容层
- 为 v2 parity 服务的最小桥接

进入条件：

1. `E2-F4` 已正式关闭
2. `V2-P1` 已完成并人工接受
3. 明确决定进入 `V2-P2`

一旦进入这个节点，就不再接受新的“纯 v1 功能扩面”需求。

#### 2. V1 Runtime Sunset

满足以下条件前，`v1` 仍然是正式受支持运行路径：

1. `V2-P5` 已完成并人工接受
2. 至少一条正式 v2 工作流已可替代对应 v1 路径
3. v1 / v2 parity 与迁移说明已经文档化

在达到这个节点之前，禁止把“开始做 v2”误解成“可以删 v1”。

### v1 当前明确不包含

- `timeline.tracks`
- clip / track 级 `effects`
- `transitions`
- `EffectRegistry`
- 第二套 timeline render builder
- 查询式素材引用与自动素材索引
- 数据源驱动模板渲染
- 图表渲染后端
- 面向 NLE 的正式互操作导出链

### v1 阶段内允许继续增强的内容

以下增强仍可在不启动 v2 的前提下推进，但必须保持 `v1-compatible`：

| 项目 | 允许方式 | 不允许方式 |
| --- | --- | --- |
| `validate-plan` 增强 | 补更细的结构化 issue、stats、可选 deep 校验 | 借机引入 `timeline/effects` 必填语义 |
| `auto-cut-silence` | 输出现有 `clips` 或标准 v1 plan | 直接要求 schema v2 |
| `export` L1 | 用 v1 plan 包装为粗粒度导出模型 | 反向要求先落完整 v2 render |
| 未来的素材辅助能力 | 作为显式 CLI 辅助命令 | 自动改写 render 主流程、引入隐式执行语义 |

## V2 边界

`v2` 指会改变长期计划模型、渲染执行语义或 CLI 合约形态的一组新能力。

### v2 命名约定

为避免混淆，后续统一采用：

- `TimelineEffect`
  - 表示 `edit.json timeline` 中的 effect 实例
- `IEffectDefinition`
  - 表示 effect registry / plugin / built-in 使用的效果描述符

除非进入专门的重命名卡，否则后续不要再把这两层都叫 `EffectDefinition`。

### v2 的核心组成

| 能力 | 对应文档 | 说明 |
| --- | --- | --- |
| `edit.json schema v2` | `2026-04-24-edit-json-schema-v2-design.md` | 引入 timeline / tracks / effects / transitions |
| 效果库 | `2026-04-24-template-effects-library-design.md` | 引入效果描述符、参数 schema、注册表、发现命令 |
| v2 渲染引擎 | `2026-04-24-v2-render-engine-design.md` | 第二套基于 timeline 的 filter graph builder |
| 智能工作流 | `2026-04-24-ai-intelligent-workflows-design.md` | 包含 validate 增强、auto-cut-silence、resolve-assets |
| 数据驱动批量生产 | `2026-04-24-batch-production-design.md` | 引入 `${var}` 模板注入、slot 条件裁剪、图表效果 |
| 导出互操作 | `2026-04-24-export-interop-design.md` | 导出到 Premiere XML / fcpxml / EDL |

### v2 的正式特征

只要出现以下任一项，就应视为进入 `v2` 范围，而不是 `v1` 增量：

1. `EditPlan` 长期模型新增 `timeline/tracks/effects/transitions`
2. `render` 引入 timeline 分发逻辑
3. CLI 新增围绕 `effect registry` 的正式命令面
4. batch 渲染从“消费既有 plan”转为“模板 + 数据源直接生成 N 个视频”
5. 模板 / 插件开始声明新的执行语义，而不只是现有 plan schema 的变体

## V1 / V2 分界规则

后续评估或实施时，先用下面这张表判断归属：

| 问题 | 归到 v1 | 归到 v2 |
| --- | --- | --- |
| 是否改变正式 `edit.json` 长期结构？ | 否 | 是 |
| 是否需要第二套 render command builder？ | 否 | 是 |
| 是否会新增新的长期 owner（effects/export/indexing）？ | 否 | 是 |
| 是否会让现有 batch contract 失效或并存第二套？ | 否 | 是 |
| 是否可以只作为显式 CLI 辅助，不改变 render 主路径？ | 是 | 否 |

如果答案混杂，默认按 `v2` 处理，先走阶段门，而不是直接实现。

## 依赖链总览

```text
V2-P0 边界冻结与准入门
   ↓
V2-P1 v1-compatible 孵化项
   ↓
V2-P2 Schema v2 合约层
   ↓
V2-P3 效果描述与 validator 扩展
   ↓
V2-P4 v2 渲染引擎与 v1 parity
   ↓
V2-P5 首批用户能力交付
   ├─ export L1
   ├─ auto-cut-silence v2 接线
   ├─ effect discovery
   └─ 首个 v2 模板工作流
   ↓
V2-P6 数据驱动批量 / resolve-assets / 图表能力
```

说明：

- `export L1` 可以先作为 `V2-P1` 预研项存在，但要正式交付为长期能力，仍建议等 `V2-P2` 后统一收口
- `resolve-assets`、`${var}` 批量模板、`data-visualize` 都放在更后阶段，避免与现有 batch / render contract 冲突

## 分阶段实施清单

## V2-P0：边界冻结与准入门

**状态：当前阶段**

### 目标

- 明确 `v1` 和 `v2` 的正式边界
- 明确哪些设计只保留为 backlog，不进入实现
- 明确 v2 的开工前提，避免直接与当前 `E2-F*` 冲突

### 本阶段交付物

1. 本文档
2. `docs/plans/v2/*.md` 与本文档的角色分工固定
3. roadmap 中明确 v2 当前不是活跃实现线

### 准入门

只有同时满足以下条件，才允许进入 `V2-P1` 之后的正式实施：

1. `E2-F4` 已正式收口，不再继续滚动扩命令面
2. `schema v1` 已进入低频变更窗口，至少不再处于高频加字段阶段
3. 已明确 v2 的 canonical owner 列表，而不是只在文档里列概念
4. 已同意 v1 / v2 双轨维护的测试成本

### 暂停条件

- 发现当前仍需要继续扩 `E2-F*` 的高频工作流
- 发现 Desktop 判断已比 v2 更紧急
- 发现 v2 会引入新的外部运行时依赖，但没有明确接受结论

## V2-P1：V1-Compatible 孵化项

**状态：候选阶段，允许择优单独立项**

### 目标

先从 v2 文档中拆出少量不破坏 `v1` 契约的高价值项，验证需求和 owner，而不是整包推进。

### 可进入本阶段的项

| 条目 | 来源文档 | 当前判断 |
| --- | --- | --- |
| `validate-plan` issue / stats / optional deep mode | `ai-intelligent-workflows-design.md` | 可单独推进 |
| `auto-cut-silence` 输出 v1 clips / v1 plan | `ai-intelligent-workflows-design.md` | 可单独推进 |
| `export` L1 feasibility spike | `export-interop-design.md` | 可预研，可择机交付 |

### 本阶段不做

- 不改 `EditPlan` 正式长期结构
- 不引入 `timeline`
- 不引入效果注册表
- 不改 `render` 主路径
- 不新开数据源驱动批量 contract

### 验收标准

1. 每个子项都能在现有 v1 contract 下解释清楚
2. 不需要改写现有模板 / plugin / batch 语义
3. 文档和测试能证明它是 `v1-compatible`

## V2-P2：Schema v2 合约层

**状态：已完成**

### 目标

只落 `EditPlan` 的 v2 合约层和 validator 双轨支持，不进入渲染引擎实现。

### 范围

- `timeline / tracks / clips / effects / transitions` 类型
- `schemaVersion = 2`
- v2 validator 扩展
- v1 / v2 双轨反序列化与基础校验

### 本阶段不做

- 不改 `render`
- 不改 `mix-audio`
- 不开放生产可用的 v2 模板
- 不把 v2 plan 当成默认输出

### 交付清单

1. 类型定义落地
2. v2 validator 落地
3. v1 parity 测试基线建立
4. 典型 v1 case 的兼容守护测试冻结

### 进入条件

1. `V2-P1` 已结束或明确暂停
2. 已接受新增测试预算
3. 已确认 `Core.Editing` 与 `Core.Execution` 的双轨边界

## V2-P3：效果描述与发现层

**状态：已完成**

### 目标

在不直接上线完整 v2 render 的前提下，先把 effect schema、注册表和发现命令收口。

### 范围

- `IEffectDefinition`
- `EffectParameterSchema`
- `EffectRegistry`
- `list effects`
- `describe effect`
- validator 与 effect schema 的接线

### 本阶段不做

- 不承诺所有效果都可执行
- 不引入图表渲染后端
- 不开放插件 effects 覆盖内置 effects，除非 owner 已明确

### 风险

- 新增长期 owner
- 插件 effects 容易让模板扩展面升级为“执行语义插件”

## V2-P4：v2 渲染引擎与 v1 Parity

**状态：已完成**

### 目标

引入 timeline render builder，但必须先证明不会打破现有 v1 render 结果。

### 范围

- `FfmpegTimelineRenderCommandBuilder`
- effect template engine / executor 接线
- render 分发逻辑
- 日志与错误定位

### 强制验收

1. v1 plan 继续走既有路径，行为不变
2. v2 plan 有独立测试集
3. 至少一组 v1 parity case 持续通过
4. CLI failure envelope 不因 v2 路径回退成新的不一致结构

## V2-P5：首批正式交付能力

**状态：已完成并人工接受**

### 目标

在 v2 合约和执行层稳定后，交付第一批用户可见能力。

### 候选优先级

1. `export` L1 粗粒度导出
2. `auto-cut-silence` 的 v2 接线
3. `effect discovery` 相关 CLI
4. 首个真正消费 timeline/effects 的模板示例

### 进入标准

只有当 `V2-P2 ~ V2-P4` 都已经达到“可回归、可文档化、可解释错误”时，才进入本阶段。

### 阶段收口结果

- `export L1` 已可统一消费 v1 / v2 plan
- `auto-cut-silence --template timeline-effects-starter` 已可真实产出 v2 plan
- 首个 built-in v2 模板工作流已正式跑通
- 当前阶段已关闭，不再继续向 `V2-P5` 混入新能力
- 阶段后的 parity / migration 口径已补到：
  - [2026-04-25-v1-v2-parity-and-migration.md](./2026-04-25-v1-v2-parity-and-migration.md)

## V2-P6：高风险扩面项

**状态：未开始，默认延后**

### 包含内容

- `resolve-assets`
- 数据源驱动 `run-batch`
- `${var}` 深度注入
- `slot` 条件裁剪
- `data-visualize`
- Cairo / Magick.NET 等额外图形依赖

### 延后原因

1. 会与现有 batch contract 形成第二套实施线
2. 会引入新的外部依赖或运行时复杂度
3. 会把模板系统从“声明 plan”推进到“声明执行逻辑”
4. 对 future Desktop、plugin、文档和测试的冲击都更大

因此这些项默认不进入当前短期 backlog，除非前面阶段已经收口并重新评估。

## 设计文档到阶段的映射

| 文档 | 主要落点 | 当前动作 |
| --- | --- | --- |
| `2026-04-24-edit-json-schema-v2-design.md` | `V2-P2` | 暂不实现，只保留设计 |
| `2026-04-24-template-effects-library-design.md` | `V2-P3` | 暂不实现，只保留设计 |
| `2026-04-24-v2-render-engine-design.md` | `V2-P4` | 暂不实现，只保留设计 |
| `2026-04-24-export-interop-design.md` | `V2-P1` / `V2-P5` | 允许先做 feasibility，正式交付放后 |
| `2026-04-24-ai-intelligent-workflows-design.md` | `V2-P1` / `V2-P6` | validate / auto-cut 可拆，resolve-assets 延后 |
| `2026-04-24-batch-production-design.md` | `V2-P6` | 当前整包延后 |
| `2026-04-25-narrated-slides-video-spec.md` | `V2-P6` | 已完成 `C1 ~ C5` 首轮实现，当前待 `C6` 人工反馈 |

## 当前可执行清单

如果后续需要按任务追踪，默认优先从上到下选，不跳阶段：

### Ready Now

1. 为 `validate-plan` 增强单独做 feasibility / 设计收口
2. 把 `auto-cut-silence` 改写为 `v1-compatible` 交付草案
3. 为 `export L1` 做格式优先级和 golden file 策略预研

### Not Ready Yet

1. `schema v2` 正式类型落地
2. effect registry
3. v2 render builder
4. 查询式素材索引
5. 数据驱动 `run-batch`
6. 图表渲染后端

## 阶段追踪模板

后续如果进入任一 `V2-P*` 阶段，统一按以下模板补阶段文档或阶段检查：

```text
阶段：
状态：
目标：
范围：
本阶段不做：
前置依赖：
交付物：
验收标准：
必须停下重判的情况：
当前结论：
下一步：
```

## 阶段执行循环

后续每个 `V2-P*` 阶段都不直接按“想到什么做什么”的方式推进，而是固定采用以下循环：

```text
规格 -> 计划 -> 执行 -> 测试 -> 修复 -> 阶段完成人工反馈
```

### 循环规则

1. `V2-P*` 才是人工验收粒度；`C1 ~ C5` 是阶段内内部卡，不是默认对外停顿点。
2. 一个阶段内可以串行推进多个已选子项，但每个子项都必须按 `规格 -> 计划 -> 执行 -> 测试 -> 修复` 走完。
3. `规格` 卡未完成前，不进入具体开发。
4. `计划` 卡未完成前，不开始正式改代码。
5. `测试` 卡必须覆盖本阶段声明的最小回归面，而不是只跑“能过的那几个”。
6. `修复` 卡结束后，如果阶段范围内仍有剩余子项，继续推进剩余子项；只有阶段范围整体完成后，才进入人工反馈。
7. 阶段未提供可手动验证的验收包前，不得标记为 `ready_for_acceptance`。
8. 验收包至少必须包含：
   - 可直接运行的命令或脚本
   - 最小样例输入或样例生成步骤
   - 预期输出字段或可见结果
   - 明确的通过 / 不通过标准
9. 每个阶段完成后，必须停在“人工反馈”节点，由人工决定：
   - 进入下一阶段
   - 留在当前阶段补阶段缺口
   - 回退到上一阶段重判
   - 暂停整个 v2 线

### 任务卡状态

统一使用以下状态，便于追踪：

- `pending`：未开始
- `in_progress`：正在处理
- `blocked`：被前置依赖或人工决策阻塞
- `done`：本卡完成
- `ready_for_acceptance`：本阶段范围已完成，等待人工阶段验收
- `accepted`：该阶段人工反馈已确认，可进入下一阶段

### 统一任务卡模板

每张任务卡统一采用以下结构：

```text
卡片编号：
所属阶段：
卡片类型：规格 / 计划 / 执行 / 测试 / 修复 / 人工反馈
目标：
输入：
输出：
完成标准：
阻塞条件：
完成后下一张卡：
```

## 分阶段任务卡

以下任务卡是当前默认追踪视图。除 `V2-P0` 外，其余阶段默认初始状态为 `pending` 或 `blocked`。

## V2-P0 任务卡

### V2-P0-C1 规格

- 目标：冻结 `v1` / `v2` 边界、准入门与延期项定义
- 输入：`docs/roadmap.md`、`docs/plans/v2/*.md`、当前代码事实
- 输出：边界文档与阶段准入门
- 完成标准：`v1` 正式边界、`v2` 组成、准入门、延后项都已文档化
- 阻塞条件：对当前活跃工作面判断不清
- 完成后下一张卡：`V2-P0-C2`

### V2-P0-C2 计划

- 目标：确定 `V2-P*` 依赖顺序与阶段命名
- 输入：`docs/plans/v2/*.md`
- 输出：`V2-P0 ~ V2-P6` 依赖链与阶段说明
- 完成标准：每阶段有目标、范围、进入条件、暂停条件
- 阻塞条件：专项设计之间依赖关系不明
- 完成后下一张卡：`V2-P0-C3`

### V2-P0-C3 执行

- 目标：写出边界与阶段总文档
- 输入：`V2-P0-C1/C2`
- 输出：当前这份总文档
- 完成标准：文档已落盘，可作为后续 v2 唯一总入口
- 阻塞条件：文档角色与 roadmap 冲突
- 完成后下一张卡：`V2-P0-C4`

### V2-P0-C4 测试

- 目标：检查文档是否与现有 roadmap、guardrails、代码现状一致
- 输入：总文档、`docs/roadmap.md`、`MODULE.md`
- 输出：一致性检查结论
- 完成标准：没有把 v2 误写成当前活跃实现线
- 阻塞条件：发现边界与现有文档冲突
- 完成后下一张卡：`V2-P0-C5`

### V2-P0-C5 修复

- 目标：修正文档中的阶段冲突、边界冲突或依赖表述问题
- 输入：`V2-P0-C4` 发现的问题
- 输出：修正版文档
- 完成标准：v2 只保留为边界冻结与候选 backlog 线
- 阻塞条件：需要变更当前产品路线
- 完成后下一张卡：`V2-P0-C6`

### V2-P0-C6 人工反馈

- 目标：由人工确认是否接受这套边界、阶段门与追踪结构
- 输入：`V2-P0` 全部产出
- 输出：`accepted / stay / rollback / pause`
- 完成标准：人工明确给出下一步方向
- 阻塞条件：人工尚未反馈
- 完成后下一张卡：`V2-P1-C1` 或返回当前阶段

## V2-P1 任务卡

### 阶段说明

本阶段默认只从以下三个候选中选一到两个推进，不并行铺开全部；但一旦进入本阶段，默认由 AI 在阶段内连续做完，不按单子项停顿：

- `validate-plan` 增强
- `auto-cut-silence` 的 `v1-compatible` 落地
- `export L1` feasibility / 预研

### 当前轮次

- 上一阶段：`V2-P1`
- 上一阶段状态：`accepted`
- 上一阶段已完成子项：
  - `validate-plan` 增强
  - `auto-cut-silence` 的 `v1-compatible` 落地
- 当前阶段：`V2-P5`
- 阶段状态：`accepted`
- 当前子项：首个真正消费 timeline/effects 的模板示例
- 当前阶段验收状态：`accepted`
- 当前稿件：
  - [2026-04-24-v2-render-engine-design.md](2026-04-24-v2-render-engine-design.md)
  - [2026-04-24-v2-p4-phase-check.md](2026-04-24-v2-p4-phase-check.md)
  - [2026-04-24-v2-p4-acceptance-checklist.md](2026-04-24-v2-p4-acceptance-checklist.md)
  - [2026-04-24-v2-p5-phase-check.md](2026-04-24-v2-p5-phase-check.md)
  - [2026-04-24-v2-p5-acceptance-checklist.md](2026-04-24-v2-p5-acceptance-checklist.md)
  - [2026-04-25-v1-v2-parity-and-migration.md](2026-04-25-v1-v2-parity-and-migration.md)
- 当前说明：
  - `V2-P1` 已通过人工反馈并进入下一阶段
  - `SchemaVersions.V2`、`EditPlan.Timeline` 与 timeline 类型已落地
  - `EditPlanValidator` 已补 `schema v2` timeline 结构校验
  - CLI `validate-plan` 已支持装载并校验 `schemaVersion = 2` 的 plan
  - built-in effect catalog、`effects list/describe` 与 `validate-plan` 的内置 effect 识别已落地
  - `render --plan` 已支持装载 `schemaVersion = 2`，并在 `Core.Execution` 内部完成 v1/v2 builder 分发
  - `FfmpegTimelineRenderCommandBuilder` 已具备基础 timeline render baseline：输入收集、模板型 effect filter、基础 transition / overlay / amix、preview / execute dispatch
  - 已完成全量验证：`dotnet build OpenVideoToolbox.sln`、`dotnet test OpenVideoToolbox.sln`
  - `V2-P4` 阶段检查已完成，当前已完成最小 render baseline
  - 已新增 `template.planModel`，作为 v1 / v2 模板路径的显式字段
  - 首个 built-in v2 模板 `timeline-effects-starter` 已落地，并已打通：
    - `templates timeline-effects-starter`
    - `init-plan --template timeline-effects-starter`
    - `render --plan <generated-v2-plan> --preview`
    - `init-plan --template timeline-effects-starter --seed-from-transcript/--seed-from-beats`
  - `V2-P5` 阶段检查、验收清单与 parity / migration 文档已补齐
  - 当前阶段仍只覆盖首个正式模板样例、其 transcript / beats seed 接线与首个信号驱动 planner，不纳入插件 v2 模板、数据驱动 batch 或更多 effect 扩面
  - 当前结论已固定为：`V2-P5` 关闭、`v1` 继续正式支持、是否进入下一阶段需单独决策

### V2-P1-C1 规格

- 目标：为本轮选中的孵化项固定范围、非目标与 `v1-compatible` 约束
- 输入：对应专项设计文档、当前 `v1` contract
- 输出：单项规格说明
- 完成标准：明确“不改 schema 长期结构、不改 render 主路径、不新开第二套 batch contract”
- 阻塞条件：选题需要进入 `timeline/effects/render-v2`
- 完成后下一张卡：`V2-P1-C2`

### V2-P1-C2 计划

- 目标：拆出实现顺序、测试面和文档同步点
- 输入：`V2-P1-C1`
- 输出：实现计划与最小验证清单
- 完成标准：明确 owner、文件范围、测试命令、失败回滚点
- 阻塞条件：无法复用现有 owner
- 完成后下一张卡：`V2-P1-C3`

### V2-P1-C3 执行

- 目标：实现当前选中的孵化项
- 输入：`V2-P1-C2`
- 输出：代码与文档变更
- 完成标准：实现保持 `v1-compatible`
- 阻塞条件：需要触碰 `schema v2` 或第二套 builder
- 完成后下一张卡：`V2-P1-C4`

### V2-P1-C4 测试

- 目标：验证单项功能、回归现有 `v1` contract、检查 failure path
- 输入：代码改动、现有测试基线
- 输出：测试结果与未覆盖风险
- 完成标准：新增测试通过，现有相关回归不过载
- 阻塞条件：测试暴露出 `v1` 契约破坏
- 完成后下一张卡：`V2-P1-C5`

### V2-P1-C5 修复

- 目标：修复测试失败、契约漂移和文档不一致
- 输入：`V2-P1-C4` 问题清单
- 输出：最终可提交版本
- 完成标准：维持 `v1-compatible`、文档同步完成
- 阻塞条件：修复后仍需要升级为 `v2`
- 完成后下一张卡：`V2-P1-C6`

### V2-P1-C6 人工反馈

- 目标：由人工确认整个 `V2-P1` 阶段是否接受，而不是单个子项是否接受
- 输入：本阶段全部已选子项的实现结果、测试结果、剩余风险与阶段总结
- 输出：`accepted / continue-P1 / escalate-to-P2 / pause`
- 完成标准：人工明确给出下一步
- 阻塞条件：人工尚未反馈
- 完成后下一张卡：下一个 `V2-P1-C1` 或 `V2-P2-C1`

## V2-P2 任务卡

### V2-P2-C1 规格

- 目标：固定 schema v2 只包含哪些类型与校验，不包含 render 改造
- 输入：`edit-json-schema-v2-design.md`
- 输出：schema v2 最小合约清单
- 完成标准：`timeline / tracks / clips / effects / transitions` 边界清楚，且明确“不进 render”
- 阻塞条件：需求要求同时交付执行能力
- 完成后下一张卡：`V2-P2-C2`

### V2-P2-C2 计划

- 目标：制定类型落地、序列化、validator、parity 测试的拆分顺序
- 输入：`V2-P2-C1`
- 输出：实施计划
- 完成标准：已列出代码文件、测试集、兼容 case、文档同步点
- 阻塞条件：v1 parity 策略不清
- 完成后下一张卡：`V2-P2-C3`

### V2-P2-C3 执行

- 目标：落类型、schema 版本与双轨 validator
- 输入：`V2-P2-C2`
- 输出：`Core.Editing` 相关代码改动
- 完成标准：v2 合约可被解析和校验，但不进入 render 主路径
- 阻塞条件：需要顺手改执行链
- 完成后下一张卡：`V2-P2-C4`

### V2-P2-C4 测试

- 目标：覆盖序列化、反序列化、validator 与 v1 parity
- 输入：代码改动、典型 v1 case
- 输出：测试结果
- 完成标准：v1 case 继续通过，v2 case 有最小保护
- 阻塞条件：v1 case 出现行为漂移
- 完成后下一张卡：`V2-P2-C5`

### V2-P2-C5 修复

- 目标：修复 schema 双轨冲突、序列化问题和 validator 回归
- 输入：`V2-P2-C4` 失败项
- 输出：稳定的 schema v2 合约层
- 完成标准：未引入 render 分流，未破坏 v1
- 阻塞条件：必须连带修改 render 才能通过
- 完成后下一张卡：`V2-P2-C6`

### V2-P2-C6 人工反馈

- 目标：由人工决定是否接受 schema v2 合约层并进入效果层
- 输入：类型、validator、测试结果、兼容性说明
- 输出：`accepted / stay / rollback / pause`
- 完成标准：人工明确允许或拒绝进入 `V2-P3`
- 阻塞条件：人工尚未反馈
- 完成后下一张卡：`V2-P3-C1`

## V2-P3 任务卡

### V2-P3-C1 规格

- 目标：固定 effect schema、发现命令与执行器边界
- 输入：`template-effects-library-design.md`
- 输出：效果层规格说明
- 完成标准：明确“发现层”和“执行层”的边界，不把插件 effect 直接升级为运行时代码插件
- 阻塞条件：owner 无法明确
- 完成后下一张卡：`V2-P3-C2`

### V2-P3-C2 计划

- 目标：拆分 `EffectRegistry`、CLI 命令、validator 接线、内置效果最小集
- 输入：`V2-P3-C1`
- 输出：实施顺序与测试面
- 完成标准：至少明确 P0 效果最小集与哪些只描述不执行
- 阻塞条件：需要同时引入图表后端
- 完成后下一张卡：`V2-P3-C3`

### V2-P3-C3 执行

- 目标：实现 effect 描述、注册、列举和说明命令
- 输入：`V2-P3-C2`
- 输出：`EffectRegistry`、`list/describe effects` 等
- 完成标准：效果可被发现和校验，不要求全部渲染可用
- 阻塞条件：必须依赖 v2 render 才能落地
- 完成后下一张卡：`V2-P3-C4`

### V2-P3-C4 测试

- 目标：验证效果参数 schema、注册表行为、CLI 输出和错误路径
- 输入：代码改动
- 输出：测试结果
- 完成标准：CLI 输出稳定，validator 能消费 effect schema
- 阻塞条件：效果定义与现有插件/模板边界冲突
- 完成后下一张卡：`V2-P3-C5`

### V2-P3-C5 修复

- 目标：修复 effect 描述层与 CLI / validator 的不一致
- 输入：`V2-P3-C4` 问题清单
- 输出：修正版实现
- 完成标准：效果层可独立存在，不强绑 v2 render
- 阻塞条件：修复需要引入新的外部运行时依赖
- 完成后下一张卡：`V2-P3-C6`

### V2-P3-C6 人工反馈

- 目标：由人工决定是否接受 effect 描述层并进入 v2 render
- 输入：功能结果、测试结果、owner 风险
- 输出：`accepted / stay / rollback / pause`
- 完成标准：人工明确是否进入 `V2-P4`
- 阻塞条件：人工尚未反馈
- 完成后下一张卡：`V2-P4-C1`

## V2-P4 任务卡

### V2-P4-C1 规格

- 目标：固定 v2 render 只解决哪些执行语义，以及 v1 parity 要求
- 输入：`v2-render-engine-design.md`
- 输出：render v2 规格说明
- 完成标准：已明确 builder 范围、错误定位要求、v1/v2 分发逻辑
- 阻塞条件：还想继续扩大 effect / export / batch 范围
- 完成后下一张卡：`V2-P4-C2`

### V2-P4-C2 计划

- 目标：拆分 builder、filter graph、错误映射、preview / execute 接线与 parity 测试
- 输入：`V2-P4-C1`
- 输出：执行计划
- 完成标准：先后顺序明确，且 parity 测试先于大规模功能扩面
- 阻塞条件：无法定义最小 v2 render case
- 完成后下一张卡：`V2-P4-C3`

### V2-P4-C3 执行

- 目标：实现 timeline render builder 与分流逻辑
- 输入：`V2-P4-C2`
- 输出：`Core.Execution` 与 CLI 相关改动
- 完成标准：v2 plan 可走新路径，v1 plan 继续走旧路径
- 阻塞条件：导致现有 v1 render 行为漂移
- 完成后下一张卡：`V2-P4-C4`

### V2-P4-C4 测试

- 目标：验证 v2 render case、preview / execute、v1 parity 和 failure envelope
- 输入：代码改动、parity case
- 输出：测试结果
- 完成标准：v1 parity 通过，v2 路径有最小可回归集
- 阻塞条件：错误路径无法稳定定位
- 完成后下一张卡：`V2-P4-C5`

### V2-P4-C5 修复

- 目标：修复 v1 parity 回归、错误映射和 filter 定位问题
- 输入：`V2-P4-C4` 问题清单
- 输出：稳定的 render v2
- 完成标准：满足阶段强制验收
- 阻塞条件：需要扩大 schema 或 effect 范围才能修
- 完成后下一张卡：`V2-P4-C6`

### V2-P4-C6 人工反馈

- 目标：由人工决定是否接受 v2 render 并开放首批用户能力
- 输入：执行结果、测试结果、剩余风险
- 输出：`accepted / stay / rollback / pause`
- 完成标准：人工明确是否进入 `V2-P5`
- 阻塞条件：人工尚未反馈
- 完成后下一张卡：`V2-P5-C1`

## V2-P5 任务卡

### V2-P5-C1 规格

- 目标：确定首批对外开放的 v2 能力组合与非目标
- 输入：`export-interop-design.md`、`ai-intelligent-workflows-design.md`、前置阶段结果
- 输出：首批交付规格
- 完成标准：只选择少量高价值能力，不整包开放
- 阻塞条件：想同时开放高风险扩面项
- 完成后下一张卡：`V2-P5-C2`

### V2-P5-C2 计划

- 目标：拆分首批交付的实现顺序、文档策略和兼容说明
- 输入：`V2-P5-C1`
- 输出：实施计划
- 完成标准：明确先做哪个命令、哪个模板示例、哪些输出要加快照保护
- 阻塞条件：无法给出最小首发集合
- 完成后下一张卡：`V2-P5-C3`

### V2-P5-C3 执行

- 目标：交付首批正式对外能力
- 输入：`V2-P5-C2`
- 输出：代码、文档、命令面、示例
- 完成标准：至少一个用户可见的 v2 工作流可跑通
- 阻塞条件：实现过程中反向要求改前置阶段边界
- 完成后下一张卡：`V2-P5-C4`

### V2-P5-C4 测试

- 目标：验证命令契约、示例工作流、文档与输出一致性
- 输入：代码改动、示例输入、快照
- 输出：测试结果
- 完成标准：对外工作流可回归，文档不漂移
- 阻塞条件：外部使用方式仍不稳定
- 完成后下一张卡：`V2-P5-C5`

### V2-P5-C5 修复

- 目标：修复首发能力中的契约、示例、工作流问题
- 输入：`V2-P5-C4`
- 输出：可交付版本
- 完成标准：首批能力达到“可解释、可测试、可文档化”
- 阻塞条件：需要引入 `V2-P6` 的高风险项才能成立
- 完成后下一张卡：`V2-P5-C6`

### V2-P5-C6 人工反馈

- 目标：由人工决定是否接受首批 v2 正式能力
- 输入：实现结果、测试结果、对外文档
- 输出：`accepted / continue-P5 / enter-P6 / pause`
- 完成标准：人工明确后续方向
- 阻塞条件：人工尚未反馈
- 完成后下一张卡：下一轮 `V2-P5-C1` 或 `V2-P6-C1`

## V2-P6 任务卡

### 阶段说明

本阶段是高风险扩面项，默认不自动进入。只有人工明确批准后，才允许启动。

当前已选定的首个主题：

- `讲解型 / narrated-slides 视频装配`

当前状态（2026-04-25）：

- `C1 ~ C5` 已完成
- 当前停在 `C6` 人工反馈

### V2-P6-C1 规格

- 目标：从 `resolve-assets`、数据驱动 batch、图表能力中只选一个主题，固定边界
- 输入：`batch-production-design.md`、`ai-intelligent-workflows-design.md`
- 输出：单主题规格
- 完成标准：只做一个主题，不并行摊大
- 阻塞条件：范围过大或需要新运行时依赖但未获批准
- 完成后下一张卡：`V2-P6-C2`

### V2-P6-C2 计划

- 目标：明确 owner、依赖、回滚、测试面和性能/安全基线
- 输入：`V2-P6-C1`
- 输出：实施计划
- 完成标准：已回答为何不能复用现有 batch/render owner
- 阻塞条件：canonical owner 无法确认
- 完成后下一张卡：`V2-P6-C3`

### V2-P6-C3 执行

- 目标：实现被批准的高风险主题
- 输入：`V2-P6-C2`
- 输出：代码与文档改动
- 完成标准：实现没有悄悄改写现有 v1/v2 contract
- 阻塞条件：出现第二套不可审计执行语义
- 完成后下一张卡：`V2-P6-C4`

### V2-P6-C4 测试

- 目标：验证功能、性能、安全和回归
- 输入：代码改动、专项测试
- 输出：测试结果
- 完成标准：不仅功能可用，还能说明成本与风险没有失控
- 阻塞条件：需要新的基线脚本或外部依赖验证但未准备好
- 完成后下一张卡：`V2-P6-C5`

### V2-P6-C5 修复

- 目标：修复功能缺陷、性能回退、安全或 owner 漂移问题
- 输入：`V2-P6-C4`
- 输出：修正版实现
- 完成标准：风险已重新收敛
- 阻塞条件：修复意味着主题本身不应做
- 完成后下一张卡：`V2-P6-C6`

### V2-P6-C6 人工反馈

- 目标：由人工判断这一轮高风险扩面是否值得继续
- 输入：功能结果、测试结果、风险复盘
- 输出：`accepted / stay / rollback / pause`
- 完成标准：人工明确是否继续该主题
- 阻塞条件：人工尚未反馈
- 完成后下一张卡：下一轮 `V2-P6-C1` 或暂停整个阶段

## 当前推荐使用方式

后续实际推进时，默认按下面的节奏循环：

1. 先确认当前在哪个 `V2-P*` 阶段
2. 只处理该阶段的当前卡片
3. 卡片完成后更新状态
4. 阶段到 `人工反馈` 卡时停止
5. 人工明确反馈后，再决定是否进入下一阶段或继续当前阶段

这意味着后续对话里最稳的工作方式不是说“继续做 v2”，而是明确说：

- 进入 `V2-P1-C1`
- 继续 `V2-P2-C3`
- 完成 `V2-P4-C6`，等待人工反馈

这样任务追踪会比“做 schema v2”这种宽泛说法稳定得多。

## 当前结论

到目前为止，`docs/plans/v2/*.md` 更适合作为：

- 已整理的候选能力池
- 后续 `schema v2` 实施前的设计输入
- 明确准入门后的阶段 backlog

而不是：

- 当前活跃实现线
- 可以直接并行全面开工的开发计划

短期最合理的动作仍然是：

1. 先收掉当前 `E2-F4` 的阶段门判断
2. 再从 `V2-P1` 中挑单项、低破坏、可回归的内容
3. 只有在明确接受双轨成本后，才进入 `V2-P2` 之后的正式 v2 实施
