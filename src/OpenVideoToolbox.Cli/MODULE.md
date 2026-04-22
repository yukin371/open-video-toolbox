# OpenVideoToolbox.Cli

> 最后更新：2026-04-20

## 职责

CLI 是脚本化和调试入口，负责把命令行参数映射成对 `Core` 的调用，并把结果输出为 JSON 或退出码。它不拥有核心业务规则，同时也是外部 AI 代理调用仓库能力的唯一正式入口。

## Owns

- 命令分发：`presets`、`templates`、`doctor`、`validate-plugin`、`init-plan`、`scaffold-template`、`beat-track`、`audio-analyze`、`audio-gain`、`audio-normalize`、`transcribe`、`detect-silence`、`separate-audio`、`probe`、`plan`、`run`、`cut`、`concat`、`extract-audio`、`subtitle`、`validate-plan`、`mix-audio`、`render`
- 参数解析、默认值选择、帮助输出
- CLI 级错误提示和退出码语义
- 稳定的结构化输出契约
- 共享 command output / failure envelope helper 的组织与复用边界（当前 owner 文件：`CliCommandOutput.cs`）
- 通用 CLI option / argument 解析 helper 的组织与复用边界（当前 owner 文件：`CliOptionParsing.cs`）
- 模板命令族 wrapper 的组织边界（当前 owner 文件：`TemplateCommandHandlers.cs`）
- 媒体基础命令 wrapper 的组织边界（当前 owner 文件：`MediaCommandHandlers.cs`）
- 音频 / speech / subtitle 命令 wrapper 的组织边界（当前 owner 文件：`AudioCommandHandlers.cs`）
- `mix-audio` / `render` 执行命令 wrapper 与 plan loading glue 的组织边界（当前 owner 文件：`RenderCommandHandlers.cs`）
- 模板命令的目录发现、guide/example 输出与校验 glue 的组织边界（当前 owner 文件：`TemplateCommandPresentation.cs`、`TemplatePlanValidationSupport.cs`）
- 模板 plan build / input loading glue 的组织边界（当前 owner 文件：`TemplatePlanBuildSupport.cs`）

## Must Not Own

- `ffmpeg` / `ffprobe` 命令拼接规则
- 外部进程执行、超时、取消和输出采集
- 媒体探测解析逻辑
- 预设语义和内置预设定义
- 外部 AI 推理或任何供应商集成

## 关键依赖

- `OpenVideoToolbox.Core.Media`
- `OpenVideoToolbox.Core.Presets`
- `OpenVideoToolbox.Core.Execution`
- `OpenVideoToolbox.Core.Serialization`
- `OpenVideoToolbox.Core.Subtitles`
- `OpenVideoToolbox.Core.Beats`
- `OpenVideoToolbox.Core.Audio`
- `OpenVideoToolbox.Core.Speech`
- `OpenVideoToolbox.Core.AudioSeparation`

## 不变量

