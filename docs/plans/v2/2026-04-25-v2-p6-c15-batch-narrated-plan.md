# V2-P6-C15 数据驱动 Batch Narrated 实施计划

最后更新：2026-04-25

## 实施目的

把现有单项 `init-narrated-plan` 包成稳定的 batch 入口，先回答“仓库是否能以数据驱动方式批量起 narrated 草稿”这个问题。

## 实施范围

1. 在 `OpenVideoToolbox.Cli` 新增 narrated batch manifest 模型
2. 在 `TemplateCommandHandlers.cs` 新增 `init-narrated-plan-batch` handler
3. 复用现有 narrated build support，不新增第二套 builder
4. 补 CLI 集成测试，覆盖成功与部分失败
5. 同步命令文档、roadmap 与 `MODULE.md`

## 不做

- 不把 narrated batch 下沉到 `Core.Editing`
- 不引入 `.pptx`、图表、页面或第二套模板平台
- 不在这一卡里追加 title-card、placeholder 样式配置、section 删除

## 风险控制

- 路径和结果落盘语义完全复用现有 batch 约定：`summary.json` + `results/<id>.json`
- narrated plan payload 复用单项命令的输出 shape，避免形成两套 envelope
- 只允许 `Cli` 做 item 级 option overlay，变量解析与 plan 投影仍留在 `Core.Editing`

## 验证

- `OpenVideoToolbox.Cli.Tests`
  - `InitNarratedPlanBatch_WritesPlansAndSummary`
  - `InitNarratedPlanBatch_ReturnsPartialFailureSummary`
- 定向执行 `dotnet test`
