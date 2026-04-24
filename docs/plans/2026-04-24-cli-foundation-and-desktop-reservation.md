# CLI 基础功能整理与 Desktop 预留边界

最后更新：2026-04-24

## 目的

当前阶段不正式启动 `D1 Desktop MVP`，但也不能只是“继续做 CLI”这一句空话。

因此本文件要回答三个问题：

1. 当前版本的基础功能到底有哪些
2. 接下来丰富 CLI 时，优先补哪些能力最划算
3. 如果未来启动 Desktop，今天的 CLI / Core 应先预留哪些稳定边界

本文档是当前阶段的执行清单，不替代 `docs/roadmap.md` 的阶段判断，也不替代未来 `D1` 的正式实施计划。

## 当前结论

- 近期主工作面继续放在 CLI / Core，而不是 Desktop。
- 下一阶段不是“再堆更多分散命令”，而是把高频基础能力整理成更完整、更可编排、更利于未来 UI 复用的能力面。
- Desktop 预留的重点不是先写 UI，而是先固定它未来只能消费的边界对象。
- 功能排序上不采用“先把所有底层原语做满，再考虑真实工作流”的路线；应改为：**常用功能优先，基础能力只补到足以稳定支撑这些高频工作流。**

## 优先级原则

### 结论

接下来更合适的路线是：

**常用功能优先，基础层托底。**

意思不是跳过基础能力，而是：

- 不为了“基础层完整感”先补一批低频原语
- 优先实现个人工作者和外部 AI 最常遇到的工作流
- 只在这些工作流确实依赖某个底层能力时，再补那块基础层

### 为什么不是“基本功能优先”

如果按“基本功能优先”推进，最容易出现的问题是：

- 命令数量越来越多，但真实工作流还是断的
- 补出很多低频音频原语，却没解决素材替换、字幕挂载、配音接回这类高频问题
- 未来 Desktop 只能消费一堆离散命令，而不是稳定闭环

### 为什么也不是“只做常用功能，不补基础层”

如果完全忽略基础层，也会马上失控：

- 高层工作流会依赖隐式脚本拼接
- CLI / Desktop 会开始复制规则
- 未来 `edit.json`、preview、validate 的边界会变脏

所以更准确的口径是：

> 以高频工作流定义优先级，以最小必要基础层保证这些工作流可维护、可测试、可复用。

## 基础功能列表

### A. 输入与探测

这些能力已经构成当前 CLI 的第一层基础盘：

| 能力 | 当前入口 | 产物 / 边界 | 当前状态 |
| --- | --- | --- | --- |
| 媒体探测 | `probe` | 规范化媒体信息 JSON | 已可用 |
| 计划预览 | `plan` | `JobDefinition` / `CommandPlan` 预览 | 已可用 |
| 直接执行 | `run` | 探测 + 执行 + 结构化结果 | 已可用 |
| 依赖体检 | `doctor` | required / optional 依赖状态 | 已可用 |

### B. 模板与草稿生成

这是当前仓库最重要的第二层基础盘，也是未来 Desktop 最值得复用的主闭环：

| 能力 | 当前入口 | 产物 / 边界 | 当前状态 |
| --- | --- | --- | --- |
| 模板列表 / 筛选 | `templates` | 模板 catalog / summary / guide | 已可用 |
| 初始化计划 | `init-plan` | `edit.json` skeleton | 已可用 |
| 工作目录脚手架 | `scaffold-template` | guide / examples / `edit.json` | 已可用 |
| 计划校验 | `validate-plan` | 结构化 issues / isValid | 已可用 |

### C. 基础信号与素材辅助

这些能力现在已经有独立入口，但还没有完全被整理成“统一的基础信号层”：

