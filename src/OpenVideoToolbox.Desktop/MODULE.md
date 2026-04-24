# OpenVideoToolbox.Desktop

> 最后更新：2026-04-24

## 职责

Desktop 是未来的交互壳。它的职责是把已经由 `Core` 与 CLI 验证过的能力，以更适合人工操作的方式组织成图形界面。

它不是新的业务 owner，也不是第二套执行内核。

## Owns

- Desktop 入口与应用启动组织
- 未来的 UI framework glue
- 页面导航、视图状态与交互事件组织
- 对以下稳定对象的展示和编辑：
  - 模板 / 插件发现结果
  - `edit.json`
  - `validate-plan` 结果
  - `render` / `mix-audio` preview 结果
  - 执行日志、produced paths、错误摘要
- 文件选择、结果查看、最小表单式参数编辑

## Must Not Own

- `ffmpeg` / `ffprobe` 命令拼接
- 外部进程启动、超时、取消和输出采集
- 模板 schema、插件 schema、`edit.json` schema
- 音频分析、节拍分析、字幕生成、转写解析规则
- 第二套独立于 `edit.json` 的编辑模型
- 任何内置 AI provider / SDK / 远程推理逻辑

## 关键依赖

- `OpenVideoToolbox.Core`

说明：

- Desktop 可以依赖 `Core`
- Desktop 不得直接依赖 `Cli`
- 如需复用 CLI 级别的呈现语义，应下沉为 `Core` 可复用模型，而不是让 Desktop 反向调用 CLI 入口

## 不变量

- Desktop 只是交互壳，不是新的业务 owner
- Desktop 只能消费与展示稳定边界对象，不重新解释核心规则
- Desktop 不得旁路 `Core.Execution` 直接启动外部工具
- Desktop 必须把 `edit.json` 视为唯一计划边界，不得发明 UI 私有计划格式
- Desktop MVP 第一轮只允许覆盖“导入素材 -> 选模板 -> 编辑少量参数 -> 校验 -> 预览 -> 执行 -> 看日志”这一条闭环

## 当前状态

- 当前目录仍是占位入口
- 尚未引入正式 UI framework
- 尚未建立页面、状态管理或持久化 owner

## 启动前必须明确的事项

1. UI framework owner
2. 状态管理 owner
3. Desktop 首轮只消费哪些稳定对象
4. 哪些能力必须继续停留在 `Core`

## 文档同步触发条件

- Desktop 引入实际 UI framework
- Desktop 新增页面 / 状态管理 / 日志查看等长期结构
- Desktop 改变允许依赖或 owner 边界
