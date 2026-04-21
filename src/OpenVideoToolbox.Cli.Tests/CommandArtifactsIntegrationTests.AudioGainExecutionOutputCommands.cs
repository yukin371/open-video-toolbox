using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task AudioGain_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-gain-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "gain.wav");
        var jsonOutPath = Path.Combine(outputDirectory, "audio-gain.json");
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

        try
        {
            var result = await RunCliAsync(
                "audio-gain",
                "input.mp4",
                "--gain-db",
                "-6",
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
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("audio-gain", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["audioGain"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(-6, envelope["audioGain"]!["gainDb"]!.GetValue<double>());
            Assert.Equal("succeeded", envelope["execution"]!["status"]!.GetValue<string>());
            Assert.Contains(Path.GetFullPath(outputPath), envelope["execution"]!["producedPaths"]!.AsArray().Select(node => node!.GetValue<string>()));
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