| 能力 | 当前入口 | 产物 / 边界 | 当前状态 |
| --- | --- | --- | --- |
| 节拍分析 | `beat-track` | `beats.json` | 已可用 |
| 响度分析 | `audio-analyze` | `audio.json` | 已可用 |
| 音量调整 | `audio-gain` | 新音频文件 + envelope | 已可用 |
| 响度归一化 | `audio-normalize` | 新音频文件 + envelope | 已可用 |
| 转写 | `transcribe` | `transcript.json` | 已可用 |
| 静音检测 | `detect-silence` | `silence.json` | 已可用 |
| 音频分离 | `separate-audio` | stem 输出目录 | 已可用 |
| 字幕导出 | `subtitle` | `srt` / `ass` | 已可用 |

### D. 执行与导出

| 能力 | 当前入口 | 产物 / 边界 | 当前状态 |
| --- | --- | --- | --- |
| 基础裁切 | `cut` | 单段媒体文件 | 已可用 |
| 基础拼接 | `concat` | 合并媒体文件 | 已可用 |
| 提取音轨 | `extract-audio` | 独立音轨文件 | 已可用 |
| 混音预览 / 导出 | `mix-audio` | preview / 混音文件 | 已可用 |
| 最终渲染 | `render` | preview / 成片输出 | 已可用 |

### E. 模板插件与生态入口

| 能力 | 当前入口 | 产物 / 边界 | 当前状态 |
| --- | --- | --- | --- |
| 插件校验 | `validate-plugin` | 结构化校验结果 | 已可用 |
| 插件模板发现 | `templates --plugin-dir` | catalog + source metadata | 已可用 |
| 插件模板计划生成 | `init-plan --plugin-dir` | 带 `template.source` 的 `edit.json` | 已可用 |

## 下一阶段 CLI 丰富方向

### 高频工作流优先级

相比“继续扩一批通用音频命令”，当前更值得优先补的是这几类高频工作流：

| 优先级 | 工作流 | 说明 | 主要支撑边界 |
| --- | --- | --- | --- |
| P0 | 素材堆积与替换 | 用新的旁白、BGM、字幕或片段替换现有计划中的对应素材 | `edit.json` / artifacts / preview |
| P0 | 字幕识别与挂载 | 从素材得到 transcript / subtitle，并稳定接回 plan / render | `transcribe` / `subtitle` / `init-plan` / `render` |
| P0 | 语音素材接入 | 把外部 TTS 产物或配音文件接回现有计划 | audio tracks / artifacts / mix preview |
| P1 | 音色处理 / 变音接入 | 把外部 voice conversion 结果接回现有计划 | audio replacement / validation / preview |
| P1 | 批量素材工作流 | 多文件重复生成草稿、替换素材、批量导出 | batch probe / batch init-plan / batch render |

### 这些工作流对应的 CLI 能力方向

围绕上面的高频场景，下一阶段更合理的命令面不是“更多音频滤镜”，而是：

1. **素材替换与重绑定能力**
   - 针对 clips / bgm / subtitle / voice-over / transcript 等已有槽位做显式替换
   - 优先做计划内替换，不先做复杂编辑器语义

2. **字幕链路收口能力**
   - `transcribe -> subtitle -> attach/burn-in -> render` 更短更稳定
   - 明确 transcript / subtitle 对 `edit.json` 的回接方式

3. **配音接入能力**
   - 允许把外部生成的 TTS / dubbing 音频文件稳定接入 `edit.json`
   - 支持 preview 中明确看到“替换了哪条主音轨 / BGM / voice-over”

4. **批量工作流能力**
   - 多输入批量生成草稿
   - 多输入批量套模板
   - 多输入批量替换素材并导出

5. **inspect / summary 能力**
   - 让人和未来 Desktop 更容易看清“当前计划里有哪些素材、哪些可替换、哪些缺失”

### P0. 先补“更完整的基础盘”，不是再堆随机命令

近期最值得继续做的是以下三类：

1. **计划可读性能力**
   - 目标：让人和未来 Desktop 都能更容易读懂 `edit.json`
   - 代表方向：
     - `edit.json` 的稳定摘要输出
     - plan / render preview 的更清晰分层摘要
     - 对 clips / tracks / subtitles / artifacts 的结构化概览
   - 原因：这是 Desktop 的直接前置能力，也是 CLI 人工调试体验的短板

