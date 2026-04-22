# 外部依赖安装指南

最后更新：2026-04-21

## 必需依赖

### ffmpeg / ffprobe

所有媒体处理命令的底层工具。

**安装方式：**

| 平台 | 方式 |
|------|------|
| Windows | 从 [ffmpeg.org](https://ffmpeg.org/download.html) 下载，解压后将 `bin/` 目录加入 `PATH` |
| macOS | `brew install ffmpeg` |
| Linux | `sudo apt install ffmpeg` 或 `sudo dnf install ffmpeg` |

**验证：**

```sh
ffmpeg -version
ffprobe -version
```

**CLI 参数覆盖：**

```sh
--ffmpeg <path>
--ffprobe <path>
```

## 可选依赖

### whisper-cli（语音转写）

用于 `transcribe` 命令。基于 [whisper.cpp](https://github.com/ggerganov/whisper.cpp)。

**安装方式：**

1. 从 [whisper.cpp releases](https://github.com/ggerganov/whisper.cpp/releases) 下载预编译二进制
2. 或从源码编译：`cmake -B build && cmake --build build --config Release`
3. 确保可执行文件名为 `whisper-cli`（或通过环境变量指定）

**还需下载模型文件：**

从 [whisper.cpp models](https://github.com/ggerganov/whisper.cpp/tree/master/models) 下载，推荐 `ggml-base.bin`。

**环境变量：**

| 变量 | 用途 |
|------|------|
| `OVT_WHISPER_CLI_PATH` | `whisper-cli` 可执行文件路径 |
| `OVT_WHISPER_MODEL_PATH` | whisper 模型文件路径（如 `ggml-base.bin`） |

**CLI 参数覆盖：**

```sh
--whisper-cli <path>
--whisper-model <path>
```

### demucs（人声/伴奏分离）

用于 `separate-audio` 命令。基于 [Demucs](https://github.com/facebookresearch/demucs)。

**安装方式：**

```sh
pip install demucs
```

**环境变量：**

| 变量 | 用途 |
|------|------|
| `OVT_DEMUCS_PATH` | `demucs` 可执行文件路径 |

**CLI 参数覆盖：**

```sh
--demucs <path>
```

## 依赖预检

使用 `doctor` 命令一次性检查所有依赖状态：

```sh
dotnet run --project src/OpenVideoToolbox.Cli -- doctor --json-out doctor.json
```

`doctor` 的解析优先级：

1. 命令行参数（`--ffmpeg`、`--ffprobe`、`--whisper-cli`、`--whisper-model`、`--demucs`）
2. 环境变量（`OVT_WHISPER_CLI_PATH`、`OVT_WHISPER_MODEL_PATH`、`OVT_DEMUCS_PATH`）
3. 默认可执行名（`ffmpeg`、`ffprobe`、`whisper-cli`、`demucs`）
4. `unset`（未配置）

输出中 `required: true` 的依赖缺失时返回非零退出码；`required: false` 的缺失只影响对应命令可用性。

## 最小兼容性基线

当前仓库不维护一份“所有平台 / 所有版本”的完整兼容矩阵，先维护一份更可执行的验证约定。

| 依赖 | 当前分类 | 最低通过标准 |
|------|----------|--------------|
| `ffmpeg` | required | `doctor` 可用 + ffmpeg 相关 real smoke 可通过 |
| `ffprobe` | required | `doctor` 可用 + probe 相关 real smoke 可通过 |
| `whisper-cli` | optional | `doctor` 能解析状态；需要 `transcribe` 时再补 optional real smoke |
| `whisper-model` | optional | `doctor` 能解析状态；需要 `transcribe` 时与 `whisper-cli` 一起验证 |
| `demucs` | optional | `doctor` 能解析状态；需要 `separate-audio` 时再补 optional real smoke |

推荐验证顺序：

1. 先跑 `doctor`
2. 再跑整仓测试或 real smoke
3. 如果只缺 optional 依赖，不应阻塞默认 CLI 主链验证

如果想把这组检查收成一条维护命令，可直接运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify-DependencyBaseline.ps1
```

## 推荐验证命令

```powershell
dotnet run --project src/OpenVideoToolbox.Cli/OpenVideoToolbox.Cli.csproj -- doctor --json-out doctor.json
dotnet test src/OpenVideoToolbox.Core.Tests/OpenVideoToolbox.Core.Tests.csproj --filter "FullyQualifiedName~RealMediaSmokeTests"
dotnet test src/OpenVideoToolbox.Cli.Tests/OpenVideoToolbox.Cli.Tests.csproj --filter "FullyQualifiedName~CliRealMediaSmokeTests"
```

说明：

- 默认环境缺少 `whisper-cli` / `demucs` / `whisper model` 时，对应 optional smoke 允许跳过。
- 这套约定的更多背景和当前观测样本，见 `docs/plans/2026-04-22-e2-a4-runtime-baseline.md`。
