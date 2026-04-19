# CLI 维护性重构计划

最后更新：2026-04-19

## 背景

当前 `OpenVideoToolbox.Cli` 已完成 Wave 1 命令面与大部分 hardening：

- 模板插件显式目录发现、`template.source` 来源元数据、guide / preview / commands 输出已接入
- 主要执行/分析类命令已基本收敛到统一 command envelope 与 failure envelope
- CLI 集成测试已扩到大量脚手架、错误路径与真实工具 smoke 场景

这让 CLI 能力面已经足够宽，但也暴露了新的维护性问题：

- `src/OpenVideoToolbox.Cli/Program.cs` 体量过大，命令分发、参数解析、命令实现、输出 helper、模板示例拼装都堆在一个文件里
- `src/OpenVideoToolbox.Cli.Tests/CommandArtifactsIntegrationTests.cs` 已变成超大测试文件，不利于继续扩 CLI 契约测试
- 新增命令或继续 hardening 时，开发成本正从“实现逻辑”转向“定位与安全修改大文件”

当前阶段更适合先做维护性重构，而不是继续扩新命令。

## 目标

1. 在不改变现有 CLI 行为与 owner 边界的前提下，降低 `Program.cs` 和大测试文件的编辑摩擦
2. 让命令族、输出 helper、模板示例构建逻辑更容易定位与复用
3. 为后续继续 hardening、真实依赖 smoke 和 Desktop 前置整理留出更稳定的代码结构

## 非目标

- 不改 `Core` 的 canonical owner，不把 CLI 逻辑下沉到 `Core`
- 不引入新的命令行框架或大规模参数解析重写
- 不在本轮把所有命令处理函数完全拆出 `Program.cs`
- 不改变现有 JSON 契约、退出码语义或插件边界

## 约束

- `OpenVideoToolbox.Cli` 仍只负责参数解析、调用编排、结构化输出与退出码
- `OpenVideoToolbox.Core` 仍是命令构建、执行、模板模型与解析规则的唯一业务 owner
- 所有重构必须由现有测试保护；若测试组织变化，应优先保持断言行为不变
- 文档应先解释拆分策略，再进入大批量迁移

## 现状问题

### 1. CLI 入口耦合过高

`Program.cs` 当前同时承载：

- 顶层命令分发
- 参数解析 helper
- 单命令实现
- command envelope / failure envelope helper
- 模板 guide / preview / commands 生成
- usage 文本

这让任何非平凡修改都要在同一个大文件里穿梭，容易误碰无关逻辑。

### 2. 测试文件粒度过粗

`CommandArtifactsIntegrationTests.cs` 当前混合了：

- 模板 commands / guide / scaffold 测试
- plugin catalog / source metadata 测试
- 各执行类命令的 success / failure 契约测试
- `beat-track` / `audio-*` / `transcribe` / `subtitle` / `validate-plan` 等不同主题

结果是：

- 文件过大，新增测试容易冲突
- helper 与断言虽然可复用，但被隐藏在超大文件底部
- 继续扩 CLI 契约时，很难快速找到自然归属

## 拆分原则

### 1. 先抽“稳定共享逻辑”，再拆“命令实现”

第一批优先抽离：

- command envelope / failure envelope / output file 写出 helper
- 纯模板命令产物拼装 helper
- 测试公共 helper / fixture

这些逻辑稳定、依赖少、最适合先迁出。

### 2. 命令实现按命令族聚合，而不是按单命令碎裂

建议后续把 CLI 按命令族拆分：

- `Foundation commands`
  - `probe` / `plan` / `run` / `doctor`
- `Template commands`
  - `templates` / `init-plan` / `scaffold-template` / `validate-plan`
- `Execution commands`
  - `render` / `mix-audio` / `cut` / `concat` / `extract-audio`
- `Audio/Speech commands`
  - `audio-analyze` / `audio-gain` / `transcribe` / `detect-silence` / `separate-audio` / `beat-track` / `subtitle`

这样既能保留“入口文件统一分发”的简单性，也能降低单文件复杂度。

### 3. 测试按命令域拆分

建议后续把 `CommandArtifactsIntegrationTests.cs` 至少拆为：

- `TemplateCommandArtifactsIntegrationTests.cs`
- `TemplatePluginIntegrationTests.cs`
- `ExecutionCommandIntegrationTests.cs`
- `AudioSpeechCommandIntegrationTests.cs`
- `ValidationAndUtilityCommandIntegrationTests.cs`

公共 helper（例如 CLI 进程启动、脚本生成、测试波形写出）应留在独立 helper 文件。

## 分阶段实施

### Phase 1：抽共享 helper，保持命令入口不变

目标：

- 抽出 command output / failure output helper 到独立文件
- 让 `Program.cs` 不再承载这部分稳定实现细节
- 保持所有命令处理函数和顶层分发不变

预期收益：

- 降低 `Program.cs` 尾部杂项 helper 的体积
- 为后续拆命令族文件腾出更清晰的依赖面

### Phase 2：拆测试文件与测试 helper

目标：

- 按命令域拆分超大集成测试文件
- 保持现有 helper 复用，但减少单文件体积和冲突面

预期收益：

- 新增或修改某类命令测试时，更容易定位
- 降低后续 hardening 的编辑摩擦

### Phase 3：拆命令族实现文件

目标：

- 让 `Program.cs` 只保留命令分发、最小入口 glue、usage
- 把单命令实现迁入命令族文件

预期收益：

- 继续扩命令或收敛错误路径时，不必在一个超大文件里工作

## 第一批实施建议

本轮先做：

1. 新增本计划文档
2. 抽离 `Program.cs` 中稳定的 output / failure helper 到独立文件
3. 同步 `docs/roadmap.md`，把 CLI maintainability 作为当前 active track 之一

可选跟进：

4. 把测试类声明改为 `partial`，为下一轮按命令域拆分测试文件做准备

## 风险

### 1. Top-level `Program.cs` 的作用域限制

当前 CLI 使用 top-level statements。直接大拆命令函数，容易被 top-level 局部函数作用域绊住。

应对：

- 第一批先抽稳定 helper，不直接大拆命令处理函数
- 后续如果要大拆命令族，优先考虑显式静态类或 partial 入口组织

### 2. 行为漂移风险

CLI 现在已经有大量 envelope / failure 语义测试。重构如果顺手改行为，很容易制造隐形破坏。

应对：

- 第一批只做组织性迁移
- 所有输出 helper 迁移后必须跑完整 solution 测试

### 3. 测试文件拆分引发的大 patch 风险

超大测试文件拆分本身就容易产出很大的 diff。

应对：

- 先补计划文档，再分批迁移
- 以“按命令域”而不是“随机搬动测试”方式拆分

## 验证策略

- 每一批组织性迁移后都跑 `dotnet test OpenVideoToolbox.sln`
- 对涉及 envelope helper 的变更，优先确保 CLI tests 全绿
- 测试拆分阶段尽量避免同时修改断言行为

## 文档同步

本计划推进过程中，需要同步：

- `docs/roadmap.md`
  - 把 CLI maintainability 重构写入活跃工作面
- `src/OpenVideoToolbox.Cli/MODULE.md`
  - 若 CLI 内部组织方式或长期维护约束有变化，需要同步
- 必要时更新阶段收尾总结

