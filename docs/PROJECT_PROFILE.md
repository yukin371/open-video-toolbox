# Project Profile

最后更新：2026-04-15

## 项目类型

- 桌面应用主仓库，当前以 `.NET` 解决方案形式组织。
- 同时包含 `Core` 类库、`Cli` 入口、`Desktop` 入口和 `Core.Tests` 测试项目。

## 技术栈

- 语言与运行时：`.NET 8`、`C#`
- 解决方案：`OpenVideoToolbox.sln`
- 测试：`xUnit`、`Microsoft.NET.Test.Sdk`、`coverlet.collector`
- 外部工具边界：`ffmpeg`、`ffprobe`
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
- `probe <input> [--ffprobe <path>]`
- `plan <input> [--preset <id>] [--output-dir <dir>] [--output-name <name>] [--ffmpeg <path>] [--overwrite]`
- `run <input> [--preset <id>] [--output-dir <dir>] [--output-name <name>] [--ffprobe <path>] [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]`

## 仓库拓扑

- `src/OpenVideoToolbox.Core`
  - 领域模型、预设模型、命令计划、进程执行、媒体探测、JSON 序列化
- `src/OpenVideoToolbox.Cli`
  - 当前最小可运行入口，直接组合 `Core` 能力完成 `presets` / `probe` / `plan` / `run`
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

## 当前已知缺口

- CI / workflow 文件：`TBD`
  - 确认路径：新增 `.github/workflows/*` 或其他 CI 配置后补充
- Desktop 实际 UI 框架接入状态：`TBD`
  - 确认路径：当 `OpenVideoToolbox.Desktop` 引入 Avalonia 或其他桌面框架时更新
- 打包 / 发布流程：`TBD`
  - 确认路径：新增发布脚本、安装包流程或发行说明后更新
