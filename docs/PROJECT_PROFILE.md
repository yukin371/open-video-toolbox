# Project Profile

最后更新：2026-04-25

## 项目类型

- 面向外部 AI 代理与脚本调用的 CLI 媒体处理仓库，当前以 `.NET` 解决方案形式组织。
- 同时包含 `Core` 类库、`Cli` 入口、`Desktop` 入口和 `Core.Tests` 测试项目。

## 技术栈

- 语言与运行时：`.NET 8`、`C#`
- 解决方案：`OpenVideoToolbox.sln`
- 测试：`xUnit`、`Microsoft.NET.Test.Sdk`、`coverlet.collector`、契约快照黄金文件
- 外部工具边界：`ffmpeg`、`ffprobe`
- AI 集成策略：软件内不内置 AI，外部代理通过 CLI 编排
- 代码风格：全局启用 `Nullable`、`ImplicitUsings`，`LangVersion=latest`

## 运行入口

- CLI 入口：`src/OpenVideoToolbox.Cli/Program.cs`
- Desktop 入口：`src/OpenVideoToolbox.Desktop/Program.cs`
  - 当前状态：占位入口，只输出 `Desktop bootstrap placeholder`

## 用户文档入口

- 最短上手路径：`docs/QUICK_START.md`
- 完整功能与使用说明：`docs/FEATURES_AND_USAGE.md`
- 精确命令签名速查：`docs/COMMAND_REFERENCE.md`

## 已确认的验证命令

- 构建解决方案：`dotnet build OpenVideoToolbox.sln`
- 运行测试：`dotnet test OpenVideoToolbox.sln`
- 运行 CLI：`dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- <command>`
- 运行 Desktop 占位入口：`dotnet run --project ./src/OpenVideoToolbox.Desktop/OpenVideoToolbox.Desktop.csproj`

## CLI 已确认命令面

