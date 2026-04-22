# E2-A3 社区模板 / 插件贡献路径

最后更新：2026-04-23

## 背景

- 模板插件的第一阶段入口已经落地：
  - `templates --plugin-dir <path>`
  - `init-plan --plugin-dir <path>`
  - `scaffold-template --plugin-dir <path>`
  - `validate-plan --plugin-dir <path>`
- 当前也已经有：
  - `docs/plugin-development-guide.md`
  - `examples/plugin-example/`
  - 插件目录发现、guide 输出、plan 校验与执行链测试
- 但现状仍偏“开发者看完能理解”。
- `E2-A3` 要补的是“社区贡献者不靠口头说明，也能自建、自测、提交”的闭环。

## 本轮目标

1. 明确社区模板插件的最小提交物
2. 明确本地自测命令与通过标准
3. 明确什么属于模板插件，什么不属于
4. 给出一个可以直接照抄的示例目录与提交流程

## 本轮不做

- 不引入默认插件目录自动发现
- 不引入远程插件市场
- 不允许插件附带脚本、二进制或私有执行逻辑
- 不把社区插件提交流程升级成新的仓库子系统

## 贡献对象定义

社区模板 / 插件贡献，当前只接受这一类扩展：

- 基于既有 template schema 的静态模板插件
- 通过 `plugin.json` + `template.json` 描述
- 执行能力完全复用现有 CLI 命令与 `Core.Editing` 语义

以下情况不属于当前允许的社区模板插件：

- 需要新的 `ffmpeg` 拼接语义才能成立
- 依赖自定义脚本、二进制或安装钩子
- 要求 `Core` 理解未声明的私有字段
- 把“内容理解”“自动决策”伪装成模板默认行为

## 最小提交物

一个可提交的社区模板插件，最少应包含：

```text
<plugin-root>/
  plugin.json
  templates/
    <template-id>/
      template.json
```

建议同时包含：

```text
<plugin-root>/
  README.md
  plugin.json
  templates/
    <template-id>/
      template.json
      artifacts.json
      template-params.json
```

说明：

- `README.md` 用于说明模板适用场景、输入假设和推荐工作流。
- `artifacts.json`、`template-params.json` 只是示例 skeleton，不是新 schema。
- 不要求社区贡献者提交可执行脚本；如果需要命令样例，应优先复用 `templates <id>` / `--write-examples` 的稳定输出。

## 本地自测闭环

社区贡献者至少应能在本地完成以下检查：

### 1. 发现插件

```powershell
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- templates --plugin-dir <plugin-root> --summary
```

通过标准：

- 命令退出码为 `0`
- 输出中能看到插件 `id`
- 输出中能看到模板 `id`

### 2. 查看单模板 guide

```powershell
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- templates <template-id> --plugin-dir <plugin-root>
```

通过标准：

- `payload.source.kind` 为 `plugin`
- 输出 guide 中带有 `<plugin-dir>` 占位符
- preview plan 中的 `template.source.kind` 为 `plugin`

### 3. 写出示例目录

```powershell
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- templates <template-id> --plugin-dir <plugin-root> --write-examples .plugin-guide
```

通过标准：

- 成功生成 `guide.json`
- 成功生成 `commands.json`
- `commands.*` 中的工作流命令显式带 `--plugin-dir`

### 4. 生成并校验计划

```powershell
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- init-plan <input> --template <template-id> --plugin-dir <plugin-root> --output edit.json --render-output final.mp4
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- validate-plan --plan edit.json --plugin-dir <plugin-root>
```

通过标准：

- `edit.json` 写出成功
- `edit.json.template.source.kind` 为 `plugin`
- `validate-plan` 返回 `payload.isValid = true`

## 审核清单

社区模板插件在提交前至少应满足：

1. 插件是纯静态文件，不包含脚本、可执行文件或下载逻辑
2. `plugin.json` 与 `template.json` 只使用当前 schema 已知字段
3. 模板 ID 不与内置模板冲突
4. `templates --plugin-dir`、`templates <id> --plugin-dir`、`init-plan --plugin-dir`、`validate-plan --plugin-dir` 能闭环通过
5. 模板描述里能说明适用场景、推荐 seed 模式与需要的 supporting signals
6. 若模板依赖字幕、beats、stems 等输入，说明必须是显式 artifact / signal，不允许隐式约定

## 推荐文档分工

- `docs/plans/2026-04-19-template-plugin-entry-boundary.md`
  - 写边界，不写提交流程
- `docs/plugin-development-guide.md`
  - 写 schema、目录结构、开发与自测
- `examples/plugin-example/README.md`
  - 写可照抄的最小示例与本地演示步骤
- `scripts/Verify-ExamplePlugin.ps1`
  - 写仓库内的一键闭环验证，确保示例插件、guide、write-examples 与 plan 校验仍能一起工作
- `.github/ISSUE_TEMPLATE/community-plugin-submission.yml`
  - 写仓库内固定提交入口，收集插件摘要、链接、自测结果与边界确认
- 本文档
  - 写 `E2-A3` 为什么做、做到什么程度算完成

## 仓库内固定提交入口

当前仓库已补一个 maintainer 入口：

- `.github/ISSUE_TEMPLATE/community-plugin-submission.yml`

用途：

1. 收集插件 `id`、模板 `id`、适用场景和推荐 signal
2. 收集最小本地自测结果，而不是只收一句“本地可用”
3. 让 maintainer 直接按静态边界、私有语义、模板冲突等清单做第一轮筛查

边界：

- 它不是新的插件市场
- 它不替代 PR
- 它不引入自动审核或远程目录发现
- 它只把当前文档里的提交与审核清单固定成仓库入口

## 仓库内示例插件闭环验证

当前仓库也已补一个维护脚本：

- `scripts/Verify-ExamplePlugin.ps1`

用途：

1. 自动生成样例输入，不要求贡献者手工准备 `input.mp4`
2. 用仓库现有 CLI 命令跑通 `examples/plugin-example/`
3. 把 `validate-plugin`、`templates --summary`、`templates <id> --write-examples`、`init-plan`、`validate-plan` 收成同一条维护入口
4. 让 CI 直接回归示例插件是否仍代表一个真实可提交的最小闭环

边界：

- 它不替代插件作者自己的本地自测
- 它只验证仓库内示例插件，不自动扫描任意社区插件目录
- 它复用现有 CLI 契约，不引入新的插件测试协议

## 完成判定

满足以下条件后，可认为 `E2-A3` 首轮已形成阶段价值：

1. 新贡献者只看仓库文档，也能做出一个最小插件
2. 新贡献者知道至少要跑哪几条命令做本地自测
3. maintainer 能按清单判断一个插件贡献是否越界
4. 社区模板贡献不会倒逼 `Core` 接受私有执行语义
