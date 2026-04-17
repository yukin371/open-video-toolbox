# Open Video Toolbox

一个面向外部 AI 代理与脚本工作流的 CLI 媒体工具箱项目。

项目目标不是复刻旧式 GUI 工具，也不是把 AI 能力内嵌到软件里，而是提供一组可维护、可测试、可扩展的确定性命令，让 Claude、Codex 等外部工具能够组合这些 CLI 能力完成媒体处理和简易剪辑编排。

## 当前阶段

当前仓库完成了以下工作：

- 仓库与 Git 初始化
- 最小 .NET 解决方案骨架
- 产品需求文档
- 技术选型说明
- 开发原则与协作约束
- 面向 AI/自动化协作者的 `AGENTS.md`
- Phase 1 领域模型与序列化结构
- Phase 2 外部工具抽象、`ffprobe` 解析、`ffmpeg` 命令构建与进程执行器
- Phase 3 CLI 命令面：`presets`、`probe`、`plan`、`run`
- Wave 1 与常用增强命令已落地：`templates`、`doctor`、`init-plan`、`beat-track`、`audio-analyze`、`audio-gain`、`transcribe`、`detect-silence`、`separate-audio`、`cut`、`concat`、`extract-audio`、`subtitle`、`mix-audio`、`render` 已可运行，`edit.json schema v1` 已接入执行链

## 建议的近期目标

1. 先做 CLI-first MVP，不追求完整 GUI。
2. 先稳定媒体模型、任务模型、`edit.json` 和结构化 JSON 输出。
3. 先补可供外部 AI 编排的确定性编辑子命令，再决定是否需要 Desktop。

## 仓库结构

