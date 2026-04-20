using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task BeatTrack_RequiresOutputOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("beat-track", "input.mp4");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("beat-track <input> --output <beats.json>", result.StdOut, StringComparison.Ordinal);
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
    public async Task BeatTrack_RejectsInvalidSampleRate()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-sample-rate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "beats.json");

        try
        {
            var result = await RunCliAsync(
                "beat-track",
                "input.mp4",
                "--output",
                outputPath,
                "--sample-rate",
                "oops");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--sample-rate' expects an integer value.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("beat-track <input> --output <beats.json>", result.StdOut, StringComparison.Ordinal);
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
    public async Task BeatTrack_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "beats.json");

        try
        {
            var result = await RunCliAsync(
                "beat-track",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("beat-track", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["beatTrack"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Null(envelope["extraction"]);
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
    public async Task BeatTrack_ReturnsFailureEnvelope_WhenExtractionExitsNonZero()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-process-failed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "beats.json");
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
                "beat-track",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath);

            Assert.Equal(2, result.ExitCode);
            Assert.Contains("Process exited with code 7.", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("beat-track", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["beatTrack"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("Process exited with code 7.", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Equal("failed", envelope["extraction"]!["status"]!.GetValue<string>());
            Assert.Equal(7, envelope["extraction"]!["exitCode"]!.GetValue<int>());
            Assert.Contains("Process exited with code 7.", envelope["extraction"]!["errorMessage"]!.GetValue<string>(), StringComparison.Ordinal);
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
    public async Task BeatTrack_CanWriteEnvelopeToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "beats.json");
        var jsonOutPath = Path.Combine(outputDirectory, "beat-track.json");
        var sourceWavePath = Path.Combine(outputDirectory, "source.wav");
        WriteMonoPcmWave(sourceWavePath, sampleRateHz: 16000, sampleCount: 16000);
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg-copy-wave",
            $"""
            @echo off
            set "out=%~1"
            :loop
            if "%~2"=="" goto done
            shift
            set "out=%~1"
            goto loop
            :done
            copy /Y "{sourceWavePath}" "%out%" >nul
            exit /b 0
            """,
            $"""
            #!/bin/sh
            out=""
            for arg in "$@"; do
              out="$arg"
            done
            cp "{sourceWavePath.Replace("\\", "/")}" "$out"
            """);

        try
        {
            var result = await RunCliAsync(
                "beat-track",
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
            var beatsFile = JsonNode.Parse(await File.ReadAllTextAsync(outputPath))!.AsObject();

            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("beat-track", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());

            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["beatTrack"]!["outputPath"]!.GetValue<string>());
            Assert.True(envelope["beatTrack"]!["beatCount"]!.GetValue<int>() >= 0);
            Assert.Equal("succeeded", envelope["extraction"]!["status"]!.GetValue<string>());
            Assert.Equal(1, beatsFile["schemaVersion"]!.GetValue<int>());
            Assert.Equal("input.mp4", beatsFile["sourcePath"]!.GetValue<string>());
            Assert.Equal(envelope["beatTrack"]!["beatCount"]!.GetValue<int>(), beatsFile["beats"]!.AsArray().Count);
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
