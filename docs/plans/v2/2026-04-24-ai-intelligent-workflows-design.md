# AI 智能工作流设计

> 状态：设计完成，待实施
> 依赖：edit.json Schema v2、模板效果库
> 前置文档：[edit-json-schema-v2-design.md](2026-04-24-edit-json-schema-v2-design.md)

## 1. 模块概览

三个子模块按优先级排序：

| 子模块 | 功能 | 与 v1 关系 |
|--------|------|-----------|
| auto-cut-silence | 消费静音检测结果，生成去除静音的剪辑计划 | 新命令 |
| validate-plan 增强 | 深度校验素材时长、效果参数、跨轨道引用 | 增强现有 |
| resolve-assets | 查询语句匹配本地素材，替换为实际路径 | 新命令 |

## 2. auto-cut-silence

### 2.1 流程

```
detect-silence → silence.json
                     ↓
            auto-cut-silence [--template <id>] [--output <path>] [--clips-only]
                     ↓
              edit.json（完整）或 clips 片段（JSON）
```

### 2.2 核心算法

输入：silence.json 的静音区间列表 + 源素材总时长

```
静音区间: [(3.5, 5.2), (8.0, 10.5)]
源时长:   15.0s

反转 → 非静音区间: [(0, 3.5), (5.2, 8.0), (10.5, 15.0)]
应用 padding → 每段前后各保留 0.2s: [(0.2, 3.3), (5.0, 7.8), (10.3, 14.8)]
                          实际 clamp 到 [0, 15.0]
应用 mergeGap → 间距 < 0.5s 的合并
应用 minSegmentDuration → 丢弃 < 1.0s 的片段
输出 → clips 列表
```

### 2.3 参数

| 参数 | CLI 标志 | 类型 | 默认值 | 说明 |
|------|---------|------|--------|------|
| padding | `--padding` | float | 0.2 | 每段前后保留缓冲（秒） |
| minSegmentDuration | `--min-duration` | float | 1.0 | 低于此时长的片段丢弃（秒） |
| mergeGap | `--merge-gap` | float | 0.5 | 间距低于此值合并（秒） |
| template | `--template` | string | null | 生成完整 plan 时使用的模板 ID |
| output | `--output` | string | null | 输出 plan 路径 |
| clipsOnly | `--clips-only` | flag | false | 只输出 clips 片段 |

### 2.4 输出模式

**完整模式**（默认）：生成包含 source、clips、output 的完整 edit.json，可直接 render。

```jsonc
// auto-cut-silence --silence signals/silence.json --template shorts-basic --output plan.json
{
  "schemaVersion": 2,
  "source": { "inputPath": "raw/interview.mp4" },
  "template": { "id": "shorts-basic", "source": { "kind": "builtIn" } },
  "clips": [
    { "id": "c1", "in": "00:00:00", "out": "00:00:03.300" },
    { "id": "c2", "in": "00:00:05.000", "out": "00:00:07.800" },
    { "id": "c3", "in": "00:00:10.300", "out": "00:00:14.800" }
  ],
  "output": { "path": "output/final.mp4", "container": "mp4" }
}
```

**clips-only 模式**：只输出 clips 数组，用于合并到已有 plan。

```jsonc
// auto-cut-silence --silence signals/silence.json --clips-only
[
  { "id": "c1", "in": "00:00:00", "out": "00:00:03.300" },
  { "id": "c2", "in": "00:00:05.000", "out": "00:00:07.800" },
  { "id": "c3", "in": "00:00:10.300", "out": "00:00:14.800" }
]
```

### 2.5 类型定义

