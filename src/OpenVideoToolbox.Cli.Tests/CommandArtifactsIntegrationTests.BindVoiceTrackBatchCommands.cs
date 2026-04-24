using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task BindVoiceTrackBatch_BindsMultiplePlansFromManifest()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-bind-voice-batch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var planAPath = Path.Combine(outputDirectory, "a", "edit.json");
        var planBPath = Path.Combine(outputDirectory, "b", "edit.json");
        var voiceAPath = Path.Combine(outputDirectory, "audio", "a.wav");
        var voiceBPath = Path.Combine(outputDirectory, "audio", "b.wav");
        var manifestPath = Path.Combine(outputDirectory, "batch.json");

        Directory.CreateDirectory(Path.GetDirectoryName(planAPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(planBPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(voiceAPath)!);

        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "a", "input.mp4"), "video-a");
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "b", "input.mp4"), "video-b");
        await File.WriteAllTextAsync(voiceAPath, "audio-a");
        await File.WriteAllTextAsync(voiceBPath, "audio-b");
        await File.WriteAllTextAsync(
            planAPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "output": { "path": "output.mp4", "container": "mp4" }
            }
            """);
        await File.WriteAllTextAsync(
            planBPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "output": { "path": "output.mp4", "container": "mp4" }
            }
            """);
        await File.WriteAllTextAsync(
            manifestPath,
            """
            {
              "schemaVersion": 1,
              "items": [
                {
                  "plan": "a/edit.json",
                  "path": "audio/a.wav",
                  "checkFiles": true,
                  "pathStyle": "relative"
                },
                {
                  "plan": "b/edit.json",
                  "path": "audio/b.wav",
                  "trackId": "voice-alt",
                  "role": "voice",
                  "checkFiles": true,
                  "pathStyle": "relative"
                }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync("bind-voice-track-batch", "--manifest", manifestPath);

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!["payload"]!.AsObject();
            Assert.Equal(2, payload["itemCount"]!.GetValue<int>());
            Assert.Equal(2, payload["succeededCount"]!.GetValue<int>());
            Assert.Equal(0, payload["failedCount"]!.GetValue<int>());

            var results = payload["results"]!.AsArray();
            Assert.Equal("succeeded", results[0]!["status"]!.GetValue<string>());
            Assert.Equal("voice-main", results[0]!["result"]!["voiceTrack"]!["trackId"]!.GetValue<string>());
            Assert.Equal("voice-alt", results[1]!["result"]!["voiceTrack"]!["trackId"]!.GetValue<string>());

            var planA = JsonNode.Parse(await File.ReadAllTextAsync(planAPath))!.AsObject();
            var planB = JsonNode.Parse(await File.ReadAllTextAsync(planBPath))!.AsObject();
            Assert.Equal("voice-main", planA["audioTracks"]![0]!["id"]!.GetValue<string>());
            Assert.Equal("voice-alt", planB["audioTracks"]![0]!["id"]!.GetValue<string>());
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
    public async Task BindVoiceTrackBatch_ReturnsPartialFailureSummary()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-bind-voice-batch-fail-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var planPath = Path.Combine(outputDirectory, "edit.json");
        var voicePath = Path.Combine(outputDirectory, "audio", "dub.wav");
        var manifestPath = Path.Combine(outputDirectory, "batch.json");

        Directory.CreateDirectory(Path.GetDirectoryName(voicePath)!);
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "input.mp4"), "video");
        await File.WriteAllTextAsync(voicePath, "audio");
        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "output": { "path": "output.mp4", "container": "mp4" }
            }
            """);
        await File.WriteAllTextAsync(
            manifestPath,
            """
            {
              "schemaVersion": 1,
              "items": [
                {
                  "plan": "edit.json",
                  "path": "audio/dub.wav",
                  "checkFiles": true
                },
                {
                  "plan": "missing/edit.json",
                  "path": "audio/dub.wav"
                }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync("bind-voice-track-batch", "--manifest", manifestPath);

            Assert.Equal(2, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!["payload"]!.AsObject();
            Assert.Equal(2, payload["itemCount"]!.GetValue<int>());
            Assert.Equal(1, payload["succeededCount"]!.GetValue<int>());
            Assert.Equal(1, payload["failedCount"]!.GetValue<int>());
            Assert.Equal("failed", payload["results"]![1]!["status"]!.GetValue<string>());
            Assert.NotNull(payload["results"]![1]!["error"]);
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