- CLI 只组合 `Core` 能力，不复制核心逻辑
- `plan` 只能生成 `JobDefinition` 和 `CommandPlan` 预览，不直接执行任务
- `run` 先探测再执行，并把结果序列化输出
- `init-plan` 只负责消费模板目录、生成 `edit.json` skeleton，并把模板规则留在 `Core.Editing`
- `templates` 无参只列模板；可按 `category` / `seed-mode` / `output-container` / `artifact-kind` / `has-artifacts` / `has-subtitles` 过滤列表，也可用 `--summary` 输出稳定摘要、用 `--json-out` 把结果写到文件；传 `--plugin-dir` 时还会显式发现一个插件目录下的模板并把插件清单一起输出，但不引入运行时代码加载；按模板 id 查询时返回可直接用于生成辅助 JSON 文件的单模板指南、推荐 seed 模式、supporting signal guidance、artifact preparation commands，以及最小 preview plan；传 `--write-examples` 时负责把这些文件和命令脚本写到目录
- `init-plan` / `scaffold-template` 可以接收 `--plugin-dir` 做显式模板目录发现，但插件模板仍必须复用既有 `Core.Editing` 模板 schema 和 plan 生成语义
- `init-plan` / `scaffold-template` 在插件模板场景下只负责把稳定的 `template.source` 元数据写进 `edit.json`，不能把插件目录等环境路径塞进计划文件
- `init-plan` 可以接收 `--artifacts` 载入模板 slot 绑定；已声明的常见 slot 仍可通过 `--subtitle` / `--bgm` 作为快捷入口
- `init-plan` 可以接收 `--template-params` 覆盖模板默认参数，但参数合并规则仍由 `Core.Editing` 持有
- `init-plan` 可以接收 `--transcript` 作为 transcript 引用，也可以在显式传入 `--seed-from-transcript` 时按 segment 生成初始 clips
- `init-plan` 可以接收 `--beats` 作为节奏引用，也可以在显式传入 `--seed-from-beats` 时按固定节拍组生成初始 clips
- `scaffold-template` 只负责组合 `templates --write-examples` 与 `init-plan` 的既有能力，把模板工作目录一次落盘，并可选附带 `validate-plan` 级别的校验结果；它不额外发明新的模板语义
- `doctor` 只负责解析依赖 override / 环境变量约定、调用 `Core.Execution` 的统一探测服务，并输出稳定 envelope；缺失 required 依赖时返回非零退出码，但不退回到非结构化错误文本
- `validate-plugin` 只负责显式加载插件目录、复用现有 manifest / template 解析和目录发现逻辑，并输出稳定校验结果；它不发明新的插件 schema，也不替代 `templates` / `init-plan` / `validate-plan` 的既有职责
- `CliCommandOutput.cs` 负责共享 command envelope、failure envelope、JSON stdout / `--json-out` 写出 helper；`Program.cs` 和后续命令族文件应直接复用，不再在入口层保留一层纯转发 wrapper
- `CliOptionParsing.cs` 负责共享 CLI option / argument 解析 helper；后续命令族文件应直接复用，不再在 `Program.cs` 或单命令 helper 里各自复制一套 `TryGet*` / `TryParse*` 逻辑
- `TemplateCommandHandlers.cs` 负责 `templates`、`init-plan`、`scaffold-template` 这组模板命令的入口 wrapper；`Program.cs` 只保留顶层分发，不再保留这组 wrapper 的内联实现
- `MediaCommandHandlers.cs` 负责 `cut`、`concat`、`extract-audio` 这组媒体基础命令的入口 wrapper；`Program.cs` 只保留顶层分发，不再保留这组 wrapper 的内联实现
- `AudioCommandHandlers.cs` 负责 `audio-analyze`、`audio-gain`、`audio-normalize`、`transcribe`、`detect-silence`、`separate-audio`、`beat-track`、`subtitle` 这组音频 / speech 命令的入口 wrapper；`Program.cs` 只保留顶层分发，不再保留这组 wrapper 的内联实现
- `RenderCommandHandlers.cs` 负责 `mix-audio`、`render` 这组执行命令 wrapper，以及仅服务这组命令的 plan loading glue；`Program.cs` 只保留顶层分发，不再保留这组 wrapper 的内联实现
- `TemplateCommandPresentation.cs` 负责模板目录发现后的 guide / preview / commands / example 写盘组织；`TemplatePlanValidationSupport.cs` 负责 `validate-plan` 相关的模板校验 glue；两者都只组合 `Core.Editing` 与现有 CLI helper，不拥有模板 schema 或 plan 生成规则
- `TemplatePlanBuildSupport.cs` 负责模板 plan build、transcript / beats 输入加载、artifact / template params string-map 解析；它只组合 `Core.Editing` 与现有 CLI parsing helper，不拥有模板 schema 或 plan 生成规则
- `cut` / `concat` / `extract-audio` 只负责解析请求参数、组织输出路径并把执行委托给 `Core.Execution`；当请求已建立后的执行阶段失败时，也应继续输出统一 command envelope，并在可用时带回 `execution` 与错误消息，而不是退回纯 usage；传 `--json-out` 时只负责把同一份 envelope 落盘，不维护第二套输出结构
- `beat-track` 只负责调用统一波形提取和节拍分析，不持有分析规则；传 `--json-out` 时只负责把同一份 command envelope 落盘；当波形提取阶段失败时，也应继续输出统一 command envelope，并在可用时带回 `extraction` 与错误消息，而不是退回纯 usage
- `audio-analyze` 只负责调用统一响度分析服务并落盘 `audio.json`；传 `--json-out` 时只负责把同一份 command envelope 落盘；当分析阶段失败时，也应继续输出统一 command envelope 与错误消息，而不是退回纯 usage；不持有 `ffmpeg loudnorm` 解析规则
- `audio-gain` 只负责解析增益参数、组织输出路径与返回执行结果；传 `--json-out` 时只负责把同一份 command envelope 落盘；当执行阶段失败时，也应继续输出统一 command envelope，并在可用时带回 `execution` 与错误消息，而不是退回纯 usage；不持有 `ffmpeg volume` 规则
- `audio-normalize` 只负责解析响度归一化参数、组织输出路径与返回执行结果；传 `--json-out` 时只负责把同一份 command envelope 落盘；当执行阶段失败时，也应继续输出统一 command envelope，并在可用时带回 `execution` 与错误消息，而不是退回纯 usage；不持有 `ffmpeg loudnorm` 命令拼装之外的额外分析逻辑
- `transcribe` 只负责转写参数解析、输出路径落盘和结构化摘要；传 `--json-out` 时只负责把同一份 command envelope 落盘；当音频预处理或转写阶段失败时，也应继续输出统一 command envelope 与错误消息，而不是退回纯 usage；不持有 `whisper.cpp` JSON 映射规则
- `detect-silence` 只负责静音检测参数解析、输出路径落盘和结构化摘要；传 `--json-out` 时只负责把同一份 command envelope 落盘；当检测阶段失败时，也应继续输出统一 command envelope 与错误消息，而不是退回纯 usage；不持有 `ffmpeg silencedetect` 解析规则
- `separate-audio` 只负责分离参数解析、输出目录落盘和结构化摘要；传 `--json-out` 时只负责把同一份 command envelope 落盘；当分离阶段失败时，也应继续输出统一 command envelope 与错误消息，而不是退回纯 usage；不持有 `demucs` 目录映射或命令拼接规则
- `subtitle` 只负责加载 `transcript.json`、选择输出格式、落盘 sidecar 文件，并把渲染委托给 `Core.Subtitles`；传 `--json-out` 时只负责把同一份结构化结果 envelope 落盘，不维护第二套输出结构
- `validate-plan` 负责加载 `edit.json`、解析相对路径、调用 `Core.Editing.EditPlanValidator` 并输出结构化校验结果；传 `--plugin-dir` 时只负责提供插件模板目录上下文，不复制 `Core.Editing` 的来源校验规则
- `mix-audio` 只负责加载 `edit.json`、应用输出路径 override，并把音频执行委托给 `Core.Execution`
- `mix-audio --preview` 只负责加载 `edit.json`、应用输出路径 override，并输出 `Core.Execution` 的统一 `executionPreview`，不触发进程执行；如 plan 已带 `template.source`，CLI 只做原样透传，帮助后续执行阶段继续审计来源；当 plan 已成功加载但后续 preview / 执行阶段失败时，无论是抛异常还是底层执行返回 failed status，也应继续输出同一套结构化 envelope，并在可用时带回 `executionPreview` / `execution` 与非零退出码，而不是退回纯 usage；传 `--json-out` 时只负责把同一份 envelope 落盘，不维护第二套输出结构
- `render` 只负责加载 `edit.json`、应用 CLI override，并把执行委托给 `Core`
- `render --preview` 只负责加载 `edit.json` 并输出 `Core.Execution` 的统一 `executionPreview`，不触发进程执行；如 plan 已带 `template.source`，CLI 只做原样透传，帮助后续执行阶段继续审计来源；当 plan 已成功加载但后续 preview / 执行阶段失败时，无论是抛异常还是底层执行返回 failed status，也应继续输出同一套结构化 envelope，并在可用时带回 `executionPreview` / `execution` 与非零退出码，而不是退回纯 usage；传 `--json-out` 时只负责把同一份 envelope 落盘，不维护第二套输出结构
- 外部 AI 只能消费 CLI 输出，不能要求仓库内出现新的 AI provider 层

