# WinGet Submission Checklist

最后更新：2026-04-22

本清单只服务 `E2-A2` 的 `winget portable` 路径。

## 目标

在提交 `microsoft/winget-pkgs` 之前，确保仓库侧元数据、发布资产和 manifest 草稿都处于可审计状态。

## 当前结论

- 首选渠道：`winget`
- 首选安装器类型：`portable`
- canonical 资产源：本仓库 GitHub Release

## 提交前检查

1. 已发布一个新的 tag release。
2. Windows release 资产同时包含：
   - `ovt-win-x64.zip`
   - `ovt-win-x64.exe`
3. 已确认 `ovt-win-x64.exe` 下载 URL 可公开访问。
4. 已计算 `ovt-win-x64.exe` 的 SHA256。
5. 已确认当前版本号与 release tag 一致。
6. 已确认 release notes URL 可公开访问。
7. 已确认许可证元数据有真实来源。
8. 已用 `Prepare-WinGetSubmission.ps1` 或 `Render-WinGetManifest.ps1` 生成三份 manifest。
9. 已本地执行：
   - `winget validate <manifest-folder>`
   - `winget install --manifest <manifest-folder>`
10. 已准备向 `microsoft/winget-pkgs` 提交 PR。

## 当前 blocker

### P0

- 仓库当前未发现稳定的许可证来源文件；在该问题解决前，不应提交正式 manifest。

## 交付物位置

- 模板与说明：`packaging/winget/`
- 准备与渲染脚本：`packaging/winget/Prepare-WinGetSubmission.ps1`、`packaging/winget/Render-WinGetManifest.ps1`
- 渠道决策：`docs/plans/2026-04-22-e2-a2-distribution-channel-evaluation.md`

## 备注

- 不要在 manifest 中猜许可证值。
- 不要绕过 GitHub Release 直接引用临时文件托管地址。
- 不要在仓库内复制第二套 release pipeline 来服务 winget。
