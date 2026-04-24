# 导出与互操作设计（Export L1）

> 状态：已实施，待阶段验收
> 阶段：`V2-P5` 子项 `export L1`
> 前置文档：
> - [2026-04-24-v1-v2-boundary-and-phased-delivery-plan.md](2026-04-24-v1-v2-boundary-and-phased-delivery-plan.md)
> - [docs/ARCHITECTURE_GUARDRAILS.md](../../ARCHITECTURE_GUARDRAILS.md)

## 1. 本轮设计结论

`export L1` 本轮只做一个目标：

> 让 `v1` 与 `v2` 的 `edit.json` 都能通过同一条 `Core.Execution` 导出路径，稳定导出为一个可被外部 NLE 手动导入和复核的粗粒度 `EDL` cut list。

对应收口结论：

- 只实现 `edl`
- 不在本轮同时做 `premiere-xml` / `fcpxml`
- `Core.Execution` 持有导出语义、v1 包装和文本生成
- `Cli` 只新增 `export` 命令入口与 envelope 输出
- `v1` 与 `v2` 必须先归一到同一份导出投影，再进入 `EDL` writer
- 本轮 fidelity 固定为 `L1`
  - 只保证 cut list / 时间点 / 主视频轨
  - 不承诺效果、转场、文字、动画、多轨音频保真

这不是“最终导出体系”，而是第一条真正可验证、可测试、可追踪的互操作路径。

## 2. 为什么先做 EDL

先做 `EDL`，不先做 `Premiere XML` / `FCPXML`，理由固定如下：

- `EDL` 的文本结构最小，golden file 最容易锁定
- 它足够验证最关键的问题：
  - v1 / v2 是否能被统一归一
  - clip in/out 与 timeline start/duration 是否稳定落盘
  - CLI / Core 的 owner 是否清晰
- 它天然逼着我们先把“导出中间语义”收口，而不是直接陷入某个 NLE 私有 XML 细节
- 现阶段 `timeline/effects` 还在持续演化，先做复杂 XML 只会把映射债务前置

因此本轮明确不做“大而全导出矩阵”，只做一条最小正式能力。

## 3. Canonical Owner 与边界

### 3.1 `OpenVideoToolbox.Core.Execution`

本轮新增 owner：

- `edit.json -> 导出投影` 的归一化
- `v1` plan 的包装策略
- `v2` timeline 的主轨选择策略
- `EDL` 文本渲染
- 导出 warning / failure 结果模型

本轮仍不得拥有：

- CLI 参数解析
- CLI 帮助文本
- NLE 专用 UI 或交互逻辑

### 3.2 `OpenVideoToolbox.Cli`

本轮新增 owner：

- `export` 命令分发
- `--plan` / `--format` / `--output` / `--json-out` / `--overwrite` / `--frame-rate` 参数解析
- 统一 command envelope 与退出码映射

本轮仍不得拥有：

- v1 / v2 导出骨架
- `EDL` 行格式拼接
- effect / transition 映射规则

## 4. L1 范围

### 4.1 保证支持

`L1` 只保证这些内容：

- 输入 plan 可为 `schemaVersion = 1` 或 `schemaVersion = 2`
- 导出一个 `EDL` 文件
- 保留主视频 cut list：
  - source in
  - source out
  - record in
  - record out
- 保留 clip 顺序
- 保留 gap 布局
- 保留素材路径的可追踪注释
- 返回结构化 warning / summary

### 4.2 明确不保证

本轮不导出：

- clip / track effects
- transitions
- text overlay / subtitles
- 多视频轨混合
- 多音频轨
- 音频 gain / volume
- drop-frame timecode
- `premiere-xml`
- `fcpxml`
- 插件式 exporter 注册

## 5. 统一导出投影

本轮先在 `Core.Execution` 内引入一层最小导出投影，再由 `EDL` writer 消费。

建议形状：

```csharp
public enum ProjectExportFormat
{
    Edl = 0
}

public sealed record ProjectExportRequest
{
    public required EditPlan Plan { get; init; }
    public required ProjectExportFormat Format { get; init; }
    public required string OutputPath { get; init; }
    public int? FrameRate { get; init; }
    public string? Title { get; init; }
    public bool Overwrite { get; init; }
}

public sealed record ProjectExportResult
{
    public required string Format { get; init; }
    public required string FidelityLevel { get; init; }
    public required string OutputPath { get; init; }
    public required int FrameRate { get; init; }
    public required int EventCount { get; init; }
    public required IReadOnlyList<ProjectExportWarning> Warnings { get; init; }
}

public sealed record ProjectExportWarning
{
    public required string Code { get; init; }
    public string? Target { get; init; }
    public required string Message { get; init; }
}
```

内部归一化投影不需要现在就做成公共插件扩展面；只要能让 `v1` 和 `v2` 走同一 writer 前置路径即可。

## 6. v1 / v2 兼容策略

### 6.1 v1 plan

`v1` 没有结构化 timeline，因此本轮按单主轨包装：

- 使用 `plan.Source.InputPath` 作为唯一素材源
- `plan.Clips` 依次包装为一条主视频轨事件序列
- `record in/out` 由 clip 顺序累加得到
- `AudioTracks` 不导出
- `Subtitles` / `Artifacts` / `Transcript` / `Beats` 不导出

