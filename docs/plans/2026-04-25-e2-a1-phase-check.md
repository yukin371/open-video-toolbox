# E2-A1 阶段检查：契约兼容性护栏

最后更新：2026-04-25

## 目的

这份文档只回答一个问题：

> `E2-A1` 当前是否已经形成第一层兼容性护栏？

它不替代 `E2-A1` 的实施清单，也不替代 `CHANGELOG.md`、snapshot README 或 PR 模板。它只把当前已经落地的规则、检查点和仓库入口，与 `docs/plans/2026-04-22-e2-ecosystem-sustainability-plan.md` 中定义的目标逐条对照。

## 当前已落地能力

### 规则层

当前仓库内已经有：

- `CHANGELOG.md` 中的 breaking change policy
- `src/OpenVideoToolbox.Cli.Tests/snapshots/README.md`
- `docs/development-principles.md`

它们已经覆盖：

- 什么时候允许更新 golden files
- 哪些变化不能只靠更新 snapshot 掩盖
- 什么变化应被视为 breaking / 需要迁移说明

### 流程层

当前仓库内已经有：

- `.github/PULL_REQUEST_TEMPLATE.md`
- `src/OpenVideoToolbox.Cli.Tests/ContractSnapshotTests.cs`
- `.github/workflows/ci.yml`

它们已经让以下检查变成显式流程：

- golden file 变更是否被解释
- `stdout` 与 `--json-out` 一致性是否被考虑
- snapshot 更新是否只是掩盖回归

## 与阶段目标对照

### 条件 1

> 贡献者能明确知道什么变化需要更新 golden files

当前判断：**满足**

### 条件 2

> breaking change 的定义不只存在于 changelog，而能被测试和流程共同约束

当前判断：**满足**

### 条件 3

> 至少有一条可复用的 CI / review 规则保护 CLI 契约

当前判断：**满足**

## 当前结论

`E2-A1` 当前判断为：**已形成第一层阶段价值，可进入收口 / 持有状态**

更准确地说：

- 契约兼容性已不再只靠口头约束
- golden file review 已成为显式检查项
- snapshot 规则已有固定文档落点

## 当前不继续扩实现的原因

当前不适合继续把 `E2-A1` 扩成更大的系统，原因是：

- 仓库已经有足够清晰的规则与 review 入口
- 再往前走会更像长期治理迭代，而不是当前缺失 blocker

后续如果要继续，只应在真实 PR / 真实 snapshot 变更暴露缺口时再补二轮。

## 本轮阶段检查输出

```text
阶段：E2-A1
阶段目标是否完成：已形成第一层阶段价值
golden file 维护规则是否已固定：是
breaking change 判断是否已有显式落点：是
review / CI 是否已有显式检查项：是
如果现在停止，仓库是否仍处于一致状态：是
```