```
AutoCutSilenceRequest
├── silencePath     : string        — 静音检测结果路径
├── sourcePath      : string        — 源素材路径（用于获取总时长）
├── sourceDuration? : TimeSpan      — 可选，显式指定（避免 ffprobe 调用）
├── padding         : double        — 0.2
├── minDuration     : double        — 1.0
├── mergeGap        : double        — 0.5
├── clipsOnly       : bool          — false
├── templateId?     : string        — 生成完整 plan 时使用
└── outputPath?     : string        — 输出路径

AutoCutSilenceResult
├── clips           : EditClip[]
├── removedSegments : (TimeSpan start, TimeSpan end)[]  — 被移除的静音段
├── originalDuration: TimeSpan
└── resultDuration  : TimeSpan
```

## 3. validate-plan 增强

### 3.1 校验层级

```
基础校验（默认，纯文件系统 + JSON 结构）
  ├── JSON 结构合法性
  ├── 必填字段存在性
  ├── ID 唯一性
  ├── 路径存在性（source、audioTrack、artifact）
  ├── 时间范围合法性（out > in、start >= 0）
  ├── 效果 type 注册检查（unknown → warning）
  ├── 效果参数 schema 校验
  ├── 轨道 kind 合法性
  ├── 转场 duration <= clip 时长
  └── 跨轨道引用存在性

深度校验（--deep，调用 ffprobe）
  ├── 基础校验全部
  ├── clip in/out 不超出源素材时长
  ├── 分辨率信息一致性
  └── 帧率信息一致性
```

### 3.2 结构化错误码

| 错误码 | 严重度 | 触发条件 | AI 自修正建议 |
|--------|--------|---------|-------------|
| `ERR_SOURCE_NOT_FOUND` | error | clip.src 文件不存在 | 检查路径拼写或运行 resolve-assets |
| `ERR_CLIP_RANGE_EXCEEDS_SOURCE` | error | clip in/out 超出素材时长（--deep） | 缩小 in/out 范围到素材时长内 |
| `ERR_EFFECT_TYPE_UNKNOWN` | warning | effect.type 未在注册表中找到 | 运行 list effects 查看可用类型 |
| `ERR_EFFECT_PARAM_INVALID` | error | 参数类型/范围/required 校验失败 | 查看效果参数 schema |
| `ERR_EFFECT_PARAM_MISSING` | error | 缺少 required 参数 | 补充参数 |
| `ERR_TRANSITION_EXCEEDS_CLIP` | error | transition duration > clip 时长 | 减小 transition duration |
| `ERR_TRACK_REFERENCE_NOT_FOUND` | error | auto_ducking reference 指向不存在的 track | 检查 track id |
| `ERR_FONT_NOT_FOUND` | error | text_overlay fontFile 不存在 | 使用系统字体或指定正确路径 |
| `ERR_TRACK_KIND_INVALID` | error | track.kind 不是 video/audio | 使用 "video" 或 "audio" |
| `ERR_DUPLICATE_CLIP_ID` | error | clip id 在轨道内或跨轨道重复 | 使用唯一 id |
| `ERR_OVERLAPPING_CLIPS` | warning | 同轨道 clip 时间线重叠（无转场） | 添加转场或调整 start |
| `WARN_UNUSED_TRACK` | warning | track 存在但没有任何 clip | 移除空轨道 |

### 3.3 校验结果增强

```jsonc
{
  "issues": [
    {
      "severity": "error",
      "path": "timeline.tracks[0].clips[1].effects[0]",
      "code": "ERR_EFFECT_PARAM_INVALID",
      "message": "参数 'brightness' 值 2.5 超出范围 [-1.0, 1.0]",
      "suggestion": "将 brightness 调整到 [-1.0, 1.0] 范围内"
    }
  ],
  "isValid": false,
  "stats": {
    "totalChecks": 42,
    "errors": 1,
    "warnings": 0
  }
}
```

每个 issue 新增 `suggestion` 字段，给出人类/AI 可操作的修正建议。

## 4. resolve-assets

### 4.1 命令

| 命令 | 说明 |
|------|------|
| `index-assets <directory>` | 生成/更新素材索引文件 |
| `resolve-assets <plan.json>` | 替换 plan 中的查询语句为实际路径 |

