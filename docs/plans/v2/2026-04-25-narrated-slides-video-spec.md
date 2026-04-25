# 讲解型 / PPT 风格视频能力规格草案

最后更新：2026-04-25

> 状态：规格草案，未立项  
> 阶段归属：`V2-P6-C1` 候选规格  
> 目标：为“讲解型 / 幻灯片型 / 课程型”视频定义一个适合当前仓库边界的最小实现切口

## 1. 背景

当前仓库已经具备：

- `template -> init-plan -> validate-plan -> render`
- `schemaVersion = 2` 的 `timeline` / `effects` / `render --preview`
- 外部配音 / TTS 结果接回：`bind-voice-track`
- 字幕、BGM、批量 workdir 与结构化结果

但当前能力更偏向：

- 对现有视频素材做剪辑、挂载、替换、批量处理
- 基于单主素材生成 `edit.json`

它还没有正式覆盖一类越来越常见的工作流：

- 讲解型视频
- 幻灯片视频
- 类 PPT 配音视频
- 教程 / 课程 / 评测 / 信息整理型视频

这类视频的核心不是“从长视频里剪段落”，而是：

1. 先有讲解结构
2. 再有每一节对应的页面素材或动画素材
3. 再把旁白、字幕、BGM 和页面切换组装成一个可预览、可审计、可批量化的 `edit.json`

## 2. 与外部项目的关系

外部参考对象可以包含类似 `video-podcast-maker` 这类工作流项目，但本仓库只吸收其中可被审计的“视频装配内核”部分。

本仓库明确不引入以下能力作为本规格的一部分：

- 选题研究
- 自动写稿
- 内置 AI provider / SDK
- 内置 TTS 云服务适配
- Remotion Studio 式可视化编辑器
- 一键平台发布
- 把 `.pptx` 直接转成成片的完整 Office 渲染链

换句话说，本规格解决的是：

`外部 AI / 人工已准备好结构化稿件与素材 -> ovt 负责稳定装配成可渲染 plan`

而不是：

`输入一个 topic -> 仓库内部自动完成研究、生成、配音、编辑、发布`

## 3. 目标

### 3.1 本轮目标

为当前仓库补一条最小正式能力：

- 让外部 AI 或人工可以用一份显式 manifest
- 把“章节页面 + 旁白音频 + 字幕 + 可选 BGM”
- 转成 `schemaVersion = 2` 的讲解型 `edit.json`
- 再复用现有 `validate-plan` / `render --preview` / `render`

### 3.2 本轮不做

- 不替换现有 `render` 主路径
- 不引入第二套视频运行时（如 Remotion runtime）
- 不把模板系统升级成完整页面组件 DSL
- 不引入图表渲染后端
- 不引入 `${var}` 深度注入或数据源驱动 `run-batch`
- 不直接支持 `.pptx` / `.keynote` / `.slides` 导入
- 不做平台特化 CTA、封面生成、标题 SEO 自动化

## 4. 定位判断

这项能力应视为：

- `v2` 范围内的新用户能力
- 更接近 `V2-P6` 的“讲解型模板 / 页面装配能力”
- 不是当前 `E2-F*` 的直接延长线

原因：

1. 它不是对现有单主素材剪辑的小修小补
2. 它会引入新的 plan 生成语义
3. 它与数据驱动模板、slot 条件裁剪、图表化页面能力存在天然耦合
4. 它如果做大，很容易滑向第二套模板 / 渲染 / 编辑产品线

因此本规格只定义最小切口，不直接承诺整包实现。

## 5. 典型用户故事

### 5.1 教程讲解

用户已经准备好：

- 每章节一张讲解页或一段预渲染页面动画
- 每章节对应旁白音频
- 一份整视频字幕

希望：

- 生成一个可预览、可校验、可继续微调的 `edit.json`
- 统一渲染成 16:9 或 9:16 成片

### 5.2 AI 辅助课程草稿

外部 AI 先生成：

- `sections.json`
- `voice/*.wav`
- `slides/*.png` 或 `slides/*.mp4`
- `subtitles.srt`

再调用 CLI：

- 生成讲解型 plan
- 预览执行图
- 渲染成片

### 5.3 批量信息视频的前置最小内核

