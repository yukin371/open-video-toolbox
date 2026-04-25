# OpenVideoToolbox.Core.Editing

> 最后更新：2026-04-24

## 职责

本模块是 `edit.json` 计划模型的 canonical owner，负责承接 AI 生成结果与人工二次修正之间的共同边界。

## Owns

- `EditPlan`
- `EditPlanTimeline`
- `TimelineTrack`
- `TimelineClip`
- `Transition`
- `ClipTransitions`
- `TimelineEffect`
- `IEffectDefinition`
- `EffectParameterSchema`
- `EffectRegistry`
- `BuiltInEffectCatalog`
- `EditTemplateReference`
- `EditTemplateSourceReference`
- `EditClip`
- `AudioTrackMix`
- `EditArtifactReference`
- `EditTranscriptPlan`
- `EditBeatTrackPlan`
- `EditSubtitlePlan`
- `EditOutputPlan`
- `EditPlanPathResolver`
- `EditPlanValidator`
- `EditPlanInspector`
- `EditPlanMaterialReplacer`
- `EditPlanMaterialAttacher`
- `AutoCutSilencePlanner`
- `AutoCutSilenceRequest`
- `AutoCutSilenceResult`
- `AutoCutSilenceStats`
- `BuiltInEditPlanTemplateCatalog`
- `EditPlanTemplateCatalog`
- `EditPlanTemplateCatalogQuery`
- `EditPlanTemplateSummary`
- `EditPlanTemplatePlanModel`
- `EditPlanTemplateExampleBuilder`
- `EditPlanTemplateFactory`
- `NarratedSlidesManifest`
- `NarratedSlidesVideoManifest`
- `NarratedSlidesProgressBarManifest`
- `NarratedSlidesResolutionManifest`
- `NarratedSlidesTemplateManifest`
- `NarratedSlidesSubtitleManifest`
- `NarratedSlidesBgmManifest`
- `NarratedSlidesSectionManifest`
- `NarratedSlidesVisualManifest`
- `NarratedSlidesVoiceManifest`
- `NarratedSlidesResolvedSection`
- `NarratedSlidesPlanBuildRequest`
- `NarratedSlidesPlanBuildStats`
- `NarratedSlidesPlanBuildResult`
- `NarratedSlidesPlanBuilder`

## Must Not Own

- CLI 参数解析
- `ffmpeg` 命令拼接与进程执行
- GUI 时间线状态
- AI provider 或推理逻辑

## 关键依赖

- `OpenVideoToolbox.Core.Serialization`

## 不变量

