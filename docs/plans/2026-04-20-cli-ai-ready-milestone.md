# CLI AI-ready Milestone

日期：2026-04-20

## 背景

- 当前仓库的架构方向判断为健康：
  - `Core` 仍是唯一业务 owner
  - `Cli` 继续收敛为薄入口
  - `Desktop` 仍处于后置阶段
- 当前问题已不再是“是否要重画架构”，而是：
  - 何时可认定 CLI 已足够稳定，可直接提供给外部 AI 使用
  - 在什么门槛下才启动 Desktop MVP
  - 如何把阶段目标与文档更新机制一起写实，避免后续边做边漂移

## 本轮目标

- 把当前阶段正式定义为 `CLI AI-ready` 里程碑。
- 为 CLI、模板平台、插件边界、Desktop MVP 提供一组清晰的阶段门槛。
- 明确本阶段的 In scope / Out of scope / 验收标准。
- 建立低成本、可持续的文档保鲜机制，降低 roadmap、guardrails、plans 腐化风险。

## 核心判断

- 当前仓库已经完成从“零散命令集合”向“可编排 CLI 媒体工具链”的跃迁。
- 下一阶段不应继续优先扩命令数量，而应优先把既有 CLI 契约、模板工作流和重依赖链路做实。
- Desktop 不是当前阻塞项；它应在 CLI 契约进入低频变更后再启动，而不是反过来驱动内核变化。

## 阶段定义

### Phase A: CLI AI-ready

目标：

- 让外部 AI 能依赖稳定 CLI 契约完成多数基础剪辑工作。
- 把“命令能跑”推进到“工作流可闭环、失败可诊断、输出可消费”。

In scope：

- `probe` / `plan` / `run`
- `templates` / `init-plan` / `scaffold-template` / `validate-plan`
- `render` / `mix-audio`
- `cut` / `concat` / `extract-audio`
- `audio-analyze` / `audio-gain` / `transcribe` / `detect-silence` / `separate-audio` / `subtitle`
- `doctor`
- 统一 success / failure envelope
- `--json-out`
- 真实依赖 smoke 与失败路径沉淀

验收标准：

1. 下列链路都可稳定闭环：
   - `probe -> plan -> run`
   - `templates -> init-plan / scaffold-template -> validate-plan -> render`
   - `transcribe -> subtitle -> render`
   - `audio-analyze / audio-gain / detect-silence / separate-audio`
2. 关键命令成功和失败都输出结构化 JSON，而不是在错误路径退回纯 usage。
3. 关键命令的 `--json-out` 契约齐备。
4. `doctor` 能明确表达 required / optional 依赖缺失、来源、默认值与 unset 语义。
5. 至少一套真实依赖 smoke 跑通，并保留回归入口。

### Phase B: Template-ready

目标：

- 让模板工作流成为外部 AI 的默认入口，而不是让 AI 自己拼命令。

In scope：

- `guide.json`
- `commands.json` / `commands.*`
- supporting signal guidance
- transcript / silence / stems / beats 的接线说明
- template preview / scaffold 输出稳定化

验收标准：

1. 外部 AI 不需要自己猜 transcript / silence / stems 等基础信号怎么接回 `edit.json`。
2. 模板 guide / preview / commands 输出字段与路径语义低频变更。
3. 模板脚手架目录可以直接作为 AI 的后续工作目录，而不是仅供人工阅读。

### Phase C: Plugin-ready

目标：

- 提供模板插件的稳定扩展面，但不引入运行时代码加载。

In scope：

- 显式 `--plugin-dir`
- 静态 manifest
- 复用现有 template schema
- `template.source` 跨 discovery / build / validate / execute 一致

Out of scope：

- 能力插件
- 远程插件市场
- 安装脚本
- 任意运行时代码

验收标准：

1. 插件来源在 `templates`、`init-plan`、`validate-plan`、`render` / `mix-audio` 全链可审计。
2. 插件模板不会要求 `Core` 理解新的私有执行语义。
3. 显式目录发现稳定后，再评估用户级默认目录，而不是反过来。

### Phase D: Desktop MVP

目标：

- 在不破坏既有 owner 的前提下，为 CLI 提供轻量交互壳。

