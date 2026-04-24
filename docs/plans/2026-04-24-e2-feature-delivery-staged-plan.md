# E2 功能交付分阶段实施计划

最后更新：2026-04-24

## 目的

当前仓库已经进入 `E2`，但最近这条线的实际推进方式偏向“看到一个高频问题，就补一个命令或一个局部工作流”。

这样短期能出结果，但长期会有三个问题：

- 容易只看到单点需求，看不清整体实施顺序
- 容易把已经完成、正在进行、尚未开始的事项混在一起
- 容易在 Desktop 尚未启动前，把 CLI / Core 做成一堆彼此缺少阶段检查的零散能力

因此这份文档的目标很明确：

1. 给当前 `E2` 的**功能交付线**建立分阶段计划
2. 每个阶段都明确目标、范围、不做、验收和暂停条件
3. 后续推进默认先判断“当前属于哪个阶段、阶段是否已完成”，再决定做哪一项实现

## 与现有文档的关系

- `docs/plans/2026-04-22-e2-ecosystem-sustainability-plan.md`
  - 负责 `E2` 的生态 / 分发 / 社区 / 长期维护线
- 本文档
  - 负责 `E2` 的功能交付线
- `docs/roadmap.md`
  - 负责声明当前活跃阶段与下一步
- `docs/plans/2026-04-24-edit-plan-inspect-and-material-replacement.md`
  - 作为当前功能交付线中素材工作流的专项设计

本文档不改长期路线，也不覆盖 owner 边界；owner 仍以 `docs/ARCHITECTURE_GUARDRAILS.md` 与各模块 `MODULE.md` 为准。

## 当前结论

- 当前应继续走 `E2`，但要把 `E2` 明确拆成两条并行但不混写的线：
  - `E2-A*`：生态 / 分发 / 社区 / 基线
  - `E2-F*`：功能交付 / 工作流收口 / Desktop 预留
- 近期功能交付不应继续按“单命令补丁式”推进。
- 当前最合理的做法是：
  - 先按阶段推进高频工作流
  - 每个阶段结束时做一次阶段检查
  - 只有阶段检查通过，才进入下一阶段

## 阶段总览

```text
E2-F1 基线盘点与边界固定 ─────────── (已完成)
   ↓
E2-F2 计划内素材与配音工作流收口 ──── (当前活跃阶段)
   ↓
E2-F3 字幕与 supporting signal 工作流收口 (下一阶段)
   ↓
E2-F4 批量工作流与工作目录编排 ────── (其后阶段)
   ↓
E2-G1 Desktop 启动重判 ──────────── (阶段门)
```

说明：

- `F` 表示功能交付阶段。
- `G` 表示阶段门，不等于立刻启动新实施线。
- 只有 `E2-F4` 通过后，才适合重新判断是否启动 `D1 Desktop MVP`。

## E2-F1: 基线盘点与边界固定

**状态：已完成**

### 目标

- 固定“当前基础功能列表”
- 固定“Desktop 未来只能消费哪些边界对象”
- 固定“高频工作流优先、基础层托底”的排序原则

### 已落地结果

- `docs/plans/2026-04-24-cli-foundation-and-desktop-reservation.md`
- `docs/roadmap.md` 中的优先级纪律与 Desktop 预留约束
- `inspect-plan` / `replace-plan-material` / `attach-plan-material` / `bind-voice-track` / `bind-voice-track-batch` 的基础路线选择

### 验收结论

- 当前基础功能盘已经可被文档明确枚举
- Desktop 尚未启动，但禁止边界已经落文档
- 功能优先级已经明确转为“常用工作流优先”

## E2-F2: 计划内素材与配音工作流收口

**状态：当前活跃阶段**

### 阶段目标

把“已有 `edit.json` 之后的高频素材操作”收成稳定工作流，而不是继续依赖手改 JSON。

### 本阶段范围

- `inspect-plan`
- `replace-plan-material`
- `attach-plan-material`
- `bind-voice-track`
- `bind-voice-track-batch`
- 与这些命令直接相关的结构化摘要、失败语义、测试与文档

### 本阶段不做

- 不启动 Desktop
- 不开放通用 `patch-plan`
- 不做复杂 clip 编辑器
- 不在仓库内置 TTS / voice conversion provider
- 不把批量能力直接做成第二套 plan 模型

### 当前完成度

已完成：

- `inspect-plan`
- `replace-plan-material`
- `attach-plan-material`
- `bind-voice-track`
- `bind-voice-track-batch`

本阶段剩余建议收尾项：

1. 对素材工作流命令形成统一的“选择器 / 路径写回 / 局部修复”说明
2. 判断是否需要补 `replace-plan-material-batch` 或 `attach-plan-material-batch`
3. 把 future Desktop 需要的素材面板数据继续保持在 `Core.Editing` owner 内

### 阶段检查

只有同时满足以下条件，才算 `E2-F2` 完成：

1. 人和外部 AI 都能在不手改整份 JSON 的前提下完成常见素材替换与挂载
2. 外部配音 / TTS / voice conversion 结果至少已有一条稳定接回路径
3. 单项与批量配音接回都有结构化部分成功语义
4. 文档能明确说明这些命令各自解决什么问题、边界在哪

### 必须停下重判的情况

