# 架构草图

对应的可视化架构图见 `docs/architecture-overview.drawio`，可直接用 draw.io / diagrams.net 打开维护。  
Wave 1 的细化执行流见 `docs/wave1-cli-flow.drawio`。  
代码模块落点图见 `docs/code-module-map.drawio`。

## 1. 顶层模块

### Core

负责以下内容：

- 媒体信息模型
- 任务定义
- 剪辑计划模型
- 预设定义
- 外部工具抽象
- 命令生成
- 执行与日志模型

### Cli

负责以下内容：

- 调试入口
- 批处理脚本入口
- 命令预览
- 结构化 JSON 输出
- 剪辑计划读写入口
- 回归验证辅助

### Desktop

负责以下内容：

- 媒体文件导入
- 任务队列视图
- 预设编辑器
- 轻量剪辑计划可视化
- 执行进度与日志界面

## 2. 核心数据流

```text
输入文件
  -> 媒体探测
  -> 生成规范化媒体信息
  -> 生成 transcript / beats / 可选中间产物
  -> 生成 edit.json
  -> 人工二次修正 edit.json
  -> 构建外部命令
  -> 执行任务
  -> 采集日志与产物元数据
```

## 3. 关键对象建议

- `MediaProbeResult`
- `PresetDefinition`
- `JobDefinition`
- `CommandPlan`
- `ExecutionResult`
- `EditPlan`
- `EditClip`
- `AudioTrackMix`
- `BeatMarker`

## 4. 后续演进方向

- 引入更多确定性 CLI 编辑子命令
- 引入字幕生成与多导出目标
- 让 `edit.json` 成为 CLI 与未来轻量 UI 的共同边界
- 引入任务历史与恢复
- 将 CLI 已验证的流程接入 Desktop
