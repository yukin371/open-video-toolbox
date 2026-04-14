# Open Video Toolbox

一个面向“小丸工具箱”使用场景的现代化开源替代项目。

项目目标不是复刻旧实现，而是沉淀一套可维护、可测试、可扩展的媒体处理工作台，优先覆盖桌面批量压制、预设管理、日志追踪、命令可视化和任务编排。

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

## 建议的近期目标

1. 先做 MVP，不追求复刻全部旧行为。
2. 先稳定任务模型、预设模型和日志模型。
3. 先把命令生成与执行层抽离，再做 UI。

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
- 内核层不依赖 GUI。
- 复杂行为先建模型与测试，再接 UI。

## CLI

```powershell
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- presets
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- probe <input> --ffprobe <path>
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- plan <input> --preset h264-aac-mp4
dotnet run --project E:\Github\open-video-toolbox\src\OpenVideoToolbox.Cli\OpenVideoToolbox.Cli.csproj -- run <input> --preset h264-aac-mp4 --ffprobe <path> --ffmpeg <path>
```

说明：

- `presets` 列出内置预设
- `probe` 执行真实 `ffprobe` 并输出规范化 JSON
- `plan` 生成任务定义和 `ffmpeg` 命令计划
- `run` 先探测再执行真实任务

## 下一步

下一阶段建议先落地桌面端 MVP：

1. 文件导入与媒体信息展示
2. 预设选择与参数编辑
3. 任务列表、进度和日志视图
4. 将 CLI 已验证的能力接入 Desktop
