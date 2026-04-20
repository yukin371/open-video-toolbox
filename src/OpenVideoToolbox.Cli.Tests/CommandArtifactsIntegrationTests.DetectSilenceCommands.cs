using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task DetectSilence_RequiresOutputOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-detect-silence-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("detect-silence", "input.mp4");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("detect-silence <input> --output <silence.json>", result.StdOut, StringComparison.Ordinal);
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
    public async Task DetectSilence_RejectsInvalidNoiseDb()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-detect-silence-noise-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "silence.json");

        try
        {
            var result = await RunCliAsync(
                "detect-silence",
                "input.mp4",
                "--output",
                outputPath,
                "--noise-db",
                "oops");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--noise-db' expects a numeric value.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("detect-silence <input> --output <silence.json>", result.StdOut, StringComparison.Ordinal);
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
    public async Task DetectSilence_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-detect-silence-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "silence.json");

        try
        {
            var result = await RunCliAsync(
                "detect-silence",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("detect-silence", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["detectSilence"]!["outputPath"]!.GetValue<string>());
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
    public async Task DetectSilence_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-detect-silence-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "silence.json");
        var jsonOutPath = Path.Combine(outputDirectory, "detect-silence.json");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg",
            """
            @echo off
            1>&2 echo silence_start: 1.25
            1>&2 echo silence_end: 2.75 ^| silence_duration: 1.50
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'silence_start: 1.25\n' >&2
            printf 'silence_end: 2.75 | silence_duration: 1.50\n' >&2
            """);

        try
        {
            var result = await RunCliAsync(
                "detect-silence",
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
            var silenceFile = JsonNode.Parse(await File.ReadAllTextAsync(outputPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("detect-silence", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["detectSilence"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(1, envelope["detectSilence"]!["segmentCount"]!.GetValue<int>());
            Assert.True(JsonNode.DeepEquals(envelope["silence"], silenceFile));
            Assert.Equal("00:00:01.2500000", envelope["silence"]!["segments"]![0]!["start"]!.GetValue<string>());
            Assert.Equal("00:00:02.7500000", envelope["silence"]!["segments"]![0]!["end"]!.GetValue<string>());
            Assert.Equal("00:00:01.5000000", envelope["silence"]!["segments"]![0]!["duration"]!.GetValue<string>());
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
