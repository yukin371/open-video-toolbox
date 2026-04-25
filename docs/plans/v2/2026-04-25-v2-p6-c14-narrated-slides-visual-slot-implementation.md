# V2-P6-C14 narrated-slides visual slot 最小实现

最后更新：2026-04-25

## 卡片信息

```text
卡片编号：V2-P6-C14
所属阶段：V2-P6
卡片类型：执行
目标：在不扩张 narrated-slides 范围的前提下，落地 visual slot 的首个最小实现
输入：docs/plans/v2/2026-04-25-v2-p6-c12-narrated-slides-visual-slot-spec.md、docs/plans/v2/2026-04-25-v2-p6-c13-narrated-slides-visual-slot-plan.md
输出：Core.Editing / Cli glue / 测试 / 文档同步
完成标准：`sections[].visual.slot.required = false` 未绑定时，保留 voice 并生成 black color placeholder main clip
下一张卡：待定；如继续推进，应先判断是否还值得做 section 删除或可配置 placeholder，而不是直接扩到 batch / 图表后端
```

后续补记：

- 其后第三类路线 `数据驱动 batch narrated` 已按新明确卡片方式单独重开并落到 `V2-P6-C15`
- 这不改变本卡原始判断：继续 narrated 时必须以新的明确卡片进入，不能把多个方向打包追加

## 本轮落地

1. `Core.Editing`
   - `NarratedSlidesVisualManifest.Path` 改为可空
   - `NarratedSlidesVisualManifest` 新增 `Slot`
   - `NarratedSlidesPlanBuilder` 现支持在 optional visual 缺失时投影 placeholder clip
   - `visual.slot.required = true` 仍显式拒绝

2. `Cli`
   - narrated build support 现允许 optional visual 未绑定时跳过文件解析与媒体探测
   - 入口层不自行决定 placeholder 语义，只把缺失 visual 交回 `Core.Editing`

3. 测试
   - `NarratedSlidesPlanBuilderTests` 新增 optional visual slot 成功与失败回归
   - `InitNarratedPlanCommands` 新增 integration 闭环，确认写出的 main clip 为 black color placeholder

## 当前 contract

第一版只支持：

```json
{
  "visual": {
    "kind": "image",
    "slot": {
      "name": "cover-visual",
      "required": false
    }
  }
}
```

当 `visual.path` 缺失时：

- 保留 section voice
- `main` 轨生成：

```json
{
  "duration": "same as voice",
  "placeholder": {
    "kind": "color",
    "color": "black"
  }
}
```

## 明确不纳入

1. `visual.slot.required = true`
2. section 删除
3. 可配置 placeholder 样式
4. title-card / quote-card / chart card
5. batch / resolve-assets
6. `.pptx` / Markdown 页面生成

## 验证

```powershell
dotnet test src/OpenVideoToolbox.Core.Tests/OpenVideoToolbox.Core.Tests.csproj --no-restore --filter NarratedSlidesPlanBuilderTests
dotnet test src/OpenVideoToolbox.Cli.Tests/OpenVideoToolbox.Cli.Tests.csproj --no-restore --filter InitNarratedPlan
dotnet build OpenVideoToolbox.sln
dotnet test OpenVideoToolbox.sln
```
