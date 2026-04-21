# Open Video Toolbox 长期演化路线

最后更新：2026-04-21

## 当前状态快照

| 维度 | 数值 |
|------|------|
| Core 源文件 | 82 个 .cs，约 12,680 行 |
| Cli 源文件 | 16 个 .cs，约 7,142 行 |
| Desktop 源文件 | 1 个 .cs（空骨架） |
| Core.Tests | 52 个 .cs，约 10,390 行，130 测试 |
| Cli.Tests | 55 个 .cs，约 10,624 行，94 测试 |
| CLI 命令 | 21 条 |
| CI | GitHub Actions: restore + build + test |
| 外部依赖 | ffmpeg / ffprobe（required），whisper-cli / demucs（optional） |

## 演化原则（所有阶段通用）

1. **可维护性优先于功能数量** — 不为短期堆功能牺牲 owner 清晰度
2. **确定性优先于智能化** — AI 在外部编排，软件内部不做黑盒
3. **契约先行** — 先稳定 JSON schema，再围绕它建 UI / 插件 / 社区
4. **阶段门槛不妥协** — 未完成当前阶段验收标准时，不启动下一阶段
5. **文档即护栏** — roadmap / guardrails / plans 保持事件触发更新
6. **Core 单一 owner** — 命令构建、媒体探测、模板模型、执行语义只在 Core 里有一份实现

---

## 阶段总览

```text
当前
  │
  ├─ H1: Hardening 收口 ─────────── (当前进行中)
  │
  ├─ H2: 契约冻结与真实工具验证 ──── (H1 完成后启动)
  │
  ├─ T1: 模板输出稳定化 ─────────── (与 H2 并行)
  │
  ├─ T2: 模板插件扩展面稳定 ──────── (T1 完成后启动)
  │
  ├─ P1: 插件开发者体验 ─────────── (T2 完成后启动)
  │
  ├─ D1: Desktop MVP ────────────── (T2 + H2 完成后启动)
  │
  ├─ D2: Desktop 工作流收敛 ──────── (D1 完成后启动)
  │
  ├─ E1: 发布与分发 ────────────── (D1 或 T2 完成后启动)
  │
  └─ E2: 生态与可持续演进 ────────── (E1 完成后启动)
```

阶段之间的依赖关系：

- H1 → H2（hardening 基本收口后才能冻结契约）
- H2 + T1 可并行（一个是契约层，一个是模板输出层）
- T1 → T2（模板输出稳定后才能稳定插件扩展面）
- T2 → P1（插件扩展面稳定后才能优化开发者体验）
- H2 + T2 → D1（契约和模板都稳定后才启动 Desktop）
- D1 → D2（Desktop MVP 交付后才收敛工作流）
- T2 或 D1 → E1（发布不依赖 Desktop，但需要模板平台稳定）
- E1 → E2（分发机制到位后才谈生态）

---

## H1: Hardening 收口

**当前阶段，正在进行中**

### 目标

把"命令能跑"推进到"失败路径可诊断、输出可消费"，收完当前 hardening 的剩余尾巴。

### 范围

| 工作面 | 具体交付物 |
|--------|-----------|
| 重依赖 smoke | `whisper.cpp` / `demucs` 的真实工具 smoke 与失败路径沉淀 |
| 依赖预检 | `doctor` 收敛 required / optional 依赖来源、默认值、unset 语义 |
| CLI 重构收口 | 评估测试拆分是否到达边际收益递减点；如已到达则停止 |
| 测试组织 | 完成当前 CLI 集成测试按单命令 + 单结果路径的拆分收尾 |

### 不做

- 不新增 CLI 命令
- 不改 JSON 契约
- 不启动 Desktop
- 不引入新的外部依赖

### 验收标准

1. `doctor` 能明确区分 required / optional 依赖，输出包含来源、默认值、unset 状态
2. 所有命令的错误路径优先返回结构化 failure envelope，不退回纯 usage
3. CLI 测试拆分评估有明确结论（继续 or 停止），并写入 roadmap
4. 真实依赖 smoke 至少在本机可通过

### 风险

- `whisper.cpp` / `demucs` 在不同机器上的安装差异可能导致 smoke 不稳定 → smoke 设计为自动跳过

---

## H2: 契约冻结与真实工具验证

**H1 完成后启动**

