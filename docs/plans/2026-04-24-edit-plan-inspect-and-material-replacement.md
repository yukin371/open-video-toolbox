# `edit.json` inspect 与素材替换设计

最后更新：2026-04-24

## 目的

在继续扩 CLI 能力之前，先把一个高频但当前仍不顺手的场景收口：

- 我现在这个 `edit.json` 里到底挂了哪些素材
- 哪些素材是可替换的
- 哪些引用已经丢失、缺失或未绑定
- 我如何在不手改整份 JSON 的前提下，把新的字幕、旁白、BGM 或 supporting signal 接回现有计划

本文档只做设计，不直接引入实现。

## 结论

当前最合适的下一步不是直接接 TTS、变音或再加一批零散命令，而是先补一层稳定的：

1. `inspect-plan`
2. 受控的 plan 内素材替换能力

原因很直接：

- 这能立刻解决人工和外部 AI 都会遇到的“看不清 / 不敢改 / 改完不放心”问题
- 这层能力未来可被 Desktop 直接消费
- 这能为字幕挂载、外部 TTS 接回、外部 voice conversion 接回提供统一落点
- 这比一开始开放任意 JSON patch 更安全，也更符合 `Core.Editing` 的 canonical owner 边界

## 当前边界

### 已存在的基础

- `OpenVideoToolbox.Core/Editing` 已经是 `edit.json` 的唯一 canonical owner
- `EditPlan` 当前已稳定承载：
  - `source.inputPath`
  - `clips`
  - `audioTracks`
  - `artifacts`
  - `transcript`
  - `beats`
  - `subtitles`
  - `output`
- `EditPlanValidator` 已经覆盖 plan 级校验与 issue path 语义
- `EditPlanPathResolver` 已经定义了 plan 路径解析规则
- `validate-plan` 已经提供稳定 envelope
- `render --preview` / `mix-audio --preview` 已经提供执行级 preview

### 不能做的事

- 不能在 CLI 层发明第二套 plan schema
- 不能先做通用 `patch-plan` / `set-json-path`
- 不能把替换逻辑做成字符串级 JSON 改写
- 不能让 Desktop 成为新的 plan 语义 owner
- 不能把 TTS / voice conversion provider 直接塞进仓库

## 用户问题

当前高频工作流里，最不顺手的是这几类：

1. 已有 `edit.json`，但不知道里面有哪些素材可以换
2. 已有 `edit.json`，但想把 BGM、字幕、配音替换成新文件时，必须手改 JSON
3. 外部已经产出了 `transcript.json`、`srt`、`tts.wav` 或变音文件，但缺少稳定的“接回计划”入口
4. 计划本身不一定完全健康，但用户仍希望先修其中一个素材引用，而不是被全局校验完全阻塞

这意味着下一层能力必须同时满足：

- 能看清当前绑定状态
- 能显式替换单个目标
- 能返回替换后的校验结果
- 但不能因为 plan 里存在无关问题，就完全阻断局部修复

## 设计目标

- 给 `edit.json` 提供稳定的 inspect 摘要输出
- 给 plan 内已有材料提供显式、可审计、可测试的替换入口
- 尽量复用现有 `validate-plan` issue path 与路径解析语义
- 为 future Desktop 提供可直接消费的材料清单与替换目标模型
- 为字幕挂载、配音接回、外部 AI 编排提供统一边界

## 非目标

- 不在第一版里做任意字段修改器
- 不在第一版里做复杂 clip 编辑器
- 不在第一版里做批量 mutation DSL
- 不在第一版里内置 TTS / 配音 / 变音 provider
- 不在第一版里承诺“自动理解内容并猜该换哪条轨道”

## 推荐命令面

### Phase 1: `inspect-plan`

建议新增只读命令：

```text
inspect-plan --plan <edit.json> [--check-files [true|false]] [--plugin-dir <path>] [--json-out <path>]
```

#### 作用

- 输出 plan 摘要
- 输出当前已绑定的素材列表
- 输出可替换目标列表
- 输出缺失 / 未绑定 / 不合法引用提示
- 复用现有 plan 校验结果

#### 建议输出

仍沿用当前 command envelope：

