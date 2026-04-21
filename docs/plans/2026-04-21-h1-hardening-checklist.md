# H1: Hardening 收口 — 任务清单

最后更新：2026-04-21

## 当前状态

| 维度 | 现状 | 差距 |
|------|------|------|
| CLI 命令 | 21 条 | 4 条旧命令仍用裸 JSON / fail() |
| smoke 测试 | Core 7 个 + CLI 2 个 | CLI 层只覆盖 transcribe / separate-audio |
| failure envelope | 已迁移 17 条命令 | probe / plan / run / doctor 异常路径仍退回 fail() |
| `--json-out` | 17 条命令已支持 | probe / plan / run / presets 不支持 |
| 测试拆分 | 55 个 partial，~94 测试 | 已接近单命令+单关注点，待评估停止线 |
| CI | restore + build + test | 无真实工具 smoke（预期行为，CI 不装重依赖） |

---

## W1: 旧命令 envelope 收口

**目标：** 把 `probe` / `plan` / `run` / `doctor` 的成功/失败输出统一收进 command envelope

### W1.1: `probe` envelope 收口

- **现状：** 成功时输出裸 `MediaProbeResult` JSON，失败时 `fail(ex.Message)` 退回 usage 文本；无 `--json-out`
- **改动：**
  1. 成功路径改为 `WriteCommandEnvelope("probe", ...)`
  2. 失败路径改为 `WriteCommandEnvelope("probe", ...)` 带 error payload
  3. 参数解析失败也走结构化 error envelope
  4. 补 `--json-out` 支持
- **测试：**
  - 成功 envelope 形状测试
  - 输入文件不存在时的 failure envelope 测试
  - `--json-out` 一致性测试
  - 参数缺失时的结构化错误测试
- **Owner：** `FoundationCommandHandlers.cs`
- **不变：** `Core` 侧不改动，`MediaProbeResult` schema 不变

### W1.2: `plan` envelope 收口

- **现状：** 成功时输出裸 `{ job, commandPlan }` JSON，失败时 `fail(ex.Message)`；无 `--json-out`
- **改动：**
  1. 成功路径改为 `WriteCommandEnvelope("plan", ...)`
  2. 失败路径改为结构化 error envelope
  3. 补 `--json-out` 支持
- **测试：**
  - 成功 envelope 形状测试
  - 参数缺失时的结构化错误测试
  - `--json-out` 一致性测试
- **Owner：** `FoundationCommandHandlers.cs`
- **不变：** `Core` 侧不改动

### W1.3: `run` envelope 收口

- **现状：** 成功时输出裸 `{ job, execution }` JSON，失败时 `fail(ex.Message)`；无 `--json-out`
- **改动：**
  1. 成功路径改为 `WriteCommandEnvelope("run", ...)`
  2. 失败路径改为结构化 error envelope（包含可用的 `execution` 信息）
  3. 补 `--json-out` 支持
- **测试：**
  - 成功 envelope 形状测试
  - probe 失败 / execute 失败的 failure envelope 测试
  - `--json-out` 一致性测试
- **Owner：** `FoundationCommandHandlers.cs`
- **不变：** `Core` 侧不改动

### W1.4: `doctor` 异常路径 envelope 收口

- **现状：** 成功路径已用 envelope，但 `catch (Exception ex)` 仍退回 `fail(ex.Message)`
- **改动：**
  1. catch 路径改为结构化 failure envelope，保留异常消息
- **测试：**
  - Inspector 抛异常时的 failure envelope 测试
- **Owner：** `FoundationCommandHandlers.cs`
- **不变：** doctor 成功路径和依赖解析逻辑不变

### W1.5: `presets` envelope 收口

- **现状：** 只输出裸 `WriteJson(BuiltInPresetCatalog.GetAll())`，无 envelope 无 `--json-out`
- **改动：**
  1. 包进 `WriteCommandEnvelope("presets", ...)` 
  2. 可选补 `--json-out`
- **测试：**
  - envelope 形状测试
  - `--json-out` 一致性测试（如补）
- **Owner：** `Program.cs`
- **判断：** 预设列表是纯只读枚举，可能不需要 envelope。**先评估再决定是否做。**

---

## W2: CLI 真实工具 smoke 扩展

**目标：** 把 CLI 层 smoke 从 2 个扩展到覆盖所有使用 ffmpeg 的命令

### 现有 smoke

| 文件 | 已有 smoke |
|------|-----------|
| `RealMediaSmokeTests.cs` (Core) | probe, render, mix-audio, cut, concat, whisper, demucs |
| `CliRealMediaSmokeTests.cs` (CLI) | transcribe, separate-audio |

### W2.1: CLI 层 ffmpeg smoke

为以下命令补 CLI 入口层真实 smoke（每个 1 个测试，验证 stdout JSON + `--json-out` + 退出码）：

| 命令 | 前置条件 | 验证点 |
|------|---------|--------|
| `probe` | ffmpeg + ffprobe 在 PATH | stdout 是结构化 envelope，包含 format/streams |
| `render` | ffmpeg 在 PATH | stdout 是结构化 envelope，输出文件存在 |
| `mix-audio` | ffmpeg 在 PATH | stdout 是结构化 envelope，混合文件存在 |
| `cut` | ffmpeg 在 PATH | stdout 是结构化 envelope，裁切文件存在 |
| `concat` | ffmpeg 在 PATH | stdout 是结构化 envelope，合并文件存在 |
| `extract-audio` | ffmpeg 在 PATH | stdout 是结构化 envelope，音频文件存在 |
| `beat-track` | ffmpeg 在 PATH | stdout 是结构化 envelope，beats.json 存在 |
| `audio-analyze` | ffmpeg 在 PATH | stdout 是结构化 envelope，audio.json 存在 |
| `detect-silence` | ffmpeg 在 PATH | stdout 是结构化 envelope，silence.json 存在 |
| `audio-gain` | ffmpeg 在 PATH | stdout 是结构化 envelope，输出文件存在 |
| `subtitle` | ffmpeg 在 PATH | stdout 是结构化 envelope，srt 文件存在 |

