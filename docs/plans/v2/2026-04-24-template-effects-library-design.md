# 模板效果库设计

> 状态：设计完成，待实施
> 依赖：edit.json Schema v2
> 前置文档：[edit-json-schema-v2-design.md](2026-04-24-edit-json-schema-v2-design.md)

## 1. 架构概览

```
IEffectDefinition (效果描述符，对 AI/插件/内置统一接口)
    ├── type              : 效果标识
    ├── category          : 分类
    ├── parameters        : 参数 schema
    └── ffmpegTemplates?  : FFmpeg filter 模板数组 (简单效果)
         │
         ├── 非 null → 模板引擎替换 {param} → filter chain
         └── null     → IEffectExecutor (硬编码兜底，仅内部使用)
```

核心原则：**描述符统一，执行分层。** 外部（AI、插件、CLI）只接触描述符；渲染引擎内部根据是否有模板决定走模板路径还是执行器路径。

## 2. IEffectDefinition 描述符

```csharp
interface IEffectDefinition
{
    string Type { get; }           // "scale", "fade", "text_overlay" ...
    string Category { get; }       // "transition", "filter", "animation", "text", "audio", "layout"
    string DisplayName { get; }    // 人类可读名
    string? Description { get; }   // 说明
    EffectParameterSchema Parameters { get; }
    FfmpegFilterTemplateSet? FfmpegTemplates { get; }
}
```

### 2.1 EffectParameterSchema

定义效果接受的参数及其约束：

```jsonc
{
  "parameters": {
    "duration": {
      "type": "float",
      "required": true,
      "min": 0.1,
      "max": 10.0,
      "description": "效果持续时间（秒）"
    },
    "easing": {
      "type": "string",
      "enum": ["linear", "ease_in", "ease_out", "ease_in_out"],
      "default": "ease_out",
      "description": "缓动函数"
    }
  }
}
```

参数类型：`int`, `float`, `string`, `bool`, `array`, `object`。

每个参数支持：`required`, `default`, `min`, `max`, `enum`, `description`。

### 2.2 FfmpegFilterTemplateSet

```csharp
record FfmpegFilterTemplateSet
{
    // 通用效果：按顺序拼接为 filter chain
    IReadOnlyList<string>? Filters { get; init; }

    // 转场效果：区分 in/out
    TransitionTemplates? Transitions { get; init; }
}

record TransitionTemplates
{
    string In { get; init; }   // "fade=t=in:d={duration}"
    string Out { get; init; }  // "fade=t=out:d={duration}"
}
```

**模板语法规则：**

- `{param_name}` — 简单字符串替换，引擎替换前做类型检查
- 数组内每个元素是一个独立 filter，按顺序拼接
- 模板无法胜任时（需要跨轨道计算、条件分支、自适应参数），返回 null 走 IEffectExecutor

示例：

```jsonc
// scale 效果
{
  "type": "scale",
  "ffmpegTemplates": {
    "filters": ["scale={width}:{height}:flags={flags}"]
  }
}

// fade 转场
{
  "type": "fade",
  "ffmpegTemplates": {
    "transitions": {
      "in": "fade=t=in:d={duration}:alpha=1",
      "out": "fade=t=out:d={duration}:alpha=1"
    }
  }
}

// auto_ducking — 无模板，走硬编码
{
  "type": "auto_ducking",
  "ffmpegTemplates": null
}
```

## 3. IEffectExecutor（内部硬编码）

仅当 `FfmpegTemplates` 为 null 时使用，渲染引擎内部机制，不对外暴露。

```csharp
interface IEffectExecutor
{
    string[] GenerateFilterChain(
        IReadOnlyDictionary<string, JsonElement> parameters,
        EffectRenderContext context);
}

record EffectRenderContext
{
    TimelineClip Clip { get; init; }
    TimelineTrack Track { get; init; }
    EditPlanTimeline Timeline { get; init; }
    // 可扩展：素材探针信息、全局设置等
}
```

注册方式：渲染引擎内部 `Dictionary<string, IEffectExecutor>`，键为 effect type。

复杂效果注册示例：

| type | 需要硬编码的原因 |
|------|---------------|
| `auto_ducking` | 需要读取参考轨道的音量信息，生成动态 volume filter |
| `text_overlay` | drawtext 参数复杂（字体、阴影、边框、位置计算） |

## 4. 效果注册与发现

```
应用启动
  │
  ├── 1. 注册内置效果（C# 代码，实现 IEffectDefinition）
  │      BuiltInEffectCatalog.RegisterAll()
  │
  ├── 2. 扫描插件目录下的 effects/*.json
  │      解析 JSON → IEffectDefinition → 注册
  │
  └── 3. 效果注册表就绪
         EffectRegistry.Get(type) → IEffectDefinition
         EffectRegistry.GetAll(category?) → IEffectDefinition[]
```

查找规则：内置优先，插件覆盖（同 type 时插件定义替换内置）。

## 5. 内置效果清单

### 5.1 P0 — 首批交付（随效果库发布）

