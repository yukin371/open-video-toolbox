# Example Plugin

这个目录演示一个最小可工作的模板插件。

它的目标不是提供复杂工作流，而是给贡献者一份可以直接照抄、修改、再本地自测的起点。

## 目录结构

```text
plugin-example/
  README.md
  plugin.json
  templates/
    quick-subtitle/
      artifacts.json
      template-params.json
      template.json
```

这两个额外文件不是新的 schema，只是和 `templates quick-subtitle --plugin-dir examples/plugin-example` 当前 guide 输出保持一致的可复制 skeleton：

- `artifacts.json`
  - 演示如何把 `subtitles` slot 显式绑定到 `subtitles.srt`
- `template-params.json`
  - 演示模板参数文件入口；当前模板没有默认参数，所以内容为空对象

## 这个示例演示了什么

- 插件通过 `plugin.json` 暴露元数据
- 模板通过 `template.json` 复用现有 template schema
- 模板需要的字幕输入被声明为显式 `artifactSlots`
- 模板推荐 transcript 作为 supporting signal
- 模板目录直接附带可复制的 `artifacts.json` 与 `template-params.json` 示例骨架

## 本地验证

### 1. 列出插件模板

```powershell
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- templates --plugin-dir examples/plugin-example --summary
```

### 2. 查看单模板 guide

```powershell
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- templates quick-subtitle --plugin-dir examples/plugin-example
```

### 3. 写出示例目录

```powershell
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- templates quick-subtitle --plugin-dir examples/plugin-example --write-examples .plugin-guide
```

写出的 `.plugin-guide/artifacts.json` 与 `.plugin-guide/template-params.json` 会和当前示例目录里的骨架保持同一结构。

### 4. 生成并校验计划

```powershell
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- init-plan input.mp4 --template quick-subtitle --plugin-dir examples/plugin-example --output edit.json --render-output final.mp4 --artifacts examples/plugin-example/templates/quick-subtitle/artifacts.json --template-params examples/plugin-example/templates/quick-subtitle/template-params.json
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- validate-plan --plan edit.json --plugin-dir examples/plugin-example
```

说明：

- 第 4 步需要你自己提供一个真实的 `input.mp4`
- `artifacts.json` 和 `template-params.json` 只是示例起点；复制后按你的实际字幕路径和参数覆盖即可
- 如果你还没有输入文件，先完成前 3 步即可验证插件目录、guide 和命令脚手架是否闭环

如果你想一次性跑完整示例插件闭环，也可以直接运行仓库脚本：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify-ExamplePlugin.ps1
```

这个脚本会自动生成样例输入，并串起来执行：

- `validate-plugin`
- `templates --summary`
- `templates quick-subtitle --write-examples`
- `init-plan`
- `validate-plan`

## 复制这个示例时要改什么

至少修改这些字段：

- `plugin.json`
  - `id`
  - `displayName`
  - `description`
- `template.json`
  - `id`
  - `displayName`
  - `description`
  - `category`
  - `artifactSlots`
  - `supportingSignals`
- `artifacts.json`
  - 把 `subtitles.srt` 改成你实际准备的 artifact 路径
- `template-params.json`
  - 如果你的模板需要参数覆盖，再把空对象补成实际参数；不需要时可保持为空

## 不要把它改成什么

- 不要加入脚本或二进制
- 不要加 CLI 目前不认识的私有字段
- 不要依赖新的执行语义
- 不要让模板 ID 与内置模板重名

完整开发说明见 `docs/plugin-development-guide.md`。
