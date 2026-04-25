# V2-P6-C15 阶段检查：数据驱动 Batch Narrated

最后更新：2026-04-25

## 目的

这份文档只回答一个问题：

> 作为 `V2-P6` 在 stop-point 之后显式重开的窄卡，`C15 数据驱动 batch narrated` 是否已经完成当前这一轮可回归、可验收、边界清晰的实现？

这里检查的不是 narrated 主题是否继续扩张，而是确认：

- `init-narrated-plan-batch` 已作为独立 CLI batch wrapper 落地
- 单项 narrated build 语义仍完全复用既有 `init-narrated-plan`
- batch 结果目录、退出码和 envelope 已固定
- 当前没有把 batch 语义下沉到 `Core.Editing`

## 当前纳入范围

本轮只纳入：

- `init-narrated-plan-batch`
- batch manifest contract
- `summary.json` / `results/<id>.json`
- batch 级 `--json-out`
- batch 级 `--ffprobe` / `--timeout-seconds` 透传
- 对应 CLI integration tests 与 normalized contract snapshot
- 对应文档同步

本轮明确不纳入：

- section 删除 / 条件裁剪增强
- `.pptx` / chart / Markdown page / title-card
- richer placeholder 样式
- 第二套 narrated plan builder
- 把 narrated batch 下沉到 `Core.Editing`

## 当前已落地能力

### Cli

当前已新增：

- `init-narrated-plan-batch --manifest <batch.json>`
- `InitNarratedPlanBatchManifest`
- item 级 `manifest` / `output` / `template` / `renderOutput` / `vars`
- 默认 `tasks/<id>/edit.json`
- `summary.json` / `results/<id>.json`
- 退出码：
  - `0` 全部成功
  - `2` 部分或全部条目失败
  - `1` manifest 装载失败

当前已固定的边界：

- narrated manifest -> v2 plan 投影继续留在 `Core.Editing`
- batch 只做 manifest 装载、路径解析、option overlay 和结果汇总
- success item payload 继续复用单项 `init-narrated-plan` 的既有结构

### 测试

当前已补：

- `CommandArtifactsIntegrationTests.InitNarratedPlanBatchCommands`
  - 成功路径
  - 部分失败路径
  - `--json-out` 等价性
  - batch 级 `--ffprobe` 透传
- `ContractSnapshotTests`
  - `init-narrated-plan-batch` normalized contract snapshot

## 与卡片目标对照

### 条件 1

> 必须保持为 `Cli` batch wrapper，而不是把 batch 逻辑下沉到 narrated plan owner

当前判断：**满足**

### 条件 2

> 必须逐项复用单项 `init-narrated-plan` 的既有 build 语义

当前判断：**满足**

### 条件 3

> 结果目录、逐项结果文件和退出码必须沿用现有 batch 约定

当前判断：**满足**

### 条件 4

> 必须有可回归测试，覆盖成功、部分失败和稳定 contract

当前判断：**满足**

## 当前验证结果

本轮已执行：

- `dotnet test src/OpenVideoToolbox.Cli.Tests/OpenVideoToolbox.Cli.Tests.csproj --filter "InitNarratedPlanBatch|InitNarratedPlan_WritesPlanAndReturnsStructuredEnvelope"`
- `dotnet test src/OpenVideoToolbox.Cli.Tests/OpenVideoToolbox.Cli.Tests.csproj --filter "ScaffoldTemplateBatch|RenderBatch|InitNarratedPlanBatch"`
- `dotnet test src/OpenVideoToolbox.Cli.Tests/OpenVideoToolbox.Cli.Tests.csproj --filter "InitNarratedPlanBatch|ContractSnapshotTests"`
- `dotnet build OpenVideoToolbox.sln`

当前结论：

- `init-narrated-plan-batch` 已具备独立、可回归、可脚本消费的最小实现
- `C15` 已回答“仓库是否能以数据驱动方式批量起 narrated 草稿”这个问题
- 当前仍守住了“不把 narrated 继续扩成第二套内容平台”的边界

## 当前结论

`V2-P6-C15` 当前判断为：**已完成当前一轮实现，可进入手动验收**

后续如继续推进 narrated，必须重新选卡，而不是把 `C15` 继续横向扩成 section 删除、placeholder 样式或 `.pptx` 主题。

## 手动验收入口

当前手动验收清单见：

- `docs/plans/v2/2026-04-25-v2-p6-acceptance-checklist.md`