- `edit.json` 字段语义必须稳定，不能在多个命令里各自发明变体
- `timeline` 只表达 v2 编辑意图与执行输入，不表达 UI 瞬时状态
- v2 clip 的 `src` 允许缺失并 fallback 到顶层 `source.inputPath`；不能在骨架阶段把它重新收紧成每个 clip 必填
- v2 clip 的时长语义必须允许 `duration` 与 `in/out` 两种来源，不能提前把所有 clip 都收缩成单一视频裁切模型
- 顶层 `artifacts` 只表达模板声明 slot 与文件路径的绑定，不能混入执行参数或 UI 临时态
- 顶层 `transcript` 只表达 transcript 引用与基础元数据，不能混入摘要结果或 AI 判断
- 顶层 `beats` 只表达节奏引用与可选 BPM 元数据，不能混入 CLI 专用临时状态
- 模板标识和扩展字段必须保持可选，不得强行绑定单一模板系统
- `template.planModel` 是 v1 / v2 模板生成路径的显式 owner 字段；区分逻辑必须留在本模块，不能在 CLI 层靠模板 id 或输出 shape 猜测
- `template.source` 只表达稳定模板来源元数据；插件场景只允许写入 `kind` / `pluginId` / `pluginVersion` 这类可移植字段，不能把插件目录等环境路径持久化进 `edit.json`
- 模板示例输出必须从模板定义稳定派生，不能在 CLI 层随手拼出另一套不一致 skeleton
- 模板推荐 seed 模式必须由模板定义显式声明，不能在 CLI 层靠启发式猜测
- preview plan 必须复用真实模板工厂生成，不能维护一份平行的假 schema 示例
- 对插件模板，preview plan 也必须沿用同一份稳定 `template.source` 元数据，不能在 guide / scaffold 示例里回落成 built-in 来源
- CLI 写出的 example 文件必须来源于同一套模板示例输出，不能额外生成只存在于磁盘模式的变体
- 模板分类和 seed 能力元数据必须稳定，便于 CLI 做可预测过滤
- 模板列表摘要字段必须稳定，便于外部 AI 和未来 UI 做低成本模板发现
- 模板 supporting signal guidance 必须由模板定义显式声明，不能在 CLI 层按类别或命令名临时猜测
- supporting signal command 里的外部依赖占位符必须具体且稳定；例如 transcript signal 应显式写成 `<whisper-model-path>`，不能退回泛化的 `<path>`
- transcript / beats 的 supporting signal consumption 必须同时覆盖“init-plan 前接入”和“已有 edit.json 后 attach”这两条稳定路径，不能只写其中一条
- 模板 artifact 示例路径必须从模板 slot 与 supporting signal 组合稳定派生；例如带 `stems` signal 的 `bgm` slot 应直接给出 `Demucs` accompaniment stem 路径，而不是退回泛化占位
- 模板参数覆盖必须先落到稳定的 `template.parameters`，不能直接分裂成零散 CLI 特判
- v2 模板 preview / init-plan 必须真实产出 `schemaVersion = 2` 与 `timeline`，不能只在 guide 文本里宣称支持
- 模板清单与 plan skeleton 规则必须留在本模块，不得散落到 CLI
- 计划模型只表达编辑意图，不直接表达 UI 状态
- 人工修改后的计划必须仍可被 CLI 消费
- 基于节拍的 clip 初始化必须保持确定性，便于外部 AI 和人工复现
- 基于 transcript segment 的 clip 初始化必须保持确定性，便于外部 AI 和人工复现
- `init-plan` 的 transcript / beats seed 规则必须由本模块统一生成，并可同时复用于 v1 `clips` 与 v2 `timeline` 路径；不能在 CLI 层拆出第二套 seed clip / timeline clip 生成逻辑
- `auto-cut-silence` 必须先基于 `silence.json` 与显式总时长生成确定性的 clip 区间；默认仍输出 v1 clips / v1 plan，但当模板显式声明 `planModel = v2Timeline` 时，只允许复用本模块模板工厂生成 v2 baseline plan，并在本模块内替换 timeline clip 内容，不能在 CLI 层手搓 `timeline`、`effects` 或隐式 render 前处理
- `schema v2` 的 timeline 结构校验必须留在 `EditPlanValidator`，不能在 CLI 或后续 render builder 再复制一套平行规则
- built-in effect catalog 只承接 effect descriptor discovery 与 validator 识别，不直接拥有 render 执行逻辑
- narrated-slides manifest 到 `schemaVersion = 2` plan 的投影规则必须留在本模块；CLI 只允许做 manifest 装载、相对路径解析和必要的媒体时长探测 glue，不能手搓 timeline
- narrated-slides 第一版必须保持独立显式入口，不得为了复用现有 `templates` / `init-plan <input>` 流程而把单素材模板语义和 section manifest 语义混在一起
- narrated-slides 的可选 `video.progressBar` 必须在本模块统一投影为稳定的 built-in effect 语义，不能在 CLI 或执行层各自发明第二套开关/参数名

## 常见坑

- 把命令执行细节直接漏进 `edit.json`
- 把未来 GUI 的瞬时状态混进计划模型

## 文档同步触发条件

- `edit.json` schema 变化
- 字段语义变化
- inspection / material summary 输出语义变化
- `inspect-plan` 的 `signals[]` 总状态字段或取值变化
- material replacement / path-style 写回语义变化
- material attachment / artifact slot upsert 语义变化
- `render` / `mix-audio` 等命令消费方式变化
- `beats` 字段或节拍种子语义变化
- `transcript` 字段或 transcript 种子语义变化
- `artifacts` 字段或模板 slot 绑定语义变化
- `template.parameters` 覆盖语义变化
- `template.source` 字段或插件来源校验语义变化
- 模板 supporting signal 元数据或 template guide 派生规则变化
- transcript / beats seed 规则在 v1 / v2 模板路径上的复用语义变化
- `auto-cut-silence` 的 clips 生成语义、默认阈值、v1/v2 plan 分流约定或 timeline clip 替换语义变化
- `schema v2` timeline 字段、validator 规则或 v1/v2 双轨校验语义变化
- built-in effect catalog 的 effect 类型、参数 schema、template mode 或 discovery 输出语义变化
- narrated-slides manifest 字段、section 投影规则、默认轨道结构或首版支持范围变化
