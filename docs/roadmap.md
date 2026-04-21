# Roadmap

最后更新：2026-04-20

本文件只保留当前版本目标、实施顺序与活跃工作面，不记录完整历史流水账。

## 产品定位

- 产品不是专业 NLE，也不是缩小版 PR。
- 当前目标是交付一个“轻量剪映式”的 CLI 媒体内核：
  - 常见视频场景优先
  - 默认路径简单
  - 对外结构化、可编排、可审计
  - 通过模板降低人工操作和 AI 编排难度
- 软件内不内置 AI provider；AI 只通过 CLI、结构化文件和模板工作流与仓库交互。

## 实施原则

- 新功能默认先找已有开源实现，再决定是否补自研。
- 优先复用成熟工具、库或现有 CLI，而不是在仓库里重复开发同类能力。
- 只有在以下情况同时成立时，才考虑自研：
  - 现有开源实现无法满足确定性、可审计或可测试要求
  - 无法通过适配层或 CLI 集成方式复用
  - 自研后的 canonical owner 明确，不会把边界打散
- 能通过“封装现有开源能力 + 稳定结构化输出”解决的问题，不应直接升级为新的复杂内核实现。

## 当前版本目标

- 交付可供外部 AI 代理稳定调用的 CLI 媒体工具链：
  - 文件探测
  - 任务规划
  - 剪辑计划
  - 执行
  - 结构化 JSON 输出
- 把项目收敛为“基础能力层 + 模板层 + 插件扩展层”的结构，而不是继续堆离散命令。
- 保持 `Core` 作为唯一业务 owner，避免在 `Cli` 或未来 Desktop 重复实现命令构建和外部工具调用。
- 当前实施计划：`docs/plans/2026-04-15-cli-mvp-implementation.md`
- 当前里程碑计划：`docs/plans/2026-04-20-cli-ai-ready-milestone.md`
- 相关专项计划：`docs/plans/2026-04-16-audio-speech-foundation.md`
- CLI 可维护性重构计划：`docs/plans/2026-04-19-cli-maintainability-refactor-plan.md`
- 模板插件入口草案：`docs/plans/2026-04-19-template-plugin-entry-boundary.md`
- 长期演化路线（H1~E2）：`docs/plans/2026-04-21-long-term-evolution-roadmap.md`
- H1 收口任务清单：`docs/plans/2026-04-21-h1-hardening-checklist.md`
- 最近一次功能/里程碑盘点：`docs/plans/2026-04-17-feature-design-milestone-check.md`

## 阶段检查（2026-04-20）

