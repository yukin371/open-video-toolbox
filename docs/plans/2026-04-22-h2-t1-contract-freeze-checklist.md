# H2+T1: 契约冻结与模板输出稳定

最后更新：2026-04-22

## 前置条件

H1 已全部完成（13/13 验收项通过）。

## H2 差距分析

| 验收标准 | 现状 | 差距 |
|----------|------|------|
| 21 命令 `--json-out` 测试覆盖 | 13 个 smoke 覆盖执行类命令；模板/doctor/validate-plan 有非 smoke 的 envelope 测试 | `presets` / `plan` 缺 `--json-out` 测试；其余已由现有集成测试间接覆盖 |
| 契约快照测试 | **无** | 核心差距，需新建 |
| CI ffmpeg smoke | CI 只跑 build+test | 需补 CI 安装 ffmpeg + ffprobe |
| 外部依赖安装指南 | ✅ `docs/external-dependencies.md` 已完成 | 无 |

## T1 差距分析

| 验收标准 | 现状 | 差距 |
|----------|------|------|
| guide 告知 AI 如何生成信号 | ✅ supporting signals / signal commands / consumption 已有测试 | 无 |
| scaffold 产物完整 | ✅ `--write-examples` 测试覆盖 guide/template/artifacts/commands/preview | 无 |
| commands bundle 由测试锁住 | ✅ TemplateGuideExamplesCommands 覆盖 | 无 |
| 连续 2 周无 breaking change | 需要时间验证 | 只能靠时间，但有契约快照后可自动检测 |

**结论：** T1 的实质性工作已在 H1 期间同步完成。剩余的是"时间验证"，而契约快照测试能自动化检测 breaking change。因此 H2+T1 合并为一个阶段，以契约快照为核心交付物。

---

## 任务清单

### A1: 契约快照测试框架

**目标：** 建立一套机制，让 CLI 输出的 JSON 结构变化能被自动检测。

**方案：** 为关键命令建立"黄金文件"快照测试。每个命令跑一次，把 stdout JSON 写入 `.verified.json` 文件。后续运行时对比输出与快照，不匹配则测试失败。

**实施：**
1. 创建 `src/OpenVideoToolbox.Cli.Tests/ContractSnapshotTests.cs`
2. 为以下关键命令建立快照测试：
   - `presets` — 预设列表
   - `doctor` — 依赖预检（无真实依赖时的输出形状）
   - `templates` — 内置模板目录
   - `templates shorts-captioned` — 单模板 guide
   - `validate-plan` — 校验结果（合法 plan + 非法 plan）
   - `init-plan` — 初始 plan 骨架
   - `probe` / `plan` — envelope 形状（用 mock 输入）
3. 快照文件放在 `src/OpenVideoToolbox.Cli.Tests/snapshots/` 目录
4. CI 中运行快照测试，检测 breaking change

**验收：** 快照测试能检测到 payload 字段增删或类型变化。

### A2: `presets` / `plan` --json-out 测试补齐

**目标：** 补上这两个命令的 `--json-out` 路径测试。

**实施：**
1. `presets --json-out` — 验证 stdout 与文件内容一致
2. `plan <input> --json-out` — 用 mock 输入验证 stdout 与文件内容一致

**验收：** 两个命令都有 `--json-out` 测试。

### A3: CI ffmpeg smoke

**目标：** 在 GitHub Actions CI 中安装 ffmpeg/ffprobe，让 CLI smoke 在 CI 中跑过。

**实施：**
1. 修改 `.github/workflows/ci.yml`，在 ubuntu-latest 上 `apt-get install ffmpeg`
2. 跑 `dotnet test` 时会自动执行 smoke 测试
3. 重依赖 smoke（whisper/demucs）仍然自动跳过

**验收：** CI 中 CLI smoke 测试通过。

### A4: 阶段文档收口

**实施：**
1. 更新 `docs/roadmap.md` 阶段检查
2. 更新长期演化路线阶段映射

---

## 执行顺序

```text
A1 契约快照 ──┐
A2 --json-out ┼── 可并行
A3 CI smoke  ──┘
     │
     ▼
A4 文档收口
```

---

## 验收检查清单

- [x] A1: 关键命令都有契约快照测试
- [x] A2: `presets` / `plan` 有 `--json-out` 测试
- [x] A3: CI 中 ffmpeg smoke 通过
- [x] A4: roadmap 阶段检查已更新
- [x] `dotnet test OpenVideoToolbox.sln` 全绿
- [x] 快照测试能检测到 JSON 字段 breaking change