## 常见坑

- 新增 CLI 选项时，容易把默认值、路径规则或命令拼接塞回入口层
- supporting signal guidance 必须透传 `Core.Editing` 的模板定义，不能在 `commands.json`、`guide.json`、帮助脚本里各写一份不同版本
- supporting signal 的 command / consumption / script 注释必须从同一套模板示例输出派生，不能在 `guide.json`、`commands.json`、`commands.*` 各自改写
- seed mode commands 与 transcript strategy variants 也必须从同一套模板示例输出派生，`commands.json` 与 `commands.*` 不能一边有、一边缺，或各自维护不同排序
- 插件模板的 `init-plan` / seed / workflow commands 也必须显式沿用同一套 `<plugin-dir>` 占位符与变量声明，不能只在某一种脚本里补 `--plugin-dir`
- supporting signal 的外部依赖占位符也必须显式、稳定；例如 transcript signal 应给出 `<whisper-model-path>`，不能退回泛化的 `<path>`
- subtitle artifact preparation commands 必须和 guide / `commands.*` 共用同一套命令列表，不能在不同输出面各自拼接
- 一旦 JSON 结构漂移，外部 AI 代理和脚本编排会直接失效
- `Program.cs` 当前是手写参数解析，改命令面时要同时验证帮助输出和错误消息
- 共享输出 helper 已迁到 `CliCommandOutput.cs`；继续做 CLI 可维护性重构时，应优先直接消费该 owner，而不是把同一套 helper 重新包一层本地静态函数
- 通用 option 解析 helper 已迁到 `CliOptionParsing.cs`；继续重构时，应优先复用这里，而不是再在命令实现里复制 `TryGetRequiredOption`、`TryGetBoolOption`、`TryParseOptions` 之类的方法
- 模板命令 wrapper 已迁到 `TemplateCommandHandlers.cs`；继续重构时，应优先在命令族文件内扩展，而不是再把模板命令入口逻辑塞回 `Program.cs`
- 媒体基础命令 wrapper 已迁到 `MediaCommandHandlers.cs`；继续重构时，应优先在命令族文件内扩展，而不是再把 `cut` / `concat` / `extract-audio` 入口逻辑塞回 `Program.cs`
- 音频 / speech 命令 wrapper 已迁到 `AudioCommandHandlers.cs`；继续重构时，应优先在命令族文件内扩展，而不是再把 `audio-*` / `transcribe` / `detect-silence` / `separate-audio` / `beat-track` / `subtitle` 入口逻辑塞回 `Program.cs`
- `mix-audio` / `render` wrapper 已迁到 `RenderCommandHandlers.cs`；继续重构时，应优先在命令族文件内扩展，并把仅服务执行命令的 plan loading glue 保持在同一 owner 内
- 模板 guide / commands / preview 的组织逻辑已迁到 `TemplateCommandPresentation.cs`；继续重构时应优先复用这里，而不是再把相同的模板输出拼装塞回 `Program.cs`

## 文档同步触发条件

- 新增或删除 CLI 命令
- CLI 参数约定、退出码语义或帮助输出结构变化
- CLI 输出契约变化