- `2026-04-20` 已将 CLI 可维护性重构第一批组织性迁移并入主干：共享 command output / failure helper 已抽到 `src/OpenVideoToolbox.Cli/CliCommandOutput.cs`，`CommandArtifactsIntegrationTests` 也已开始按命令域拆分。
- `2026-04-20` 已继续完成第二批低风险整理：`validate-plan` / `init-plan` / `scaffold-template` 的 CLI 集成测试已迁到独立 partial 文件，`Program.cs` 底部仅承载模板命令结果形状的 record 也已迁到独立文件，继续缩小入口文件体量。
- `2026-04-20` 已继续把模板命令的 presentation / validation helper 从 `Program.cs` 迁到独立文件：`TemplateCommandPresentation.cs` 负责模板 guide / preview / commands / example 写盘组织，`TemplatePlanValidationSupport.cs` 负责 `validate-plan` 的模板校验 glue，入口文件进一步收缩但命令契约保持不变。
- `2026-04-20` 已继续把通用 CLI option parsing 与模板 plan build glue 从 `Program.cs` 迁到独立文件：`CliOptionParsing.cs` 统一承接 `TryParse*` / `TryGet*` 解析 helper，`TemplatePlanBuildSupport.cs` 承接模板 plan build、transcript / beats 输入加载与 string-map 解析，为下一步继续拆命令族 wrapper 预留更清晰的依赖面。
- `2026-04-20` 已继续把模板命令 wrapper 本身迁出 `Program.cs`：`TemplateCommandHandlers.cs` 现承接 `templates`、`init-plan`、`scaffold-template` 三条命令的入口 glue，主入口文件进一步收缩到顶层分发为主，但命令参数、输出契约和退出码保持不变。
- `2026-04-20` 已继续把剩余 CLI wrapper 按命令族迁出 `Program.cs`：`MediaCommandHandlers.cs` 现承接 `cut` / `concat` / `extract-audio`，`AudioCommandHandlers.cs` 现承接 `audio-analyze` / `audio-gain` / `transcribe` / `detect-silence` / `separate-audio` / `beat-track` / `subtitle`，`RenderCommandHandlers.cs` 现承接 `mix-audio` / `render` 与局部 `edit.json` loading glue；主入口文件已进一步回收到顶层分发、usage 与失败出口，但命令契约保持不变。
- `2026-04-20` 已继续把 CLI 集成测试按命令域收口：模板发现 / guide 测试已迁到独立 partial，原先 1500+ 行的 `AudioSpeechCommands` 也已拆成 `AudioAnalysisCommands`、`SpeechSignalCommands`、`SubtitleBeatCommands` 三组，并顺手去掉 `subtitle` / `beat-track` 的重复参数校验测试，降低后续命令族重构时的测试维护摩擦。
- `2026-04-20` 已继续把执行与模板校验测试按 owner 收口：`ExecutionCommands` 已先拆成 `RenderMixCommands` 与 `MediaExecutionCommands`，随后又把 `RenderMixCommands` 继续细分为 `RenderCommands` 与 `MixAudioCommands`；`validate-plan` 相关测试已迁到独立 `ValidatePlanCommands` partial，而原本仅保留 `init-plan` / `scaffold-template` 主线的 `TemplatePlanningCommands` 也已继续拆成 `InitPlanCommands` 与 `ScaffoldTemplateCommands`，进一步降低跨命令域修改时的测试噪音。
- `2026-04-20` 已继续把剩余大体量测试 partial 再按真实命令 owner 细分：`SpeechSignalCommands` 已拆成 `TranscribeCommands`、`DetectSilenceCommands`、`SeparateAudioCommands`，`TemplateDiscoveryCommands` 也已拆成 `TemplateCatalogCommands`、`TemplateGuideCommands`、`TemplatePluginCommands`，同时把 plugin `init-plan` / `scaffold-template` 用例迁回各自命令文件，进一步压低未来修改时的跨域噪音。
- `2026-04-20` 已继续把 `AudioAnalysisCommands`、`UtilityCommands` 与 `InitPlanCommands` 进一步拆到单命令 / 单职责 partial：`AudioAnalysisCommands` 已拆成 `ExtractAudioCommands`、`AudioAnalyzeCommands`、`AudioGainCommands`，`UtilityCommands` 已拆成 `ProbePlanRunCommands` 与 `DoctorCommands`，`InitPlanCommands` 也已拆成 `InitPlanSeedCommands` 与 `InitPlanValidationCommands`，让命令契约测试与真实 owner 更对齐。
- `2026-04-20` 已继续把剩余混合测试再拆薄一层：`RenderCommands` 已拆成 `RenderPreviewCommands` 与 `RenderExecutionCommands`，`DoctorCommands` 已拆成 `DoctorEnvelopeCommands` 与 `DoctorResolutionCommands`，`SubtitleBeatCommands` 也已拆成 `SubtitleCommands` 与 `BeatTrackCommands`，让 preview / execution、依赖解析与命令本体的边界更清楚。
- `2026-04-20` 已继续把最后几组“单文件里仍混了多个关注点”的测试 partial 再按真实场景拆细：`MixAudioCommands` 已拆成 `MixAudioPreviewCommands` 与 `MixAudioExecutionCommands`，`InitPlanSeedCommands` 已拆成 `InitPlanTranscriptSeedCommands` 与 `InitPlanBeatSeedCommands`，`DoctorResolutionCommands` 已拆成 `DoctorEnvironmentResolutionCommands` 与 `DoctorOptionResolutionCommands`，`ScaffoldTemplateCommands` 已拆成 `ScaffoldTemplateValidationCommands` 与 `ScaffoldTemplateOutputCommands`；这轮仍只调整测试 owner，不改变 CLI 契约。
- `2026-04-21` 已继续把 `CommandArtifactsIntegrationTests` 里最后几组混合关注点 partial 再按真实 owner 收口：`audio-gain` 已拆成 validation / execution，`init-plan` 已拆成 artifact / plugin / transcript filtering / transcript grouping / validation，`render --preview` 也已拆成 preview / output / validation，并把跨 `render` / `mix-audio` 的 preview envelope 用例迁回 `MixAudioPreviewCommands`；这轮仍只调整测试组织与 roadmap，同步收敛维护边界，不改变 CLI 契约。
- `2026-04-21` 已继续把几组仍混合“参数校验 / 执行失败 / 结构化输出 / plugin 验证”的测试 partial 再拆细一轮：`mix-audio --preview` 已拆成 preview / output / validation，`beat-track` 已拆成 validation / execution，`transcribe` 已拆成 validation / execution，`validate-plan` 已拆成 success / failure / plugin；当前大部分命令测试已经接近“单命令 + 单关注点”边界。
- `2026-04-21` 已继续把最后一批明显混合“validate 结果断言 / 输入校验”或“多命令共享 execution”语义的 partial 再收口一层：`audio-analyze` 已拆成 validation / execution，`scaffold-template` validate 测试已拆成 result / input validation，`MediaExecutionCommands` 也已回收到单纯 `cut` 参数校验，并把 `cut` / `concat` 执行路径迁到独立 partial。到这一轮为止，绝大多数较大的测试文件已经是单命令或单一 execution 主题。
- `2026-04-21` 已继续把三组仍混合“dependency/process failure”与“json-out 成功路径”的 execution partial 再拆开：`audio-gain`、`audio-analyze`、`beat-track` 都已拆成 failure 与 output 两层，`detect-silence` 也已拆成 validation / execution / execution-output 三层。到这一轮后，CLI 集成测试里大部分命令已经接近“单命令 + 单结果路径”的维护粒度。
- `2026-04-21` 已继续把最后两组仍明显混合 validation / failure / output 的命令测试收口：`separate-audio` 已拆成 validation / execution / execution-output，`transcribe` 也已拆成 failure / output。到这一轮后，除少数仍偏长但主题单一的 execution partial 外，CLI 集成测试基本都已达到“单命令 + 单结果路径”的维护边界。
- `2026-04-20` 已继续把 `templates <id>` 相关测试再按真实关注点拆开：原 `TemplateGuideCommands` 已拆成 `TemplateGuideExamplesCommands` 与 `TemplateGuidePreviewCommands`，分别承接 `--write-examples` 脚手架产物验证，以及 guide / preview 响应形状验证，避免模板脚手架调整时再牵动 guide 预览断言。
- 当前仓库已经完成 Wave 1 命令面的主要铺设，工作重心正式转入 `Hardening`。
- 最近一轮小提交主要完成了两类收敛：
  - `audio-analyze`、`audio-gain`、`transcribe`、`detect-silence`、`separate-audio`、`subtitle` 已补齐或延续 `--json-out` 路径，进一步稳定外部 AI / 脚本消费契约。
  - 模板脚手架与 `commands.json` / `commands.*` 已继续收敛 supporting signal guidance、consumption 提示和外部依赖占位符，其中 transcript signal 显式使用 `<whisper-model-path>`。