- `presets`
- `templates [<template-id>] [--template <id>] [--category <id>] [--seed-mode <manual|transcript|beats>] [--output-container <ext>] [--artifact-kind <kind>] [--has-artifacts [true|false]] [--has-subtitles [true|false]] [--summary [true|false]] [--plugin-dir <path>] [--json-out <path>] [--write-examples <dir>]`
- `effects [list [--category <id>] | describe <type> | <type>] [--json-out <path>]`
- `doctor [--ffmpeg <path>] [--ffprobe <path>] [--whisper-cli <path>] [--whisper-model <path>] [--demucs <path>] [--json-out <path>] [--timeout-seconds <n>]`
- `validate-plugin --plugin-dir <path> [--json-out <path>]`
- `init-plan <input> --template <id> --output <edit.json> [--render-output <path>] [--probe] [--ffprobe <path>] [--transcript <transcript.json>] [--seed-from-transcript] [--transcript-segment-group-size <n>] [--min-transcript-segment-duration-ms <n>] [--max-transcript-gap-ms <n>] [--beats <beats.json>] [--seed-from-beats] [--beat-group-size <n>] [--artifacts <artifacts.json>] [--template-params <template-params.json>] [--subtitle <path>] [--subtitle-mode <sidecar|burnIn|none>] [--bgm <path>] [--plugin-dir <path>] [--timeout-seconds <n>]`
- `init-narrated-plan --manifest <narrated.json> --output <edit.json> [--template <id>] [--render-output <path>] [--vars <vars.json>] [--ffprobe <path>] [--timeout-seconds <n>] [--json-out <path>]`
- `scaffold-template <input> --template <id> --dir <workdir> [--validate [true|false]] [--check-files [true|false]] [--render-output <path>] [--probe] [--ffprobe <path>] [--transcript <transcript.json>] [--seed-from-transcript] [--transcript-segment-group-size <n>] [--min-transcript-segment-duration-ms <n>] [--max-transcript-gap-ms <n>] [--beats <beats.json>] [--seed-from-beats] [--beat-group-size <n>] [--artifacts <artifacts.json>] [--template-params <template-params.json>] [--subtitle <path>] [--subtitle-mode <sidecar|burnIn|none>] [--bgm <path>] [--plugin-dir <path>] [--timeout-seconds <n>]`
- `scaffold-template-batch --manifest <batch.json> [--plugin-dir <path>] [--json-out <path>]`
- `export --plan <edit.json> --format <edl> --output <path> [--frame-rate <fps>] [--title <name>] [--json-out <path>] [--overwrite]`
- `beat-track <input> --output <beats.json> [--ffmpeg <path>] [--sample-rate <hz>] [--json-out <path>] [--timeout-seconds <n>]`
- `audio-analyze <input> --output <audio.json> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>]`
- `audio-gain <input> --gain-db <n> --output <path> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>] [--overwrite]`
- `audio-normalize <input> --output <path> [--target-lufs <n>] [--lra <n>] [--true-peak-db <n>] [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>] [--overwrite]`
- `transcribe <input> --model <path> --output <transcript.json> [--language <id>] [--translate [true|false]] [--whisper-cli <path>] [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>]`
- `detect-silence <input> --output <silence.json> [--noise-db <n>] [--min-duration-ms <n>] [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>]`
- `auto-cut-silence --silence <silence.json> [--clips-only] [--padding-ms <n>] [--merge-gap-ms <n>] [--min-clip-duration-ms <n>] [--source-duration-ms <n>] [--ffprobe <path>] [--template <id>] [--render-output <path>] [--output <path>] [--json-out <path>]`
- `separate-audio <input> --output-dir <path> [--model <id>] [--demucs <path>] [--json-out <path>] [--timeout-seconds <n>]`
- `cut <input> --from <hh:mm:ss.fff> --to <hh:mm:ss.fff> --output <path> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>] [--overwrite]`
- `concat --input-list <path> --output <path> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>] [--overwrite]`
- `extract-audio <input> --track <n> --output <path> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>] [--overwrite]`
- `subtitle <input> --transcript <transcript.json> --format <srt|ass> --output <path> [--max-line-length <n>] [--json-out <path>]`
- `inspect-plan --plan <edit.json> [--check-files [true|false]] [--plugin-dir <path>] [--json-out <path>]`
- `replace-plan-material --plan <edit.json> [--write-to <path>] [--in-place [true|false]] --path <new-file> (--source-input | --audio-track-id <id> | --artifact-slot <slotId> | --transcript | --beats | --subtitles) [--subtitle-mode <sidecar|burnIn>] [--path-style <auto|relative|absolute>] [--check-files [true|false]] [--plugin-dir <path>] [--require-valid [true|false]] [--json-out <path>]`
- `replace-plan-material-batch --manifest <batch.json> [--plugin-dir <path>] [--json-out <path>]`
- `attach-plan-material --plan <edit.json> [--write-to <path>] [--in-place [true|false]] --path <new-file> (--transcript | --beats | --subtitles | --audio-track-id <id> [--audio-track-role <original|voice|bgm|effects>] | --artifact-slot <slotId>) [--subtitle-mode <sidecar|burnIn>] [--path-style <auto|relative|absolute>] [--check-files [true|false]] [--plugin-dir <path>] [--require-valid [true|false]] [--json-out <path>]`
- `attach-plan-material-batch --manifest <batch.json> [--plugin-dir <path>] [--json-out <path>]`
- `bind-voice-track --plan <edit.json> --path <audio-file> [--track-id <id>] [--role <original|voice|bgm|effects>] [--write-to <path>] [--in-place [true|false]] [--path-style <auto|relative|absolute>] [--check-files [true|false]] [--plugin-dir <path>] [--require-valid [true|false]] [--json-out <path>]`
- `bind-voice-track-batch --manifest <batch.json> [--plugin-dir <path>] [--json-out <path>]`
- `validate-plan --plan <edit.json> [--check-files [true|false]] [--plugin-dir <path>] [--json-out <path>]`
- `mix-audio --plan <edit.json> --output <path> [--preview [true|false]] [--json-out <path>] [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]`
- `render --plan <path> [--output <path>] [--preview [true|false]] [--json-out <path>] [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]`
- `render-batch --manifest <batch.json> [--preview [true|false]] [--ffmpeg <path>] [--timeout-seconds <n>] [--json-out <path>]`
- `probe <input> [--ffprobe <path>]`
- `plan <input> [--preset <id>] [--output-dir <dir>] [--output-name <name>] [--ffmpeg <path>] [--overwrite]`
- `run <input> [--preset <id>] [--output-dir <dir>] [--output-name <name>] [--ffprobe <path>] [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]`

