# ADR-0002: Keep AI Outside The Repository And Expose A Deterministic CLI

日期：2026-04-15

## 状态

Accepted

## 背景

仓库当前已经具备稳定的媒体探测、命令构建和执行能力。用户澄清后的目标是：

- 软件本身只提供 CLI 命令
- Claude、Codex 等外部 AI 工具负责推理和编排
- 仓库内部不引入 AI provider、LLM API 或供应商耦合

## 决策

- 产品定位调整为“供外部 AI 代理调用的 CLI 媒体工具箱”。
- 仓库内部不建立 AI provider 层，不接 Claude、Codex、LLM API 或其他模型服务。
- `Cli` 只暴露确定性命令和结构化输出，供外部 AI 决策下一步。
- `Core` 继续只承接媒体模型、任务模型、命令构建、执行和日志，不承接 AI 推理职责。

## 结果

正面影响：

- 项目边界更清晰，避免把外部 AI 供应商耦合到仓库主干
- 现有 `Media` / `Execution` 基础可继续复用
- 外部 AI 代理可以稳定依赖 CLI 的 JSON 输出

代价：

- 文档、roadmap 和 CLI 设计需要围绕“可编排的确定性命令”重写
- AI 能力不再体现在仓库代码里，而体现在外部代理工作流里

## 不采纳的方案

- 在仓库内建立 AI provider 抽象
  - 原因：这会引入不必要的供应商边界、配置复杂度和架构漂移
- 直接做大而全 GUI 编辑器
  - 原因：在 CLI 能力尚未稳定前，UI 会放大返工成本

## 后续动作

- 为第一个确定性编辑子命令开单独计划
- 继续稳定 CLI 输出模型
- Desktop 是否保留为长期目标后续再决定