- 最近补充的 hardening 还包括：
  - `commands.json` 与 `commands.ps1` / `commands.cmd` / `commands.sh` 现已共同覆盖 seed mode commands、transcript strategy variants、subtitle artifact commands 与 stems guidance，并有 CLI 集成测试锁住。
  - 可选重依赖 smoke 已扩到 CLI 入口层，`transcribe` / `separate-audio` 在本机依赖满足时会额外验证 stdout envelope、`--json-out` 与产物落盘一致性。
  - `doctor` 已补 environment fallback、option 优先级，以及 `default` / `unset` / `resolvedValue` / `detail` 等输出语义测试，进一步收敛依赖预检语义。
- 仓库级基础 CI 已落地为 GitHub Actions `restore + build + test`，先保护主干提交与 PR 的回归底线。
- 当前判断不需要继续优先扩新命令；更高价值的是把重依赖 smoke、模板信号接线和模板扩展边界做实。
- 模板插件扩展面现已先落文档草案，下一步只应围绕显式目录发现与清单输出验证推进，不直接跳到运行时代码加载。
- `templates`、`init-plan`、`scaffold-template` 已支持 `--plugin-dir` 显式目录发现；当前插件入口仍限定为“静态 manifest + 现有 template schema”，不引入运行时代码加载。
- `init-plan` / `scaffold-template` 现会把稳定的 `template.source` 元数据写进 `edit.json`，`validate-plan` 也已支持 `--plugin-dir` 把插件模板目录接回校验链，避免插件 plan 静默回退到 built-in 目录。
- `render` / `mix-audio` 的 preview 与执行 envelope 现也会透出稳定 `templateSource`，让插件来源在执行阶段继续可审计，而不是只停留在 `init-plan` / `validate-plan`。
- 插件模板的 `commands.json` / `commands.*` 现也会显式携带 `<plugin-dir>` 占位符和变量声明，避免脚手架目录里的 workflow commands 因缺少插件上下文而失效。
- 插件模板 guide / scaffold 里的 preview plan 现也会沿用稳定 `template.source`，使模板来源在 discovery、生成、校验和执行四个阶段保持一致。
- `render` / `mix-audio` 在 plan 已成功加载后的 preview / 执行失败，现也会回到结构化 failure envelope；即使是底层执行返回 failed status 而不是抛异常，也会继续保留 `templateSource`、可用的 `executionPreview` / `execution`，并返回非零退出码，避免插件来源在真正出错时再次丢失。
- `cut` / `concat` / `extract-audio` 也开始从旧的裸 `execution` JSON 收敛到统一 command envelope；这几条命令在请求已建立后的执行失败场景下，现也会优先返回结构化 failure envelope，而不是 usage 文本。
- `beat-track` 也已从旧的裸 JSON / 退出码分支收进统一 command envelope，并补上 `--json-out`；当波形提取失败时，现也会优先返回结构化 failure envelope，而不是 usage 文本。
- `audio-analyze` / `detect-silence` / `separate-audio` 也已从旧的裸结果 JSON 收进统一 command envelope；这几条命令在分析 / 检测 / 分离阶段失败时，现也会优先返回结构化 failure envelope，而不是 usage 文本。
- `audio-gain` / `transcribe` 现也已切到统一 command envelope；至此当前 Wave 1 的主要执行/分析类命令都已收进同一套成功/失败输出语义，减少外部 AI 在错误路径上遇到 usage 文本回退。
- `2026-04-21` 已把 `probe` / `plan` / `run` / `doctor` / `presets` 的成功与失败路径统一收进 command envelope：至此全部 21 条命令都使用同一套 `{ command, preview, payload }` 输出契约，所有命令的 `--json-out` 齐备，所有命令的运行时错误都返回结构化 JSON 而不是退回 usage 文本；W1 envelope 收口已完成，下一步是 W2 CLI smoke 扩展与 W3 测试拆分停止线评估。
- CLI 可维护性重构已启动，第一批先把共享 command output / failure helper 迁到 `src/OpenVideoToolbox.Cli/CliCommandOutput.cs`，并开始清理 `Program.cs` 里的纯转发 wrapper；当前策略仍是”先做组织性迁移，不改命令行为”。
- `2026-04-21` CLI 测试拆分停止线评估结论：53 个 partial 文件已全部达到”单命令 + 单结果路径”粒度（最大 3 facts / 文件），继续拆分的 diff 成本已超过维护收益，CLI 可维护性重构 active track 正式关闭。
- `2026-04-21` CLI smoke 已扩展到 13 个测试，覆盖 probe / cut / concat / extract-audio / audio-analyze / audio-gain / detect-silence / beat-track / subtitle / render / mix-audio / transcribe / separate-audio，全部验证 stdout envelope + `--json-out` 一致性 + 产物落盘。
- `2026-04-22` H2+T1 契约冻结与模板稳定已启动：契约快照测试已建立（5 个黄金文件覆盖 presets / templates / templates <id> / validate-plan），CI 已补 ffmpeg 安装，`presets` / `plan` 已补 `--json-out` 测试，presets 已补 `--json-out` 支持；当前 CLI 测试从 105 增至 112，全部 242 测试通过。
- `2026-04-22` T2 模板插件扩展面评估结论：`--plugin-dir` 显式目录发现、`template.source` 全链路审计、插件模板 schema 校验已在 H1 期间同步完成，8 个测试文件覆盖，`docs/plans/2026-04-19-template-plugin-entry-boundary.md` 验收标准全部满足，T2 无需额外实施即可关闭。
- `CommandArtifactsIntegrationTests` 也已开始按命令域拆成 partial files；当前已先分出 `utility`、`execution`、`audio-speech` 三组，主测试文件回收到 template / init / scaffold / validate 主线。

