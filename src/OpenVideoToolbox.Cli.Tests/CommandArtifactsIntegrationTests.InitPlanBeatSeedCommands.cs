using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task InitPlan_SeedsClipsFromBeatGroups()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-beats-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var beatsPath = Path.Combine(outputDirectory, "beats.json");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(
            beatsPath,
            """
            {
              "schemaVersion": 1,
              "sourcePath": "input.mp4",
              "sampleRateHz": 16000,
              "frameDuration": "00:00:00.0500000",
              "estimatedBpm": 128,
              "beats": [
                { "index": 0, "time": "00:00:00", "strength": 0.9 },
                { "index": 1, "time": "00:00:01", "strength": 0.88 },
                { "index": 2, "time": "00:00:02", "strength": 0.9 },
                { "index": 3, "time": "00:00:03", "strength": 0.87 },
                { "index": 4, "time": "00:00:04", "strength": 0.92 }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync(
                "init-plan",
                "input.mp4",
                "--template",
                "beat-montage",
                "--output",
                planPath,
                "--render-output",
                "final.mp4",
                "--beats",
                beatsPath,
                "--seed-from-beats",
                "--beat-group-size",
                "2");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var editPlan = payload["editPlan"]!.AsObject();
            var clips = editPlan["clips"]!.AsArray();

            Assert.Equal(2, clips.Count);
            Assert.Equal("beat-group-001", clips[0]!["label"]!.GetValue<string>());
            Assert.Equal("00:00:02", clips[0]!["out"]!.GetValue<string>());
            Assert.Equal("beat-group-002", clips[1]!["label"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(beatsPath), editPlan["beats"]!["path"]!.GetValue<string>());
            Assert.Equal(128, editPlan["beats"]!["estimatedBpm"]!.GetValue<int>());
            Assert.True(File.Exists(planPath));
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
    public async Task InitPlan_TimelineEffectsStarter_SeedsTimelineFromBeatGroups()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-beats-v2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var beatsPath = Path.Combine(outputDirectory, "beats.json");
        var planPath = Path.Combine(outputDirectory, "edit.v2.json");

        await File.WriteAllTextAsync(
            beatsPath,
            """
            {
              "schemaVersion": 1,
              "sourcePath": "input.mp4",
              "sampleRateHz": 16000,
              "frameDuration": "00:00:00.0500000",
              "estimatedBpm": 128,
              "beats": [
                { "index": 0, "time": "00:00:00", "strength": 0.9 },
                { "index": 1, "time": "00:00:01", "strength": 0.88 },
                { "index": 2, "time": "00:00:02", "strength": 0.9 },
                { "index": 3, "time": "00:00:03", "strength": 0.87 },
                { "index": 4, "time": "00:00:04", "strength": 0.92 }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync(
                "init-plan",
                "input.mp4",
                "--template",
                "timeline-effects-starter",
                "--output",
                planPath,
                "--render-output",
                "final.mp4",
                "--beats",
                beatsPath,
                "--seed-from-beats",
                "--beat-group-size",
                "2");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var editPlan = payload["editPlan"]!.AsObject();
            var timeline = editPlan["timeline"]!.AsObject();
            var videoTrack = timeline["tracks"]!.AsArray()[0]!.AsObject();
            var clips = videoTrack["clips"]!.AsArray();

            Assert.Equal(2, editPlan["schemaVersion"]!.GetValue<int>());
            Assert.True(editPlan["clips"]!.AsArray().Count == 0);
            Assert.Equal(2, clips.Count);
            Assert.Equal("00:00:02", clips[0]!["out"]!.GetValue<string>());
            Assert.Equal("00:00:02", clips[1]!["start"]!.GetValue<string>());
            Assert.Equal("brightness_contrast", clips[1]!["effects"]![0]!["type"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(beatsPath), editPlan["beats"]!["path"]!.GetValue<string>());
            Assert.Equal(128, editPlan["beats"]!["estimatedBpm"]!.GetValue<int>());
            Assert.True(File.Exists(planPath));
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
