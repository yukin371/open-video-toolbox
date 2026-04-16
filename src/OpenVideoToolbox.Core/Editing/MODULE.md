# OpenVideoToolbox.Core.Editing

> 最后更新：2026-04-15

## 职责

本模块是 `edit.json` 计划模型的 canonical owner，负责承接 AI 生成结果与人工二次修正之间的共同边界。

## Owns

- `EditPlan`
- `EditTemplateReference`
- `EditClip`
- `AudioTrackMix`
- `EditArtifactReference`
- `EditTranscriptPlan`
- `EditBeatTrackPlan`
- `EditSubtitlePlan`
- `EditOutputPlan`
- `EditPlanPathResolver`
- `EditPlanValidator`
- `BuiltInEditPlanTemplateCatalog`
- `EditPlanTemplateCatalogQuery`
- `EditPlanTemplateSummary`
- `EditPlanTemplateExampleBuilder`
- `EditPlanTemplateFactory`

## Must Not Own

- CLI 参数解析
- `ffmpeg` 命令拼接与进程执行
- GUI 时间线状态
- AI provider 或推理逻辑

## 关键依赖

- `OpenVideoToolbox.Core.Serialization`

## 不变量

- `edit.json` 字段语义必须稳定，不能在多个命令里各自发明变体
- 顶层 `artifacts` 只表达模板声明 slot 与文件路径的绑定，不能混入执行参数或 UI 临时态
- 顶层 `transcript` 只表达 transcript 引用与基础元数据，不能混入摘要结果或 AI 判断
- 顶层 `beats` 只表达节奏引用与可选 BPM 元数据，不能混入 CLI 专用临时状态
- 模板标识和扩展字段必须保持可选，不得强行绑定单一模板系统
- 模板示例输出必须从模板定义稳定派生，不能在 CLI 层随手拼出另一套不一致 skeleton
- 模板推荐 seed 模式必须由模板定义显式声明，不能在 CLI 层靠启发式猜测
- preview plan 必须复用真实模板工厂生成，不能维护一份平行的假 schema 示例
- CLI 写出的 example 文件必须来源于同一套模板示例输出，不能额外生成只存在于磁盘模式的变体
- 模板分类和 seed 能力元数据必须稳定，便于 CLI 做可预测过滤
- 模板列表摘要字段必须稳定，便于外部 AI 和未来 UI 做低成本模板发现
- 模板参数覆盖必须先落到稳定的 `template.parameters`，不能直接分裂成零散 CLI 特判
- 模板清单与 plan skeleton 规则必须留在本模块，不得散落到 CLI
- 计划模型只表达编辑意图，不直接表达 UI 状态
- 人工修改后的计划必须仍可被 CLI 消费
- 基于节拍的 clip 初始化必须保持确定性，便于外部 AI 和人工复现
- 基于 transcript segment 的 clip 初始化必须保持确定性，便于外部 AI 和人工复现

## 常见坑

- 把命令执行细节直接漏进 `edit.json`
- 把未来 GUI 的瞬时状态混进计划模型

## 文档同步触发条件

- `edit.json` schema 变化
- 字段语义变化
- `render` / `mix-audio` 等命令消费方式变化
- `beats` 字段或节拍种子语义变化
- `transcript` 字段或 transcript 种子语义变化
- `artifacts` 字段或模板 slot 绑定语义变化
- `template.parameters` 覆盖语义变化