未来如果要进入数据驱动讲解视频，本仓库首先需要一条“单视频讲解型 plan 装配”能力作为基础。否则后续 `${var}`、slot、批量数据行替换都没有可靠落点。

## 6. 推荐的最小切口

本规格推荐的第一阶段不直接碰“数据驱动批量”，而是先做：

### 单视频讲解型 plan 生成

建议新增一条显式 CLI 入口：

```text
init-narrated-plan --manifest <narrated.json> --template <id> --output <edit.json> --render-output <final.mp4>
```

说明：

- 不建议把当前 `init-plan <input>` 直接硬改成“既支持单主素材，也支持无主素材的章节装配”。
- 讲解型视频天然是“section manifest first”，不是“single source first”。
- 单独命令更符合当前 CLI 的确定性边界，也更利于外部 AI 调用。

如果后续坚持复用 `init-plan`，也应在 `V2-P6-C2` 再专门评估，不在本规格阶段直接默认。

## 7. 输入模型

## 7.1 Manifest 文件

建议新增一个显式的讲解型 manifest，而不是复用当前 batch manifest。

示例：

```json
{
  "schemaVersion": 1,
  "video": {
    "id": "rust-vs-go-cli",
    "title": "Rust vs Go for CLI Tools",
    "aspectRatio": "16:9",
    "resolution": { "w": 3840, "h": 2160 },
    "frameRate": 30,
    "output": "exports/final.mp4"
  },
  "template": {
    "id": "narrated-slides-starter"
  },
  "subtitles": {
    "path": "subtitles/podcast.srt",
    "mode": "sidecar"
  },
  "bgm": {
    "path": "audio/bgm.mp3",
    "gainDb": -18
  },
  "sections": [
    {
      "id": "intro",
      "title": "Why compare them",
      "visual": {
        "kind": "video",
        "path": "slides/intro.mp4"
      },
      "voice": {
        "path": "audio/intro.wav"
      }
    },
    {
      "id": "tradeoffs",
      "title": "Tradeoffs",
      "visual": {
        "kind": "video",
        "path": "slides/tradeoffs.mp4"
      },
      "voice": {
        "path": "audio/tradeoffs.wav"
      }
    }
  ]
}
```

## 7.2 第一阶段允许的输入

第一阶段建议只承诺：

- `sections[].visual.kind = "video"`
- `sections[].voice.path = <audio-file>`
- 顶层单份 `subtitles`
- 可选顶层 `bgm`

原因：

1. 当前 `FfmpegTimelineRenderCommandBuilder` 更接近“消费既有媒体输入”，而不是“生成静态页面视频”
2. 直接承诺图片、图表、代码块、流程图，会把范围拉进新的渲染后端
3. 先让“预渲染页面视频 + 旁白装配”跑通，更符合最小实现

## 7.3 后续可扩展输入

以下能力明确后置：

- `visual.kind = "image"` 的静态页持续时长支持
- `visual.kind = "color"` / `title-card` / `quote-card`
- 章节级字幕文件
- 自动依据音频长度回填章节时长
- 章节封面、结尾 CTA、平台特化尾页
- 直接从 Markdown / `.pptx` / 结构化文稿生成视觉页

## 8. 输出模型

命令输出目标仍是标准 `edit.json`，而不是第二套成片描述文件。

### 8.1 输出要求

- `schemaVersion = 2`
- `template.id = narrated-slides-starter`
- `template.planModel = v2Timeline`
- 可被现有 `validate-plan` / `render --preview` / `render` 消费

### 8.2 推荐的 plan 投影

讲解型视频的最小 v2 plan 推荐投影为：

- 一条主视频轨 `main`
  - 每个 section 一个 clip
  - `src` 指向预渲染页面视频
- 一条旁白音轨 `voice`
  - 每个 section 一个 clip
  - `src` 指向该章节配音
- 一条可选 BGM 音轨 `bgm`
  - 可复用顶层 artifact slot 或音轨约定
- 顶层 `subtitles`
  - 继续复用现有 subtitle 引用语义

示意：