说明：
- 上述命令均已实现；当前 `Project Profile` 只记录仓库已落地命令面，不再混写未来规划命令。
- `doctor` 已实现，用于把 required / optional 外部依赖状态收敛为稳定 JSON；命令会优先读取 CLI 参数，再读取 `OVT_WHISPER_CLI_PATH`、`OVT_DEMUCS_PATH`、`OVT_WHISPER_MODEL_PATH`。
- `doctor` 的 CLI 测试现已覆盖依赖 envelope、`--json-out`、environment fallback、option 优先级，以及 `default` / `unset` / `resolvedValue` / `detail` 等关键输出语义，进一步锁住外部依赖边界。
- `validate-plugin` 已实现，用于显式校验插件目录、`plugin.json` 和模板定义是否合规，并输出插件/模板清单与结构化失败原因；传 `--json-out` 时会把同一份 envelope 直接写到文件。
- `inspect-plan` 已实现，用于把 `edit.json` 的素材绑定、可替换目标、缺失引用与校验结果收敛成稳定结构化输出；当 plan 来自插件模板时，可通过 `--plugin-dir` 把插件模板目录显式接回 inspection / 校验链。
- `replace-plan-material` 已实现，用于对 plan 中已存在的素材绑定做受控替换，并返回后置校验结果；第一版只支持单目标 replace，不支持 attach / upsert。
- `replace-plan-material-batch` 已实现，用于从 manifest 批量读取多条素材替换任务，统一解析相对路径、复用单项 replace 流程，并在 manifest 同目录固定写出 `summary.json` 与 `results/<id>.json`。
- `attach-plan-material` 已实现，用于对当前缺失的 `transcript` / `beats` / `subtitles` / `audioTracks` 做显式挂载，并对模板已声明的 artifact slot 做 upsert；它不承担通用 patch。
- `attach-plan-material-batch` 已实现，用于从 manifest 批量读取多条素材挂载任务，统一解析相对路径、复用单项 attach 流程，并在 manifest 同目录固定写出 `summary.json` 与 `results/<id>.json`。
- `bind-voice-track` 已实现，用于把外部配音、TTS 或 voice conversion 结果按默认 `voice-main` / `voice` 约定接回 plan；底层仍复用 `audioTracks` attach/upsert 语义，不引入第二套模型。
- `bind-voice-track-batch` 已实现，用于从 manifest 批量读取多条配音接回任务，统一解析相对路径、复用单项 voice bind 流程，并在 manifest 同目录固定写出 `summary.json` 与 `results/<id>.json`。
- `validate-plan` 已实现，用于在真正执行前校验外部 AI 或人工修改后的 `edit.json` 是否仍满足当前 schema v1，或当前 `schemaVersion = 2` 的 timeline 基础语义约束；当 plan 来自插件模板时，可通过 `--plugin-dir` 把插件模板目录显式接回校验链。
- `effects` 已实现，用于发现当前内置 v2 effect 描述符，并输出单 effect 的参数 schema、模板模式与 FFmpeg 模板骨架；当前不负责插件 effect 加载，也不代表 render 已进入 effect 执行阶段。
- `mix-audio --preview` 与 `render --preview` 已实现，用于在真实执行前输出稳定的 `executionPreview`；传 `--json-out` 时会把统一 envelope 原样写到文件。
- `mix-audio` / `render` 的 preview 与执行 envelope 现会额外透出 `templateSource`，直接复用 plan 中已有的 `template.source` 元数据，便于后续执行阶段继续审计插件来源。
- 当 `mix-audio` / `render` 已成功加载 plan、但在 preview 构建或执行阶段失败时，无论是抛错还是底层执行返回 failed status，CLI 现都会输出结构化 failure envelope，继续保留 `templateSource`、局部操作上下文、可用时的 `executionPreview` / `execution` 与错误消息，并返回非零退出码，而不退回纯 usage 文本。
- `cut`、`concat`、`extract-audio` 现也已切到统一 command envelope；当请求已建立但执行失败时，无论是底层返回 failed status 还是进程启动抛错，CLI 都会继续输出结构化 failure envelope，而不是退回 usage 文本。
- `beat-track` 现也已切到统一 command envelope，并补上 `--json-out`；当波形提取失败时，无论是底层返回 failed status 还是进程启动抛错，CLI 都会继续输出结构化 failure envelope，而不是退回 usage 文本。
- `audio-analyze` 现也已切到统一 command envelope；当响度分析阶段失败时，会继续输出结构化 failure envelope，而不是退回 usage 文本；`--json-out` 会把同一份 envelope 直接写到文件。
- `audio-gain` 现也已切到统一 command envelope；当执行阶段失败时，会继续输出结构化 failure envelope，而不是退回 usage 文本；`--json-out` 会把同一份 envelope 直接写到文件。
- `audio-normalize` 现也已切到统一 command envelope；当执行阶段失败时，会继续输出结构化 failure envelope，而不是退回 usage 文本；`--json-out` 会把同一份 envelope 直接写到文件。
- `transcribe` 现也已切到统一 command envelope；当音频预处理或转写阶段失败时，会继续输出结构化 failure envelope，而不是退回 usage 文本；`--json-out` 会把同一份 envelope 直接写到文件。
- `detect-silence` 现也已切到统一 command envelope；当检测阶段失败时，会继续输出结构化 failure envelope，而不是退回 usage 文本；`--json-out` 会把同一份 envelope 直接写到文件。
- `auto-cut-silence` 已实现，用于把 `silence.json` 反推成确定性的非静音 clips，或直接生成 `edit.json`；默认仍走既有 v1 plan 路径，但当内置模板显式声明 `planModel = v2Timeline` 时，会复用 `Core.Editing` 的模板工厂生成真实 v2 `timeline` plan，再替换主视频轨 clips；总时长优先使用 `--source-duration-ms`，否则通过 `Core.Media` 探测 `silence.json.inputPath`，CLI 只负责参数解析、时长来源接线与 envelope 输出。
- `init-plan` 的 transcript / beats seed 现已可同时复用于 v1 与显式 `v2Timeline` 模板；当模板走 v2 路径时，seed 结果会在 `Core.Editing` 内部转成真实 `timeline` clips，而不是由 CLI 手写另一套骨架。
- `separate-audio` 现也已切到统一 command envelope；当分离阶段失败时，会继续输出结构化 failure envelope，而不是退回 usage 文本；`--json-out` 会把同一份 envelope 直接写到文件。
- `subtitle` 已支持 `--json-out`，用于把 sidecar 生成结果的同一份结构化 envelope 直接写到文件。
- `scaffold-template` 已实现，用于一次性落出模板指南、示例文件与初始 `edit.json` 工作目录；传 `--validate` 时还会同步返回校验结果。
- `init-narrated-plan` 已实现，用于从显式 narrated manifest 装配讲解型 `schemaVersion = 2` `edit.json`；当前支持 `video + image` 两类 visual section、可选 `video.progressBar`、单视频 `${var}` / `${var:-default}` / `$${text}` 变量解析、`bgm.slot.required = false` 的可选 BGM 轨裁剪，以及 `sections[].visual.slot.required = false` 时投影 black color placeholder 的首个 optional visual slot；CLI 额外支持 `--vars <vars.json>` overlay，owner 仍固定在 `Core.Editing` + `Cli` glue，不纳入现有 `templates` catalog，也不引入 AI/TTS provider。
- `scaffold-template-batch` 已实现，用于从 manifest 批量生成模板工作目录；相对路径按 manifest 所在目录解析，默认工作目录为 `tasks/<id>`，并会在同目录固定写出 `summary.json`、`results/<id>.json` 与部分成功摘要。
- `render-batch` 已实现，用于从 manifest 批量读取多份 `edit.json` 并复用单项 `render` 语义；当前支持全局 `--preview` / `--ffmpeg` / `--timeout-seconds` 与 item 级 `output` / `overwrite`，并会在 manifest 同目录写出 `summary.json` 与 `results/<id>.json`。
- `export` 已实现，用于把 `schemaVersion = 1` 或 `schemaVersion = 2` 的 `edit.json` 统一导出为粗粒度 `EDL` cut list；本轮只支持 `edl`，owner 固定在 `Core.Execution`，CLI 只负责参数解析、路径解析与 envelope 输出；`v1` 会先包装成单主视频轨再导出，`v2` 只导出 `main` 或首条 video track，并通过 warning 明确说明 audio / effect / transition / extra video track 的忽略语义。
- `templates <id>` / `--write-examples` 已把 transcript、beats、silence、stems 等 supporting signal guidance 纳入稳定输出，外部 AI 不必再自己猜前置命令和接入方式。
- 对带 `stems` supporting signal 的模板，`artifacts.json` / preview plan 里的 `bgm` 示例现在会直接预填 `stems/htdemucs/input/no_vocals.wav`，减少外部 AI 自己猜 `Demucs` 目录结构。
- 对支持字幕的模板，`templates <id>` / `commands.*` 现在会给出稳定的 subtitle artifact preparation 与 attach 命令，并把 `inspect-plan --check-files`、`validate-plan --check-files` 纳入工作流，帮助外部 AI 串起 `transcribe -> subtitle -> attach -> inspect / validate -> render`。
- `templates` 无参返回模板列表；`templates <id>` 返回单模板详情、建议 skeleton、推荐的 seed 模式，以及最小 preview plan；transcript 模式还会附带 grouped / min-duration / max-gap 策略变体示例，并标记模板推荐组合。
- `templates --category` / `--seed-mode` / `--output-container` / `--artifact-kind` / `--has-artifacts` / `--has-subtitles` 支持先过滤模板列表；`--summary` 支持输出稳定摘要视图；`--json-out` 支持把当前返回结果直接写到文件。
- `templates <id> --write-examples <dir>` 会把模板指南相关文件直接落到目录，包含 `guide.json` 与各类示例文件，减少外部 AI 自己拆 stdout。
- 这些模板目录产物现在还会附带 `commands.json`、`commands.ps1`、`commands.cmd`、`commands.sh`，用于直接驱动后续 CLI 流程。
- 对插件模板，这些 `commands.json` / `commands.*` 示例现会显式带上 `<plugin-dir>` 占位符和对应变量声明，保证示例目录里的 `init-plan` / `inspect-plan` / `validate-plan` 工作流可以闭环复用插件上下文。
- 插件模板的 preview plan 示例现也会沿用同一份 `template.source` 元数据，避免 guide 顶层标记为 plugin、但示例 `edit.json` 仍像 built-in 的断层。
- 模板插件扩展面已完成第一阶段收口：`--plugin-dir` 显式目录发现、静态 manifest、`template.source` 全链路审计、插件模板 schema 校验均已落地；仍不引入运行时代码插件；见 `docs/plans/2026-04-19-template-plugin-entry-boundary.md`。
- `templates` 现已支持 `--plugin-dir <path>` 做显式目录发现，并在结构化输出里附带插件清单。
- `init-plan` / `scaffold-template` 现也支持 `--plugin-dir <path>`；插件模板只要继续满足既有模板 schema，就可以直接复用 `Core.Editing` 的 plan 生成、preview 和脚手架输出，不需要额外引入运行时代码插件。
- 插件模板生成的 `edit.json` 现会在 `template.source` 中持久化稳定来源元数据，只保留 `kind` / `pluginId` / `pluginVersion`，不把机器相关的插件目录写进 plan。
- 详细草案见 `docs/CLI_MVP.md`。
- 可选的重依赖 real smoke 现已同时接入 `src/OpenVideoToolbox.Core.Tests/RealMediaSmokeTests.cs` 与 `src/OpenVideoToolbox.Cli.Tests/CliRealMediaSmokeTests.cs`；默认环境缺依赖时会自动跳过。
- 推荐先跑 `doctor` 确认依赖解析状态，再跑上述 real smoke；否则很容易把环境缺失误判成命令实现故障。
- 契约冻结与模板稳定收口后，当前阶段已推进到：`H1 -> H2+T1 -> T2 -> P1 -> E1` 完成；下一候选阶段为 `D1` 或 `E2`。
- 当前测试基线：`OpenVideoToolbox.Core.Tests` 172，`OpenVideoToolbox.Cli.Tests` 186，总计 358。
- 发布链现状：`src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj` 已明确程序集名 `ovt` 与版本 `0.1.0`，`.github/workflows/release.yml` 已支持 tag 触发的跨平台 single-file 发布。
- Windows 常用环境变量：
  - `OVT_WHISPER_CLI_PATH`
  - `OVT_WHISPER_MODEL_PATH`
  - `OVT_DEMUCS_PATH`

