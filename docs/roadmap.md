# Roadmap

最后更新：2026-04-23

本文件只保留当前版本目标、实施顺序与活跃工作面，不记录完整历史流水账。

## 产品定位

- 产品不是专业深度编辑器，也不是缩小版 PR / Resolve。
- 产品也不是“只能做一点事”的极窄工具；它应尽量整合个人工作者最常用、最可复用的视频生产能力。
- 当前目标是交付一个面向个人工作者的 CLI 视频生产内核：
  - 常见视频场景优先
  - 能快速生成可修改草稿
  - 能批量产出模板化视频
  - 对外结构化、可编排、可审计
  - 通过模板降低人工操作和 AI 编排难度
- 对更专业的调色、复杂时间线、深度特效和精修需求，允许自然衔接外部编辑器继续完成。
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

- 交付可供外部 AI 代理稳定调用的 CLI 媒体工具箱，包含跨平台 single-file 发布能力。
- 当前状态（2026-04-22）：H1→H2+T1→T2→P1→E1 已全部完成，CLI 契约已冻结，插件开发者体验已落地，发布流程已就绪。
- 下一步：继续推进 `E2`，优先完成社区模板 / 插件贡献路径与运行时基线收口；`D1` 最早在 `2026-05-22` 后重判。
- 长期演化路线：`docs/plans/2026-04-21-long-term-evolution-roadmap.md`

## 阶段检查（2026-04-22）

- `H1` 已完成：
  - 21 条 CLI 命令已统一到同一套 `{ command, preview, payload }` command envelope。
  - 运行时错误路径优先返回结构化 failure envelope，不再退回纯 usage 文本。
  - CLI 可维护性重构已收口，测试拆分停止线已明确。
  - CLI smoke 已扩展到高频执行链路，`doctor` 依赖预检语义已稳定。
- `H2 + T1` 已完成：
  - 契约快照测试已建立，核心命令输出已受黄金文件保护。
  - `presets` / `plan` 的 `--json-out` 路径已补齐。
  - CI 已补 ffmpeg 安装并可重复通过 ffmpeg / ffprobe 相关验证。
  - `docs/external-dependencies.md` 已落地，模板 guide / scaffold / commands bundle 已进入稳定期。
- `T2` 已完成：
  - `--plugin-dir` 显式目录发现、`template.source` 全链路审计、插件模板 schema 校验已收口。
  - `docs/plans/2026-04-19-template-plugin-entry-boundary.md` 的验收标准已满足。
- `P1` 已完成：
  - 示例插件 `examples/plugin-example/`、插件开发指南 `docs/plugin-development-guide.md` 与插件验证错误路径测试已落地。
- `E1` 核心发布链已完成：
  - CLI 程序集名与版本号已明确为 `ovt` / `0.1.0`。
  - GitHub Release workflow 已支持 tag 触发的跨平台 single-file 发布。
  - `CHANGELOG.md` 与 `docs/README.md` 已落地版本策略与受众分层。
  - 包管理器渠道未在本阶段落地，后续如仍有价值，转入 `E2` 评估。

## 当前阶段映射

- 已完成阶段：
  - `H1 Hardening 收口`
  - `H2 契约冻结与真实工具验证`
  - `T1 模板输出稳定化`
  - `T2 模板插件扩展面稳定`
  - `P1 插件开发者体验`
  - `E1 发布与分发（核心发布链）`
- 当前候选阶段：
  - `D1 Desktop MVP`
    - 前提：继续观察 `edit.json schema v1` 是否进入稳定低频变更窗口，并确认确有交互壳需求。
  - `E2 生态与可持续演进`
    - 适用场景：如果近期更高价值的是兼容性测试、安装渠道扩展、社区模板与维护自动化，而不是立即启动 Desktop。

## 当前活跃工作面（2026-04-22）

