# Project Profile

最后更新：2026-04-15

## 项目类型

- 面向外部 AI 代理与脚本调用的 CLI 媒体处理仓库，当前以 `.NET` 解决方案形式组织。
- 同时包含 `Core` 类库、`Cli` 入口、`Desktop` 入口和 `Core.Tests` 测试项目。

## 技术栈

- 语言与运行时：`.NET 8`、`C#`
- 解决方案：`OpenVideoToolbox.sln`
- 测试：`xUnit`、`Microsoft.NET.Test.Sdk`、`coverlet.collector`
- 外部工具边界：`ffmpeg`、`ffprobe`
- AI 集成策略：软件内不内置 AI，外部代理通过 CLI 编排
- 代码风格：全局启用 `Nullable`、`ImplicitUsings`，`LangVersion=latest`

## 运行入口

- CLI 入口：`src/OpenVideoToolbox.Cli/Program.cs`
- Desktop 入口：`src/OpenVideoToolbox.Desktop/Program.cs`
  - 当前状态：占位入口，只输出 `Desktop bootstrap placeholder`

## 已确认的验证命令

- 构建解决方案：`dotnet build E:\Github\open-video-toolbox\OpenVideoToolbox.sln`
- 运行测试：`dotnet test E:\Github\open-video-toolbox\OpenVideoToolbox.sln`
- 运行 CLI：`dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- <command>`
- 运行 Desktop 占位入口：`dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Desktop\OpenVideoToolbox.Desktop.csproj`

## CLI 已确认命令面

- `presets`
- `templates [<template-id>] [--template <id>] [--category <id>] [--seed-mode <manual|transcript|beats>] [--output-container <ext>] [--artifact-kind <kind>] [--has-artifacts [true|false]] [--has-subtitles [true|false]] [--summary [true|false]] [--json-out <path>] [--write-examples <dir>]`
- `init-plan <input> --template <id> --output <edit.json> [--render-output <path>] [--probe] [--ffprobe <path>] [--transcript <transcript.json>] [--seed-from-transcript] [--beats <beats.json>] [--seed-from-beats] [--beat-group-size <n>] [--artifacts <artifacts.json>] [--template-params <template-params.json>] [--subtitle <path>] [--subtitle-mode <sidecar|burnIn|none>] [--bgm <path>] [--timeout-seconds <n>]`
- `scaffold-template <input> --template <id> --dir <workdir> [--validate [true|false]] [--check-files [true|false]] [--render-output <path>] [--probe] [--ffprobe <path>] [--transcript <transcript.json>] [--seed-from-transcript] [--beats <beats.json>] [--seed-from-beats] [--beat-group-size <n>] [--artifacts <artifacts.json>] [--template-params <template-params.json>] [--subtitle <path>] [--subtitle-mode <sidecar|burnIn|none>] [--bgm <path>] [--timeout-seconds <n>]`
- `beat-track <input> --output <beats.json> [--ffmpeg <path>] [--sample-rate <hz>] [--timeout-seconds <n>]`
- `cut <input> --from <hh:mm:ss.fff> --to <hh:mm:ss.fff> --output <path> [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]`
- `concat --input-list <path> --output <path> [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]`
- `extract-audio <input> --track <n> --output <path> [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]`
- `subtitle <input> --transcript <transcript.json> --format <srt|ass> --output <path> [--max-line-length <n>]`
- `validate-plan --plan <edit.json> [--check-files [true|false]] [--json-out <path>]`
- `mix-audio --plan <edit.json> --output <path> [--preview [true|false]] [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]`
- `render --plan <path> [--output <path>] [--preview [true|false]] [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]`
- `probe <input> [--ffprobe <path>]`
- `plan <input> [--preset <id>] [--output-dir <dir>] [--output-name <name>] [--ffmpeg <path>] [--overwrite]`
- `run <input> [--preset <id>] [--output-dir <dir>] [--output-name <name>] [--ffprobe <path>] [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]`

## 已规划但未实现的命令方向

- `separate-audio`

说明：
- `templates`、`init-plan`、`scaffold-template`、`beat-track`、`cut`、`concat`、`extract-audio`、`subtitle`、`validate-plan`、`mix-audio`、`render` 已实现；其余命令当前仍是产品规划，不是仓库现状。
- `validate-plan` 已实现，用于在真正执行前校验外部 AI 或人工修改后的 `edit.json` 是否仍满足当前 schema v1 与基础语义约束。
- `mix-audio --preview` 与 `render --preview` 已实现，用于在真实执行前输出稳定的 `executionPreview`。
- `scaffold-template` 已实现，用于一次性落出模板指南、示例文件与初始 `edit.json` 工作目录；传 `--validate` 时还会同步返回校验结果。
- `templates` 无参返回模板列表；`templates <id>` 返回单模板详情、建议 skeleton、推荐的 seed 模式，以及最小 preview plan。
- `templates --category` / `--seed-mode` / `--output-container` / `--artifact-kind` / `--has-artifacts` / `--has-subtitles` 支持先过滤模板列表；`--summary` 支持输出稳定摘要视图；`--json-out` 支持把当前返回结果直接写到文件。
- `templates <id> --write-examples <dir>` 会把模板指南相关文件直接落到目录，包含 `guide.json` 与各类示例文件，减少外部 AI 自己拆 stdout。
- 这些模板目录产物现在还会附带 `commands.json`、`commands.ps1`、`commands.cmd`、`commands.sh`，用于直接驱动后续 CLI 流程。
- 详细草案见 `docs/CLI_MVP.md`。

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
- 媒体探测与 `ffprobe` 解析：`src/OpenVideoToolbox.Core/Media`
- 预设定义与内置预设：`src/OpenVideoToolbox.Core/Presets`
- `edit.json` 计划模型：`src/OpenVideoToolbox.Core/Editing`
  - 含顶层 `transcript` 引用，供 `init-plan`、字幕流程和后续编辑辅助共享 transcript 元数据
  - 含顶层 `beats` 引用，供 `init-plan` / `render` / 外部 AI 共享节奏元数据
  - 含顶层 `artifacts` 绑定，供模板 slot、外部 AI 和未来轻量 UI 共享文件引用语义
- `edit.json` 执行与最终导出：`src/OpenVideoToolbox.Core/Execution`
- `transcript.json` 与字幕导出：`src/OpenVideoToolbox.Core/Subtitles`
- `beats.json` 与波形分析：`src/OpenVideoToolbox.Core/Beats`
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
- 模板 artifact 绑定
  - `init-plan --artifacts` 只应做模板声明 slot 的路径装配，不能在 CLI 层发明未声明的隐式 slot
- 模板参数覆盖
  - `init-plan --template-params` 只应做参数覆盖，不应把模板局部参数膨胀成新的专用 CLI flag

## 当前已知缺口

- CI / workflow 文件：`TBD`
  - 确认路径：新增 `.github/workflows/*` 或其他 CI 配置后补充
- Desktop 实际 UI 框架接入状态：`TBD`
  - 确认路径：当 `OpenVideoToolbox.Desktop` 引入 Avalonia 或其他桌面框架时更新
- 打包 / 发布流程：`TBD`
  - 确认路径：新增发布脚本、安装包流程或发行说明后更新