## 仓库拓扑

- `src/OpenVideoToolbox.Core`
  - 领域模型、预设模型、媒体探测、命令计划、进程执行、JSON 序列化
- `src/OpenVideoToolbox.Cli`
  - 当前最小可运行入口，直接组合 `Core` 能力完成媒体处理命令与结构化输出
- `src/OpenVideoToolbox.Desktop`
  - 桌面入口占位，尚未接入实际 UI 框架与业务流
- `src/OpenVideoToolbox.Core.Tests`
  - `Core` 的模型、命令构建、进程执行、媒体探测和作业执行测试
- `docs`
  - PRD、技术路线、开发原则、架构草图、roadmap，以及本次补齐的仓库治理文档

## 共享能力候选 owner

- 命令构建与进程执行：`src/OpenVideoToolbox.Core/Execution`
  - 含 `doctor` 依赖探测 owner，负责统一外部命令探活与结果建模
- 媒体探测与 `ffprobe` 解析：`src/OpenVideoToolbox.Core/Media`
- 预设定义与内置预设：`src/OpenVideoToolbox.Core/Presets`
- `edit.json` 计划模型：`src/OpenVideoToolbox.Core/Editing`
  - 含顶层 `transcript` 引用，供 `init-plan`、字幕流程和后续编辑辅助共享 transcript 元数据
  - 含顶层 `beats` 引用，供 `init-plan` / `render` / 外部 AI 共享节奏元数据
  - 含顶层 `artifacts` 绑定，供模板 slot、外部 AI 和未来轻量 UI 共享文件引用语义
