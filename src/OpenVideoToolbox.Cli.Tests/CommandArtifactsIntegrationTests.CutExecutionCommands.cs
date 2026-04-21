using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task Cut_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-cut-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "clip.mp4");

        try
        {
            var result = await RunCliAsync(
                "cut",
                "input.mp4",
                "--from",
                "00:00:00",
                "--to",
                "00:00:02",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("cut", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["cut"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Null(envelope["execution"]);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Cut_ReturnsFailureEnvelope_WhenExecutableExitsNonZero()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-cut-process-failed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "clip.mp4");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg-fail",
            """
            @echo off
            echo fake ffmpeg failure 1>&2
            exit /b 7
            """,
            """
            #!/bin/sh
            echo "fake ffmpeg failure" >&2
            exit 7
            """);

        try
        {
            var result = await RunCliAsync(
                "cut",
                "input.mp4",
                "--from",
                "00:00:00",
                "--to",
                "00:00:02",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath);

            Assert.Equal(2, result.ExitCode);
            Assert.Contains("Process exited with code 7.", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("cut", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["cut"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("Process exited with code 7.", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Equal("failed", envelope["execution"]!["status"]!.GetValue<string>());
            Assert.Equal(7, envelope["execution"]!["exitCode"]!.GetValue<int>());
            Assert.Contains("Process exited with code 7.", envelope["execution"]!["errorMessage"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
