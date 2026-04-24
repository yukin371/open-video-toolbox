# v2 渲染引擎设计

> 状态：设计完成，待实施
> 依赖：edit.json Schema v2、模板效果库
> 前置文档：[edit-json-schema-v2-design.md](2026-04-24-edit-json-schema-v2-design.md)、[template-effects-library-design.md](2026-04-24-template-effects-library-design.md)

## 1. 架构决策

| 决策 | 选择 | 理由 |
|------|------|------|
| Builder 隔离 | 独立 `FfmpegTimelineRenderCommandBuilder` | v2 filter graph 逻辑与 v1 完全不同，分离职责 |
| Filter graph 构建 | 模板化字符串拼接（方案 A） | 可调试、可导出、AI 可读，符合"依赖外部库"原则 |
| 性能瓶颈预案 | 仅当 CLI 启动延迟成为实测瓶颈时才考虑 libavfilter 绑定 | YAGNI |
| 转场重叠策略 | 重叠区 xfade 合成 | 标准 NLE 行为，输出时长 = sum(clips) - sum(overlap) |

## 2. 渲染三阶段

```
阶段 1: Track 内部处理（每条轨道独立）
  clip 切段(trim) → clip 效果(filter chain) → 转场(xfade/concat) → track 级效果
  输出: 每条轨道一个带标签的流 [v_trackN] 或 [a_trackN]

阶段 2: 全局合成（跨轨道）
  video 轨道: overlay 叠加（底层 base，上层依次覆盖）
  audio 轨道: amix 混合
  输出: [v_out] 和 [a_out]

阶段 3: 输出
  -map "[v_out]" -map "[a_out]" → 输出文件
```

## 3. Filter Graph 映射表

| 概念 | FFmpeg 映射 | 说明 |
|------|-----------|------|
| 单 clip 切段 | `trim` + `setpts=PTS-STARTPTS` | 从源素材取时间段 |
| clip 级简单效果 | filter chain 串联 | 接在 trim 后，由模板生成 |
| clip 级关键帧效果 | `zoompan` / LUT / `sendcmd` | 由 IEffectExecutor 生成 |
| 同轨 clip 拼接（无转场） | `concat=n=M:v=1:a=0` | 直接拼接 |
| 同轨 clip 转场 | `xfade=transition=fade:duration=0.5:offset=N` | 相邻 clip 交叉过渡 |
| 多 video 轨道合成 | `overlay=x:y` | 底→顶依次叠加 |
| 多 audio 轨道合成 | `amix=inputs=N` | 所有音频轨混合 |
| track 级效果 | filter chain 串联在 track 输出后 | 整条轨道统一处理 |

## 4. 标签命名约定

filter_complex 中所有流标签遵循统一命名，便于调试和错误定位：

```
输入流:   [0:v], [0:a], [1:v], [1:a]       — FFmpeg 自动分配
clip 流:  [v_t{trackId}_c{clipId}]          — 如 [v_tmain_vc1]
track 流: [v_t{trackId}]                    — 如 [v_tmain]
合成流:   [v_out], [a_out]                  — 最终输出
```

音频轨标签前缀用 `a_` 替代 `v_`。

## 5. 完整生成示例

### 5.1 输入 edit.json（timeline 部分）

```jsonc
{
  "timeline": {
    "resolution": { "w": 1920, "h": 1080 },
    "frameRate": 30,
    "tracks": [
      {
        "id": "main", "kind": "video",
        "clips": [
          {
            "id": "c1", "src": "raw/a.mp4",
            "in": "00:00:00", "out": "00:00:05", "start": "00:00:00",
            "effects": [{ "type": "brightness_contrast", "brightness": 0.1 }],
            "transitions": { "out": { "type": "fade", "duration": 0.5 } }
          },
          {
            "id": "c2", "src": "raw/b.mp4",
            "in": "00:00:00", "out": "00:00:05", "start": "00:00:04.5",
            "effects": [],
            "transitions": { "in": { "type": "fade", "duration": 0.5 } }
          }
        ]
      },
      {
        "id": "logo", "kind": "video",
        "clips": [
          {
            "id": "logo1", "src": "assets/logo.png",
            "start": "00:00:00",
            "effects": [{ "type": "scale", "from": [1.0, 1.0], "to": [0.5, 0.5] }]
          }
        ]
      },
      {
        "id": "bgm", "kind": "audio",
        "clips": [
          {
            "id": "bgm1", "src": "audio/bgm.wav",
            "start": "00:00:00",
            "effects": [{ "type": "volume", "level": 0.3 }]
          }
        ]
      }
    ]
  }
}
```

### 5.2 生成的 FFmpeg 命令