- `edit.json` 执行与最终导出：`src/OpenVideoToolbox.Core/Execution`
- `transcript.json` 与字幕导出：`src/OpenVideoToolbox.Core/Subtitles`
- `beats.json` 与波形分析：`src/OpenVideoToolbox.Core/Beats`
- `audio.json` 与响度分析：`src/OpenVideoToolbox.Core/Audio`
- `silence.json` 与静音检测：`src/OpenVideoToolbox.Core/Audio`
- `transcript.json` 外部转写适配：`src/OpenVideoToolbox.Core/Speech`
- CLI 参数解析与命令出口语义：`src/OpenVideoToolbox.Cli`
- 桌面交互与视图模型：`src/OpenVideoToolbox.Desktop`
  - 当前状态：`TBD`
  - 确认路径：Desktop MVP 启动时明确 UI 框架、导航和状态管理 owner

## 已知高风险区域

- `Core.Execution`
  - 这里承接外部进程启动、超时、取消、输出采集和命令行引用规则，改动容易引入行为回归
- `Core.Media`
  - 这里承接 `ffprobe` 调用与 JSON 解析，容易受到外部工具输出差异影响
- `Core.Presets` 与 `Core.Execution` 的边界
  - 新增编码参数时容易把预设语义、命令映射和 UI 参数编辑耦合在一起