## 总体路线

### 1. Foundation

- 稳定媒体探测、命令构建、执行、`edit.json schema v1` 与结构化输出。
- 保持 `Core.Media`、`Core.Execution`、`Core.Editing`、`Core.Subtitles`、`Core.Beats` 的清晰 owner。
- 这一层只提供确定性原语，不直接承载大量场景逻辑。

### 2. Audio / Speech Base

- 为模板能力补地基，而不是直接为每个模板硬编码特例。
- 这层能力优先评估可复用的开源实现，例如现有音频分析、分离、语音识别和字幕工具链。
- 第一波 CLI 基础命令已落地，当前重点从“补入口”转向“收敛契约、错误路径和真实工具验证”。
- 优先补齐：
  - 音量检测与分贝识别
  - 分贝调整 / 增益控制
  - 静音 / 停顿检测
  - 人声与背景音分离
  - 音频转 transcript
  - 外挂字幕 / 烧录字幕的稳定工作流

### 3. Template Platform

- 模板不再只是内置样板，而是可扩展的场景编排单元。
- 模板平台优先复用基础能力与外部工具，不为单个模板发明新的私有处理链。
- 模板层负责：
  - 场景抽象
  - 参数默认值
  - artifact slot 声明
  - 推荐 seed 模式
  - 推荐 transcript / beat 辅助策略
