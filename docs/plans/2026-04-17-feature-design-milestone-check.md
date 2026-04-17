# Feature Design And Milestone Check

日期：2026-04-17

## 目的

把当前仓库已经列出的功能清单重新映射到一组可快速迭代的里程碑，并明确每组能力的设计状态、owner、验证情况和下一轮建议，避免后续开发继续按零散命令堆功能。

## 检查口径

- 功能清单来源：`README.md`、`docs/CLI_MVP.md`、`docs/roadmap.md`
- 设计检查重点：
  - canonical owner 是否清晰
  - CLI 是否只做参数解析与结构化输出
  - `Core` 是否保持单一业务 owner
  - 是否已有稳定测试或 smoke
  - 是否适合作为下一轮迭代入口
- 状态定义：
  - `Completed`
  - `Hardening`
  - `Planned`

## 当前结论

- 当前仓库已经跨过“有没有功能”的阶段，进入“功能收敛与契约稳定化”阶段。
- `Core` / `Cli` 的边界总体健康，没有发现明显把命令拼接、外部进程调用回流到 CLI 的结构性问题。
- 第一优先级不再是继续补很多新命令，而是把已经落地的基础能力继续固化为可复用、可组合、可预测的契约。
- 下一轮迭代最适合围绕 `Hardening` 项推进，而不是直接跳到 Desktop 或插件运行时。

## Milestone Board

| Milestone | 范围 | 状态 | 设计结论 | 进入下一阶段前的门槛 |
| --- | --- | --- | --- | --- |
| M0 Foundation Baseline | `presets` / `probe` / `plan` / `run`、命令计划、进程执行、媒体探测 | Completed | 基线 owner 清晰，适合作为后续所有命令的约束样板 | 保持输出契约稳定，不在 CLI 复制 `Core.Execution` / `Core.Media` 逻辑 |
| M1 Edit Plan Loop | `templates` / `init-plan` / `scaffold-template` / `validate-plan` / `render` / `mix-audio` | Hardening | 已形成 AI 可编排的最小闭环，但模板输出、preview envelope、plan 校验仍需持续收敛 | 模板 guide / preview / commands 契约稳定；`edit.json` 修改后仍可被校验与执行 |
| M2 Audio And Speech Base | `beat-track` / `audio-analyze` / `audio-gain` / `transcribe` / `detect-silence` / `separate-audio` / `subtitle` | Hardening | 命令面已齐，但重依赖能力仍缺真实工具 smoke、安装前提和错误路径的进一步沉淀 | `ffmpeg` / `whisper.cpp` / `demucs` 依赖边界清晰，JSON 结构不再频繁变动 |
| M3 Template Platform | 模板分类、artifact slot、template params、seed 策略、preview plan、commands bundle | Hardening | 模板已不是简单样板，已经接近“场景单元”；下一步应继续把基础信号接进模板，而不是盲目扩模板数量 | transcript / beat / stems 等信号能稳定注入模板工作流，模板推荐策略可持续维护 |
| M4 Extension Surface | 模板插件、能力插件预留入口 | Planned | 方向合理，但现在还不适合启动运行时设计 | 先完成前 3 个里程碑的契约固化，再谈扩展加载机制 |
| M5 Desktop Layer | 轻量壳层、可视化辅助 | Planned | 当前没有证据表明 UI 已经成为瓶颈，继续后置是正确的 | CLI 闭环成熟，模板工作流稳定，输出契约足够固定 |

## Feature Checklist

| Feature | Canonical Owner | 状态 | 设计检查 | 验证状态 | 下一步建议 |
| --- | --- | --- | --- | --- | --- |
| `presets` | `Core.Presets` + `Cli` | Completed | 作为基线能力边界清晰 | 已有测试 | 仅在输出契约变化时再动 |
| `probe` | `Core.Media` + `Cli` | Completed | 仍是后续工作流入口 | 已有测试，真实 `ffprobe` smoke 已通过 | 保持 JSON 稳定 |
| `plan` | `Core.Execution` + `Cli` | Completed | 仍承担基线计划预览语义 | 已有测试 | 不建议扩成复杂编辑入口 |
| `run` | `Core.Execution` + `Cli` | Completed | 作为传统作业链保留合理 | 已有测试 | 仅做回归保护 |
| `templates` | `Core.Editing` + `Cli` | Hardening | 已承担模板发现与指导职责，方向正确 | 已有测试 | 继续压实 summary / guide / commands 输出 |
| `init-plan` | `Core.Editing` + `Cli` | Hardening | 已是 AI 编排主入口之一 | 已有测试 | 继续围绕 transcript / beats / artifacts 固化语义 |
| `scaffold-template` | `Cli` orchestrator + `Core.Editing` | Hardening | 适合外部 AI 快速起步，组合方式合理 | 已有测试 | 把更多基础信号收敛进脚手架而不是加新命令 |
| `validate-plan` | `Core.Editing` + `Cli` | Hardening | 是当前 `edit.json` 边界护栏 | 已有测试 | 保持 envelope 与错误路径稳定 |
| `render` | `Core.Execution` + `Cli` | Hardening | 执行 owner 清晰；preview 已统一 envelope | 已有测试，真实 smoke 已通过 | 继续补 `json-out`、真实素材 smoke 和 side effect 约束 |
| `mix-audio` | `Core.Execution` + `Cli` | Hardening | 与 `render` 复用音频图方向正确 | 已有测试，真实 smoke 已通过 | 继续和 `render` 保持同一 preview / output 语义 |
| `cut` | `Core.Execution` + `Cli` | Completed | 单一原语边界清晰 | 已有测试，真实 smoke 已通过 | 作为稳定基元保留 |
| `concat` | `Core.Execution` + `Cli` | Completed | 单一原语边界清晰 | 已有测试，真实 smoke 已通过 | 作为稳定基元保留 |
| `extract-audio` | `Core.Execution` + `Cli` | Completed | 单一原语边界清晰 | 已有测试 | 如无新需求暂不扩展 |
| `subtitle` | `Core.Subtitles` + `Cli` | Hardening | schema 与 sidecar owner 清晰 | 已有测试 | 继续收敛 subtitle -> render 双路径体验 |
| `beat-track` | `Core.Beats` + `Cli` | Hardening | 信号边界清晰，未见 owner 漂移 | 已有测试 | 继续把 `beats.json` 接入模板工作流 |
| `audio-analyze` | `Core.Audio` + `Core.Execution` + `Cli` | Hardening | 当前定位为测量原语合理 | 已有测试 | 继续稳定字段语义和真实 smoke |
| `audio-gain` | `Core.Execution` + `Cli` | Hardening | 显式 gain 原语清晰，但后续归一化入口尚未定案 | 已有测试 | 保持命令单义，若做 normalize 应优先单独入口 |
| `transcribe` | `Core.Speech` + `Core.Subtitles` + `Cli` | Hardening | 映射层与 schema owner 清晰 | 已有测试 | 重点补 `whisper.cpp` 真实机器验证与错误路径文档 |
| `detect-silence` | `Core.Audio` + `Core.Execution` + `Cli` | Hardening | 作为辅助信号而非自动剪辑，设计正确 | 已有测试 | 尽快接入模板 seed / guide 语义 |
| `separate-audio` | `Core.AudioSeparation` + `Core.Execution` + `Cli` | Hardening | 模块边界已清晰，但仍处在重依赖命令的早期阶段 | 已有测试 | 先做双 stem 高频场景验证，不急着扩 4-stem/6-stem |

