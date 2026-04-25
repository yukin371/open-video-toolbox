# V2-P6-C15 数据驱动 Batch Narrated 规格

最后更新：2026-04-25

## 目标

在不扩大 `narrated-slides` 业务语义的前提下，补一个可批量消费 narrated manifest 的 CLI 入口，服务“多期讲解视频批量起稿”场景。

## 边界

- 只新增 `Cli` 批量 wrapper，不改 `Core.Editing` 的 narrated manifest -> plan 投影规则
- 只复用现有 `init-narrated-plan` 单项语义，不新增第二套 narrated plan builder
- 不进入：
  - `.pptx` / Markdown / chart / page renderer 后端
  - richer placeholder / title-card
  - section 删除 / 条件裁剪增强

## 新命令

```text
init-narrated-plan-batch --manifest <batch.json> [--ffprobe <path>] [--timeout-seconds <n>] [--json-out <path>]
```

## manifest 约定

```json
{
  "schemaVersion": 1,
  "items": [
    {
      "id": "episode-01",
      "manifest": "episodes/episode-01/narrated.json",
      "output": "tasks/episode-01/edit.json",
      "template": "narrated-slides-starter",
      "renderOutput": "exports/episode-01.mp4",
      "vars": "vars/episode-01.json"
    }
  ]
}
```

## 字段语义

- `schemaVersion`
  - 当前固定为 `1`
- `items[].id`
  - 必填；同时作为 `results/<id>.json` 文件名
- `items[].manifest`
  - 必填；指向 narrated manifest
- `items[].output`
  - 选填；未提供时默认写到 `tasks/<id>/edit.json`
- `items[].template`
  - 选填；透传到单项 narrated build 路径
- `items[].renderOutput`
  - 选填；覆盖单项命令的 `--render-output`
- `items[].vars`
  - 选填；覆盖单项命令的 `--vars`

## 路径规则

- batch manifest 内的 `manifest` / `output` / `renderOutput` / `vars` 相对路径统一按 batch manifest 所在目录解析
- narrated manifest 自身内部的 `sections[].visual.path`、`voice.path`、`subtitles.path`、`bgm.path` 仍按 narrated manifest 自身所在目录解析

## 输出与退出码

- 根目录固定写：
  - `summary.json`
- 每个条目固定写：
  - `results/<id>.json`
- 退出码约定：
  - `0`：全部成功
  - `2`：部分或全部条目失败
  - `1`：batch manifest 解析或装载失败

## 验收点

- 可批量写出多份 narrated `edit.json`
- 可同时覆盖默认输出路径和显式输出路径
- 可复用单项 `--vars` 覆盖语义
- 可在部分失败时保留 `summary.json` 与逐项结果文件
