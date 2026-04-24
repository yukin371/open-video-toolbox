# edit.json Schema v2 设计

> 状态：设计完成，待实施
> 范围：类型定义 + 校验逻辑（渲染引擎不在本阶段）

## 1. 设计决策

| 决策 | 选项 | 理由 |
|------|------|------|
| 升级策略 | 向上兼容增量 | 现有 293 测试、7 模板、47 命令不受冲击 |
| timeline 与 v1 关系 | fallback 共存 | 有 timeline 走 v2 路径，否则走 v1 路径 |
| 转场模型 | 放在 clip 上 | AI 操作更直觉（"给这个片段加淡入"），引擎算重叠 |
| 轨道模型 | video/audio 统一 | 通过 kind 区分，简化 AI 认知成本 |
| 素材引用 | clip 级 src | 缺失时 fallback 到 source.inputPath |
| 实施顺序 | 类型+校验先行 | 数据模型是合约，渲染引擎是实现 |

## 2. v2 EditPlan 完整结构

```jsonc
{
  "schemaVersion": 2,

  // ── v1 字段全部保留，v1 工具照常读取 ──
  "source": { "inputPath": "raw/interview.mp4" },
  "template": { "id": "commentary-captioned", "source": { "kind": "builtIn" } },
  "clips": [
    { "id": "c1", "in": "00:00:00", "out": "00:00:10" },
    { "id": "c2", "in": "00:00:12", "out": "00:00:20" }
  ],
  "audioTracks": [
    { "id": "bgm", "role": "bgm", "path": "audio/bgm.wav", "gainDb": -10 }
  ],
  "artifacts": [],
  "transcript": { "path": "signals/transcript.json" },
  "subtitles": { "mode": "burnIn", "path": "signals/subs.srt" },
  "output": { "path": "output/final.mp4", "container": "mp4" },

  // ── v2 新增：结构化时间线 ──
  "timeline": {
    "duration": "00:00:20",
    "resolution": { "w": 1920, "h": 1080 },
    "frameRate": 30,
    "tracks": [
      {
        "id": "main_video",
        "kind": "video",
        "clips": [
          {
            "id": "vc1",
            "src": "raw/interview.mp4",
            "in": "00:00:00",
            "out": "00:00:10",
            "start": "00:00:00",
            "effects": [
              {
                "type": "scale",
                "from": [0.8, 0.8],
                "to": [1.0, 1.0],
                "duration": 1.0,
                "easing": "ease_out"
              }
            ],
            "transitions": {
              "in": { "type": "fade", "duration": 0.5 }
            }
          },
          {
            "id": "vc2",
            "src": "raw/interview.mp4",
            "in": "00:00:12",
            "out": "00:00:20",
            "start": "00:00:10",
            "effects": [
              {
                "type": "text_overlay",
                "text": "精彩片段",
                "position": "center",
                "fontSize": 48
              }
            ],
            "transitions": {
              "out": { "type": "fade", "duration": 0.5 }
            }
          }
        ],
        "effects": []
      },
      {
        "id": "bgm",
        "kind": "audio",
        "clips": [
          {
            "id": "bgm_01",
            "src": "audio/bgm.wav",
            "start": "00:00:00",
            "effects": [
              {
                "type": "auto_ducking",
                "reference": "main_video",
                "duckTo": 0.1
              }
            ]
          }
        ],
        "effects": [
          { "type": "volume", "level": 0.3 }
        ]
      }
    ]
  }
}
```

## 3. 新增类型定义

### 3.1 EditPlanTimeline

```
EditPlanTimeline
├── duration?       : TimeSpan        // 显式总时长，可选
├── resolution?     : { w, h }        // 输出分辨率
├── frameRate?      : int             // 帧率
└── tracks          : TimelineTrack[] // 轨道列表
```

### 3.2 TimelineTrack

```
TimelineTrack
├── id              : string
├── kind            : "video" | "audio"
├── clips           : TimelineClip[]
├── effects?        : TimelineEffect[] // 轨道级特效
└── muted?          : bool            // 静音/隐藏轨道
```

### 3.3 TimelineClip

