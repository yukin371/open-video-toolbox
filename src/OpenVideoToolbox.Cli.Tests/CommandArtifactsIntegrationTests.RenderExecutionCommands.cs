using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task Render_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "template": {
                "id": "plugin-captioned",
                "source": {
                  "kind": "plugin",
                  "pluginId": "community-pack",
                  "pluginVersion": "1.0.0"
                },
                "parameters": {}
              },
              "clips": [
                { "id": "clip-001", "in": "00:00:00", "out": "00:00:02", "label": "intro" }
              ],
              "audioTracks": [],
              "artifacts": [],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync("render", "--plan", planPath, "--ffmpeg", "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);
            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("render", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal("plugin", envelope["templateSource"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", envelope["templateSource"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", envelope["templateSource"]!["pluginVersion"]!.GetValue<string>());
            Assert.Equal("plugin-captioned", envelope["render"]!["template"]!["id"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.NotNull(envelope["executionPreview"]);
            Assert.False(File.Exists(Path.Combine(outputDirectory, "final.mp4")));
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
    public async Task Render_ReturnsFailureEnvelope_WhenExecutableExitsNonZero()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-process-failed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
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

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "template": {
                "id": "plugin-captioned",
                "source": {
                  "kind": "plugin",
                  "pluginId": "community-pack",
                  "pluginVersion": "1.0.0"
                },
                "parameters": {}
              },
              "clips": [
                { "id": "clip-001", "in": "00:00:00", "out": "00:00:02", "label": "intro" }
              ],
              "audioTracks": [],
              "artifacts": [],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync("render", "--plan", planPath, "--ffmpeg", ffmpegPath);

            Assert.Equal(2, result.ExitCode);
            Assert.Contains("Process exited with code 7.", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("render", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal("plugin", envelope["templateSource"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", envelope["templateSource"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", envelope["templateSource"]!["pluginVersion"]!.GetValue<string>());
            Assert.Equal("plugin-captioned", envelope["render"]!["template"]!["id"]!.GetValue<string>());
            Assert.Contains("Process exited with code 7.", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.NotNull(envelope["executionPreview"]);
            Assert.Equal("failed", envelope["execution"]!["status"]!.GetValue<string>());
            Assert.Equal(7, envelope["execution"]!["exitCode"]!.GetValue<int>());
            Assert.Contains("Process exited with code 7.", envelope["execution"]!["errorMessage"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.False(File.Exists(Path.Combine(outputDirectory, "final.mp4")));
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