- 先做模板插件，再评估能力插件；避免一开始就在仓库里引入不可控的扩展面。

### 4. User-facing Simplicity

- 面向用户与 AI 的目标不是“功能最多”，而是“高频任务最短路径”。
- 优先围绕以下体验收敛：
  - 导入素材
  - 生成初始计划
  - 字幕
  - BGM / 音频混合
  - 常见比例输出
  - 模板一键起步
- Desktop 只在 CLI / 模板工作流稳定后再评估，且应定位为简单操作壳，而不是完整时间线系统。

## 分阶段实施

### Phase A: 稳定基础能力

- 目标：
  - 固化 `edit.json schema v1`
  - 稳定 `probe` / `plan` / `run` / `render` / `mix-audio` / `validate-plan`
  - 稳定 transcript / beats / artifacts 的结构化边界
- 完成判定：
  - CLI 输出契约稳定
  - `edit.json` 能支撑后续模板与外部 AI 二次编辑

### Phase B: 夯实模板地基

- 目标：
  - 音量检测
  - 分贝调整
  - 静音 / 停顿检测
  - 音频转 transcript
  - 外挂字幕 / 烧录字幕双路径收敛
  - 人声 / 背景音分离
- 说明：
  - 这些能力属于模板平台地基，不应被塞进某个模板专属逻辑里。
  - 这阶段优先级高于继续无节制扩模板数量。
- 当前状态：
  - `audio-analyze`、`audio-gain`、`transcribe`、`detect-silence`、`separate-audio`、`subtitle` 命令面已进入仓库
  - 上述音频 / 语音命令的 `--json-out` 契约已基本补齐
  - 当前剩余重点是稳定 JSON 契约、补真实工具 smoke，并把这些基础信号继续收敛进模板工作流与脚手架目录产物

### Phase C: 模板平台与插件扩展

- 目标：
  - 模板 schema 继续完善
  - 模板 guide / preview / commands 输出继续稳定
  - 模板级推荐策略可配置
  - 为外部模板 / 插件预留稳定入口
- 约束：
  - 先支持“模板插件”
  - 再评估“能力插件”
  - 插件仍应以 deterministic CLI 能力为主，不引入仓库内 AI 黑盒

### Phase D: 轻量用户层

- 目标：
  - 用更少参数覆盖更多高频场景
  - 用模板把常见短视频工作流收敛成简单入口
  - 视 CLI 成熟度决定是否接入 Desktop MVP

## 阶段分界线

### 何时认定 CLI 已足够提供给外部 AI 使用

只有同时满足以下条件，才可认定当前 CLI 已足够支撑多数基础剪辑工作：

1. 关键工作流闭环：
   - `probe -> plan -> run`
   - `templates -> init-plan / scaffold-template -> validate-plan -> render`
   - `transcribe -> subtitle -> render`
   - `audio-analyze` / `audio-gain` / `detect-silence` / `separate-audio`
2. 关键命令成功与失败都输出结构化 JSON，而不是在错误路径退回纯 usage。
3. 关键命令的 `--json-out` 契约齐备。
4. `doctor` 能清晰表达依赖来源、默认值、unset、缺失原因与 required/optional 语义。
5. 至少一套真实依赖 smoke 能重复通过，并有测试或 smoke 入口可追踪。