```
TimelineClip
├── id              : string
├── src?            : string          // 素材路径，缺失时 fallback source.inputPath
├── in?             : TimeSpan        // 素材入点（视频类必填）
├── out?            : TimeSpan        // 素材出点（视频类必填）
├── start           : TimeSpan        // 时间线上的起始位置
├── duration?       : TimeSpan        // 显式时长（可从 in/out 推算）
├── effects?        : TimelineEffect[] // 片段级特效
└── transitions?    : Transitions     // 入/出转场
```

### 3.4 TimelineEffect

```
TimelineEffect
├── type            : string          // 模板名/效果标识
└── [key: string]   : any             // 该效果类型的参数，全部可选、有默认值
```

TimelineEffect 使用 JsonExtensionData 接收任意参数。type 决定参数含义，校验由效果注册表完成。

### 3.5 Transitions

```
Transitions
├── in?             : Transition      // 入转场
└── out?            : Transition      // 出转场

Transition
├── type            : string          // "fade", "slide_left", "zoom_blur" 等
├── duration        : double          // 秒
└── [key: string]   : any             // 可选参数
```

### 3.6 TimelineResolution

```
TimelineResolution
├── w               : int
└── h               : int
```

## 4. 渲染引擎分支逻辑

```
schemaVersion == 2 && timeline != null
  → v2 路径：使用 timeline.tracks 构建 filter graph
  → 完全忽略顶层 clips 和 audioTracks

否则
  → v1 路径：使用 source + clips + audioTracks
  → 零修改，现有行为不变
```

## 5. 素材引用规则

| clip.src 值 | 解析行为 |
|-------------|---------|
| 缺失/null | 使用顶层 `source.inputPath` |
| `"$source"` | 显式引用顶层 `source.inputPath` |
| 相对路径 | 相对于 edit.json 所在目录解析 |
| 绝对路径 | 直接使用 |

## 6. 特效渲染顺序

clip.effects（片段级）→ track.effects（轨道级）→ 全局合成（多轨道混合）

同一级别内，effects 按数组顺序串联（FFmpeg filter chain）。

## 7. 校验规则扩展

v2 validator 在 v1 校验基础上新增：

- `timeline.tracks` 不能为空（如果 timeline 存在）
- 每个 track 的 `kind` 必须是 "video" 或 "audio"
- 每个 track 内 clip id 唯一
- 全局 clip id 唯一（跨轨道）
- video 类 clip 的 `in`/`out` 必须存在且 out > in
- `start` 必须非负
- effect 的 `type` 必须在已注册的效果列表中（未知 type 产生 warning，不阻塞）
- transition 的 `duration` 必须 > 0 且 <= clip 时长
- auto_ducking 类效果的 `reference` 必须指向已存在的 track id

## 8. 本阶段交付物

1. 新增 record 类型：EditPlanTimeline, TimelineTrack, TimelineClip, Effect, Transitions, Transition, TimelineResolution
2. EditPlan 新增 `Timeline` 可选属性
3. SchemaVersions 新增 V2 = 2
4. EditPlanValidator 扩展 v2 校验规则
5. 单元测试覆盖所有新增类型的序列化和校验
6. 现有 293 个测试全部通过（零破坏）

不包含：渲染引擎改造、模板效果库、导出功能。

## 9. 实施护栏

### 9.1 v1 向后兼容测试集冻结

在效果库和渲染引擎依赖 Schema v2 之前，必须先建立 v1 兼容性守护测试：

- 列出所有 v1 edit.json 的典型 Case（每个内置模板至少一个）
- 自动化测试：v1 plan → v2 引擎读取 → 行为与 v1 引擎一致
- 这些测试在 Schema v2 实施阶段同步建立，作为 CI gate
- 任何导致 v1 plan 渲染结果变化的改动都会被拦截

**迁移 Case 清单（最小集）：**

| Case | 模板 | 验证点 |
|------|------|--------|
| 基础剪切 | shorts-basic | clips in/out 正确切分 |
| 带字幕 | shorts-captioned | subtitle burnIn 正常 |
| 带 BGM | commentary-bgm | audioTracks 混音正确 |
| 带转写 | commentary-captioned | transcript + subtitle 联合 |
| 节拍剪辑 | beat-montage | beats 驱动的 clips 生成 |
| 插件模板 | 任意 plugin 模板 | plugin source 引用正确 |
| 批量渲染 | scaffold-template-batch | manifest 每项正确处理 |