```bash
ffmpeg \
  -i raw/a.mp4 -i raw/b.mp4 -i assets/logo.png -i audio/bgm.wav \
  -filter_complex "
    # ── 阶段1: Track 内部 ──

    # main track - clip c1: trim + brightness
    [0:v]trim=0:5,setpts=PTS-STARTPTS,eq=brightness=0.1[v_tmain_vc1];

    # main track - clip c2: trim (no effects)
    [1:v]trim=0:5,setpts=PTS-STARTPTS[v_tmain_vc2];

    # main track - xfade with fade transition (0.5s overlap)
    [v_tmain_vc1][v_tmain_vc2]xfade=transition=fade:duration=0.5:offset=4.5[v_tmain];

    # logo track - scale animation (hardcoded executor)
    [2:v]scale=960:540[v_tlogo];

    # bgm track - volume
    [3:a]volume=0.3[a_tbgm];

    # ── 阶段2: 全局合成 ──
    [v_tmain][v_tlogo]overlay=10:10[v_out];
  " \
  -map "[v_out]" -map "[a_tbgm]" \
  -c:v libx264 -c:a aac \
  output/final.mp4
```

## 6. 转场重叠计算

当相邻 clip 有转场时，`xfade` 的 `offset` 参数计算：

```
offset = 前一 clip 在时间线上的 start + 前一 clip 时长 - transition duration
```

输出总时长 = sum(clip 时长) - sum(转场重叠)

规则：
- 如果两个相邻 clip 之间既有 out transition 又有 in transition，取较短的 duration 作为重叠区
- 如果转场 duration 超过 clip 时长，校验阶段报错
- 无转场的 clip 之间用 concat 拼接

## 7. Builder 类设计

```
FfmpegTimelineRenderCommandBuilder
├── Build(EditPlan plan) → FfmpegCommand
│   ├── BuildInputArgs(timeline)        — 收集所有 -i 参数
│   ├── BuildFilterComplex(timeline)    — 构建三阶段 filter graph
│   │   ├── ProcessTrack(track)         — 阶段1: 单轨道处理
│   │   │   ├── ProcessClip(clip)       — 单 clip: trim + effects
│   │   │   ├── ApplyTransitions(clips) — 转场拼接
│   │   │   └── ApplyTrackEffects(track)— 轨道级效果
│   │   ├── CompositeVideo(tracks)      — 阶段2: video overlay
│   │   └── CompositeAudio(tracks)      — 阶段2: audio amix
│   └── BuildOutputArgs(plan.Output)    — -map + 编码参数
```

## 8. 效果集成

Builder 通过 EffectRegistry 获取效果定义：

```
对每个 clip.effects 中的 effect:
  1. 查找 EffectRegistry.Get(effect.type)
  2. 若 ffmpegTemplates 非空 → 模板引擎替换参数 → filter 片段
  3. 若 ffmpegTemplates 为 null → 查找 IEffectExecutor → 生成 filter 片段
  4. 按 effects 数组顺序串联为 filter chain
```

## 9. 错误处理

| 错误场景 | 处理方式 |
|---------|---------|
| clip src 文件不存在 | 阶段1前校验，返回结构化错误 |
| effect type 未注册 | 生成 warning，跳过该效果（不阻塞渲染） |
| 转场 duration > clip 时长 | 校验阶段报错 ERR_TRANSITION_EXCEEDS_CLIP |
| 轨道间引用不存在（如 auto_ducking reference） | 校验阶段报错 ERR_TRACK_REFERENCE_NOT_FOUND |
| FFmpeg 进程执行失败 | 捕获 stderr，解析为结构化错误返回 |

## 10. 与 v1 的共存

render 命令的分发逻辑：

```csharp
if (plan.Timeline is not null)
    // v2 路径: FfmpegTimelineRenderCommandBuilder
else
    // v1 路径: FfmpegEditPlanRenderCommandBuilder (零修改)
```

validate-plan 命令同理，根据 timeline 存在与否分别校验。

## 11. 本阶段交付物

1. `FfmpegTimelineRenderCommandBuilder` — v2 渲染命令构建
2. `TimelineFilterGraphBuilder` — filter_complex 三阶段构建器
3. `FfmpegEffectTemplateEngine` — 模板参数替换引擎
4. 集成 EffectRegistry 和 IEffectExecutor
5. render 命令分发逻辑
6. 错误处理和结构化错误码
7. 单元测试（fake process runner）和集成测试
8. 现有 v1 测试全部通过

## 10. 实施护栏

### 10.1 Filter 错误定位

FFmpeg 原生的 filter_complex 错误信息极难追溯来源。Builder 必须为每个生成的 filter 片段附加来源标注，供日志和错误报告使用。

**标注机制：**

```
生成的 filter_complex 中每个逻辑段落前插入注释：
  # [clip:vc1, effect:fade] fade=t=in:d=0.5:alpha=1

错误报告格式：
  {
    "code": "ERR_FILTER_BUILD_FAILED",
    "clipId": "vc1",
    "effectType": "fade",
    "filterIndex": 3,
    "ffmpegError": "Invalid duration 0.5",
    "message": "[clip:vc1, effect:fade] 生成 filter 失败: Invalid duration 0.5"
  }
```

**实现方式：**
- Builder 内部维护 `(filterString, clipId, effectType)` 三元组
- 生成 filter_complex 时插入 FFmpeg 注释（`# ...`）
- 错误解析时从 stderr 中提取注释上下文，关联到具体 clip 和 effect
- 对 AI 代理：错误信息包含 clipId 和 effectType，可直接定位并修正