```json
{
  "command": "inspect-plan",
  "preview": false,
  "payload": {
    "planPath": "E:\\work\\edit.json",
    "resolvedBaseDirectory": "E:\\work",
    "checkFiles": true,
    "template": {
      "id": "shorts-captioned",
      "source": {
        "kind": "builtIn",
        "pluginId": null,
        "pluginVersion": null
      }
    },
    "summary": {
      "clipCount": 8,
      "audioTrackCount": 2,
      "artifactCount": 1,
      "hasTranscript": true,
      "hasBeats": false,
      "hasSubtitles": true
    },
    "materials": [
      {
        "targetType": "source",
        "targetKey": "source.inputPath",
        "displayName": "Source Video",
        "path": ".\\input.mp4",
        "resolvedPath": "E:\\work\\input.mp4",
        "exists": true,
        "replaceable": true
      },
      {
        "targetType": "audioTrack",
        "targetKey": "audioTrack:voice-main",
        "id": "voice-main",
        "role": "voice",
        "path": ".\\dub.wav",
        "resolvedPath": "E:\\work\\dub.wav",
        "exists": false,
        "replaceable": true
      },
      {
        "targetType": "artifact",
        "targetKey": "artifact:bgm",
        "slotId": "bgm",
        "kind": "audio",
        "required": false,
        "path": ".\\music.wav",
        "resolvedPath": "E:\\work\\music.wav",
        "exists": true,
        "replaceable": true
      },
      {
        "targetType": "subtitles",
        "targetKey": "subtitles",
        "mode": "sidecar",
        "path": ".\\subtitles.srt",
        "resolvedPath": "E:\\work\\subtitles.srt",
        "exists": true,
        "replaceable": true
      }
    ],
    "replaceTargets": [
      {
        "targetType": "artifact",
        "targetKey": "artifact:bgm",
        "selector": {
          "artifactSlot": "bgm"
        }
      },
      {
        "targetType": "audioTrack",
        "targetKey": "audioTrack:voice-main",
        "selector": {
          "audioTrackId": "voice-main"
        }
      },
      {
        "targetType": "subtitles",
        "targetKey": "subtitles",
        "selector": {
          "singleton": "subtitles"
        }
      }
    ],
    "missingBindings": [
      {
        "targetType": "artifact",
        "slotId": "bgm",
        "reason": "pathMissing"
      }
    ],
    "validation": {
      "isValid": false,
      "issues": []
    }
  }
}
```

#### 关键语义

- `materials` 是当前已经挂在 plan 里的实际引用
- `replaceTargets` 是 CLI 可以稳定接受的显式替换目标
- `missingBindings` 是“用户常需要补材料”的摘要层，不等价于全部校验 issue
- `validation.issues` 继续沿用 `EditPlanValidator` 的 issue path / code 语义，不另起一套

### Phase 2: `replace-plan-material`

建议新增显式 mutation 命令：

```text
replace-plan-material --plan <edit.json> [--write-to <path>] [--in-place [true|false]] --path <new-file> (--source-input | --audio-track-id <id> | --artifact-slot <slotId> | --transcript | --beats | --subtitles) [--subtitle-mode <sidecar|burnIn>] [--path-style <auto|relative|absolute>] [--check-files [true|false]] [--plugin-dir <path>] [--require-valid [true|false]] [--json-out <path>]
```

#### 第一版只支持这些显式 target

- `source.inputPath`
- `audioTracks[*]`，按 `id` 选择
- `artifacts[*]`，按 `slotId` 选择
- `transcript`
- `beats`
- `subtitles`

这里刻意不先做：

- `clips[*]` 的复杂替换
- 按任意 JSON path 改字段
- 基于启发式猜测“哪条 audio track 才是你想换的”

#### 为什么 `audioTrack` 第一版按 `id` 而不是按 `role`

- `role` 可能重复
- `id` 才是 plan 内稳定 selector
- Desktop 未来也更适合直接持有 `targetKey` / `id`

后续如果要支持 `--audio-role voice` 这类更方便的入口，也应建立在 inspect 结果可明确判定唯一性的前提下，而不是先把歧义塞进 CLI。

#### 写回策略建议

- 默认允许对“目标明确”的单个材料做局部替换
- 替换后总是返回一份新的 `validation`
- 默认不要求 plan 在替换前就是全局健康
- `--require-valid true` 才启用“替换后必须整体有效，否则拒绝写回”

原因：

- 用户很多时候正是在修一个已经坏掉的 plan
- 如果命令过度严格，会把最需要的修复入口变成不可用
- 但通过稳定 `validation` 返回，仍能保留审计与自动化能力

#### 路径写回策略建议

建议新增统一 path-style 规则：

- `auto`：默认值
  - 若原字段是相对路径，优先写回相对路径
  - 若原字段是绝对路径，优先维持绝对路径
  - 若目标原本不存在，则优先尝试相对写回；跨盘或无法安全相对化时回退为绝对路径
- `relative`
  - 强制相对 `edit.json` 所在目录写回；无法相对化则报错
- `absolute`
  - 强制写回绝对路径

这样做的原因不是“路径看起来整洁”，而是：

- 保持 plan 可移植性
- 降低把本机路径无意扩散进样例、提交记录或 future Desktop 状态里的风险
- 与现有 `EditPlanPathResolver` 的解析模型自然兼容

#### 建议输出

```json
{
  "command": "replace-plan-material",
  "preview": false,
  "payload": {
    "planPath": "E:\\work\\edit.json",
    "outputPlanPath": "E:\\work\\edit.json",
    "target": {
      "targetType": "artifact",
      "targetKey": "artifact:bgm",
      "selector": {
        "artifactSlot": "bgm"
      },
      "previousPath": ".\\old-bgm.wav",
      "nextPath": ".\\new-bgm.wav",
      "pathStyleApplied": "relative"
    },
    "changed": true,
    "validation": {
      "isValid": true,
      "issues": []
    }
  }
}
```

