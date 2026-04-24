# 批量生产增强设计

> 状态：设计完成，待实施
> 依赖：edit.json Schema v2、模板效果库
> 前置文档：[edit-json-schema-v2-design.md](2026-04-24-edit-json-schema-v2-design.md)

## 1. 模块概览

| 子模块 | 功能 | 与 v1 关系 |
|--------|------|-----------|
| 数据驱动批量渲染 | 模板 + N 行数据 = N 个视频 | 增强 render-batch |
| data-visualize | 数据图表渲染为视频叠加层 | 新命令/新效果 |

## 2. 数据驱动批量渲染

### 2.1 两层数据注入模型

| 层次 | 作用 | 机制 |
|------|------|------|
| 文本/路径替换 | 修改文字内容、素材路径、效果参数 | `${var}` 占位符 |
| Slot 开关 | 控制整个 clip/track 的启用/禁用 | slot 属性 + required 标志 |

### 2.2 ${var} 字符串替换

**作用范围：**
- text_overlay 的 text 字段
- clip 的 src（动态指定素材路径）
- 任何效果参数值（如 `volume: ${volume_level}`）
- 输出文件名（CLI `--output-name "${product_id}.mp4"`）

**语法：**

| 语法 | 说明 |
|------|------|
| `${column_name}` | 替换为数据行中对应列的值 |
| `${column_name:-default}` | 数据缺失时使用默认值 |
| `$${text}` | 输出字面量 `${text}` |

**替换规则：**

- 数据行中存在对应列 → 替换
- 数据行缺失且无默认值 → 报错 `ERR_VARIABLE_UNRESOLVED`（必填）或保留原值（可选，由模板标记）
- 替换在所有字符串字段上执行，深度遍历 JSON 树

### 2.3 条件性 Slot

**问题：** 模板设计了"产品图片"轨道，但某些数据行没有图片，希望自动隐藏。

**解决方案：** clip 上增加可选 `slot` 字段。

```jsonc
{
  "id": "product_image_track",
  "kind": "video",
  "slot": { "name": "product_img", "required": false },
  "clips": [
    {
      "id": "img_clip",
      "src": "${product_img}",
      "start": "00:00:00",
      "duration": "00:00:03",
      "slot": "product_img"
    }
  ]
}
```

**批量渲染时的行为：**

| slot 数据状态 | required | 行为 |
|--------------|----------|------|
| 数据行提供且有效 | 任意 | 替换 src，clip 正常渲染 |
| 数据行为空/不存在 | false | 移除该 clip；若轨道所有 clip 移除则移除整条 track |
| 数据行为空/不存在 | true | 报错 `ERR_REQUIRED_SLOT_EMPTY`，跳过该数据行 |

**对 AI 的意义：** AI 设计模板时描述"这里有个图片槽位，有图就放，没有就空"。引擎自动裁剪时间线。

### 2.4 CLI 命令

```
ovt run-batch --template <plan.json> --data <data_source> --output-dir <dir>
```

| 参数 | 说明 |
|------|------|
| --template | 模板 plan.json 路径（含 ${var} 占位符和 slot） |
| --data | 数据源文件（CSV 或 JSONL） |
| --output-dir | 输出目录 |
| --output-name | 文件名模式，支持 ${var}（默认 `${_index}.mp4`） |
| --on-error | `skip`（默认）/ `stop` — 单行失败时的行为 |
| --dry-run | 只解析不渲染，输出每行的变量替换结果和 slot 状态 |

### 2.5 数据源格式

**CSV 格式：**

```csv
product_name,price,product_img,volume_level
智能手表,¥1299,assets/watch.jpg,0.8
无线耳机,¥899,,0.5
充电宝,¥199,assets/powerbank.jpg,
```

- 首行为列名，对应 `${var}` 中的 var
- 空单元格视为缺失
- 编码 UTF-8（带 BOM 或不带均可）

**JSONL 格式：**

```jsonl
{"product_name":"智能手表","price":"¥1299","product_img":"assets/watch.jpg","volume_level":"0.8"}
{"product_name":"无线耳机","price":"¥899"}
{"product_name":"充电宝","price":"¥199","product_img":"assets/powerbank.jpg"}
```

- 每行一个 JSON 对象
- 键缺失视为该变量未提供

### 2.6 批量渲染流程

```
run-batch --template plan.json --data products.csv --output-dir ./out
  │
  ├── 1. 解析模板
  │   ├── 标记所有 ${...} 占位符
  │   └── 标记所有 slot 定义
  │
  ├── 2. 读取数据源（CSV/JSONL）
  │
  ├── 3. 对每一行数据：
  │   ├── 检查必填 slot 是否满足
  │   │   ├── 不满足 → skip（记录告警）或 stop
  │   │   └── 满足 → 继续
  │   ├── 深度克隆模板
  │   ├── 替换所有 ${var}
  │   ├── 移除未绑定的可选 slot clip/track
  │   ├── 输出文件名替换
  │   └── 调用标准 render 流程
  │
  └── 4. 输出 summary.json
      {
        "total": 50,
        "success": 48,
        "skipped": 1,
        "failed": 1,
        "results": [
          { "index": 0, "output": "out/001.mp4", "status": "success" },
          { "index": 1, "output": null, "status": "skipped", "reason": "ERR_REQUIRED_SLOT_EMPTY" },
          { "index": 2, "output": "out/003.mp4", "status": "failed", "error": "..." }
        ]
      }
```