### 目标

把所有 CLI 输出契约推进到"冻结"状态：字段不再随意漂移，breaking change 需要显式决策。

### 范围

| 工作面 | 具体交付物 |
|--------|-----------|
| `--json-out` 全覆盖 | 所有 21 条命令的 `--json-out` 路径齐备 |
| success / failure envelope | 所有命令在成功和失败时都输出统一结构化 JSON |
| 契约快照测试 | 为关键命令的 `--json-out` 输出建立 JSON schema 快照测试 |
| 真实工具验证 | ffmpeg / ffprobe / whisper-cli / demucs 的端到端 smoke 稳定可复现 |
| 安装前提文档 | 重型外部依赖的安装说明、环境变量约定、目录约定 |

### 不做

- 不改 `edit.json schema v1` 结构
- 不引入新的 envelope 格式
- 不做性能优化

### 验收标准

1. 所有 21 条命令都有 `--json-out` 路径的测试覆盖
2. 契约快照测试能检测到 JSON 输出的 breaking change
3. 至少 ffmpeg + ffprobe 的 smoke 在 CI 里稳定通过（ubuntu-latest）
4. `docs/` 下有明确的外部依赖安装指南

### 风险

- 契约冻结后修改成本变高 → 这正是目的，迫使变更经过显式决策
- CI 环境安装 ffmpeg 不难，但 whisper-cli / demucs 安装复杂 → CI 只跑 ffmpeg smoke，重依赖 smoke 保留在本机

### 依赖

- H1 完成：hardening 收口后契约才能冻结

---

## T1: 模板输出稳定化

**与 H2 并行启动**

### 目标

让模板 guide / preview / commands / scaffold 输出达到"外部 AI 不需要猜接线方式"的程度。

### 范围

| 工作面 | 具体交付物 |
|--------|-----------|
| supporting signal 接线 | transcript / silence / stems / beats 接入 guide / commands / scaffold |
| scaffold 产物完整 | scaffold 目录可直接作为 AI 工作目录，包含所有必要文件 |
| commands bundle 稳定 | `commands.json` / `commands.ps1` / `commands.cmd` / `commands.sh` 由集成测试锁住 |
| 模板 guide 稳定 | guide 里的 supporting signal guidance、consumption 提示、占位符语义由测试锁住 |
| artifact skeleton | artifact slot 的示例路径（如 stems bgm 路径）预填准确 |

### 不做

- 不新增模板类型
- 不改模板 schema 结构
- 不做插件模板的特殊处理（那是 T2）

### 验收标准

1. 外部 AI 拿到 `templates <id>` 的 guide 后，能直接知道需要先生成哪些信号、用什么命令、怎么接回 `edit.json`
2. `scaffold-template` 产出的目录结构、文件内容、命令脚本都有集成测试覆盖
3. 模板 guide / preview / commands 输出字段连续 2 周无 breaking change

### 风险

- 模板数量增长但 supporting signals 未同步收敛 → 每次新增模板必须声明 supporting signals，否则不合并

### 依赖

- 无强依赖，可与 H2 并行

---

## T2: 模板插件扩展面稳定

**T1 完成后启动**

### 目标

在模板输出稳定的基础上，把 `--plugin-dir` 插件发现和 `template.source` 元数据链路收口到稳定状态。

### 范围

| 工作面 | 具体交付物 |
|--------|-----------|
| 插件发现稳定 | `--plugin-dir` 显式目录发现、`plugin.json` manifest 解析稳定 |
| 来源元数据一致 | `template.source` 跨 discovery → build → validate → execute 四阶段一致可审计 |
| 插件模板校验 | 插件模板复用 Core 现有 schema 校验，不引入私有语义 |
| 插件脚手架 | 插件模板的 guide / scaffold / commands 沿用稳定 `template.source` |
| 默认目录评估 | 评估是否需要 `~/.ovt/templates` 用户级默认目录（只评估，不必须实现） |

### 不做

- 不实现运行时代码加载
- 不实现远程插件市场
- 不实现能力插件
- 不实现安装脚本

### 验收标准

1. 插件来源在 `templates` / `init-plan` / `validate-plan` / `render` / `mix-audio` 全链路可审计
2. 插件模板不要求 Core 理解新的私有执行语义
3. 插件发现、清单解析、模板加载都有结构化输出测试
4. `docs/plans/2026-04-19-template-plugin-entry-boundary.md` 的验收标准全部满足

