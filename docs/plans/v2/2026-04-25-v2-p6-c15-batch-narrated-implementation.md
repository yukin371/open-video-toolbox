# V2-P6-C15 数据驱动 Batch Narrated 最小实现

最后更新：2026-04-25

## 卡片信息

```text
卡片编号：V2-P6-C15
所属阶段：V2-P6
卡片类型：执行
目标：在不扩张 narrated-slides 业务语义的前提下，落地首个数据驱动 batch narrated CLI 入口
输入：docs/plans/v2/2026-04-25-v2-p6-c15-batch-narrated-spec.md、docs/plans/v2/2026-04-25-v2-p6-c15-batch-narrated-plan.md
输出：Cli batch wrapper / 测试 / 文档同步
完成标准：可从 batch manifest 批量生成 narrated `edit.json`，并稳定写出 `summary.json` 与 `results/<id>.json`
下一张卡：待定；如继续推进 narrated，应先重判是否仍留在 batch narrated 收口，避免滑向 section 删除或第二套内容平台
```

## 本轮落地

1. `Cli`
   - 新增 `init-narrated-plan-batch --manifest <batch.json>`
   - 新增 `InitNarratedPlanBatchManifest` 与 `InitNarratedPlanBatchItem`
   - batch item 当前支持：
     - `id`
     - `manifest`
     - `output`
     - `template`
     - `renderOutput`
     - `vars`
   - 默认 `output` 落到 `tasks/<id>/edit.json`
   - 根目录固定写 `summary.json`
   - 每个条目固定写 `results/<id>.json`

2. 复用规则
   - 单项 narrated build 继续复用 `NarratedSlidesPlanBuildSupport.BuildAsync`
   - 单项 success payload 继续复用 `init-narrated-plan` 的既有结构
   - `Core.Editing` 没有新增 batch 语义，也没有第二套 narrated builder

3. 路径与 option 约定
   - batch manifest 内 `manifest` / `output` / `renderOutput` / `vars` 相对路径统一按 batch manifest 所在目录解析
   - narrated manifest 自身内部素材路径仍按 narrated manifest 自身所在目录解析
   - batch 级 `--ffprobe` / `--timeout-seconds` 会透传到逐项 narrated build

4. 测试
   - `CommandArtifactsIntegrationTests.InitNarratedPlanBatchCommands`
     - 成功路径
     - 部分失败路径
     - `--json-out` 与 stdout 等价
     - batch 级 `--ffprobe` 透传
   - `ContractSnapshotTests`
     - `init-narrated-plan-batch` normalized contract snapshot

## 当前 contract

batch manifest 最小形状：

```json
{
  "schemaVersion": 1,
  "items": [
    {
      "id": "episode-01",
      "manifest": "episodes/episode-01/narrated.json",
      "output": "tasks/episode-01/edit.json",
      "template": "narrated-slides-starter",
      "renderOutput": "exports/episode-01.mp4",
      "vars": "vars/episode-01.json"
    }
  ]
}
```

退出码约定：

- `0`
  - 全部成功
- `2`
  - 部分或全部条目失败
- `1`
  - batch manifest 解析或装载失败

## 明确不纳入

1. `.pptx` / chart / Markdown page / title-card
2. section 删除 / 条件裁剪增强
3. richer placeholder 样式
4. 第二套 narrated 模板平台
5. 把 batch 语义下沉到 `Core.Editing`

## 验证

```powershell
dotnet test src/OpenVideoToolbox.Cli.Tests/OpenVideoToolbox.Cli.Tests.csproj --filter "InitNarratedPlanBatch|InitNarratedPlan_WritesPlanAndReturnsStructuredEnvelope"
dotnet test src/OpenVideoToolbox.Cli.Tests/OpenVideoToolbox.Cli.Tests.csproj --filter "ScaffoldTemplateBatch|RenderBatch|InitNarratedPlanBatch"
dotnet test src/OpenVideoToolbox.Cli.Tests/OpenVideoToolbox.Cli.Tests.csproj --filter "InitNarratedPlanBatch|ContractSnapshotTests"
dotnet build OpenVideoToolbox.sln
```
