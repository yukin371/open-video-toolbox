# E2-A2 分发渠道评估与首选方向

最后更新：2026-04-22

## 背景

- 当前仓库已完成 `E1` 的核心发布链：
  - `.github/workflows/release.yml` 会在 tag 推送时生成 `win-x64` / `linux-x64` / `osx-x64` 三个平台的 self-contained single-file 发布物
  - 当前公开发布资产已调整为 Windows `zip`，Linux / macOS 维持 `tar.gz`
- `E2-A2` 的目标不是同时铺开所有安装渠道，而是先把“首个值得做的渠道”收敛出来，避免长期停留在开放问题。

## 当前发布基线

### 已有条件

- CLI 程序集名已固定为 `ovt`
- GitHub Release 已是现成的 canonical 下载源
- Windows 发布物本质上已经是独立可运行的单文件可执行资产
- 仓库当前主要面向外部 AI / 脚本工作流，不应把“先安装 .NET SDK”变成默认前提

### 当前缺口

- 还没有任何包管理器所需的独立 manifest / formula / package metadata
- 发布元数据仍未完全收口到可直接提交状态，例如许可证元数据尚未在仓库内形成稳定来源
- macOS 当前仅发布 `osx-x64`，尚未覆盖 Apple Silicon
- `OpenVideoToolbox.Cli.csproj` 还没有 `PackAsTool` / `ToolCommandName` 等 `dotnet tool` 打包配置

## 评估标准

1. 是否尽量复用现有 GitHub Release 产物，而不是重建一套平行发布链
2. 是否尽量避免要求最终用户预装 `.NET SDK`
3. 是否与当前实际支持的平台矩阵一致
4. 是否能把维护成本控制在单一渠道、单一 owner 范围内
5. 是否不迫使仓库在 `E2-A2` 就扩到新的产品边界

## 渠道评估

### 1. `winget`

**适配度：高**

原因：

- Windows Package Manager 官方文档当前仍支持 `EXE`、`ZIP`、`PORTABLE` 等安装器类型，和当前 Windows self-contained CLI 的形态接近。
- `winget` 面向的就是“从公开下载地址安装应用”，与当前 GitHub Release 作为 canonical 资产源的模式一致。
- 仓库当前已存在 PowerShell / Windows 工作流语境，首个渠道先落在 Windows，新增发布复杂度最低。

**需要补的工作：**

1. 为 `winget-pkgs` 准备 manifest 与升级元数据
2. 固化 `portable` 安装策略
3. 继续暴露单独 `ovt-win-x64.exe` 作为 `InstallerUrl` 对应资产

**结论：**

- 推荐作为 `E2-A2` 的首个实施渠道。

### 2. `NuGet global tool`

**适配度：中**

原因：

- `.NET` 官方文档明确支持 global tool 与 RID-specific/self-contained tool packaging。
- 但这条路要求仓库额外维护 `nupkg` 打包形态，而当前发布链是 GitHub Release 的跨平台二进制资产，不是 `dotnet tool` 包。
- 当前项目目标用户并不限定为 `.NET` 开发者；若把 `dotnet tool install` 设为首选入口，会把 `.NET tool` 生态与其安装前提引入默认使用路径。

**需要补的工作：**

1. 在 `OpenVideoToolbox.Cli.csproj` 中补 `PackAsTool`、`ToolCommandName` 等 tool packaging 配置
2. 增加 `dotnet pack` / NuGet publish 流程
3. 评估 RID-specific tool 包、版本同步和安装说明

**结论：**

- 不作为第一优先渠道。
- 只有当仓库明确想覆盖 `.NET` 开发者工作流，或后续出现大量 `tool manifest` 场景需求时，再进入下一轮评估。

### 3. `Homebrew`

**适配度：中低**

原因：

- Homebrew 官方文档显示 formula / bottle 维护本身就是一套独立分发规则；即使复用上游 tarball，也仍需维护 formula 元数据、校验和与平台适配。
- 当前仓库缺少 `osx-arm64` 资产，这会直接削弱 Homebrew 在 macOS 上的实际覆盖价值。
- 如果在没有 Apple Silicon 发布物之前就优先做 Homebrew，会把大量维护成本提前压到一个尚未形成完整平台矩阵的渠道上。

**需要补的工作：**

1. 先补 macOS `arm64` 发布资产，至少让 macOS 分发面不是半成品
2. 再决定是维护 tap 还是进一步评估进入 `homebrew/core`
3. 为公式补 license、homepage、校验和与测试命令

**结论：**

- 当前后置，不作为首个包管理器渠道。

## 决策

`E2-A2` 的首选方向定为：**先做 `winget portable`，暂缓 `NuGet global tool` 与 `Homebrew`。**

### 选择 `winget` 的核心原因

- 与当前 Windows self-contained 发布物最接近
- 不要求用户先理解 `.NET tool` 生态
- 不依赖补齐 Apple Silicon 资产后才能成立
- 可以继续把 GitHub Release 保持为 canonical 资产源
- `portable` 与单文件 CLI 的语义更一致，避免把 `ovt` 伪装成传统安装器

### 明确不做

- 本轮不并行推进多个渠道
- 本轮不把 `NuGet global tool` 升级成默认安装方式
- 本轮不在未补齐 `osx-arm64` 的情况下优先推进 Homebrew

## 建议实施切片

如果正式启动 `E2-A2`，建议只做以下最小切片：

1. 用仓库内 `packaging/winget/` 草稿与 `Prepare-WinGetSubmission.ps1` 更新当前版本号、URL 与哈希
2. 提交 `winget-pkgs` manifest
3. 等 manifest 可安装后，再更新 `README.md` 的安装说明

说明：

- 第 0 步“补 Windows `zip` 资产”已在 release workflow 中落地。
- 第 0.5 步“同时暴露 raw `ovt-win-x64.exe` 资产以支撑 `portable` manifest”也已在当前仓库落地。
- 第 0.75 步“把 manifest 渲染收敛成仓库内确定性脚本”也已落地，但许可证元数据仍是提交前 blocker。

## 重新评估条件

出现以下任一条件时，再重新打开 `NuGet global tool` 或 `Homebrew`：

1. 发布链补齐 `osx-arm64`，且 macOS 安装诉求明显上升
2. 出现明确的 `.NET` 开发者分发需求，要求 `dotnet tool install`
3. GitHub Release 二进制资产不再适合作为唯一 canonical 下载源

## 参考资料

以下结论基于官方文档与当前仓库现状综合得出：

- Windows Package Manager supported installer formats:
  - https://learn.microsoft.com/en-us/windows/package-manager/winget/
- Windows Package Manager package submission:
  - https://learn.microsoft.com/en-us/windows/package-manager/package/
- .NET global tools overview:
  - https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools
- RID-specific / self-contained .NET tools:
  - https://learn.microsoft.com/en-us/dotnet/core/tools/rid-specific-tools
- Homebrew Formula Cookbook:
  - https://docs.brew.sh/Formula-Cookbook
- Homebrew Bottles:
  - https://docs.brew.sh/Bottles
