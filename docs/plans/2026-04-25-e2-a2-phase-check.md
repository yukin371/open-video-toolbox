# E2-A2 阶段检查：分发渠道评估与首选方向

最后更新：2026-04-25

## 目的

这份文档只回答一个问题：

> `E2-A2` 当前到底算不算已经形成阶段结论？

它不替代渠道评估正文，也不在仓库里直接代替真正的 `winget-pkgs` 提交流程。它只把当前仓库内已经落地的评估、脚本和验证结果，与 `docs/plans/2026-04-22-e2-ecosystem-sustainability-plan.md` 中定义的 `E2-A2` 目标逐条对照。

## 阶段定义回顾

`E2-A2` 的目标是：

1. 不再停留在“以后也许支持包管理器”
2. 在 `winget / NuGet global tool / Homebrew` 之间收敛首选方向
3. 如果决定先做某一条，就把仓库内 blocker 收到最小

本阶段并不要求：

- 同时推进多个分发渠道
- 立刻覆盖所有平台特性
- 在仓库里内建完整提交流程自动化到最终 PR 合并

## 当前已落地能力

### 评估结论

当前渠道评估已经明确收敛为：

- 首选：`winget portable`
- 暂缓：`NuGet global tool`
- 暂缓：`Homebrew`

这意味着 `E2-A2` 已经完成最关键的一步：

- 从开放问题变成有明确结论

### 仓库内实现与验证

当前仓库内已具备：

- `packaging/winget/Test-WinGetSubmissionReadiness.ps1`
- `packaging/winget/Export-WinGetSubmissionBundle.ps1`
- `packaging/winget/README.md`
- `release.yml` 中可直接复用的 Windows 发布资产

当前已确认的状态：

- `v0.1.0` GitHub Release 已存在
- `ovt-win-x64.exe` / `ovt-win-x64.zip` 已发布
- 仓库导出的 `v0.1.0` manifest 已在本机跑通一次 `winget validate`

## 与阶段目标对照

### 条件 1

> 至少产出一份带结论的渠道评估

当前判断：**满足**

证据：

- `docs/plans/2026-04-22-e2-a2-distribution-channel-evaluation.md` 已明确结论为 `winget portable`

### 条件 2

> 文档中明确首选渠道或明确后置理由

当前判断：**满足**

证据：

- `winget` 被明确列为首选
- `NuGet global tool` 与 `Homebrew` 的后置理由已单独写清

### 条件 3

> 若选择实施，实施范围应限定在单一渠道

当前判断：**满足**

证据：

- 当前仓库只围绕 `winget portable` 补脚本和发布资产
- 没有并行引入 `dotnet tool` 打包链或 Homebrew formula 维护链

## 当前结论

`E2-A2` 当前判断为：**已形成阶段结论，仓库内 blocker 已基本清空**

更准确地说：

- 渠道选择已经完成
- 仓库内脚本和资产已经足够支撑 maintainer 做下一步真实提交
- 当前剩余动作主要属于外部流程，而不是仓库内设计未定

## 当前不继续扩实现的原因

当前不适合继续在仓库里补更多 `E2-A2` 实现，原因很明确：

- 剩余动作主要是目标环境复核
- 视需要执行 `winget install --manifest`
- 向 `microsoft/winget-pkgs` 提交 PR

这些已经不再是仓库内部缺少 owner 或缺少脚本的问题。

## 更合理的下一步

当前更合理的动作是：

1. 把 `E2-A2` 视为已完成首轮仓库内准备
2. 如需继续推进，直接进入真实 `winget-pkgs` 提交流程
3. 仓库内不再继续横向扩更多分发渠道

## 本轮阶段检查输出

```text
阶段：E2-A2
阶段目标是否完成：已形成阶段结论
首选渠道是否已明确：是，winget portable
仓库内 blocker 是否基本清空：是
当前剩余动作是否主要在仓库外：是
如果现在停止，仓库是否仍处于一致状态：是
```
