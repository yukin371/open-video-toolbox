# V2-P6-C11 placeholder video 最小实现

最后更新：2026-04-25

## 卡片信息

```text
卡片编号：V2-P6-C11
所属阶段：V2-P6
卡片类型：执行
目标：为 narrated-slides 后续 visual slot 准备最小可消费的 timeline placeholder video 能力
输入：docs/plans/v2/2026-04-25-v2-p6-c10-timeline-gap-placeholder-plan.md、当前 timeline validator / render builder
输出：Core 模型、validator、execution 与测试回归
完成标准：schema v2 timeline 可显式表达 placeholder video，render 可直接消费 color placeholder，无磁盘中间产物
下一张卡：待定；应先重判 visual slot 的最小投影方案
```

## 本轮落地

1. `Core.Editing`
   - `TimelineClip` 新增显式 `placeholder`
   - 首版新增 `TimelineClipPlaceholder`
   - validator 新增 placeholder 合法组合与冲突组合校验

2. `Core.Execution`
   - `FfmpegTimelineRenderCommandBuilder` 新增 placeholder 输入建模
   - 首版只支持 `placeholder.kind = color`
   - 运行时直接映射到 `ffmpeg -f lavfi -i color=...`
   - 不写临时视频文件

3. 测试
   - 新增 placeholder round-trip / validator / render builder 回归
   - 验证 placeholder-only video 与 placeholder+audio 两条最小路径

## 当前 contract

placeholder clip 的首版约束：

- 只允许放在 `video` track
- 必须显式给 `duration`
- 不能同时给 `src`
- 不能同时给 `in/out`
- 当前只支持：

```json
{
  "placeholder": {
    "kind": "color",
    "color": "black"
  }
}
```

## 明确不纳入

- narrated visual slot 自动投影
- section 删除 / 自动补 gap 策略
- title card / text card / image placeholder
- 图表后端、`.pptx`、Markdown 页面渲染
- 任意磁盘中间产物链

## 验证

```powershell
dotnet test src/OpenVideoToolbox.Core.Tests/OpenVideoToolbox.Core.Tests.csproj --no-restore
dotnet test src/OpenVideoToolbox.Cli.Tests/OpenVideoToolbox.Cli.Tests.csproj --no-restore
```
