# V2-P3 阶段检查：effect descriptor / discovery

最后更新：2026-04-24

## 目的

这份文档只回答一个问题：

> `V2-P3` 现在到底算不算可以进入阶段验收？

它不替代设计稿，也不扩大本阶段范围。它只把当前已经落地的 effect descriptor / discovery 能力，与 [2026-04-24-v1-v2-boundary-and-phased-delivery-plan.md](./2026-04-24-v1-v2-boundary-and-phased-delivery-plan.md) 中定义的 `V2-P3` 目标逐条对照，避免把“效果骨架文件存在”误判成“发现层已完成”。

## 阶段定义回顾

`V2-P3` 的目标是：

- 落 `IEffectDefinition`、`EffectParameterSchema`、`EffectRegistry` 的最小可用发现层
- 让 built-in effect 可以被稳定列出和单项描述
- 让 `validate-plan` 至少能识别 built-in effect type，而不是把所有 effect 都当未知值
- 不进入 render builder 的 effect 执行与模板替换主路径

本阶段当前纳入范围：

- `BuiltInEffectCatalog`
- `EffectRegistry` 的稳定枚举与批量注册
- CLI `effects list/describe`
- `validate-plan` 对 built-in effect type 的识别

本阶段当前不纳入范围：

- 插件 `effects/*.json` 加载
- effect 参数 deep semantic 校验
- `FfmpegTimelineRenderCommandBuilder` 的正式接线
- `render` / `mix-audio` 的 v2 effect 执行

## 当前已落地能力

### Core.Editing

当前已落地：

- `BuiltInEffectCatalog.GetAll()`
- `BuiltInEffectCatalog.CreateRegistry()`
- `EffectRegistry.RegisterRange(...)`
- `EffectRegistry.GetAll(...)` 的稳定排序输出

当前 built-in catalog 已包含首批 effect 类型：

- `fade`
- `dissolve`
- `brightness_contrast`
- `gaussian_blur`
- `scale`
- `pan`
- `text_overlay`
- `volume`
- `fade_audio`
- `auto_ducking`

当前已守住的边界：

- 描述符 owner 仍在 `Core.Editing`
- built-in catalog 只暴露 descriptor，不拥有 render 执行
- `Execution` 里的 timeline render builder 仍未接到正式命令面

### CLI 发现入口

当前已落地：

- `effects`
- `effects list --category <id>`
- `effects describe <type>`
- `effects <type>` 简写描述模式

当前输出已包含：

- `type`
- `category`
- `displayName`
- `description`
- `templateMode`
- `parameters`
- `ffmpegTemplates`

### validator 接线

当前已落地：

- CLI `validate-plan` 现会接入 built-in effect registry
- 已知 built-in effect 不再被无条件标记为 `timeline.effect.type.unknown`
- 未注册 effect 仍会保留 warning，方便后续插件或扩展场景继续收口

## 与阶段验收条件对照

### 条件 1

> 外部必须至少有一条可直接发现 effect 的 CLI 路径

当前判断：**满足**

证据：

- `effects list` 可直接列出 built-in catalog
- `effects describe fade` 可直接查看单 effect descriptor

### 条件 2

> effect schema 必须继续由 `Core.Editing` 单一持有，CLI 只做透传

当前判断：**满足**

证据：

- `BuiltInEffectCatalog` 与 `EffectRegistry` 都在 `Core.Editing`
- CLI 只做 list / describe / envelope 输出

### 条件 3

> validator 至少要和 built-in effect registry 打通

当前判断：**满足**

证据：

- `validate-plan` 已通过 `BuiltInEffectCatalog.CreateRegistry()` 接入内置 registry
- 新增 CLI 集成测试已覆盖 built-in effect 识别

### 条件 4

> 本阶段不能顺手进入 render builder 正式实现

当前判断：**满足**

证据：

- 未新增 `render` / `mix-audio` 的 v2 effect 执行路径
- 未把 `FfmpegTimelineRenderCommandBuilder` 接到 CLI 命令
- 插件 effect 加载也未在本阶段开启

## 当前不纳入本阶段验收的内容

以下内容当前明确不属于 `V2-P3` 本轮阶段验收：

- 插件 effect JSON 加载
- effect executor 注册与调用
- effect 参数的运行时合法性校验
- timeline render parity

这样做的原因是：

1. `V2-P3` 只负责描述层、发现层与 validator 最小接线
2. 如果现在把执行路径拉进来，会再次跨到 `V2-P4`

## 当前验证结果

当前已完成：

- `dotnet build OpenVideoToolbox.sln`
- `dotnet test OpenVideoToolbox.sln`

当前最新全量结果为：

- `OpenVideoToolbox.Core.Tests`：153 通过
- `OpenVideoToolbox.Cli.Tests`：168 通过
- 总计：321 通过

## 当前结论

`V2-P3` 当前判断为：**本阶段已达到阶段验收输入条件，可进入人工验收**

更准确地说：

- v2 effect 现在已经不是只存在于设计稿里的接口和类型名
- CLI 已提供最小可手测的 discovery 入口
- `validate-plan` 已能识别内置 effect type
- 当前代码、测试与文档边界仍保持在“descriptor / discovery，不进 render”

因此下一步不应继续在 `V2-P3` 里无边界追加 render builder 或插件 effect 执行，而应先做阶段验收决定。

## 手动验收入口

本阶段已补可直接执行的人工验收清单：

- [2026-04-24-v2-p3-acceptance-checklist.md](./2026-04-24-v2-p3-acceptance-checklist.md)

该清单当前覆盖：

1. `effects list` 的 built-in catalog 发现
2. `effects describe` 的单 effect 描述
3. `validate-plan` 对 built-in effect 的识别
4. `validate-plan` 对 unknown effect warning 的保留

## 本轮阶段检查输出

```text
阶段：V2-P3
阶段目标是否完成：已完成当前 descriptor / discovery 范围，达到阶段验收输入条件
本阶段范围是否清楚：是，仅包含 built-in catalog、effects 命令与 validator 最小接线
当前 owner 是否保持单一：是，effect schema 仍由 Core.Editing 持有
是否出现第二套 render 或执行语义：否
当前验证是否充分：是，已完成全量 build/test
是否应继续在本阶段追加实现：否，应先进入阶段验收
如果现在停止，仓库是否仍处于一致状态：是
```