- `Cli` 当前采用手写参数解析
  - 改动命令面时需同时验证帮助输出、错误提示和默认值逻辑
- CLI 输出契约
  - 如果 JSON 结构频繁漂移，会直接影响外部 AI 代理和脚本编排稳定性
- `edit.json` 计划模型
  - 如果剪辑计划语义不稳定，AI 生成结果和人工二次修正都会频繁失效
- 节拍种子生成
  - `init-plan --seed-from-beats` 只应做确定性 clip 初始化，不能在 CLI 层引入不可解释的“自动剪辑”策略
- transcript 种子生成
  - `init-plan --seed-from-transcript` 只应按 segment 做确定性 clip 初始化，不能在 CLI 层偷偷做摘要式剪辑推断
  - `--transcript-segment-group-size` 只允许显式按固定 segment 组装 clip，规则 owner 仍在 `Core.Editing`
  - `--min-transcript-segment-duration-ms` 只允许显式过滤过短 segment，不能偷偷引入内容理解或摘要判断
  - `--max-transcript-gap-ms` 只允许显式按时间间隔断开 clip，不能把“停顿”解释成内容层面的章节理解
- 模板 artifact 绑定
  - `init-plan --artifacts` 只应做模板声明 slot 的路径装配，不能在 CLI 层发明未声明的隐式 slot
- 模板参数覆盖
  - `init-plan --template-params` 只应做参数覆盖，不应把模板局部参数膨胀成新的专用 CLI flag

## 当前已知缺口

- CI / workflow 文件：已落地基础 GitHub Actions
  - 当前已有 `.github/workflows/ci.yml`
  - 覆盖范围：`push main` 与 `pull_request` 上的 `dotnet restore`、`dotnet build`、`dotnet test`，并在 CI 中补 ffmpeg 安装
  - 另有 `.github/workflows/release.yml` 承接 tag 触发的 release 发布
  - 当前未纳入：依赖 `whisper.cpp` / `demucs` 的可选 smoke
- Desktop 实际 UI 框架接入状态：`TBD`
  - 确认路径：当 `OpenVideoToolbox.Desktop` 引入 Avalonia 或其他桌面框架时更新
- 打包 / 发布流程：已落地核心发布链
  - 当前已有 tag 触发的 GitHub Release 和跨平台 single-file 发布
  - 尚未落地：包管理器渠道；如继续推进，转入 `E2`