### 4.2 素材索引

#### 索引文件格式 (`asset-index.json`)

```jsonc
{
  "version": 1,
  "directory": "/path/to/raw",
  "indexedAt": "2026-04-24T10:30:00Z",
  "fileCount": 42,
  "files": [
    {
      "path": "raw/landscape_01.mp4",
      "duration": 12.5,
      "width": 1920,
      "height": 1080,
      "frameRate": 30.0,
      "hasAudio": true,
      "nameTokens": ["landscape", "01"],
      "extension": ".mp4",
      "fileSize": 52428800
    }
  ]
}
```

#### 索引策略

- **预建索引**：`index-assets <dir>` 扫描目录，对每个媒体文件调用 ffprobe，生成索引
- **自动失效**：检测目录 mtime 变化，索引过期时自动重建
- **按需 fallback**：索引文件不存在时，resolve-assets 自动扫描目录（内存索引，不持久化），并提示用户运行 index-assets 加速
- **索引位置**：`<directory>/.ovt-asset-index.json`，默认 gitignore

#### nameTokens 生成规则

```
"landscape_mountain_sunset_01.mp4"
→ 拆分分隔符 [_\-\.] → ["landscape", "mountain", "sunset", "01"]
→ 过滤纯数字序号 → ["landscape", "mountain", "sunset"]
→ 全小写
```

### 4.3 查询语法

edit.json 中 clip.src 支持查询对象：

```jsonc
{
  "id": "clip_01",
  "src": {
    "query": {
      "directory": "raw/",
      "nameTokens": ["landscape", "mountain"],
      "nameMatch": "any",
      "minDuration": 2.0,
      "maxDuration": 60.0,
      "extensions": [".mp4", ".mov"],
      "minResolution": "1080p",
      "hasAudio": true,
      "pick": "best"
    }
  }
}
```

#### 查询参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| directory | string | plan 所在目录 | 搜索范围 |
| nameTokens | string[] | [] | 文件名 token 匹配列表 |
| nameMatch | "any" / "all" | "any" | any=任一 token 匹配, all=全部匹配 |
| minDuration | float? | null | 最短时长（秒） |
| maxDuration | float? | null | 最长时长（秒） |
| extensions | string[] | [] | 文件格式过滤 |
| minResolution | string? | null | "720p"/"1080p"/"4k"，内部换算为最小高度 |
| hasAudio | bool? | null | 是否要求有音轨 |
| pick | string | "first" | 选取策略 |

#### pick 策略

| 策略 | 说明 |
|------|------|
| `first` | 第一个匹配结果 |
| `random` | 随机选取 |
| `longest` | 时长最长的 |
| `shortest` | 时长最短的 |
| `best` | 综合评分最优 |

**best 评分算法：**

```
score = nameTokenHitRate * 0.4
      + resolutionFitScore * 0.3
      + durationPreferenceScore * 0.3

nameTokenHitRate = 命中 token 数 / 查询 token 数
resolutionFitScore = min(queryHeight, actualHeight) / max(queryHeight, actualHeight)
durationPreferenceScore = 1.0 - abs(actualDuration - idealDuration) / idealDuration
  (idealDuration 取 minDuration 和 maxDuration 的中点)
```

### 4.4 resolve-assets 流程

```
读取 plan.json
  │
  ├── 遍历所有 timeline.tracks[*].clips[*].src
  │   │
  │   ├── src 是 string → 跳过（已解析）
  │   └── src 是 object（含 query）→ 执行查询
  │       │
  │       ├── 查找索引文件 (.ovt-asset-index.json)
  │       │   ├── 存在且未过期 → 使用索引
  │       │   └── 不存在 → fallback 扫描目录
  │       │
  │       ├── 执行过滤（nameTokens, duration, resolution, ...）
  │       │
  │       └── 应用 pick 策略选取
  │           ├── 命中 → 替换 src 为实际路径
  │           └── 未命中 → 报错 ERR_ASSET_QUERY_NO_MATCH
  │
  └── 输出新的 plan.json（查询已替换为路径）
```