### 何时启动 Desktop MVP

只有同时满足以下条件，才启动 Desktop MVP：

1. `edit.json schema v1` 已进入低频变更阶段。
2. 模板工作流稳定：
   - `templates`
   - `init-plan`
   - `scaffold-template`
   - `validate-plan`
   - `render` / `mix-audio`
3. 外部 AI 已能通过 CLI 完成多数基础剪辑任务。
4. Desktop 明确定位为“交互壳”，而不是新的业务 owner。

## Active Tracks

- `Foundation hardening`
  - 稳定 `edit.json schema`、CLI 输出契约与执行预览
  - 收敛 `doctor` 依赖预检与外部工具可用性语义
- `Audio / speech base`
  - 收敛 `audio-analyze` / `audio-gain` / `transcribe` / `detect-silence` / `separate-audio` 的输出契约、错误路径和外部依赖边界
- `Template platform`
  - 继续把模板从“内置样板”收敛为“可扩展场景单元”
- `Template guidance output`
  - 保持 `templates <id>` guide、preview、commands、supporting signal guidance 与 signal consumption 提示的稳定性
- `Transcript-assisted planning`
  - 继续扩展可解释的 transcript 辅助策略，但必须保持显式参数和 deterministic 行为
- `Beat-assisted planning`
  - 保持 `beats.json -> init-plan` 的稳定语义，不引入不可解释自动剪辑
- `Plugin extension space`
  - 为模板插件预留入口，但避免过早引入复杂运行时机制
- `Repository guardrails`
  - 维持仓库级文档、模块 owner 和验证路径，避免后续 UI 开发把边界打散
- `Repository automation`
  - 维持基础 CI 的可用性，并逐步评估哪些验证适合进入 GitHub Actions
- `CLI maintainability refactor`
  - ~~按 `docs/plans/2026-04-19-cli-maintainability-refactor-plan.md` 继续削减 `Program.cs` 与超大测试文件的维护摩擦~~ 已关闭（2026-04-21），测试拆分已达单命令+单结果路径粒度

## 已实现基础

- `.NET 8` 解决方案骨架、`Core` / `Cli` / `Desktop` / `Core.Tests` 项目已建立
- `Core` 已具备任务模型、预设模型、`ffprobe` 解析、`ffmpeg` 命令构建、进程执行和作业执行能力
- `Cli` 已提供：
  - `presets`
  - `doctor`
  - `probe`
  - `plan`
  - `run`
  - `templates`
  - `init-plan`
  - `scaffold-template`
  - `beat-track`
  - `audio-analyze`
  - `audio-gain`
  - `transcribe`
  - `detect-silence`
  - `separate-audio`
  - `cut`
  - `concat`
  - `extract-audio`
  - `subtitle`
  - `validate-plan`
  - `mix-audio`
  - `render`
- `edit.json schema v1` 已有核心模型、模板/扩展位与序列化测试
- `transcript.json` 与字幕导出 owner 已落在 `Core.Subtitles`
- `beats.json` 与波形分析 owner 已落在 `Core.Beats`
- `audio.json` 与响度分析 owner 已落在 `Core.Audio`
- `silence.json` 与静音检测 owner 已落在 `Core.Audio`
- 音频分离结果与 stem 映射 owner 已落在 `Core.AudioSeparation`

## 已实现的模板与辅助能力

- 内置模板目录已覆盖：
  - `short-form`
  - `commentary`
  - `explainer`
  - `montage`
- 已支持：
  - artifact slot 绑定
  - 模板参数覆盖
  - transcript 引用与 segment seed
  - transcript 固定 segment 分组
  - transcript 最小时长过滤
  - transcript 最大 gap 断点
  - beat seed 与固定节拍组
  - 模板 guide / preview / commands 输出
  - transcript-first 模板的推荐 transcript 策略组合

## 近期优先级

### P0