### Phase 2.5: 常用挂载增强

在 inspect + replace 稳定后，再补“常用但不是任意 mutation”的附加能力：

- `subtitles` 不存在时的 attach
- `transcript` 不存在时的 attach
- `beats` 不存在时的 attach
- 对模板声明 artifact slot 的 upsert

这里依然建议走“显式 target 类型 + 显式 owner 规则”，而不是开放通用 patch。

### Phase 3: 外部 TTS / 配音 / 变音接回

TTS 与 voice conversion 继续坚持外部工具优先：

- 仓库内不内置 provider
- 外部先产出确定性音频文件
- 仓库负责 inspect、替换、校验、preview 与最终 render / mix

这阶段的关键不是“接哪个 AI”，而是先定义好：

- 哪类模板或 plan 约定使用哪类 `audioTrack` / `artifact` 目标承接外部产物
- 哪些 target 是 replace，哪些是 attach / upsert
- 这些语义由谁在 `Core.Editing` 里持有

## Core / CLI / Desktop 边界

### `OpenVideoToolbox.Core/Editing`

应新增并持有：

- inspection 结果模型
- material target 选择模型
- material mutation 请求模型
- plan 内路径写回策略
- 替换 / attach / upsert 的显式业务规则

不应让 CLI 自己做：

- plan 内 selector 解析
- 哪些 target 合法可替换的判断
- 路径相对化 / 绝对化规则
- 对 template artifact slot 的声明判断

### `OpenVideoToolbox.Cli`

应只负责：

- 参数解析
- 命令互斥校验
- 组装 `Core.Editing` 请求
- 输出 command envelope
- 映射 exit code

CLI 不应直接：

- 手改 `JsonNode`
- 复制一份 `EditPlanValidator` 语义
- 单独定义 inspect 摘要结构

### `OpenVideoToolbox.Desktop`

未来只消费：

- inspection 结果
- replace / attach 请求结果
- validation 结果
- render / mix preview

Desktop 不应：

- 发明 UI 私有 selector 语义
- 直接写 plan JSON
- 旁路 `Core.Editing`

## 为什么不先做通用 `patch-plan`

表面上看，通用 patch 更“灵活”；实际上它有三个明显问题：

1. 会绕过 `Core.Editing` 的语义 owner
2. 会让 CLI、外部 AI、未来 Desktop 各自发明不同 patch 习惯
3. 会让测试从“显式业务规则”退化成“字符串改 JSON 后看看有没有炸”

所以这里应优先选择：

- target 明确
- 语义明确
- 输出明确
- 易于 snapshot 和模型测试

而不是一开始追求“什么都能改”。

## 推荐实现顺序

### Step 1

先在 `Core.Editing` 建 inspection 模型与 summary builder：

- 读取 plan
- 解析 template / slot 信息
- 复用 validator
- 输出 `materials` / `replaceTargets` / `missingBindings`

### Step 2

落 `inspect-plan` CLI：

- 只读
- 只消费 `Core.Editing`
- 契约先用快照测试锁住

### Step 3

在 `Core.Editing` 建显式 mutation API：

- target selector
- path-style policy
- replace request / result

### Step 4

落 `replace-plan-material` CLI：

- 单目标 mutation
- 输出替换结果与 post-validation
- 支持 `--write-to` / `--in-place`

### Step 5

补常用 attach / upsert 规则：

- `subtitles`
- `transcript`
- `beats`
- template artifact slot

### Step 6

在规则明确后，再进入：

- 外部 TTS 结果接回
- 外部 dubbing 结果接回
- 外部 voice conversion 结果接回
- batch inspect / replace

## 测试建议

### `Core.Tests`

- inspection summary model tests
- replace target resolution tests
- path-style policy tests
- template artifact slot rule tests
- mutation 后 validator 协作测试

### `Cli.Tests`

- `inspect-plan` 成功 / 失败 envelope snapshot
- `replace-plan-material` 成功 / 失败 envelope snapshot
- `--path-style` 行为测试
- `--require-valid` 行为测试
- plugin template + `--plugin-dir` 协作测试

## 对后续高频工作流的价值

当这层能力到位后，以下场景都会更顺：

- 看清现有计划里哪些素材可换
- 把外部生成的字幕接回 plan
- 把外部 TTS / 配音音频接回 plan
- 用新 BGM 替换旧素材而不手改 JSON
- 在 future Desktop 里做“素材面板 / 替换面板”而不发明第二套模型

## 本文档的最终建议

把下一阶段的设计锚点固定为：

1. 先做 `inspect-plan`
2. 再做受控的 `replace-plan-material`
3. 然后才扩到字幕挂载、TTS 接回、voice conversion 接回和批量替换

这条顺序更符合当前仓库边界，也更接近真实用户最常用的剪辑工作流。
