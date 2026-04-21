using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task Transcribe_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-transcribe-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "transcript.json");
        var jsonOutPath = Path.Combine(outputDirectory, "transcribe.json");
        var modelPath = Path.Combine(outputDirectory, "ggml-base.bin");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg",
            """
            @echo off
            setlocal
            set "out="
            :loop
            if "%~1"=="" goto done
            set "out=%~1"
            shift
            goto loop
            :done
            if "%out%"=="" exit /b 2
            break> "%out%"
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            out=""
            for arg in "$@"; do
              out="$arg"
            done
            if [ -z "$out" ]; then
              exit 2
            fi
            : > "$out"
            """);
        var whisperCliPath = WriteExecutableScript(
            outputDirectory,
            "fake-whisper",
            """
            @echo off
            setlocal EnableDelayedExpansion
            set "prefix="
            :parse
            if "%~1"=="" goto done
            if "%~1"=="-of" (
              set "prefix=%~2"
              shift
            )
            shift
            goto parse
            :done
            if "%prefix%"=="" exit /b 2
            > "%prefix%.json" (
              echo {"result":{"language":"en"},"transcription":[{"text":"hello world","offsets":{"from":0,"to":1000}}]}
            )
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            prefix=""
            while [ "$#" -gt 0 ]; do
              if [ "$1" = "-of" ]; then
                prefix="$2"
                shift 2
                continue
              fi
              shift
            done
            if [ -z "$prefix" ]; then
              exit 2
            fi
            cat > "${prefix}.json" <<'EOF'
            {"result":{"language":"en"},"transcription":[{"text":"hello world","offsets":{"from":0,"to":1000}}]}
            EOF
            """);
        await File.WriteAllTextAsync(modelPath, "model");

        try
        {
            var result = await RunCliAsync(
                "transcribe",
                "input.mp4",
                "--model",
                modelPath,
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath,
                "--whisper-cli",
                whisperCliPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            var transcriptFile = JsonNode.Parse(await File.ReadAllTextAsync(outputPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("transcribe", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["transcribe"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(modelPath), envelope["transcribe"]!["modelPath"]!.GetValue<string>());
            Assert.Equal("en", envelope["transcribe"]!["language"]!.GetValue<string>());
            Assert.Equal(1, envelope["transcribe"]!["segmentCount"]!.GetValue<int>());
            Assert.NotNull(envelope["transcript"]);
            Assert.True(JsonNode.DeepEquals(envelope["transcript"], transcriptFile));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
}