允许范围：

- 文件导入
- 模板选择
- `edit.json` 可视化
- 参数表单化编辑
- 执行按钮
- 日志查看

禁止范围：

- 直接拼接 `ffmpeg` / `ffprobe`
- 独立于 `edit.json` 的第二套编辑模型
- 复杂时间线系统
- UI 层直接拥有进程执行逻辑

启动门槛：

1. `edit.json schema v1` 进入低频变更阶段。
2. `templates -> init-plan / scaffold-template -> validate-plan -> render` 工作流稳定。
3. 外部 AI 已能用 CLI 完成多数基础剪辑任务。
4. Desktop 明确定位为“交互壳”，而不是新的业务 owner。

## 当前状态映射

- `Phase A`：基本完成，仍需 hardening 收尾
- `Phase B`：已进入主工作面，但仍需继续把基础信号接进模板链路
- `Phase C`：边界已落文档与显式发现，尚未进入扩展面稳定阶段
- `Phase D`：仍未启动，继续后置是正确决策

## 当前 In Scope

1. `whisper.cpp` / `demucs` 真实工具 smoke、安装前提说明和失败路径沉淀
2. transcript / silence / stems / beats 继续接入模板 guide、preview、commands bundle、scaffold
3. CLI 输出契约继续收敛并冻结高频字段
4. CLI maintainability refactor 继续推进，但只做组织性迁移，不改命令行为

## 当前 Out Of Scope

- 新的 Desktop 交互层实现
- 运行时代码插件
- 仓库内 AI provider / LLM SDK
- 大量新增离散 CLI 命令
- 复杂时间线编辑器

## 风险与阻塞

- `whisper.cpp` 与 `demucs` 依赖较重，真实机器差异仍可能造成 smoke 不稳定。
- 模板层如果继续增长但 supporting signals 未同步收口，会让外部 AI 重新开始猜接线方式。
- 如果过早启动 Desktop，最容易把命令拼接、计划编辑和状态逻辑重新分散到 UI 层。

## 文档保鲜机制

本仓库后续建议采用“事件触发式更新 + 少数核心文档集中承载”的方式，而不是依赖人工定期大扫除。

### 1. 只维护少数核心文档

- `docs/roadmap.md`
  - 只写当前活跃工作面、阶段检查、下一步
- `docs/ARCHITECTURE_GUARDRAILS.md`
  - 只写长期 owner、依赖方向、阶段门槛、文档更新规则
- `docs/plans/*.md`
  - 只写当前一个明确里程碑或专项计划
- 模块 `MODULE.md`
  - 只写该模块独有的边界与 owner

### 2. 改动触发更新，而不是定期想到才更新

发生以下事件时必须同步文档：

1. 当前主目标变化：更新 `roadmap.md`
2. owner / 依赖方向变化：更新 `ARCHITECTURE_GUARDRAILS.md` 与相关 `MODULE.md`
3. 新里程碑启动：新增或更新 `docs/plans/*.md`
4. 命令契约明显变化：同步 `README.md`、模块文档与相关计划

### 3. 每次收尾都回答 3 个问题

在任务结束时统一检查：

1. 本轮改动改变了当前优先级吗？
2. 本轮改动改变了 owner 或边界吗？
3. 本轮改动改变了外部使用方式或验收标准吗？

只要其中任一答案为“是”，就必须补文档。

### 4. 避免重复文档

- roadmap 不重复写架构原理
- guardrails 不重复写里程碑任务清单
- plans 不重复抄完整产品背景
- `MODULE.md` 不重复抄全仓库边界

### 5. 让测试与文档互相校验

- 命令契约、`commands.json`、`commands.*`、preview/failure envelope 尽量由测试锁住
- 文档只描述“为什么”和“门槛”，不手抄一大段容易漂移的实现细节

## 本文档完成判定

- `roadmap` 已显式写出当前阶段与下一步
- `ARCHITECTURE_GUARDRAILS` 已写出 CLI AI-ready 与 Desktop MVP 的阶段门槛
- 后续若架构结论仍为“无大改必要”，只需要更新目标、门槛和边界，不再重新讨论分层设计
