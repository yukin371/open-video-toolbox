using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task Doctor_OptionValuesOverrideEnvironmentFallback()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-doctor-option-precedence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg",
            """
            @echo off
            echo ffmpeg version n-test
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'ffmpeg version n-test\n'
            """);
        var ffprobePath = WriteExecutableScript(
            outputDirectory,
            "fake-ffprobe",
            """
            @echo off
            echo ffprobe version n-test
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'ffprobe version n-test\n'
            """);
        var whisperCliPath = WriteExecutableScript(
            outputDirectory,
            "fake-whisper-cli",
            """
            @echo off
            echo usage: whisper-cli
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'usage: whisper-cli\n'
            """);
        var demucsPath = WriteExecutableScript(
            outputDirectory,
            "fake-demucs",
            """
            @echo off
            echo usage: demucs
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'usage: demucs\n'
            """);
        var modelPath = Path.Combine(outputDirectory, "model.gguf");
        await File.WriteAllTextAsync(modelPath, "model");

        try
        {
            var result = await CliTestProcessHelper.RunCliAsync(
                new Dictionary<string, string?>
                {
                    ["OVT_WHISPER_CLI_PATH"] = whisperCliPath,
                    ["OVT_DEMUCS_PATH"] = demucsPath,
                    ["OVT_WHISPER_MODEL_PATH"] = modelPath
                },
                "doctor",
                "--ffmpeg",
                ffmpegPath,
                "--ffprobe",
                ffprobePath,
                "--whisper-cli",
                "missing-whisper",
                "--demucs",
                "missing-demucs",
                "--whisper-model",
                "missing-model.gguf");

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            var payload = envelope["payload"]!.AsObject();
            Assert.True(payload["isHealthy"]!.GetValue<bool>());

            var dependencies = payload["dependencies"]!.AsArray();
            Assert.Contains(
                dependencies,
                node => node!["id"]!.GetValue<string>() == "whisper-cli"
                    && node["source"]!.GetValue<string>() == "option"
                    && node["resolvedValue"]!.GetValue<string>() == "missing-whisper"
                    && node["detail"]!.GetValue<string>().Contains("missing-whisper", StringComparison.Ordinal)
                    && !node["isAvailable"]!.GetValue<bool>());
            Assert.Contains(
                dependencies,
                node => node!["id"]!.GetValue<string>() == "demucs"
                    && node["source"]!.GetValue<string>() == "option"
                    && node["resolvedValue"]!.GetValue<string>() == "missing-demucs"
                    && node["detail"]!.GetValue<string>().Contains("missing-demucs", StringComparison.Ordinal)
                    && !node["isAvailable"]!.GetValue<bool>());
            Assert.Contains(
                dependencies,
                node => node!["id"]!.GetValue<string>() == "whisper-model"
                    && node["source"]!.GetValue<string>() == "option"
                    && node["resolvedValue"]!.GetValue<string>().EndsWith("missing-model.gguf", StringComparison.OrdinalIgnoreCase)
                    && node["detail"]!.GetValue<string>().Contains("missing-model.gguf", StringComparison.OrdinalIgnoreCase)
                    && !node["isAvailable"]!.GetValue<bool>());
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