| type | category | 实现方式 | 说明 |
|------|----------|---------|------|
| `fade` | transition | 模板 | 淡入淡出，支持 alpha |
| `dissolve` | transition | 模板 | 交叉溶解 |
| `brightness_contrast` | filter | 模板 | 亮度对比度调节 |
| `gaussian_blur` | filter | 模板 | 高斯模糊，支持区域 |
| `scale` | animation | 模板 | 缩放动画，from/to + easing |
| `pan` | animation | 模板 | 平移动画，from/to 坐标 |
| `text_overlay` | text | 硬编码 | 文字叠加（drawtext） |
| `volume` | audio | 模板 | 音量调节 |
| `fade_audio` | audio | 模板 | 音频淡入淡出 |
| `auto_ducking` | audio | 硬编码 | 自动闪避，参考轨道 |

### 5.2 P1 — 后续扩展

| type | category | 说明 |
|------|----------|------|
| `slide_left` | transition | 向左滑动 |
| `slide_right` | transition | 向右滑动 |
| `zoom_blur` | transition | 缩放模糊转场 |
| `light_flash` | transition | 闪光转场 |
| `vintage_film` | filter | 复古胶片风格 |
| `teal_orange` | filter | 青橙色调 |
| `lut_apply` | filter | 3D LUT 调色 |
| `rotate` | animation | 旋转动画 |
| `typewriter` | text | 打字机效果 |
| `lower_third` | text | 下三分之一字幕条 |
| `compressor` | audio | 动态压缩器 |
| `noise_reduction` | audio | 降噪 |
| `side_by_side` | layout | 左右分屏 |
| `picture_in_picture` | layout | 画中画 |

### 5.3 P0 效果参数详细定义

#### fade (transition)
```json
{
  "type": "fade",
  "category": "transition",
  "displayName": "淡入淡出",
  "parameters": {
    "duration": { "type": "float", "required": true, "default": 0.5, "min": 0.1, "max": 5.0 }
  },
  "ffmpegTemplates": {
    "transitions": {
      "in": "fade=t=in:d={duration}:alpha=1",
      "out": "fade=t=out:d={duration}:alpha=1"
    }
  }
}
```

#### dissolve (transition)
```json
{
  "type": "dissolve",
  "category": "transition",
  "displayName": "交叉溶解",
  "parameters": {
    "duration": { "type": "float", "required": true, "default": 0.5, "min": 0.1, "max": 5.0 }
  },
  "ffmpegTemplates": {
    "transitions": {
      "in": "fade=t=in:d={duration}:alpha=1",
      "out": "fade=t=out:d={duration}:alpha=1"
    }
  }
}
```

#### brightness_contrast (filter)
```json
{
  "type": "brightness_contrast",
  "category": "filter",
  "displayName": "亮度对比度",
  "parameters": {
    "brightness": { "type": "float", "default": 0.0, "min": -1.0, "max": 1.0 },
    "contrast": { "type": "float", "default": 1.0, "min": 0.0, "max": 3.0 }
  },
  "ffmpegTemplates": {
    "filters": ["eq=brightness={brightness}:contrast={contrast}"]
  }
}
```

#### gaussian_blur (filter)
```json
{
  "type": "gaussian_blur",
  "category": "filter",
  "displayName": "高斯模糊",
  "parameters": {
    "sigma": { "type": "float", "required": true, "default": 5.0, "min": 0.1, "max": 50.0 },
    "region": { "type": "object", "description": "模糊区域，省略则全画面" }
  },
  "ffmpegTemplates": {
    "filters": ["boxblur=lr={sigma}:lp={sigma}"]
  }
}
```

#### scale (animation)
```json
{
  "type": "scale",
  "category": "animation",
  "displayName": "缩放动画",
  "parameters": {
    "from": { "type": "array", "required": true, "default": [1.0, 1.0], "description": "起始缩放 [w, h]" },
    "to": { "type": "array", "required": true, "default": [1.0, 1.0], "description": "目标缩放 [w, h]" },
    "duration": { "type": "float", "required": true, "default": 1.0, "min": 0.1 },
    "easing": { "type": "string", "enum": ["linear", "ease_in", "ease_out", "ease_in_out"], "default": "ease_out" }
  }
}
```

> 注：scale 动画需要关键帧插值（zoompan filter），由硬编码处理，ffmpegTemplates 不适用。

#### pan (animation)
```json
{
  "type": "pan",
  "category": "animation",
  "displayName": "平移动画",
  "parameters": {
    "from": { "type": "array", "required": true, "default": [0, 0], "description": "起始位置 [x, y]" },
    "to": { "type": "array", "required": true, "default": [0, 0], "description": "目标位置 [x, y]" },
    "duration": { "type": "float", "required": true, "default": 1.0, "min": 0.1 },
    "easing": { "type": "string", "enum": ["linear", "ease_in", "ease_out", "ease_in_out"], "default": "linear" }
  }
}
```

> 注：pan 动画同样需要关键帧，由硬编码处理。

