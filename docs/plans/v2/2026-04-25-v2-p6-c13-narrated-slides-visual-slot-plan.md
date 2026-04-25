# V2-P6-C13 narrated-slides visual slot 实施计划稿

最后更新：2026-04-25

## 卡片信息

```text
卡片编号：V2-P6-C13
所属阶段：V2-P6
卡片类型：计划
目标：为 narrated-slides visual slot 的最小实现定义具体改动面、测试面与文档同步范围
输入：docs/plans/v2/2026-04-25-v2-p6-c12-narrated-slides-visual-slot-spec.md、当前 NarratedSlides manifest / builder / CLI 集成测试
输出：本实施计划稿
完成标准：明确 owner、改动顺序、测试列表、非目标与回滚路径
下一张卡：待定；如按本计划推进，则进入 visual slot 执行卡
```

## 实施目标

在不扩张 narrated-slides 范围的前提下，补上 visual slot 的第一版最小实现：

- `sections[].visual.slot.required = false`
- 当 section 缺 `visual.path` 时，保留 section voice
- `Core.Editing` 生成 placeholder video clip
- `render --preview` / `render` 继续复用既有 timeline 执行链

## 改动顺序

## 1. Core.Editing model

文件：

- `src/OpenVideoToolbox.Core/Editing/NarratedSlidesManifest.cs`

改动：

- 允许 `NarratedSlidesVisualManifest.Path` 变为可空
- 在 `NarratedSlidesVisualManifest` 上新增 `Slot`
- 继续复用现有 `NarratedSlidesSlotManifest`

目标：

- 让 visual slot 的 contract 与已有 `bgm.slot` 形式保持一致

## 2. Core.Editing builder

文件：

- `src/OpenVideoToolbox.Core/Editing/NarratedSlidesPlanBuilder.cs`

改动：

- 在 section 投影阶段判断 visual 是否缺失
- 若 `visual.slot.required = false` 且无 path，则生成 placeholder main clip
- 若缺 path 且没有 optional slot，则继续抛错

第一版固定投影：

```json
{
  "placeholder": {
    "kind": "color",
    "color": "black"
  }
}
```

目标：

- visual slot 的业务决策留在 `Core.Editing`
- 不在 CLI 或 execution 复制判断

## 3. CLI / integration tests

文件：

- `src/OpenVideoToolbox.Cli.Tests/CommandArtifactsIntegrationTests.InitNarratedPlanCommands.cs`

改动：

- 增加一条 optional visual slot manifest 用例
- 断言写出的 main track clip 为 placeholder
- 断言 stats / track 数量 / 轨道 id 保持稳定

说明：

- CLI 入口本身预计无需新增参数
- 这张卡只验证现有 `init-narrated-plan` 闭环

## 4. Core unit tests

文件：

- `src/OpenVideoToolbox.Core.Tests/NarratedSlidesPlanBuilderTests.cs`

改动：

- 增加 optional visual slot -> placeholder 投影用例
- 增加缺 path 且无 optional slot -> failure 用例
- 增加真实 visual path 仍维持现状的回归

## 5. 文档

至少同步：

- `docs/roadmap.md`
- `README.md`
- `docs/PROJECT_PROFILE.md`
- `docs/COMMAND_REFERENCE.md`
- `src/OpenVideoToolbox.Core/Editing/MODULE.md`
- `src/OpenVideoToolbox.Cli/MODULE.md`

## 非目标

本计划明确不包含：

1. section 删除
2. `visual.slot.required = true`
3. 可配置 placeholder 颜色
4. 自动图片/图表/页面生成
5. batch manifest 扩张
6. `.pptx` / Markdown 后端

## 测试清单

最小测试集：

1. `NarratedSlidesPlanBuilderTests`
   - optional visual slot -> placeholder
   - missing visual without optional slot -> failure
2. `CommandArtifactsIntegrationTests.InitNarratedPlanCommands`
   - `init-narrated-plan` 写出 placeholder main clip
3. `dotnet build OpenVideoToolbox.sln`
4. `dotnet test OpenVideoToolbox.sln`

## 风险

## 1. contract 扩张风险

如果在本卡里顺手加入：

- per-section placeholder 样式
- section 删除
- 多种 visual slot fallback

就会把最小实现重新拖回未收敛状态。

## 2. owner 漂移风险

如果 CLI 开始直接判断“没有 visual 就自动补 placeholder”，后续 narrated builder 与 CLI 会形成两套规则。

## 回滚路径

若 visual slot 最终效果不符合预期，回滚范围应限制在：

1. `NarratedSlidesManifest`
2. `NarratedSlidesPlanBuilder`
3. narrated 相关单测与集成测试
4. 本计划稿和 roadmap 文案

不影响：

- `C11` placeholder execution 基线
- `${var}` foundation
- `bgm.slot` 最小实现
