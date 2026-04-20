using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task MixAudioPreview_ReturnsStableExecutionPreview()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-mix-preview-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var outputPath = Path.Combine(outputDirectory, "mixed.wav");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
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
            var result = await RunCliAsync("mix-audio", "--plan", planPath, "--output", outputPath, "--preview");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("mix-audio", payload["command"]!.GetValue<string>());
            Assert.True(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            var executionPreview = envelope["executionPreview"]!.AsObject();
            var commandPlan = executionPreview["commandPlan"]!.AsObject();
            var producedPaths = executionPreview["producedPaths"]!.AsArray();

            Assert.Equal(Path.GetFullPath(planPath), envelope["mixAudio"]!["planPath"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(outputPath), envelope["mixAudio"]!["outputPath"]!.GetValue<string>());
            Assert.Equal("ffmpeg", commandPlan["toolName"]!.GetValue<string>());
            Assert.Equal("ffmpeg", commandPlan["executablePath"]!.GetValue<string>());
            Assert.Contains(commandPlan["arguments"]!.AsArray().Select(node => node!.GetValue<string>()), arg => arg == "-filter_complex");
            Assert.Contains(commandPlan["arguments"]!.AsArray().Select(node => node!.GetValue<string>()), arg => arg == "pcm_s16le");
            Assert.Equal(Path.GetFullPath(outputPath), Assert.Single(producedPaths)!.GetValue<string>());
            Assert.Empty(executionPreview["sideEffects"]!.AsArray());
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
    public async Task MixAudioPreview_CanWriteEnvelopeToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-mix-preview-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var outputPath = Path.Combine(outputDirectory, "mixed.wav");
        var jsonOutPath = Path.Combine(outputDirectory, "mix-preview.json");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
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
            var result = await RunCliAsync("mix-audio", "--plan", planPath, "--output", outputPath, "--preview", "--json-out", jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();

            Assert.Equal(stdout.ToJsonString(), file.ToJsonString());
            Assert.Equal("mix-audio", stdout["command"]!.GetValue<string>());
            Assert.True(stdout["preview"]!.GetValue<bool>());
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
    public async Task MixAudioPreview_RejectsPlansWithoutClips()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-mix-preview-empty-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var outputPath = Path.Combine(outputDirectory, "mixed.wav");

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
              "clips": [],
              "audioTracks": [],
              "artifacts": [],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync("mix-audio", "--plan", planPath, "--output", outputPath, "--preview");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Edit plan must contain at least one clip.", result.StdErr, StringComparison.Ordinal);
            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("mix-audio", payload["command"]!.GetValue<string>());
            Assert.True(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal("plugin", envelope["templateSource"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", envelope["templateSource"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", envelope["templateSource"]!["pluginVersion"]!.GetValue<string>());
            Assert.Equal(outputPath, envelope["mixAudio"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("Edit plan must contain at least one clip.", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
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
    public async Task MixAudio_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-mix-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var outputPath = Path.Combine(outputDirectory, "mixed.wav");

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
            var result = await RunCliAsync("mix-audio", "--plan", planPath, "--output", outputPath, "--ffmpeg", "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);
            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("mix-audio", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal("plugin", envelope["templateSource"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", envelope["templateSource"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", envelope["templateSource"]!["pluginVersion"]!.GetValue<string>());
            Assert.Equal(outputPath, envelope["mixAudio"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.NotNull(envelope["executionPreview"]);
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
    public async Task MixAudio_ReturnsFailureEnvelope_WhenExecutableExitsNonZero()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-mix-process-failed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var outputPath = Path.Combine(outputDirectory, "mixed.wav");
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
            var result = await RunCliAsync("mix-audio", "--plan", planPath, "--output", outputPath, "--ffmpeg", ffmpegPath);

            Assert.Equal(2, result.ExitCode);
            Assert.Contains("Process exited with code 7.", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("mix-audio", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal("plugin", envelope["templateSource"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", envelope["templateSource"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", envelope["templateSource"]!["pluginVersion"]!.GetValue<string>());
            Assert.Equal(outputPath, envelope["mixAudio"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("Process exited with code 7.", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.NotNull(envelope["executionPreview"]);
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
