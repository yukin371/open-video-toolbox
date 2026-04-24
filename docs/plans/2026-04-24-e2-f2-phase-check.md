# E2-F2 阶段检查：计划内素材与配音工作流

最后更新：2026-04-24

## 目的

这份文档只回答一个问题：

> `E2-F2` 现在到底算不算完成？

它不是新的长期计划，也不新增功能范围。它只是把当前已经落地的素材 / 配音工作流能力，与 `docs/plans/2026-04-24-e2-feature-delivery-staged-plan.md` 里定义的 `E2-F2` 验收条件逐条对照，避免后续继续凭感觉推进。

## 阶段定义回顾

`E2-F2` 的目标是：

- 把“已有 `edit.json` 之后的高频素材操作”收成稳定工作流
- 让用户和外部 AI 不再依赖手改整份 JSON

本阶段覆盖的能力为：

- `inspect-plan`
- `replace-plan-material`
- `attach-plan-material`
- `bind-voice-track`
- `bind-voice-track-batch`

## 当前已落地能力

### 已实现命令

以下命令都已落地，并已有测试或契约快照保护：

- `inspect-plan`
- `replace-plan-material`
- `attach-plan-material`
- `bind-voice-track`
- `bind-voice-track-batch`

### 已经解决的问题

当前已经明确解决了以下高频问题：

1. 能看清 plan 里当前挂了哪些素材
2. 能对已有 source / audio track / artifact / transcript / beats / subtitles 做显式替换
3. 能对缺失的 transcript / beats / subtitles / audio track / artifact slot 做显式挂载
4. 能把外部配音 / TTS / voice conversion 产物按既有 `audioTracks` 语义接回
5. 能对批量配音接回返回结构化部分成功摘要，而不是只返回一坨失败文本

### 已同步的用户向文档

当前用户已经可以从这些文档看到这组能力：

- `README.md`
- `docs/COMMAND_REFERENCE.md`
- `docs/PROJECT_PROFILE.md`

本轮进一步补齐：

- `docs/FEATURES_AND_USAGE.md`

## 与阶段验收条件对照

### 条件 1

> 人和外部 AI 都能在不手改整份 JSON 的前提下完成常见素材替换与挂载

当前判断：**基本满足**

证据：

- `replace-plan-material` 已覆盖常见“已有绑定”的替换场景
- `attach-plan-material` 已覆盖常见“缺失绑定”的挂载场景
- `inspect-plan` 已提供“当前有哪些材料、哪些缺失、哪些可替换”的只读摘要

剩余缺口：

- 还没有批量 replace / batch attach 入口
- 但这不阻塞单项高频工作流闭环，因此不构成 `E2-F2` 当前的硬阻塞

### 条件 2

> 外部配音 / TTS / voice conversion 结果至少已有一条稳定接回路径

当前判断：**满足**

证据：

- `bind-voice-track` 已提供稳定单项入口
- `attach-plan-material --audio-track-id <id>` 仍可作为更底层显式入口
- 两条路径都建立在 `audioTracks` 既有 owner 语义之上，没有发明第二套模型

### 条件 3

> 单项与批量配音接回都有结构化部分成功语义

当前判断：**满足**

证据：

- 单项 `bind-voice-track` 已有结构化结果
- `bind-voice-track-batch` 已有 `results[]`、`succeededCount`、`failedCount`
- 退出码语义已经固定：
  - 全部成功 `0`
  - 条目失败 `2`
  - manifest 解析 / 装载失败 `1`

### 条件 4

> 文档能明确说明这些命令各自解决什么问题、边界在哪

当前判断：**本轮补齐后满足**

证据：

- `README.md` 已有用户向工作流说明
- `docs/COMMAND_REFERENCE.md` 已有精确签名
- `docs/FEATURES_AND_USAGE.md` 本轮补齐后会把这组命令纳入统一的“素材工作流”说明
- 模块 owner 仍在 `MODULE.md` 与 `ARCHITECTURE_GUARDRAILS.md`

## 当前结论

`E2-F2` 当前判断为：**可收口，进入完成前状态**

更准确地说：

- 代码与测试层面，`E2-F2` 的核心能力已经具备
- 当前真正剩下的是文档和阶段判断收尾，而不是新的核心命令

因此后续不建议继续把新的零散命令塞进 `E2-F2`，除非出现以下明确理由：

1. 发现现有素材工作流仍有一条高频闭环无法完成
2. 发现 batch 语义无法继续复用单项语义
3. 发现 future Desktop 需要的素材摘要数据仍不够稳定

如果没有这三类问题，`E2-F2` 不应继续膨胀。

## 对下一阶段的影响

基于当前检查，下一步更适合进入 `E2-F3`：

- 字幕链路
- transcript / subtitles / beats 的 plan 回接闭环
- supporting signal readiness / missing-signal 提示

这比继续在 `E2-F2` 里横向扩更多素材命令更合理。

## 本轮阶段检查输出

```text
阶段：E2-F2
阶段目标是否完成：基本完成，当前进入文档与判断收尾
当前 owner 是否保持单一：是
是否出现第二套模型或第二套语义：否
当前 CLI 是否已能形成稳定闭环：是，覆盖 inspect / replace / attach / voice bind / voice bind batch
下一阶段是否已满足进入条件：是，适合进入 E2-F3 设计
如果现在停止，仓库是否仍处于一致状态：是
```