2. **基础信号整合能力**
   - 目标：把 transcript / beats / silence / stems 从“分散命令”整理成更完整的工作流原语
   - 代表方向：
     - 更稳定的 signal consumption 指引
     - 围绕模板的 signal readiness / missing-signal 提示
     - 更好的 artifacts / template params / signal 接回路径
   - 原因：这比做新 UI 更直接地提升外部 AI 和人工操作效率

3. **批量与自动化友好能力**
   - 目标：覆盖个人工作者的真实高频批处理场景
   - 代表方向：
     - 多输入批量探测 / 模板初始化 / 渲染入口
     - 更稳定的输出目录组织
     - 更清晰的失败汇总与 partial success 语义
   - 原因：CLI 的真正价值往往出现在批量工作流，而不是单次调用

4. **素材替换与挂载能力**
   - 目标：让高频工作流里的“换旁白 / 换 BGM / 挂字幕 / 接 transcript”变成稳定入口
   - 代表方向：
     - 计划内素材槽位摘要
     - artifacts / subtitle / audio track 的显式替换
     - 对缺失素材、未绑定素材、引用失效素材的结构化提示
   - 原因：这比继续补更多底层音频命令更贴近真实剪辑场景

### P1. 近期更具体的 CLI 候选工作包

建议按下面顺序推进，而不是平铺：

| 优先级 | 候选方向 | 主要价值 | 与 Desktop 的关系 |
| --- | --- | --- | --- |
| P1 | `edit.json` 摘要 / inspect 类命令 | 降低人工理解成本 | 未来可直接复用为 UI 数据源 |
| P1 | 素材槽位 / 可替换素材摘要 | 降低“素材堆积但不知道怎么换”的成本 | 未来可直接映射成 Desktop 素材面板 |
| P1 | render / mix preview 摘要增强 | 更容易在执行前发现问题 | 未来可直接映射成 Desktop 预览卡片 |
| P1 | transcript / subtitle 挂载链路增强 | 缩短字幕工作流闭环 | 未来可直接映射成字幕向导 |
| P1 | 外部 TTS / dubbing 音频接回能力 | 覆盖高频配音场景 | 未来可直接映射成配音替换入口 |
| P1 | 批量 probe / 批量 init-plan | 提升个人工作者批量处理效率 | Desktop 后续也需要同类批量入口 |
| P1 | 依赖缺失与恢复建议增强 | 降低环境排障成本 | Desktop 启动前检查也需要同类结果 |
| P2 | 更稳定的 artifacts / signal 整理命令 | 减少外部 AI 自行拼接 | 未来可作为 UI 向导步骤 |
| P2 | 日志 / 运行记录摘要能力 | 提升失败诊断效率 | Desktop 日志面板可直接复用 |
| P2 | 外部 voice conversion 结果接回能力 | 支持 AI 变音 / 音色替换场景 | 未来可直接映射成高级音频入口 |

## 依赖策略补充

### 当前判断

你提到的高频功能里，真正重要的不是“再补更多基础音频 DSP”，而是：

- 素材替换
- 配音 / TTS 接回
- 字幕识别与挂载
- 变音结果接回

这类能力的依赖策略应保持克制：

1. **优先接外部 CLI 或显式文件输入**
   - 例如：外部 TTS 先输出音频文件，再由 CLI 接回 `edit.json`
   - 例如：外部 voice conversion 先输出变音文件，再由 CLI 接回音轨替换流程

2. **不在仓库里内置 AI provider / SDK**
   - 这条仍是硬边界
   - 即使要支持 TTS / 变音，也应优先做“外部工具适配”或“显式文件消费”，而不是嵌入远程模型调用

3. **新依赖优先围绕高频工作流，而不是低频原语**
   - 如果引入新依赖，应先回答：它能不能显著缩短“字幕 / 配音 / 素材替换 / 批量工作流”的闭环
   - 如果只是补一个低频音频特效，不应优先

