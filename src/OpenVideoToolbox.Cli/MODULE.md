# OpenVideoToolbox.Cli

> 最后更新：2026-04-15

## 职责

CLI 是脚本化和调试入口，负责把命令行参数映射成对 `Core` 的调用，并把结果输出为 JSON 或退出码。它不拥有核心业务规则，同时也是外部 AI 代理调用仓库能力的唯一正式入口。

## Owns

- 命令分发：`presets`、`templates`、`init-plan`、`scaffold-template`、`beat-track`、`probe`、`plan`、`run`、`cut`、`concat`、`extract-audio`、`subtitle`、`validate-plan`、`mix-audio`、`render`
- 参数解析、默认值选择、帮助输出
- CLI 级错误提示和退出码语义
- 稳定的结构化输出契约

## Must Not Own

- `ffmpeg` / `ffprobe` 命令拼接规则
- 外部进程执行、超时、取消和输出采集
- 媒体探测解析逻辑
- 预设语义和内置预设定义
- 外部 AI 推理或任何供应商集成

## 关键依赖

- `OpenVideoToolbox.Core.Media`
- `OpenVideoToolbox.Core.Presets`
- `OpenVideoToolbox.Core.Execution`
- `OpenVideoToolbox.Core.Serialization`
- `OpenVideoToolbox.Core.Subtitles`
- `OpenVideoToolbox.Core.Beats`

## 不变量

- CLI 只组合 `Core` 能力，不复制核心逻辑
- `plan` 只能生成 `JobDefinition` 和 `CommandPlan` 预览，不直接执行任务
- `run` 先探测再执行，并把结果序列化输出
- `init-plan` 只负责消费模板目录、生成 `edit.json` skeleton，并把模板规则留在 `Core.Editing`
- `templates` 无参只列模板；可按 `category` / `seed-mode` / `output-container` / `artifact-kind` / `has-artifacts` / `has-subtitles` 过滤列表，也可用 `--summary` 输出稳定摘要、用 `--json-out` 把结果写到文件；按模板 id 查询时返回可直接用于生成辅助 JSON 文件的单模板指南、推荐 seed 模式，以及最小 preview plan；传 `--write-examples` 时负责把这些文件和命令脚本写到目录
- `init-plan` 可以接收 `--artifacts` 载入模板 slot 绑定；已声明的常见 slot 仍可通过 `--subtitle` / `--bgm` 作为快捷入口
- `init-plan` 可以接收 `--template-params` 覆盖模板默认参数，但参数合并规则仍由 `Core.Editing` 持有
- `init-plan` 可以接收 `--transcript` 作为 transcript 引用，也可以在显式传入 `--seed-from-transcript` 时按 segment 生成初始 clips
- `init-plan` 可以接收 `--beats` 作为节奏引用，也可以在显式传入 `--seed-from-beats` 时按固定节拍组生成初始 clips
- `scaffold-template` 只负责组合 `templates --write-examples` 与 `init-plan` 的既有能力，把模板工作目录一次落盘，并可选附带 `validate-plan` 级别的校验结果；它不额外发明新的模板语义
- `beat-track` 只负责调用统一波形提取和节拍分析，不持有分析规则
- `subtitle` 只负责加载 `transcript.json`、选择输出格式，并把渲染委托给 `Core.Subtitles`
- `validate-plan` 负责加载 `edit.json`、解析相对路径、调用 `Core.Editing.EditPlanValidator` 并输出结构化校验结果
- `mix-audio` 只负责加载 `edit.json`、应用输出路径 override，并把音频执行委托给 `Core.Execution`
- `mix-audio --preview` 只负责加载 `edit.json`、应用输出路径 override，并输出 `Core.Execution` 的统一 `executionPreview`，不触发进程执行
- `render` 只负责加载 `edit.json`、应用 CLI override，并把执行委托给 `Core`
- `render --preview` 只负责加载 `edit.json` 并输出 `Core.Execution` 的统一 `executionPreview`，不触发进程执行
- 外部 AI 只能消费 CLI 输出，不能要求仓库内出现新的 AI provider 层

## 常见坑

- 新增 CLI 选项时，容易把默认值、路径规则或命令拼接塞回入口层
- 一旦 JSON 结构漂移，外部 AI 代理和脚本编排会直接失效
- `Program.cs` 当前是手写参数解析，改命令面时要同时验证帮助输出和错误消息

## 文档同步触发条件

- 新增或删除 CLI 命令
- CLI 参数约定、退出码语义或帮助输出结构变化
- CLI 输出契约变化