### 4.5 自动集成

`run` 命令（render 的执行入口）在渲染前自动检测 plan 中是否有未解析的查询：

```
render/edit plan → 检测到 src 含 query → 自动调用 resolve-assets → 再渲染
```

用户无需手动执行 resolve-assets，除非想预检匹配结果。

### 4.6 类型定义

```
AssetIndex
├── version          : int
├── directory        : string
├── indexedAt        : DateTime
├── fileCount        : int
└── files            : AssetEntry[]

AssetEntry
├── path             : string
├── duration         : double?
├── width            : int?
├── height           : int?
├── frameRate        : double?
├── hasAudio         : bool
├── nameTokens       : string[]
├── extension        : string
└── fileSize         : long?

AssetQuery
├── directory?       : string
├── nameTokens?      : string[]
├── nameMatch?       : "any" | "all"
├── minDuration?     : double
├── maxDuration?     : double
├── extensions?      : string[]
├── minResolution?   : string
├── hasAudio?        : bool
└── pick?            : string

ResolveAssetsResult
├── resolvedPlan     : EditPlan
├── resolutions      : AssetResolution[]

AssetResolution
├── clipId           : string
├── trackId          : string
├── query            : AssetQuery
├── matchedPath      : string?       — null 表示未匹配
├── matchScore       : double?
├── candidateCount   : int
└── error            : string?       — ERR_ASSET_QUERY_NO_MATCH 等
```

## 5. 本阶段交付物

### 5.1 auto-cut-silence

1. `AutoCutSilenceRequest` / `AutoCutSilenceResult` 类型
2. 静音反转算法（padding、mergeGap、minDuration）
3. `auto-cut-silence` CLI 命令（完整模式 + clips-only 模式）
4. 单元测试

### 5.2 validate-plan 增强

1. v2 校验规则（效果参数、转场、跨轨道引用）
2. 结构化错误码（含 suggestion 字段）
3. `--deep` 模式（调用 ffprobe 校验素材时长）
4. 校验结果 stats 统计
5. 单元测试

### 5.3 resolve-assets

1. `AssetIndex` / `AssetEntry` / `AssetQuery` 类型
2. `index-assets` 命令（索引生成 + mtime 失效检测）
3. `resolve-assets` 命令（查询执行 + 路径替换）
4. best 评分算法
5. 按需扫描 fallback
6. run 命令集成（自动解析）
7. 单元测试

## 6. 实施护栏

### 6.1 resolve-assets 与批量渲染的联动

批量渲染中，slot 可能绑定查询语句（而非固定路径）。不同数据行的 `${var}` 替换会产生不同的查询条件，resolve-assets 必须为每行数据独立执行查询。

**关键约束：**
- 索引查询必须幂等 — 同一查询条件多次执行返回相同结果
- 索引查询必须高效 — N 行批量渲染不应导致 N 次全库扫描
- 解决方案：首次查询时加载索引到内存，后续查询复用内存索引

**批量渲染中的 resolve 流程调整：**

```
run-batch
  ├── 1. 加载素材索引到内存（一次）
  ├── 2. 对每一行数据：
  │   ├── 替换 ${var}（包括 query 中的变量）
  │   ├── 用内存索引执行 query → 匹配素材
  │   ├── 替换 clip.src
  │   └── 移除未绑定的 slot clip/track
  └── 3. 渲染每一行
```

每行的 resolve 结果记录在 summary.json 中：

```jsonc
{
  "index": 0,
  "status": "success",
  "resolutions": [
    { "clipId": "img_clip", "query": { "nameTokens": ["watch"] }, "matchedPath": "assets/watch.jpg" }
  ]
}
```