### 当前更值得考虑的候选依赖类型

从高频工作流角度看，后续若要新增功能依赖，更值得评估的是：

| 候选方向 | 用途 | 当前建议 |
| --- | --- | --- |
| 本地 TTS CLI | 文案转旁白音频 | 可评估，但只能走外部 CLI 适配 |
| 本地 voice conversion CLI | 变音 / 音色替换 | 可评估，但只能走外部 CLI 适配 |
| 更强的字幕/对齐工具 | transcript / subtitle 对齐质量提升 | 可评估，优先看能否复用现有 transcript 边界 |
| 纯音频 DSP 库 | time-stretch / pitch / meter | 不否定，但优先级低于上面三类 |

### 明确不建议现在做的事

- 不建议现在引入第二套独立于 `edit.json` 的 CLI 计划模型
- 不建议为 Desktop 预先发明 UI 专用 JSON
- 不建议把“交互壳需求”提前变成新的 Core/CLI breaking change
- 不建议为了“功能看起来更丰富”而引入多个新外部依赖

## 为 Desktop 预留的稳定边界

未来 Desktop 如果启动，第一版应只消费这些对象：

1. 模板目录 / 插件目录发现结果
2. `edit.json`
3. `validate-plan` 结果
4. `mix-audio --preview` / `render --preview` 的 `executionPreview`
5. `doctor` 的依赖状态结果
6. 统一 command envelope 与失败 envelope
7. 执行日志与 produced paths

这意味着当前 CLI / Core 在继续演进时，应优先保证以下预留条件成立：

| 预留条件 | 含义 |
| --- | --- |
| `edit.json` 仍是唯一计划边界 | Desktop 不得发明第二套编辑模型 |
| preview / validate 输出继续结构化 | Desktop 只做展示和参数收集，不复制规则 |
| 模板 / 插件 discovery 结果稳定 | Desktop 不自己扫描和解释 schema |
| 执行日志统一来自 `Core.Execution` | Desktop 只展示，不启动旁路进程 |
| 依赖检查统一来自 `doctor` / Core 探测 | Desktop 不自行判断 ffmpeg/whisper/demucs |

## Desktop MVP 预留原则

即使后续正式启动 `D1`，第一轮也只建议承诺这一个闭环：

1. 导入素材
2. 选择模板
3. 编辑少量参数
4. 生成 / 校验 `edit.json`
5. 预览执行计划
6. 替换素材 / 挂载字幕 / 接回配音
7. 执行并查看日志 / 结果

明确不进第一轮的内容：

- 自建时间线编辑器
- 第二套 preset / template 编辑系统
- UI 层直接拼接 `ffmpeg` / `ffprobe`
- 独立于 CLI / Core 的任务执行器
- 复杂设置中心、历史数据库、多窗口批调度

## 本轮建议的实际推进顺序

如果接下来继续按“丰富 CLI、为 Desktop 做预留”推进，建议顺序固定为：

1. 先补 `src/OpenVideoToolbox.Desktop/MODULE.md`，锁 Desktop 未来边界
2. 再补 plan / preview / logs 这类更利于 UI 复用的 CLI 输出能力
3. 再考虑批量入口和工作目录组织
4. 最后才重判是否正式启动 `D1`

## 完成标准

当以下条件同时满足时，可认为“继续丰富 CLI，并为 Desktop 做预留”这条路线已经站稳：

1. 基础功能列表已固定，不再靠口头描述仓库能力面
2. CLI 下一阶段增强方向有明确优先级，而不是随机加命令
3. Desktop 的允许边界和禁止边界已落文档
4. `edit.json`、preview、validate、doctor、execution logs 继续保持单一 canonical owner

## 关联文档

- `docs/roadmap.md`
- `docs/ARCHITECTURE_GUARDRAILS.md`
- `docs/plans/2026-04-22-d1-desktop-mvp-start-checklist.md`
- `src/OpenVideoToolbox.Cli/MODULE.md`