### 2.7 类型定义

```
BatchRenderRequest
├── templatePath     : string
├── dataSourcePath   : string
├── outputDir        : string
├── outputNamePattern: string        — "${_index}.mp4"
├── onError          : Skip | Stop
└── dryRun           : bool

BatchRenderResult
├── total            : int
├── success          : int
├── skipped          : int
├── failed           : int
└── results          : BatchRenderItemResult[]

BatchRenderItemResult
├── index            : int
├── output           : string?
├── status           : Success | Skipped | Failed
├── reason           : string?       — 错误码
├── error            : string?       — 错误详情
└── variables        : dict?         -- dry-run 时的替换结果

TemplateVariable
├── name             : string
├── defaultValue     : string?
├── required         : bool

SlotDefinition
├── name             : string
├── required         : bool
└── defaultAsset     : string?       — slot 缺失时使用的备选素材
```

## 3. data-visualize

### 3.1 定位

将数值参数以图表形式渲染为视频叠加层，用于数据新闻、财报视频、排行榜等场景。

**实现方式：** 作为一类特殊效果注册在效果库中，而非独立命令。这样图表可以和其他效果（文字、动画）组合使用。

### 3.2 图表效果类型

| type | 说明 | 渲染方式 |
|------|------|---------|
| `bar_chart` | 柱状图 | Cairo/FFmpeg drawtext + drawbox |
| `pie_chart` | 环形图 | Cairo 预渲染为 PNG → overlay |
| `line_chart` | 折线图 | Cairo 预渲染为 PNG → overlay |
| `counter` | 数字滚动动画 | FFmpeg drawtext + 时间表达式 |
| `progress_bar` | 进度条 | FFmpeg drawbox + time overlay |

### 3.3 效果参数示例

#### bar_chart

```jsonc
{
  "type": "bar_chart",
  "data": [
    { "label": "Q1", "value": 120 },
    { "label": "Q2", "value": 180 },
    { "label": "Q3", "value": 150 }
  ],
  "position": { "x": 100, "y": 200 },
  "size": { "w": 800, "h": 400 },
  "barColor": "#4A90D9",
  "labelColor": "#FFFFFF",
  "animate": true,
  "animationDuration": 2.0
}
```

#### counter

```jsonc
{
  "type": "counter",
  "from": 0,
  "to": 1299,
  "prefix": "¥",
  "fontSize": 72,
  "position": "center",
  "duration": 1.5,
  "easing": "ease_out"
}
```

#### progress_bar

```jsonc
{
  "type": "progress_bar",
  "value": 0.75,
  "position": { "x": 100, "y": 900 },
  "size": { "w": 1720, "h": 30 },
  "fillColor": "#00C853",
  "bgColor": "#333333",
  "animate": true,
  "animationDuration": 1.0
}
```

### 3.4 渲染策略

图表效果需要预渲染为图片/视频片段，再作为素材叠加到主时间线上。

```
图表效果 → Cairo/Magick.NET 渲染为 PNG 或 MP4 → 临时文件 → overlay 到主视频
```

**执行流程：**

1. 渲染引擎识别 clip.effects 中的图表类效果
2. 调用图表渲染器生成临时素材文件（PNG 序列或带 Alpha 的 MP4）
3. 将临时素材作为额外输入插入 filter_complex
4. 与主视频 overlay 合成

### 3.5 渲染后端

| 后端 | 适用场景 | 依赖 |
|------|---------|------|
| Cairo + P/Invoke | 高质量矢量图渲染（柱状图、折线图） | libcairo |
| Magick.NET (ImageMagick) | 简单图表、进度条 | NuGet 包 |
| FFmpeg drawtext/drawbox | 简单矩形、文字、计数器 | 无额外依赖 |

优先使用 FFmpeg 原生能力（零额外依赖），复杂图表用 Cairo 或 Magick.NET。

### 3.6 与批量渲染的结合

图表效果的 data 字段支持 `${var}` 占位符，实现数据驱动图表：

```jsonc
{
  "type": "counter",
  "from": 0,
  "to": "${sales_count}",
  "prefix": "¥",
  "position": "center",
  "duration": 1.5
}
```

批量渲染时 `${sales_count}` 被数据行替换，每行生成不同数字的滚动动画。

## 4. 扩展性预留

v3 可考虑的增强（不在本阶段实现）：

- **预处理脚本** — 在变量替换前执行 Lua/JavaScript，支持条件模板切换（A/B 模板）
- **循环生成** — 数据行中的数组字段展开为多个 clip（如评论列表）
- **并行渲染** — 多行数据并行处理，利用多核
- **远程数据源** — 从 HTTP API 拉取数据（而非本地文件）

## 5. 本阶段交付物

### 5.1 数据驱动批量渲染

1. `${var}` 占位符解析器（含默认值语法和转义）
2. slot 条件性移除逻辑
3. CSV / JSONL 数据源解析器
4. `run-batch` CLI 命令
5. summary.json 输出
6. `--dry-run` 模式
7. 单元测试

### 5.2 data-visualize

1. 图表效果类型定义（bar_chart, counter, progress_bar 为首批）
2. 图表效果注册到 EffectRegistry
3. FFmpeg drawtext/drawbox 渲染后端（简单图表）
4. Cairo/Magick.NET 渲染后端（复杂图表，可选）
5. 临时素材生成 + overlay 集成到渲染引擎
6. 与批量渲染 ${var} 的结合
7. 单元测试