```text
timeline
├── track main   (video)
│   ├── section intro      -> slides/intro.mp4
│   └── section tradeoffs  -> slides/tradeoffs.mp4
├── track voice  (audio)
│   ├── intro voice        -> audio/intro.wav
│   └── tradeoffs voice    -> audio/tradeoffs.wav
└── track bgm    (audio, optional)
    └── bgm bed           -> audio/bgm.mp3
```

## 9. 模板与 owner

## 9.1 推荐模板

建议新增首个 built-in v2 模板：

- `narrated-slides-starter`

它的职责应仅限于：

- 固定讲解型视频的基础轨道结构
- 固定最小页面切换与标题样式
- 固定旁白优先、BGM 降噪的默认参数口径

它不应承担：

- 自动写稿
- 自动生成页面视觉
- 自动生成 TTS
- 平台特化发布策略

## 9.2 Canonical owner

- `OpenVideoToolbox.Core.Editing`
  - 讲解型 manifest 到 `EditPlan` 的投影规则
  - built-in 模板定义
  - section 到 timeline clip 的生成规则
- `OpenVideoToolbox.Core.Execution`
  - v2 timeline render 继续作为唯一执行 owner
  - 如未来支持静态图片页，也应在这里统一实现输入适配
- `OpenVideoToolbox.Cli`
  - 只负责新命令参数解析、manifest 装载和 envelope 输出

## 10. 与现有能力的复用关系

本规格应尽量复用：

- `schemaVersion = 2`
- `EditPlanTimeline`
- `TimelineTrack`
- `TimelineClip`
- 现有 `render --preview`
- 现有 `validate-plan`
- 现有字幕 / BGM / voice bind 语义

本规格不应重复发明：

- 第二套 timeline 模型
- 第二套渲染器
- 第二套字幕模型
- 第二套音轨挂载语义

## 11. 验收包要求

如果后续进入正式计划，最小验收包至少必须包含：

1. 一份最小 `narrated.json`
2. 两段最小章节视频素材
3. 两段最小旁白音频
4. 一份可选字幕文件
5. 一条可直接运行的命令链：

```powershell
ovt init-narrated-plan --manifest .\narrated.json --template narrated-slides-starter --output .\edit.v2.json --render-output .\final.mp4
ovt validate-plan --plan .\edit.v2.json --check-files
ovt render --plan .\edit.v2.json --preview
```

6. 预期通过标准：
   - 成功写出 `schemaVersion = 2` 的 `edit.v2.json`
   - `timeline.tracks` 至少包含 `main` 和 `voice`
   - `render --preview` 返回结构化 `executionPreview`
   - 不需要仓库内任何 AI provider / TTS provider 即可完成

## 12. 明确不做的实现方向

以下方向不应作为本规格的第一阶段：

1. 把 Remotion 模板系统直接嵌入当前仓库
2. 为讲解型视频引入 Node 运行时作为正式新 owner
3. 内置 Azure / OpenAI / ElevenLabs / Edge TTS provider
4. 直接承诺图表、代码块、流程图、Lottie 等组件库
5. 直接承诺封面生成、平台上传、SEO 文案生成
6. 直接支持 `.pptx` 解析与页面渲染

这些都可能成为未来外围工具链，但不应成为当前仓库的第一层正式实现。

## 13. 与 `V2-P6` 的关系

这份规格更适合作为 `V2-P6-C1` 的单独主题，主题名建议固定为：

- `讲解型 / narrated-slides 视频模板能力`

它和 `resolve-assets`、`${var}` 批量注入、`data-visualize` 的关系是：

- 不是同一轮整包推进
- 但可以成为后续“数据驱动讲解视频”能力的上游基础

推荐顺序：

```text
先有单视频 narrated-slides plan 装配
  ↓
再评估静态图片页 / title-card / 章节进度条
  ↓
再评估 ${var} / slot / 数据源驱动 batch
  ↓
最后才评估图表渲染与更复杂页面组件
```

## 14. 当前结论

结论不是“把外部讲解视频项目整包搬进来”，而是：

- 在当前仓库内新增一条可审计的讲解型视频装配能力
- 外部 AI 或人工负责研究、写稿、TTS、页面生成
- 仓库只负责把这些确定性输入稳定投影到 `edit.json v2` 并渲染

这样既能覆盖“像 PPT 视频讲解”的真实需求，也不会打破当前仓库的 owner 边界。
