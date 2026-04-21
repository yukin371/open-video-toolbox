# 模板插件开发指南

最后更新：2026-04-22

## 概念

模板插件是一组**静态文件**——声明、模板定义和可选的辅助文档——放在一个目录里，由 CLI 通过 `--plugin-dir` 显式发现和加载。

插件**不是**可执行代码。它不包含脚本、二进制或运行时逻辑。所有执行能力来自 CLI 已有的确定性命令。

## 最小目录结构

```
my-plugin/
  plugin.json                          # 插件清单（必需）
  templates/
    my-template/
      template.json                    # 模板定义（必需）
```

## plugin.json 清单

```json
{
  "schemaVersion": 1,
  "id": "my-plugin",
  "displayName": "My Plugin",
  "version": "1.0.0",
  "description": "A short description of what this plugin provides.",
  "templates": [
    {
      "id": "my-template",
      "path": "templates/my-template"
    }
  ]
}
```

### 字段说明

| 字段 | 必需 | 说明 |
|------|------|------|
| `schemaVersion` | 是 | 当前必须为 `1` |
| `id` | 是 | 插件唯一标识符，只能包含字母、数字、连字符 |
| `displayName` | 是 | 人类可读名称 |
| `version` | 是 | 语义版本号 |
| `description` | 是 | 插件用途描述 |
| `templates` | 是 | 模板条目数组，至少包含一个 |

### templates 条目

| 字段 | 必需 | 说明 |
|------|------|------|
| `id` | 是 | 模板 ID，必须与 template.json 中的 `id` 一致 |
| `path` | 是 | 模板目录相对于 plugin.json 的路径 |

## template.json 模板定义

```json
{
  "id": "my-template",
  "displayName": "My Template",
  "description": "What this template does.",
  "category": "custom",
  "version": "1.0.0",
  "outputContainer": "mp4",
  "defaultSubtitleMode": "sidecar",
  "recommendedSeedModes": [ "manual", "transcript" ],
  "recommendedTranscriptSeedStrategies": [ "grouped" ],
  "artifactSlots": [
    {
      "id": "subtitles",
      "kind": "subtitle",
      "description": "Subtitle sidecar file",
      "required": false
    }
  ],
  "supportingSignals": [
    {
      "kind": "transcript",
      "reason": "Transcript is needed to generate subtitles."
    }
  ]
}
```

### 字段说明

| 字段 | 必需 | 说明 |
|------|------|------|
| `id` | 是 | 模板唯一 ID，不能与内置模板重复 |
| `displayName` | 是 | 人类可读名称 |
| `description` | 是 | 模板用途 |
| `category` | 是 | 分类标识（如 `short-form`、`commentary`、`custom`） |
| `version` | 否 | 默认 `"1.0.0"` |
| `outputContainer` | 否 | 默认 `"mp4"` |
| `defaultSubtitleMode` | 否 | `"sidecar"` 或 `"burnIn"` |
| `recommendedSeedModes` | 否 | 可选值：`"manual"`、`"transcript"`、`"beats"` |
| `recommendedTranscriptSeedStrategies` | 否 | 可选值：`"grouped"`、`"minDuration"`、`"maxGap"` |
| `artifactSlots` | 否 | 声明模板需要的 artifact slot |
| `supportingSignals` | 否 | 声明模板推荐的 supporting signals |

### artifactSlots 条目

| 字段 | 必需 | 说明 |
|------|------|------|
| `id` | 是 | Slot 标识符 |
| `kind` | 是 | Artifact 类型（如 `"subtitle"`） |
| `description` | 是 | 用途说明 |
| `required` | 否 | 是否必需，默认 `false` |

### supportingSignals 条目

| 字段 | 必需 | 说明 |
|------|------|------|
| `kind` | 是 | 信号类型：`"transcript"`、`"beats"`、`"silence"`、`"stems"` |
| `reason` | 是 | 为什么推荐此信号 |

## 验证插件

用 `templates --plugin-dir <path>` 验证插件是否能被正确加载：

```sh
# 成功时返回 0
dotnet run --project src/OpenVideoToolbox.Cli -- templates --plugin-dir my-plugin --summary

# 失败时返回非零，stderr 包含具体错误
```

### 常见验证错误

| 错误信息 | 原因 |
|---------|------|
| `was not found` (plugin directory) | `--plugin-dir` 路径不存在 |
| `plugin.json ... was not found` | 缺少清单文件 |
| `invalid JSON` | plugin.json 不是合法 JSON |
| `missing 'id'` | 清单缺少必需字段 |
| `at least one template` | templates 数组为空 |
| `template.json ... was not found` | 模板目录中缺少 template.json |
| `does not match template id` | 清单中声明的 id 与 template.json 中的 id 不一致 |
| `Duplicate edit plan template id` | 模板 ID 与内置模板冲突 |

## 使用插件

```sh
# 列出插件模板
ovt templates --plugin-dir my-plugin --summary

# 查看单模板 guide
ovt templates my-template --plugin-dir my-plugin

# 生成编辑计划
ovt init-plan input.mp4 --template my-template --output edit.json --plugin-dir my-plugin

# 验证计划
ovt validate-plan --plan edit.json --plugin-dir my-plugin

# 渲染
ovt render --plan edit.json --output output.mp4
```

## 兼容性策略

- `plugin.json` 的 `schemaVersion` 用于检测不兼容变更
- 当前只支持 `schemaVersion: 1`
- 未来引入 `schemaVersion: 2` 时，CLI 会在加载时拒绝不兼容版本并给出明确提示
- 模板定义复用 `Core.Editing` 的既有 schema，不引入插件私有字段

## 示例

完整示例见 `examples/plugin-example/`。

## 约束

- 插件不能包含可执行代码
- 插件不能定义新的执行语义
- 模板 ID 不能与内置模板冲突
- `template.json` 不能包含 Core schema 未声明的字段