#### text_overlay (text)
```json
{
  "type": "text_overlay",
  "category": "text",
  "displayName": "文字叠加",
  "parameters": {
    "text": { "type": "string", "required": true },
    "position": { "type": "string", "enum": ["center", "top", "bottom", "top_left", "top_right", "bottom_left", "bottom_right"], "default": "center" },
    "fontSize": { "type": "int", "default": 48 },
    "color": { "type": "string", "default": "#FFFFFF" },
    "fontFile": { "type": "string", "description": "字体文件路径" },
    "borderWidth": { "type": "int", "default": 2 },
    "borderColor": { "type": "string", "default": "#000000" },
    "style": { "type": "string", "enum": ["plain", "pop", "bold"], "default": "plain" }
  }
}
```

> 注：text_overlay 由硬编码处理，drawtext filter 参数组合复杂。

#### volume (audio)
```json
{
  "type": "volume",
  "category": "audio",
  "displayName": "音量调节",
  "parameters": {
    "level": { "type": "float", "required": true, "default": 1.0, "min": 0.0, "max": 5.0, "description": "音量倍数，1.0=原音量" }
  },
  "ffmpegTemplates": {
    "filters": ["volume={level}"]
  }
}
```

#### fade_audio (audio)
```json
{
  "type": "fade_audio",
  "category": "audio",
  "displayName": "音频淡入淡出",
  "parameters": {
    "direction": { "type": "string", "enum": ["in", "out"], "required": true },
    "duration": { "type": "float", "required": true, "default": 0.5, "min": 0.1, "max": 10.0 }
  },
  "ffmpegTemplates": {
    "filters": ["afade=t={direction}:d={duration}"]
  }
}
```

#### auto_ducking (audio)
```json
{
  "type": "auto_ducking",
  "category": "audio",
  "displayName": "自动闪避",
  "parameters": {
    "reference": { "type": "string", "required": true, "description": "参考轨道 ID" },
    "duckTo": { "type": "float", "default": 0.1, "min": 0.0, "max": 1.0 },
    "attack": { "type": "float", "default": 0.1, "description": "降低响应时间（秒）" },
    "release": { "type": "float", "default": 0.3, "description": "恢复响应时间（秒）" }
  }
}
```

> 注：auto_ducking 需要读取参考轨道的音量数据并生成动态 volume envelope，由硬编码处理。

## 6. AI 自发现机制

```
ovt describe effect <type>
```

输出结构化文档，包含：type、category、displayName、description、parameters schema、用例示例。

AI 代理只需一次查询即可构造合法参数，无需硬编码知识。

```
ovt list effects [--category <cat>]
```

列出所有已注册效果，支持按分类过滤。

## 7. 外部 JSON 效果定义格式

插件或社区贡献的效果放在 `effects/<type>.json`：

```jsonc
{
  "type": "sepia",
  "category": "filter",
  "displayName": "怀旧色调",
  "description": "模拟老照片的棕褐色调",
  "parameters": {
    "intensity": { "type": "float", "default": 0.8, "min": 0.0, "max": 1.0 }
  },
  "ffmpegTemplates": {
    "filters": ["colorchannelmixer=.393:.769:.189:0:.349:.686:.168:0:.272:.534:.131"]
  }
}
```

加载规则：
1. 扫描插件目录下 `effects/*.json`
2. 解析并注册到 EffectRegistry
3. 同 type 时插件定义替换内置定义（插件优先）

## 8. 本阶段交付物

1. `IEffectDefinition` 接口及 `EffectParameterSchema`、`FfmpegFilterTemplateSet` 等类型
2. `IEffectExecutor` 接口及 `EffectRenderContext`
3. `EffectRegistry` 注册表（查找、枚举、注册）
4. `BuiltInEffectCatalog` — P0 的 10 个内置效果定义
5. 外部 JSON 效果文件加载器
6. `describe effect` 和 `list effects` CLI 命令
7. 效果参数校验（基于 EffectParameterSchema）
8. 单元测试覆盖

不包含：渲染引擎集成（下一阶段）。

## 9. 实施护栏

### 9.1 P0 效果的批量生成验收

text_overlay 的动态文本排版是数据驱动批量生成的核心验收用例。在数百行数据中，产品名称长度差异可能很大（"充电宝" vs "超高性价比智能降噪无线蓝牙耳机 Pro Max"），必须验证：

- **自动换行**：文本超出画面宽度时自动折行，不截断
- **自适应缩放**：可选策略 — 缩小字号以适配单行，或换行后垂直居中
- **尺寸适配**：image_overlay / scale 效果需支持 contain/cover/fill 三种适配模式

P0 效果参数中应补充这些子参数：

```
text_overlay 新增参数：
  textWrap: "none" | "auto" | "max_lines"    — 换行策略
  maxLines: int?                               — 最大行数（auto 时生效）
  shrinkToFit: bool                            — 超长文本是否自动缩小字号

image/scale 效果新增参数：
  fit: "contain" | "cover" | "fill" | "none"  — 尺寸适配策略
```

**核心验收用例：** 用一个包含 50 行不同长度文本的 CSV 驱动模板渲染，要求 50 个视频中文字均完整可见。
