# Template Plugin Entry Boundary

最后更新：2026-04-22

## 背景

- 当前仓库已完成 `H1 -> H2+T1 -> T2 -> P1 -> E1`，模板插件的第一阶段入口也已落地。
- 本文档不再承担“待设计草案”职责，转为记录当前仍然有效的插件边界与禁止事项。
- 如果不先定义发现、清单与 owner，后续很容易在 `templates`、`scaffold-template` 与外部模板仓库里各自发明一套元数据。

## 当前状态（2026-04-22）

- 已落地：
  - `templates --plugin-dir <path>`
  - `init-plan --plugin-dir <path>`
  - `scaffold-template --plugin-dir <path>`
  - `validate-plan --plugin-dir <path>`
  - `template.source` 跨 discovery -> build -> validate -> execute 的稳定来源元数据链路
- 仍未做：
  - 运行时代码加载
  - 能力插件
  - 远程插件市场
  - 自动安装与默认目录自动发现

## 文档目标

- 记录“模板插件优先”的最小扩展面。
- 固定插件发现、清单、目录约束和 CLI 责任。
- 保持 `Core` 仍是模板模型与 plan 语义的唯一 owner。
- 为后续 `E2` 或更远阶段继续扩展时提供稳定边界。

## 非目标

- 本轮不实现运行时代码加载。
- 本轮不实现能力插件、脚本钩子或远程插件市场。
- 本轮不在仓库内引入新的外部运行时、跨进程协议或闭源依赖。
- 本轮不允许模板插件绕过 `Core` 直接定义新的执行语义。

## 核心判断

- “模板插件”首先应被视为一组外部提供的模板声明与配套静态资产，而不是可执行代码。
- 插件发现和清单输出属于 `Cli` 的职责。
- 模板 schema、参数 schema、artifact slot、supporting signal、preview 语义仍由 `OpenVideoToolbox.Core/Editing` 定义和校验。
- 真正的执行能力仍只来自仓库已有 deterministic CLI 子命令和 `Core` 中的命令构建链。

## 最小形态

建议先把模板插件定义为一个目录单元，形如：

```text
<plugin-root>/
  plugin.json
  templates/
    <template-id>/
      template.json
      guide.md            # 可选
      template-params.json
      artifacts.json
```

约束：

- `plugin.json` 只承接插件级元数据与包含的模板列表。
- `template.json` 仍必须满足仓库既有模板 schema；不允许插件私自扩展未知字段后要求 `Core` 理解。
- `guide.md` 只作为人类/AI 辅助说明，不参与执行语义。
- `template-params.json`、`artifacts.json` 只是示例 skeleton，不是新 schema。

## 建议的 Manifest 边界

`plugin.json` 建议只包含以下稳定字段：

- `schemaVersion`
- `id`
- `displayName`
- `version`
- `description`
- `templates`
  - 值为插件内模板目录的相对路径与模板 id 列表

不建议在首版 manifest 中引入：

- 自定义命令
- 安装脚本
- 任意 shell hook
- 二进制依赖下载描述
- 远程 URL 自动更新
- 任意可执行代码入口

## 发现与加载边界

建议把发现分成两层，但暂时只落第一层：

### 第一层：目录发现

- `templates` 相关 CLI 命令未来可接受显式 `--plugin-dir <path>`。
- `Cli` 只负责：
  - 扫描 `plugin.json`
  - 解析其中列出的模板目录
  - 调用 `Core` 的模板解析与校验逻辑
  - 以稳定 JSON 列出“发现了哪些插件 / 模板”

### 第二层：用户级默认目录

- 等第一层收敛后，再考虑用户级默认插件目录，例如 `~/.ovt/templates`。
- 这一层不应先于显式目录方案落地，避免过早引入全局状态与不可解释来源。

## CLI 责任

首轮若要实现插件入口，建议只做以下 deterministic 能力：

- `templates --plugin-dir <path>`
  - 把插件内模板纳入现有列表、筛选与 summary 输出
- `templates <id> --plugin-dir <path>`
  - 输出单模板 guide / preview / commands / supporting signals
- `scaffold-template --plugin-dir <path>`
  - 对已发现模板沿用现有脚手架输出

CLI 不应负责：

- 解释新的模板语义
- 动态拼接未声明的 supporting signal
- 为插件模板兜底发明 artifact slot
- 直接执行插件自带脚本

## Core 责任

`Core` 未来只需要接受“插件模板来源”这一额外输入维度，但不改变 owner：

- 模板模型与校验：`OpenVideoToolbox.Core/Editing`
- supporting signal example 构造：`OpenVideoToolbox.Core/Editing`
- preview / skeleton / seed strategy：`OpenVideoToolbox.Core/Editing`

`Core` 不应知道：

- 插件目录扫描策略
- 用户主目录约定
- 任何插件安装、下载或启用状态

## 风险与防漂移规则

- 若插件需要“新能力”才能运行，先补 deterministic CLI 子命令，再回到模板层声明使用它。
- 若插件要求私有字段或私有执行语义，说明这不是“模板插件”而是新的能力扩展问题，应单独评审。
- 若后续要引入能力插件，必须与模板插件拆成独立设计，不得混进同一 manifest。

## 建议的后续扩展顺序

1. 保持 `--plugin-dir` 显式目录发现为唯一稳定入口，避免过早引入全局默认目录。
2. 若继续扩展，优先补插件兼容性与结构化输出测试，而不是增加新的运行时机制。
3. 若未来评估用户级默认目录，应在显式目录方案持续稳定后单独决策。
4. 在引入任何安装、市场或远程发现机制前，先确认它仍不破坏 `Core` / `Cli` owner 边界。

## 验收标准

- 文档能回答“模板插件是什么、不是什么”。
- 后续实现时不需要引入运行时代码加载也能交付第一版扩展入口。
- `Core` / `Cli` owner 不发生漂移。
- 外部 AI 能通过稳定目录和 manifest 理解如何提供额外模板，而不是依赖仓库内黑盒逻辑。