- 保持 `audio-analyze` / `audio-gain` / `transcribe` / `detect-silence` / `separate-audio` / `subtitle` 的结构化输出契约稳定，不再随手漂移字段或 envelope
- 补齐 `ffmpeg` / `whisper.cpp` / `demucs` 真实工具 smoke 与失败路径验证
- 用 `doctor` 收敛 `ffmpeg` / `ffprobe` / `whisper-cli` / `demucs` / `whisper model` 的依赖预检
- 继续把 transcript / silence / stems 等基础信号接入模板 guide、preview、`commands.json` / `commands.*` 和脚手架工作流
- 保持 supporting signal guidance 由模板 owner 显式声明，避免 CLI 侧临时拼接
- 为脚手架输出补更多命令快照 / 集成测试，锁住 `commands.json` / `commands.*` 的变量与占位符语义
- 明确重型外部依赖的安装前提、日志保留和错误输出约束
- 让当前阶段正式跨过 `CLI AI-ready` 门槛，而不是继续以“命令数量”衡量成熟度

### P1

- 字幕工作流继续收敛
- 保持模板输出里的 subtitle workflow glue 稳定，避免外部 AI 自己拼 `transcribe -> subtitle -> render`
- 评估 `audio-gain` 是否需要保持显式增益模式之外的独立归一化入口
- 设计“模板插件优先”的扩展入口，先明确发现、清单与加载边界，再决定是否进入运行时实现

### P2

- 继续扩模板类型
- 评估哪些增强适合做成单独 deterministic CLI 子命令
- Desktop MVP 是否值得继续推进

## 已规划但未实现的方向

- 模板插件机制
- Desktop 实际 UI 框架接入
- 打包 / 发布流程

## 待验证项

- 结构化输出约定还需要继续稳定，便于外部 AI 代理消费
- `whisper.cpp` 与 `demucs` 依赖较重，真实机器上的可用性、目录约定和错误路径还需要继续验证
- 基础 CI 当前只覆盖 `restore` / `build` / `test`，还未承接重依赖 smoke、发布物校验和缓存优化
- 模板插件机制需要在不破坏 `Core` owner 的前提下设计
- 模板脚手架生成的 `commands.json` / `commands.*` 仍需继续用集成测试锁住占位符、变量声明与 consumption 文案
- Desktop 是否保留为长期目标仍需后续确认

## 已验证

- `dotnet build OpenVideoToolbox.sln`
- `dotnet test OpenVideoToolbox.sln`
- `OpenVideoToolbox.Core.Tests`: 130/130
- `OpenVideoToolbox.Cli.Tests`: 94/94
- 当前开发机已通过真实 `ffprobe` / `ffmpeg` smoke：
  - `ffprobe` 媒体探测
  - `render` 导出与 sidecar 字幕复制
  - `mix-audio` 音频混合导出
  - `cut` 单段裁切
  - `concat` 片段拼接
- 可选的重依赖 real smoke：
  - `src/OpenVideoToolbox.Core.Tests/RealMediaSmokeTests.cs`
  - `src/OpenVideoToolbox.Cli.Tests/CliRealMediaSmokeTests.cs`

## 下一步

1. 优先补 `whisper.cpp` / `demucs` 的真实工具 smoke、安装前提说明和失败路径沉淀，先把重依赖链路做实。
2. 按 `docs/plans/2026-04-19-cli-maintainability-refactor-plan.md` 继续推进 CLI 维护性重构；当前下一步已经从“继续拆”转为“评估是否停止”：`MixAudioExecutionCommands`、`RenderExecutionCommands`、`DoctorEnvironmentResolutionCommands`、`SeparateAudioExecutionOutputCommands`、`TranscribeExecutionOutputCommands` 这些文件虽然仍不算小，但主题已经较单一，下一轮应先判断是否真的还有维护收益，再决定是否继续细分。
3. 继续把 `transcript` / `silence` / `stems` 信号稳定接进模板 guide、脚手架目录产物和命令快照测试，减少外部 AI 自己猜接线方式。
4. 基于 `docs/plans/2026-04-19-template-plugin-entry-boundary.md`，继续评估 `render` / `mix-audio` / 未来诊断命令是否还需要显式消费 `template.source` 做更清晰的插件来源提示，但仍不引入运行时代码加载。

## 文档保鲜方式

- 只维护少数核心文档：
  - `roadmap.md` 写当前活跃工作面
  - `ARCHITECTURE_GUARDRAILS.md` 写长期边界与阶段门槛
  - `plans/*.md` 写当前里程碑或专项
  - `MODULE.md` 写模块独有边界
- 每次任务收尾至少检查三件事：
  - 当前优先级是否变化
  - owner / 模块边界是否变化
  - 外部使用方式或验收标准是否变化
- 只要上述任一答案为“是”，就必须同步至少一个文档；不要把阶段目标、架构边界、模块规则分散到更多重复文档里。