**实施约束：**
- 所有 smoke 都在 `CliRealMediaSmokeTests.cs` 中追加
- ffmpeg 不可用时自动 return（不 fail），保持 CI 绿
- 每个 smoke 只验证"成功路径 + stdout 结构化 + `--json-out` 一致"
- 重依赖 smoke（whisper / demucs）已存在，不重复

### W2.2: 重依赖 smoke 路径收敛

- **现状：** whisper / demucs 的 Core 和 CLI smoke 已存在
- **差距：** 无
- **动作：** 确认现有 smoke 在本机可通过即可，不做额外工作

---

## W3: 测试拆分停止线评估

**目标：** 给出明确结论——继续拆还是停止

### 当前数据

- 55 个 CLI 测试 partial 文件，94 个测试
- 大部分 partial 只有 1~3 个 `[Fact]`
- 最大 partial 约 6 个 `[Fact]`
- 按"单命令 + 单结果路径"拆分基本完成

### 评估标准

| 条件 | 结论 |
|------|------|
| 最大 partial ≤ 6 个 Fact 且主题单一 | ✅ 已达到 |
| 新增命令时能直接定位归属文件 | ✅ 已达到 |
| 继续拆的 diff 成本 > 维护收益 | 需评估 |

### 产出

- 在 `docs/roadmap.md` 的阶段检查中写出明确结论
- 如果结论是"停止"，关闭 CLI 可维护性重构的 active track
- 如果结论是"继续"，列出剩余目标文件和预期收益

---

## W4: 文档与依赖说明收口

**目标：** 把外部依赖的安装前提和使用约束沉淀到稳定文档

### W4.1: 外部依赖安装指南

- **产出：** `docs/external-dependencies.md` 或在 `README.md` 中补章节
- **内容：**
  - ffmpeg / ffprobe（required）：安装方式（Windows / macOS / Linux）、PATH 约定
  - whisper-cli（optional）：whisper.cpp 编译或下载、`OVT_WHISPER_CLI_PATH` 环境变量
  - whisper model（optional）：下载地址、`OVT_WHISPER_MODEL_PATH` 环境变量
  - demucs（optional）：pip 安装、`OVT_DEMUCS_PATH` 环境变量
- **约束：** `doctor` 输出的字段名与文档一致

### W4.2: roadmap 阶段检查更新

- 写出 H1 验收标准达成情况
- 写出测试拆分停止线结论
- 映射当前状态到长期演化路线的阶段编号

---

## 任务依赖与执行顺序

```text
W1.1 probe envelope ──┐
W1.2 plan envelope  ──┤
W1.3 run envelope   ──┼── 可并行，互不依赖
W1.4 doctor catch   ──┤
W1.5 presets eval   ──┘
         │
         ▼
W2.1 CLI ffmpeg smoke ──── 依赖 W1（否则 smoke 验证的 envelope 格式还没定）
         │
         ▼
W3 测试拆分评估 ──────── 可与 W2 并行
W4 文档收口 ──────────── 可与 W2/W3 并行
```

**建议执行顺序：**

1. **第一批（并行）：** W1.1 + W1.2 + W1.3 + W1.4 + W1.5 评估
2. **第二批（并行）：** W2.1 + W3 + W4

---

## W1 envelope 格式约定

为了统一，以下是旧命令收口后的 envelope 格式：

### 成功

```json
{
  "command": "probe",
  "preview": false,
  "payload": {
    // 现有输出不变，直接作为 payload
  }
}
```

### 失败

```json
{
  "command": "probe",
  "preview": false,
  "payload": {
    "error": {
      "message": "具体错误消息"
    }
  }
}
```

### `--json-out`

stdout 和 `--json-out` 文件内容必须完全一致（`JsonNode.DeepEquals`）。

---

## 验收检查清单

完成以下所有项即可认定 H1 收口：

- [x] W1.1: `probe` 成功/失败都输出结构化 JSON，不退回 usage
- [x] W1.2: `plan` 成功/失败都输出结构化 JSON，不退回 usage
- [x] W1.3: `run` 成功/失败都输出结构化 JSON，不退回 usage
- [x] W1.4: `doctor` 异常路径输出结构化 JSON，不退回 usage
- [x] W1.5: `presets` 明确是否需要 envelope，结论写入文档
- [ ] W2.1: 所有 ffmpeg 命令都有 CLI 层真实 smoke
- [ ] W3: 测试拆分停止线有明确结论并写入 roadmap
- [ ] W4.1: 外部依赖安装指南已补齐
- [ ] W4.2: roadmap 阶段检查已更新
- [x] `dotnet test OpenVideoToolbox.sln` 全绿
- [x] 所有 21 条命令的成功路径都使用统一 command envelope
- [x] 所有 21 条命令的失败路径都返回结构化 JSON，不退回纯 usage
- [x] `dotnet test` 测试数量与改动前一致或更多
