# E2-A1 契约兼容性护栏实施清单

最后更新：2026-04-22

## 背景

- 仓库已经具备第一层契约护栏：
  - `CHANGELOG.md` 已定义 breaking change policy
  - `src/OpenVideoToolbox.Cli.Tests/ContractSnapshotTests.cs` 已建立 contract snapshot 测试
  - `src/OpenVideoToolbox.Cli.Tests/snapshots/` 已保存黄金文件
  - `.github/workflows/ci.yml` 已通过 `dotnet test` 执行这些测试
- 当前缺的不是“有没有 snapshot”，而是：
  - 贡献者何时应该新增或更新 golden files
  - 哪些变化必须视为 breaking change
  - golden file 更新在 review 阶段如何被显式检查
  - snapshot 覆盖范围如何扩展才不失控

因此 `E2-A1` 的目标不是再发明一套契约体系，而是把现有体系补成可持续执行的流程。

## 当前现状

### 已有能力

- `presets`
- `templates` catalog
- `templates shorts-captioned`
- `templates beat-montage`
- `validate-plan` 结构化 payload

以上命令已经纳入 snapshot 或结构对比测试。

### 当前缺口

- 缺少一份“何时更新 golden files”的维护规则
- 缺少按变更类型划分的 breaking / non-breaking 判断表
- 缺少 snapshot 覆盖对象选择规则
- 缺少 PR / review 视角的明确检查项

## 本轮目标

1. 固化 snapshot 维护规则
2. 固化 breaking change 判断规则
3. 固化 golden file review 规则
4. 给后续扩展 snapshot 覆盖范围提供选择标准

## 本轮不做

- 不改 CLI 输出契约本身
- 不新增一大批 snapshot 覆盖到全部 21 条命令
- 不引入新的测试框架
- 不把所有 JSON 字段变化都一律视为 major breaking

## 工作项

### A1. Golden File 维护规则

**目标：** 让贡献者知道何时应更新 snapshot，何时绝不能直接改 snapshot 掩盖回归。

**需要落地的规则：**

- 只有在以下情况才允许更新 golden file：
  - 有意新增向后兼容字段
  - 明确修复错误输出，且已评估下游影响
  - 经显式决策接受 breaking change
- 遇到以下情况不得只改 golden file 就结束：
  - 无说明的字段删除或重命名
  - 顶层 envelope 变化
  - `stdout` 与 `--json-out` 内容出现不一致
  - 退出码语义改变但未更新迁移说明

**建议落点：**

- `docs/README.md` 或贡献者文档索引
- `CHANGELOG.md` 的 breaking change policy 邻近位置
- `src/OpenVideoToolbox.Cli.Tests/snapshots/README.md` 作为测试目录内的就地维护说明

### A2. Breaking Change 判断表

**目标：** 把 changelog 里的原则扩成一张可执行判断表。

**最低应覆盖的变化类型：**

- top-level envelope 字段变更
- payload 已存在字段删除 / 重命名 / 类型变化
- 新增可选字段
- 字段顺序变化
- 路径类机器相关字段
- `stdout` 与 `--json-out` 一致性
- 退出码语义变化

**期望结果：**

- 每类变化都能回答：
  - 是否 breaking
  - 是否需要更新 golden files
  - 是否需要 changelog migration notes
  - 是否需要 major version bump

### A3. Review 清单

**目标：** 让 golden file 变更在 code review 中变成显式检查项，而不是顺手改掉。

**建议检查项：**

1. 这次 PR 是否改了 `snapshots/*.json`？
2. 如果改了，原因是新增能力、修 bug，还是 breaking change？
3. 是否同步更新了相关测试说明、`CHANGELOG.md` 或迁移说明？
4. 是否验证了 `stdout` 与 `--json-out` 一致性没有被破坏？
5. 是否有更适合做结构归一化比较，而不是直接改 golden file？

**建议落点：**

- 贡献者文档
- PR 模板或 review 约定文档

### A4. Snapshot 覆盖扩展规则

**目标：** 决定未来哪些命令值得进入 snapshot，避免无限制膨胀。

**建议优先纳入的对象：**

- 高复用、低噪音、结构稳定的读型命令
- 模板发现 / guide / summary 输出
- 关键校验命令的 machine-independent payload

**建议暂不优先纳入的对象：**

- 强依赖本机路径、时间戳、随机目录的输出
- 大量包含执行结果细节、容易受环境影响的命令
- 更适合做局部结构断言而不是全文快照的命令

**选择标准：**

- 是否机器无关
- 是否高频被外部 AI / 脚本消费
- 是否字段稳定且可审计
- 是否能在 CI 中稳定重放

**建议落点：**

- `src/OpenVideoToolbox.Cli.Tests/snapshots/README.md`
- 需要时再由 `docs/README.md` 或专题计划文档做上层索引，不重复维护细则正文

## 建议执行顺序

```text
A1 Golden File 维护规则
   ↓
A2 Breaking Change 判断表
   ↓
A3 Review 清单
   ↓
A4 Snapshot 覆盖扩展规则
```

说明：

- `A1` 与 `A2` 先做，先把规则写清。
- `A3` 随后做，把规则接进 review 流程。
- `A4` 最后做，避免在规则未定前盲目扩覆盖。

## 验收标准

1. 贡献者能回答“什么时候该改 snapshot，什么时候不该改”
2. breaking / non-breaking 的判断不再只靠经验
3. golden file 变更在 review 时有显式检查项
4. 后续扩 snapshot 时有清晰纳入标准，而不是按感觉追加

## 完成后的直接收益

- 减少用“更新 golden file”掩盖真实契约回归
- 降低社区贡献时对 maintainer 口头说明的依赖
- 为后续包管理器、社区模板、长期兼容性维护提供更稳定底座

## 与现有文档的关系

- `docs/plans/2026-04-22-e2-ecosystem-sustainability-plan.md`
  - 本文档是其中 `E2-A1` 的专项实施清单
- `CHANGELOG.md`
  - 保留 breaking change 的版本策略 owner；本文档只补执行规则
- `src/OpenVideoToolbox.Cli.Tests/ContractSnapshotTests.cs`
  - 保留测试 owner；本文档不替代测试代码
- `src/OpenVideoToolbox.Cli.Tests/snapshots/README.md`
  - 保留 snapshot 增补、筛选、更新的就地说明 owner；本文档不重复维护目录级细则正文
- `.github/workflows/ci.yml`
  - 保留 CI owner；本文档只说明后续应补什么流程护栏