- 当前已转入 `E2` 的连续推进：
  - `E2-A1` 契约兼容性护栏已落地到 changelog / PR 模板 / snapshot README / development principles
  - `E2-A2` 已完成首轮分发渠道评估，当前首选方向已收敛到 `winget portable`
    - 仓库内 `packaging/winget/Test-WinGetSubmissionReadiness.ps1` 已落地，并已对 `v0.1.0` 实际通过
    - `v0.1.0` GitHub Release 与 `ovt-win-x64.exe` / `ovt-win-x64.zip` 资产已发布
    - 仓库内 `packaging/winget/Export-WinGetSubmissionBundle.ps1` 已落地，可直接导出 `winget-pkgs` 目录结构
    - 当前剩余工作已从“仓库 blocker”收敛到“render manifest + 目标环境复核 + 向 winget-pkgs 提交 PR”
  - `E2-A3` 已开始补社区模板 / 插件贡献路径，当前重点是把“开发指南 + 示例插件”补成可自助提交的闭环
  - `E2-A4` 已补固定脚本入口：
    - `scripts/Measure-RuntimeBaseline.ps1`
    - `scripts/Test-RuntimeBaselineThresholds.ps1`
    - `scripts/Verify-DependencyBaseline.ps1`
    - `scripts/Write-RuntimeBaselineSummary.ps1`
    - `.github/workflows/runtime-baseline.yml`
    - 外部工具安全清单也已接入 PR 模板
  - `E2-A4` 的轻量性能样本已不再只是观察值：
    - 仓库内已新增 runtime 阈值配置与显式判定脚本
    - `runtime-baseline` workflow 现会在超阈值时显式失败
- `D1` 仍不是当前活跃面：
  - `edit.json schema v1` 稳定窗口尚未满足，最早重新判断日期仍为 `2026-05-22`

## 当前决策面

### D1: Desktop MVP

- 目标仍是轻量交互壳，而不是新的业务 owner。
- 启动前仍需再次确认：
  - `edit.json schema v1` 的低频变更窗口
  - `templates -> init-plan / scaffold-template -> validate-plan -> render` 工作流的持续稳定性
  - Desktop 是否真的比继续做 CLI / 生态收敛更有产出
- 启动前检查清单：`docs/plans/2026-04-22-d1-desktop-mvp-start-checklist.md`

### E2: 生态与可持续演进

- 如果继续走 `E2`，优先级应放在：
  - CLI 契约兼容性检测自动化
  - 包管理器或其他分发渠道扩展
    - 当前已完成首轮评估，推荐先做 `winget`；`NuGet global tool` 与 `Homebrew` 暂不作为第一优先
  - 社区模板 / 插件贡献路径
    - 当前已进入 `E2-A3`，正在补最小提交物、自测命令与示例 README
  - 外部依赖兼容性、性能与安全基线
    - 当前已进入 `E2-A4`，并已补依赖验证脚本、性能观测脚本、GitHub Actions 观测 workflow 与 review 级安全检查项
- 执行草案：`docs/plans/2026-04-22-e2-ecosystem-sustainability-plan.md`

## 已验证

- `dotnet test OpenVideoToolbox.sln`
- `OpenVideoToolbox.Core.Tests`: 130/130
- `OpenVideoToolbox.Cli.Tests`: 118/118
- 仓库内已存在以下已交付物：
  - `examples/plugin-example/`
  - `docs/plugin-development-guide.md`
  - `docs/external-dependencies.md`
  - `.github/workflows/release.yml`
  - `CHANGELOG.md`

## 待继续观察

- `edit.json schema v1` 是否在后续一段时间内保持低频变更，从而真正满足 `D1` 的启动门槛。
- `whisper.cpp` / `demucs` 重依赖 real smoke 仍以本机条件满足时的可选验证为主。
- `winget portable` 是否作为 `E2-A2` 的实际落地切口继续推进，以及何时提交首个 `winget-pkgs` manifest。
- 社区模板 / 插件贡献路径是否已在不增加新产品边界的前提下达到“新贡献者可自助提交”标准。
- `E2-A4` 的首轮阈值是否需要在更多 CI 样本后继续收紧，或扩展到更多代表性命令。

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