- 发现需要引入第二套计划模型
- 发现批量能力无法复用单项语义
- 发现 CLI 已经开始拥有 `Core.Editing` 的业务规则

## E2-F3: 字幕与 supporting signal 工作流收口

**状态：下一阶段**

### 阶段目标

把字幕、转写、节拍等 supporting signals 从“已经有命令”推进到“高频工作流闭环”。

### 本阶段范围

- `transcribe -> subtitle -> attach / validate / render` 的更短闭环
- `inspect-plan` 对 transcript / subtitles / beats 缺失状态的更稳定摘要
- supporting signal readiness / missing-signal 提示
- 模板与 `edit.json` 对 signal 的显式消费路径

### 本阶段不做

- 不新增远程 AI 能力
- 不为了 signal 工作流重写模板 schema
- 不做复杂字幕编辑器

### 建议工作包

1. 补“已有 signal、但还没接回 plan”这类状态摘要
2. 补更直接的字幕挂载与字幕链路文档
3. 明确 transcript / subtitles / beats 在 preview / validate 里的最小可见性
4. 必要时补批量字幕或 batch signal manifest，但前提是先确认复用单项语义

### 阶段检查

只有同时满足以下条件，才算 `E2-F3` 完成：

1. `transcribe -> subtitle -> plan attach -> render` 已形成稳定闭环
2. `inspect-plan` 能明确提示缺失的 transcript / subtitles / beats
3. 模板 signal consumption 说明与实际 CLI 行为一致
4. 不需要再靠零散 README 描述去猜 signal 怎么接回

### 必须停下重判的情况

- 发现 signal 接回需要新的 plan 顶层模型
- 发现字幕工作流开始要求 UI 私有状态
- 发现 signal 语义正在从 `Core.Editing` 漂移到 `Cli`

## E2-F4: 批量工作流与工作目录编排

**状态：其后阶段**

### 阶段目标

把已经存在的单项高频工作流，整理成稳定的批量入口和工作目录约定。

### 本阶段范围

- 批量 manifest 形状统一
- 批量 `init-plan` / `scaffold-template` / `render` / 素材替换的优先级梳理
- partial success / failed summary / 输出目录组织
- 更适合 future Desktop 和脚本复用的工作目录结构

### 本阶段不做

- 不做任务队列系统
- 不做跨进程调度层
- 不引入持久化数据库
- 不启动 Desktop 批处理 UI

### 建议工作包

1. 统一 batch manifest 的公共约定
2. 先补最有复用价值的批量命令，不并行铺开全部 batch 入口
3. 固定 partial success exit code 与 JSON 摘要模式
4. 固定工作目录组织，避免不同 batch 命令各自发明输出结构

### 阶段检查

只有同时满足以下条件，才算 `E2-F4` 完成：

1. 至少一组高频工作流已具备稳定 batch 入口
2. batch 命令的 manifest 约定和部分成功语义已稳定
3. 工作目录组织对人、脚本和 future Desktop 都可读
4. 批量能力仍复用单项 owner，不存在第二套语义

### 必须停下重判的情况

- 发现不同 batch 命令已经长出彼此不兼容的 manifest
- 发现工作目录组织需要新的持久化层
- 发现“批量调度”开始逼近任务队列 / 历史数据库

## E2-G1: Desktop 启动重判

**状态：阶段门，非立即启动**

### 触发条件

只有在 `E2-F2`、`E2-F3`、`E2-F4` 都通过阶段检查后，才进入这一步。

### 重判问题

1. `edit.json schema v1` 是否已进入低频变更窗口
2. 当前高频工作流是否已经有稳定 CLI 边界可供 UI 直接消费
3. Desktop 是否确实比继续丰富 CLI 更有收益
4. Desktop 是否仍能严格维持“交互壳，而不是业务 owner”

### 重判结果只允许两种

- 结果 A：继续留在 `E2`，不启动 `D1`
- 结果 B：满足门槛，正式启动 `D1 Desktop MVP`

不允许第三种模糊状态：一边说 Desktop 未启动，一边在仓库里持续塞 UI 私有逻辑。

## 每阶段统一检查模板

后续每完成一个阶段，都必须至少回答以下问题：

```text
阶段：
阶段目标是否完成：
当前 owner 是否保持单一：
是否出现第二套模型或第二套语义：
当前 CLI 是否已能形成稳定闭环：
下一阶段是否已满足进入条件：
如果现在停止，仓库是否仍处于一致状态：
```

## 当前建议执行顺序

按今天的实现状态，后续默认顺序应固定为：

1. 先把 `E2-F2` 收口，而不是继续零散加命令
2. 再进入 `E2-F3`，补字幕与 supporting signal 工作流
3. 再进入 `E2-F4`，统一 batch 与工作目录
4. 最后执行 `E2-G1`，重新判断是否启动 `D1`

## 当前下一步

如果沿着这份计划继续推进，当前最合理的下一步不是立刻再做一个新命令，而是：

1. 先对 `E2-F2` 做阶段收尾判断，明确还缺哪一块才算完成
2. 再以 `E2-F3` 为下一个实施阶段，设计字幕 / transcript / supporting signal 的完整闭环

这样后面每轮推进都能先回答“当前在哪个阶段、这一项是不是当前阶段该做的事”，避免继续碎片化推进。