### 依赖

- T1 完成：模板输出稳定后插件扩展面才有意义

---

## P1: 插件开发者体验

**T2 完成后启动**

### 目标

让第三方开发者（或外部 AI）能独立创建、测试和分发模板插件，不需要口口相传。

### 范围

| 工作面 | 具体交付物 |
|--------|-----------|
| 插件开发指南 | 独立文档：目录结构、manifest schema、模板 schema 约束、测试方式 |
| 插件验证命令 | `doctor --plugin-dir <path>` 或类似命令验证插件结构合法性 |
| 插件模板示例 | 一个最小可运行的社区模板插件示例仓库或目录 |
| 插件兼容性 | 明确 `plugin.json` 的 `schemaVersion` 语义与版本兼容策略 |

### 不做

- 不做插件市场
- 不做自动安装
- 不做远程发现

### 验收标准

1. 一个不了解项目内部的开发者，仅凭文档就能创建一个合规的模板插件
2. 插件验证命令能检测出常见的 manifest / template 错误
3. `plugin.json` 的版本策略已写入文档

### 依赖

- T2 完成：插件扩展面稳定后才能优化开发者体验

---

## D1: Desktop MVP

**H2 + T2 完成后启动**

### 目标

在不破坏既有 owner 的前提下，为 CLI 提供轻量 Avalonia 交互壳。

### 范围

| 工作面 | 具体交付物 |
|--------|-----------|
| 文件导入 | 拖放或文件选择器导入媒体文件 |
| 模板选择 | 从内置 + 插件模板列表选择 |
| `edit.json` 可视化 | 以表单或卡片形式展示剪辑计划 |
| 参数编辑 | 表单化编辑模板参数、clip 入出点、BGM 区间 |
| 执行触发 | 调用 Core 执行 render / mix-audio |
| 日志查看 | 展示执行日志和外部工具原始输出 |

### 严禁

- Desktop 不得直接拼接 ffmpeg / ffprobe 命令
- Desktop 不得独立于 `edit.json` 建第二套编辑模型
- Desktop 不得做复杂时间线系统
- Desktop 不得直接启动外部进程绕过 Core.Execution

### 验收标准（启动门槛）

1. `edit.json schema v1` 进入低频变更阶段（连续 1 个月无 breaking change）
2. `templates → init-plan / scaffold-template → validate-plan → render` 工作流稳定
3. 外部 AI 已能用 CLI 完成多数基础剪辑任务
4. Desktop 明确定位为"交互壳"

### 验收标准（交付门槛）

1. Desktop 能完成 "导入 → 选模板 → 编辑参数 → 执行 → 查看结果" 闭环
2. Desktop 所有业务逻辑调用都经过 Core，无绕过
3. Desktop 有基础的 UI 集成测试骨架
4. Desktop 的 `MODULE.md` 已写出 owner 边界

### 依赖

- H2 完成：CLI 契约冻结后 Desktop 才有稳定的消费对象
- T2 完成：模板插件稳定后 Desktop 的模板选择才有意义

---

## D2: Desktop 工作流收敛

**D1 完成后启动**

### 目标

用 Desktop 反馈驱动高频工作流的极简化，让"默认路径"收敛到 2~3 步操作。

### 范围

| 工作面 | 具体交付物 |
|--------|-----------|
| 高频工作流识别 | 基于真实使用数据识别 Top 5 工作流 |
| 极简入口 | 评估是否需要 `quick-render` / `quick-cut` 等 Desktop 一键入口 |
| 批处理 | 多文件并行处理的基础支持 |
| 历史记录 | 任务执行历史的持久化与回放 |
| 设置持久化 | 用户偏好、工具路径、默认模板的持久化 |

### 验收标准

1. Top 5 高频工作流在 Desktop 上都能 3 步内完成
2. Desktop 与 CLI 使用的模型完全一致，无漂移

### 依赖

- D1 完成

---

## E1: 发布与分发

**T2 或 D1 完成后即可启动，不依赖 Desktop**

### 目标

让项目可以从"源码编译"进化到"可直接安装使用"。

### 范围

