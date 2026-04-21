using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task AudioAnalyze_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-analyze-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "audio.json");

        try
        {
            var result = await RunCliAsync(
                "audio-analyze",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("audio-analyze", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["audioAnalyze"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
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
    public async Task AudioAnalyze_ReturnsFailureEnvelope_WhenProcessExitsNonZero()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-analyze-failed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "audio.json");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg-fail",
            """
            @echo off
            1>&2 echo loudnorm failed
            exit /b 7
            """,
            """
            #!/bin/sh
            echo "loudnorm failed" >&2
            exit 7
            """);

        try
        {
            var result = await RunCliAsync(
                "audio-analyze",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("ffmpeg audio analysis failed", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("audio-analyze", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["audioAnalyze"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("ffmpeg audio analysis failed", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
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
    public async Task AudioAnalyze_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-analyze-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "audio.json");
        var jsonOutPath = Path.Combine(outputDirectory, "audio-analyze.json");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg",
            """
            @echo off
            1>&2 echo {
            1>&2 echo   "input_i" : "-16.40",
            1>&2 echo   "input_lra" : "3.10",
            1>&2 echo   "input_tp" : "-1.20",
            1>&2 echo   "input_thresh" : "-27.30",
            1>&2 echo   "target_offset" : "0.50"
            1>&2 echo }
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            cat >&2 <<'EOF'
            {
              "input_i" : "-16.40",
              "input_lra" : "3.10",
              "input_tp" : "-1.20",
              "input_thresh" : "-27.30",
              "target_offset" : "0.50"
            }
            EOF
            """);

        try
        {
            var result = await RunCliAsync(
                "audio-analyze",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            var analysisFile = JsonNode.Parse(await File.ReadAllTextAsync(outputPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("audio-analyze", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["audioAnalyze"]!["outputPath"]!.GetValue<string>());
            Assert.True(JsonNode.DeepEquals(envelope["analysis"], analysisFile));
            Assert.Equal(-16.4, envelope["analysis"]!["analysis"]!["integratedLoudness"]!.GetValue<double>());
            Assert.Equal(3.1, envelope["analysis"]!["analysis"]!["loudnessRange"]!.GetValue<double>());
            Assert.Equal(-1.2, envelope["analysis"]!["analysis"]!["truePeakDb"]!.GetValue<double>());
            Assert.Equal(-27.3, envelope["analysis"]!["analysis"]!["thresholdDb"]!.GetValue<double>());
            Assert.Equal(0.5, envelope["analysis"]!["analysis"]!["targetOffset"]!.GetValue<double>());
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