## 设计检查结论

### 1. 基础层

- `Core.Media`、`Core.Execution`、`Core.Editing`、`Core.Subtitles`、`Core.Beats`、`Core.Audio`、`Core.Speech`、`Core.AudioSeparation` 的 owner 已基本清晰。
- 当前没有必要再拆新的 shared helper 层；现有 owner 足够覆盖当前功能集。
- `Cli` 仍应坚持薄入口策略，继续避免把路径规则、命令拼接和工具输出解析拉回 `Program.cs`。

### 2. 模板层

- 模板平台已经成为当前仓库最重要的复用层，不应再把模板看成静态样板。
- 下一步设计重点应放在“让基础信号进入模板工作流”：
  - transcript
  - beats
  - silence
  - stems
- 如果基础信号已经存在，但模板不能稳定消费，这一轮更应该补模板接入，而不是新增更多离散命令。

### 3. 音频 / 语音层

- `audio-analyze`、`audio-gain`、`transcribe`、`detect-silence`、`separate-audio` 已形成可用命令面。
- 当前最大设计风险不是 owner 不清，而是：
  - 重依赖工具安装前提没有完全沉淀
  - 真实机器 smoke 还不够系统
  - 模板层对这些信号的消费仍不足
- 这意味着下一阶段的重点应是“收敛和接线”，而不是“再发明更多音频命令”。

## 推荐迭代顺序

### Iteration A

- 目标：补齐真实工具 smoke 与失败路径沉淀
- 范围：
  - `audio-analyze`
  - `transcribe`
  - `detect-silence`
  - `separate-audio`
- 验收：
  - 文档写清安装前提
  - CLI 错误路径稳定
  - 至少有可重复的 smoke 记录

### Iteration B

- 目标：把基础信号更稳定地接进模板工作流
- 范围：
  - transcript -> template guide / preview / scaffold
  - silence -> template guidance
  - stems -> template artifact / audio track guidance
- 验收：
  - 外部 AI 不需要自己猜这些信号怎么注入 `edit.json`

### Iteration C

- 目标：评估下一批能力是否值得单独命令化
- 优先候选：
  - 显式 loudness normalize 能力
  - 更明确的 subtitle workflow glue
  - 模板插件入口草案
- 约束：
  - 只在 canonical owner 明确、测试策略明确时再实现

## 快速迭代规则

后续每轮开发建议都按以下顺序执行：

1. 从 `Hardening` 状态的项里选一个最小闭环。
2. 先确认 owner 和输出契约，不先加新 helper。
3. 先补或更新测试，再落实现。
4. 实现后同步 `roadmap`、`CLI_MVP`、模块 `MODULE.md` 中受影响的部分。
5. 以 `dotnet test OpenVideoToolbox.sln` 作为回归底线。

## 当前验证基线

- `dotnet test OpenVideoToolbox.sln`
  - `OpenVideoToolbox.Core.Tests`: 117/117
  - `OpenVideoToolbox.Cli.Tests`: 67/67
- 可选 smoke 入口：
  - `OVT_WHISPER_MODEL_PATH` + 可选 `OVT_WHISPER_CLI_PATH`
  - 可选 `OVT_DEMUCS_PATH`
  - 默认环境缺少这些依赖时，`RealMediaSmokeTests` 对应项自动跳过

## 本文档对应的下一轮候选任务

- 为 `whisper.cpp` / `demucs` 增加更明确的 smoke 与错误路径沉淀
- 把 `silence.json` / stems 接入模板 guide 与 scaffold
- 评估是否需要单独的 loudness normalize 命令，而不是扩写 `audio-gain`