同时返回 warning：

- `export.plan.v1Wrapped`
- `export.timeline.audioIgnored`

### 6.2 v2 plan

`v2` 本轮只导出一条主视频轨：

- 若存在 `id = "main"` 且 `kind = video` 的轨，优先导出它
- 否则导出第一条 `kind = video` 的轨
- 其它 video track 忽略，并返回 warning
- 全部 audio track 忽略，并返回 warning
- clip effects / transitions 忽略，并返回 warning

如果不存在可导出的 video track，则导出失败。

## 7. 帧率策略

`EDL` 是帧级文本格式，本轮只支持正整数帧率。

帧率来源优先级：

1. CLI `--frame-rate`
2. `plan.Timeline.FrameRate`
3. 默认 `30`

规则：

- 如果最终帧率不是大于 `0` 的整数，则失败
- 如果走默认 `30`，返回 warning：`export.frameRate.defaulted`
- 本轮不支持 `29.97` / drop-frame

这条规则必须明确写死，避免 v1 / v2 导出时各自猜时间基准。

## 8. CLI 命令面

本轮命令固定为：

```powershell
ovt export --plan <edit.json> --format edl --output <path> [--frame-rate <fps>] [--title <name>] [--json-out <path>] [--overwrite]
```

约束：

- `--plan` 必填
- `--format` 本轮只接受 `edl`
- `--output` 必填
- `--frame-rate` 可选，但只接受正整数
- `--title` 可选；缺省时用输出文件名去扩展名
- `--overwrite` 语义与现有产物型命令一致
- `--json-out` 继续复用统一 envelope helper

CLI 成功输出继续遵守：

```json
{
  "command": "export",
  "preview": false,
  "payload": {
    "format": "edl",
    "fidelityLevel": "L1",
    "outputPath": "...",
    "frameRate": 30,
    "eventCount": 4,
    "warnings": []
  }
}
```

## 9. Warning / Failure 契约

### 9.1 Warning

本轮 warning 只做稳定分类，不做花哨诊断。

首批固定 code：

- `export.plan.v1Wrapped`
- `export.frameRate.defaulted`
- `export.timeline.audioIgnored`
- `export.timeline.extraVideoTracksIgnored`
- `export.timeline.effectsIgnored`
- `export.timeline.transitionsIgnored`

要求：

- warning 由 `Core.Execution` 产出
- CLI 只透传，不再二次改写
- warning 数量应按类别聚合，避免每个 clip 爆一条导致输出失控

### 9.2 Failure

首批固定 failure 场景：

- `format` 不是 `edl`
- 输出文件已存在且未显式 `--overwrite`
- 最终找不到可导出的主视频轨
- `--frame-rate` 非法
- 写盘失败

其中：

- CLI 参数缺失仍按现有 usage/fail 语义处理
- 请求已经建立后的导出失败，应返回结构化 failure envelope，而不是只吐 usage 文本

## 10. 手动验收包

本轮阶段完成后，至少要提供这三组手动验证命令：

### 10.1 v1 plan 导出

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  export `
  --plan .\samples\v1-edit.json `
  --format edl `
  --output .\out\v1.edl
```

通过标准：

- 成功写出 `.edl`
- JSON payload 中 `fidelityLevel = "L1"`
- warnings 包含 `export.plan.v1Wrapped`

### 10.2 v2 主轨导出

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  export `
  --plan .\samples\v2-edit.json `
  --format edl `
  --output .\out\v2.edl
```

通过标准：

- 成功写出 `.edl`
- 事件数与主视频轨 clip 数一致
- 多轨 / effect / transition 若存在，会进入 warnings，而不是静默丢失

### 10.3 `--json-out` 与 overwrite

```powershell
dotnet run --project ./src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- `
  export `
  --plan .\samples\v2-edit.json `
  --format edl `
  --output .\out\v2.edl `
  --json-out .\out\export-result.json `
  --overwrite
```

通过标准：

- `.edl` 写出成功
- `export-result.json` 与 stdout envelope 结构一致
- 不显式 `--overwrite` 时，重复执行应失败

## 11. 测试策略

本轮测试必须至少覆盖：

- `Core.Execution`
  - v1 包装为主视频 cut list
  - v2 选择 `main` 或首条 video track
  - gap / in/out / duration 到 EDL 时间码的映射
  - warning 聚合
  - golden file 对比
- `Cli`
  - `export` 基本成功路径
  - `--json-out`
  - overwrite 失败路径
  - 非法 `--format`
  - 非法 `--frame-rate`

阶段完成标准仍是：

- 定向测试通过
- `dotnet test OpenVideoToolbox.sln` 通过
- 提供真实可跑的手动验收命令

## 12. 本轮明确延后项

以下内容不允许在 `export L1` 中顺手混入：

- `premiere-xml`
- `fcpxml`
- effect 参数映射
- transition 映射
- 音频轨导出
- 插件式 exporter registry
- Desktop 导出 UI

如果后续要扩第二种格式，再评估是否正式引入通用 `IProjectExporter` / registry；在此之前，不为了“将来可能会有多格式”先做一整套空架子。