```text
.
|- docs/
|  |- PRD.md
|  |- architecture.md
|  |- development-principles.md
|  |- roadmap.md
|  `- tech-stack.md
|- src/
|  |- OpenVideoToolbox.Cli/
|  |- OpenVideoToolbox.Core/
|  `- OpenVideoToolbox.Desktop/
|- .editorconfig
|- .gitignore
|- AGENTS.md
|- Directory.Build.props
`- OpenVideoToolbox.sln
```

## 核心原则

- 兼容的是工作流，不是历史包袱。
- 所有编码任务都应具备可预览、可重放、可审计的命令记录。
- 软件内不内置 AI provider；AI 由外部代理通过 CLI 编排。
- 内核层不依赖 GUI。
- 复杂行为先建模型与测试，再接 UI。

## CLI

```powershell
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- presets
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- templates
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- templates --category short-form
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- templates --seed-mode beats --json-out templates.json
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- templates --artifact-kind subtitle --has-subtitles --summary
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- templates shorts-captioned
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- templates shorts-captioned --write-examples .template-guide
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- doctor --json-out doctor.json
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- init-plan <input> --template shorts-captioned --output edit.json --render-output final.mp4
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- scaffold-template <input> --template shorts-captioned --dir .workspace --validate
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- init-plan <input> --template shorts-captioned --output edit.json --render-output final.mp4 --artifacts artifacts.json
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- init-plan <input> --template shorts-captioned --output edit.json --render-output final.mp4 --template-params template-params.json
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- init-plan <input> --template shorts-basic --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- init-plan <input> --template shorts-basic --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript --transcript-segment-group-size 2
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- init-plan <input> --template shorts-basic --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript --min-transcript-segment-duration-ms 500
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- init-plan <input> --template shorts-basic --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript --transcript-segment-group-size 3 --max-transcript-gap-ms 200
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- beat-track <input> --output beats.json
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- audio-analyze <input> --output audio.json --json-out audio-analyze.json
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- audio-gain <input> --gain-db -6 --output leveled.wav
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- transcribe <input> --model ggml-base.bin --output transcript.json --json-out transcribe.json
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- detect-silence <input> --output silence.json --json-out detect-silence.json
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- separate-audio <input> --output-dir stems
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- init-plan <input> --template shorts-captioned --output edit.json --render-output final.mp4 --beats beats.json --seed-from-beats --beat-group-size 2
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- cut <input> --from 00:00:12.000 --to 00:00:27.500 --output clip-01.mp4
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- concat --input-list clips.txt --output merged.mp4
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- extract-audio <input> --track 0 --output voice.m4a
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- subtitle <input> --transcript transcript.json --format srt --output subtitles.srt --json-out subtitle.json
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- validate-plan --plan edit.json --check-files
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- mix-audio --plan edit.json --output mixed.wav --preview --json-out mix-preview.json
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- render --plan edit.json --output final.mp4 --preview --json-out render-preview.json
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- mix-audio --plan edit.json --output mixed.wav
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- render --plan edit.json --output final.mp4
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- probe <input> --ffprobe <path>
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- plan <input> --preset h264-aac-mp4
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- run <input> --preset h264-aac-mp4 --ffprobe <path> --ffmpeg <path>
```

说明：

- `presets` 列出内置预设
- `doctor` 统一检查 `ffmpeg`、`ffprobe`、`whisper-cli`、`demucs` 和 `whisper model` 的依赖状态；stdout 始终输出结构化 JSON，缺失 required 依赖时返回非零退出码，也支持 `--json-out`
- `templates` 无参时列出内置 `edit.json` 模板；可选用 `--category`、`--seed-mode`、`--output-container`、`--artifact-kind`、`--has-artifacts`、`--has-subtitles` 过滤列表，也可用 `--summary` 返回稳定的机器友好摘要，并额外暴露模板级 transcript 策略推荐与 supporting signals，便于外部 AI 在不读取完整 guide 的前提下先完成首次筛选；用 `--json-out` 可直接写出结果；传模板 id 时返回单模板指南，包含 artifact skeleton、template-params skeleton、推荐 seed 模式、supporting signal 命令、artifact preparation 命令、示例命令，以及按推荐 seed 模式生成的最小 `edit.json` 预览；其中 transcript 模式还会额外挂出 grouped / min-duration / max-gap 三类显式策略变体命令与 preview，并标出模板 owner 推荐的组合；额外传 `--write-examples <dir>` 时会把 `guide.json`、`template.json`、这些 skeleton、preview plan，以及 `commands.json` / `commands.ps1` / `commands.cmd` / `commands.sh` 直接写到目录
- 当前内置模板已覆盖 `short-form`、`commentary`、`explainer`、`montage` 四类常见套路，并补齐字幕/BGM 组合模板，便于外部 AI 先按工作流类型缩小模板集合
- `init-plan` 从模板生成可编辑的 `edit.json` 骨架，并可选复用 `ffprobe` 预填整段 clip；当传入 `--artifacts artifacts.json` 时会把模板 slot 绑定写入顶层 `artifacts`，已声明的 `subtitle` / `bgm` 仍可通过专用参数直传；当传入 `--template-params template-params.json` 时会覆盖模板默认参数；当传入 `--transcript transcript.json` 时会在计划中写入顶层 `transcript` 引用，搭配 `--seed-from-transcript` 时可按 transcript segment 直接生成初始 clips，也可通过 `--transcript-segment-group-size <n>` 把相邻 segment 按固定组数合并成确定性 seed clips，通过 `--min-transcript-segment-duration-ms <n>` 过滤过短 segment，并通过 `--max-transcript-gap-ms <n>` 在 gap 过大时强制断开 seed clip；当传入 `--beats` 时会在计划中写入顶层 `beats` 引用，搭配 `--seed-from-beats` 时可按节拍组直接生成初始 clips
- `scaffold-template` 把模板指南、skeleton 文件、preview plan、命令脚本文件和初始 `edit.json` 一次写入工作目录，适合外部 AI 直接进入目录后二次修改，而不必自行串多条 `templates` / `init-plan` 命令；传 `--validate` 时会立刻附带一份 plan 校验结果，传 `--check-files` 时会连同文件存在性一起检查
- `beat-track` 把输入媒体解成统一波形并输出 `beats.json`，供节奏参考、模板填充和 clip 种子生成使用
- `audio-analyze` 通过 `ffmpeg loudnorm` 输出 `audio.json`，提供集成响度、响度范围、真峰值和门限等基础响度分析数据，供后续音量标准化、配乐 ducking 和模板决策复用；stdout 输出稳定 envelope，传 `--json-out` 时会把同一份结果写到文件
- `audio-gain` 通过 `ffmpeg volume` 做显式分贝增益处理，先提供最简单、最可解释的音量控制原语，后续再单独扩归一化模式
- `transcribe` 通过 `ffmpeg` 预抽取统一 WAV，再调用 `whisper.cpp` 官方 `whisper-cli` 输出 JSON，并映射成仓库标准 `transcript.json`；stdout 输出稳定 envelope，传 `--json-out` 时会把同一份结果写到文件
- `detect-silence` 通过 `ffmpeg silencedetect` 输出 `silence.json`，提供模板和后续编辑辅助可复用的停顿段信号；stdout 输出稳定 envelope，传 `--json-out` 时会把同一份结果写到文件
- `separate-audio` 通过 `Demucs` 输出结构化 stem 结果，先收敛高频的人声 / 伴奏双 stem 场景
- `templates <id>` 现在会显式给出 transcript / beats / silence / stems 的 supporting signal guidance，告诉外部 AI 该先生成哪些信号、用哪条命令，以及这些信号应如何接回初始 scaffold 或人工修订流程
- 对支持字幕的模板，`templates <id>` / `commands.*` 现在还会显式给出 `transcribe -> subtitle -> init-plan/render` 的 artifact preparation 命令，减少外部 AI 自己拼字幕工作流
- `cut` 通过 `ffmpeg -map 0 -c copy` 做最小单段裁切
- `concat` 通过 `ffmpeg concat demuxer` 合并片段列表
- `extract-audio` 通过 `ffmpeg -map 0:a:<n> -vn -c copy` 提取指定音频轨
- `subtitle` 把外部 `transcript.json` 渲染为 `srt` / `ass`，供 sidecar 或后续烧录使用；stdout 输出稳定 envelope，传 `--json-out` 时会把同一份结果写到文件
- `validate-plan` 对手改或 AI 生成后的 `edit.json` 做结构化语义校验；可选 `--check-files` 检查引用文件是否存在，stdout 始终输出 JSON，失败时返回非零退出码
- `mix-audio` 消费 `edit.json`，只导出混合后的音频文件，供独立检查或后续复用；传 `--preview` 时输出由 `Core.Execution` 生成的统一 `executionPreview`，传 `--json-out` 时可把同一份 envelope 原样写到文件
- `render` 消费 `edit.json`，完成片段拼接、额外音轨混入，以及字幕烧录或外挂输出；传 `--preview` 时输出由 `Core.Execution` 生成的统一 `executionPreview`，其中包含 `CommandPlan`、`ProducedPaths` 与 side effect 预览；传 `--json-out` 时可把同一份 envelope 原样写到文件
- `probe` 执行真实 `ffprobe` 并输出规范化 JSON
- `plan` 生成任务定义和 `ffmpeg` 命令计划
- `run` 先探测再执行真实任务

当前开发机已通过真实 `ffmpeg` / `ffprobe` smoke：

- `probe`
- `render`
- `mix-audio`
- `cut`
- `concat`

可选的重依赖 smoke：

- `transcribe`
  - 需先设置 `OVT_WHISPER_MODEL_PATH=<whisper model path>`
  - 如 `whisper-cli` 不在 `PATH`，再设置 `OVT_WHISPER_CLI_PATH=<whisper-cli path>`
- `separate-audio`
  - 需保证 `demucs` 可执行
  - 如 `demucs` 不在 `PATH`，设置 `OVT_DEMUCS_PATH=<demucs path>`

说明：

- 这些 smoke 已接入 `OpenVideoToolbox.Core.Tests/RealMediaSmokeTests.cs`
- 默认环境缺少对应依赖时会自动跳过，不会让整仓测试变红
- `doctor` 会优先使用命令行参数，其次读取 `OVT_WHISPER_CLI_PATH`、`OVT_DEMUCS_PATH`、`OVT_WHISPER_MODEL_PATH`，最后回退到默认可执行名或 `unset`

## 下一步

下一阶段建议先落地可被外部 AI 代理稳定调用的 CLI 剪辑基元：

1. 继续丰富模板契约，让外部 AI 能稳定复用固定剪辑套路与 artifact slot
2. 在 `beat-track` 基础上补更多节奏辅助能力，但保持 `beats.json` 与 `edit.json` 的稳定边界
3. 优先整合现有工具与格式，不重复造轮子
4. Desktop 仅在 `edit.json` 工作流稳定后再评估

## 设计文档

- CLI MVP 与命令草案：`docs/CLI_MVP.md`
- 外部 AI 边界：`docs/decisions/ADR-0002-cli-ai-editor-positioning.md`
- 二次手动剪辑边界：`docs/decisions/ADR-0003-edit-plan-manual-pass.md`