| 工作面 | 具体交付物 |
|--------|-----------|
| CLI 打包 | 跨平台 single-file 发布（win-x64 / linux-x64 / osx-x64） |
| CI 发布流 | GitHub Release 自动化：tag → build → publish |
| 安装渠道 | 至少一个包管理器入口（winget / Homebrew / NuGet global tool） |
| 容器镜像 | 可选：Docker 镜像用于 CI/CD 编排场景 |
| 版本策略 | SemVer 版本号、changelog 生成、breaking change 公告机制 |
| 文档分层 | CLI 用户指南 / AI 编排指南 / 插件开发者指南 / 贡献者指南 |

### 验收标准

1. 用户不需要 .NET SDK 就能安装和运行 CLI
2. 版本号、changelog、breaking change 都有自动化机制
3. 文档按受众分层，不混在一起

### 依赖

- T2 完成（模板插件稳定后才值得发布）
- 不依赖 D1（CLI 可以先于 Desktop 发布）

---

## E2: 生态与可持续演进

**E1 完成后启动**

### 目标

项目可以在少量维护下持续运转，社区贡献者可以独立扩展。

### 范围

| 工作面 | 具体交付物 |
|--------|-----------|
| 社区模板收集 | 社区模板插件的提交、审核、分发机制 |
| 契约兼容性测试 | 自动检测 CLI 输出 schema 的向后兼容性 |
| 性能基线 | 大文件 / 批量任务的执行时间与内存回归检测 |
| 安全审计 | 外部工具调用的输入消毒、路径遍历防护 |
| `edit.json schema v2` 评估 | 评估是否需要多轨时间线、嵌套 composition 等扩展 |

### 评估项（不做承诺）

- 是否支持远程渲染 / 任务队列
- 是否需要 Web UI 作为 Desktop 替代
- 是否引入"能力插件"（插件贡献新的 CLI 子命令）

### 验收标准

1. 新功能可以由非初始开发者独立完成
2. CLI 契约变更时有自动化工具检测 breaking change
3. 文档与代码同步率由 CI 检查

### 依赖

- E1 完成

---

## 每季度自检清单

每个季度回答以下问题，判断是否在正轨上：

1. **进度** — 当前活跃阶段的验收标准达成率是多少？
2. **文档** — 有没有因为"快"而跳过文档同步？
3. **契约稳定** — CLI 输出契约的 breaking change 次数是否 < 1/季度？
4. **测试覆盖** — 核心路径的测试覆盖率是否保持 > 90%？
5. **阶段纪律** — 是否有"想提前做下一阶段"的冲动？如果有，当前阶段门槛是否真的满足了？
6. **owner 一致** — Core 是否仍是唯一业务 owner？有没有逻辑泄漏到 Cli / Desktop？
7. **依赖健康** — 外部依赖（ffmpeg / whisper / demucs）的兼容性是否仍然可控？

---

## 文档同步规则

本计划与现有文档的关系：

| 文档 | 职责 | 与本计划的关系 |
|------|------|---------------|
| `docs/roadmap.md` | 当前活跃工作面、阶段检查、下一步 | 引用本计划的阶段编号，写当前在哪个阶段 |
| `docs/ARCHITECTURE_GUARDRAILS.md` | 长期 owner、依赖方向、阶段门槛 | D1 的启动门槛在此维护 |
| `docs/plans/*.md` | 当前里程碑或专项计划 | 本计划是长期总纲，专项计划是各阶段的执行细节 |
| `AGENTS.md` | 仓库协作约束 | 不变 |
| `MODULE.md` | 模块独有边界 | 各阶段如果改变模块组织，同步更新 |

更新触发：每当从一个阶段推进到下一个阶段时，更新 `roadmap.md` 的"当前阶段"映射。

---

## 阶段推进决策流程

推进到下一阶段前，必须完成以下检查：

```text
1. 当前阶段验收标准是否全部满足？
   ├─ 是 → 继续
   └─ 否 → 继续当前阶段

2. 下一阶段的依赖是否已满足？
   ├─ 是 → 继续
   └─ 否 → 先完成依赖阶段

3. 下一阶段的风险是否已有缓解方案？
   ├─ 是 → 继续
   └─ 否 → 补缓解方案后再推进

4. roadmap.md 是否已更新到下一阶段？
   ├─ 是 → 正式推进
   └─ 否 → 先同步文档
```
