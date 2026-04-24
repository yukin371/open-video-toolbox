using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task InitPlan_SeedsClipsFromTranscriptSegments()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-transcript-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(
            transcriptPath,
            """
            {
              "schemaVersion": 1,
              "language": "en",
              "segments": [
                { "id": "seg-001", "start": "00:00:00", "end": "00:00:01.2000000", "text": "Hello" },
                { "id": "seg-002", "start": "00:00:01.2000000", "end": "00:00:02.4000000", "text": "World" }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync(
                "init-plan",
                "input.mp4",
                "--template",
                "shorts-basic",
                "--output",
                planPath,
                "--render-output",
                "final.mp4",
                "--transcript",
                transcriptPath,
                "--seed-from-transcript");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var editPlan = payload["editPlan"]!.AsObject();
            var clips = editPlan["clips"]!.AsArray();

            Assert.Equal(2, clips.Count);
            Assert.Equal("seg-001", clips[0]!["label"]!.GetValue<string>());
            Assert.Equal("00:00:01.2000000", clips[0]!["out"]!.GetValue<string>());
            Assert.Equal("seg-002", clips[1]!["label"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(transcriptPath), editPlan["transcript"]!["path"]!.GetValue<string>());
            Assert.Equal(2, editPlan["transcript"]!["segmentCount"]!.GetValue<int>());
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
    public async Task InitPlan_TimelineEffectsStarter_SeedsTimelineFromTranscriptSegments()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-transcript-v2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var planPath = Path.Combine(outputDirectory, "edit.v2.json");

        await File.WriteAllTextAsync(
            transcriptPath,
            """
            {
              "schemaVersion": 1,
              "language": "en",
              "segments": [
                { "id": "seg-001", "start": "00:00:00", "end": "00:00:01.2000000", "text": "Hello" },
                { "id": "seg-002", "start": "00:00:01.2000000", "end": "00:00:02.4000000", "text": "World" }
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
                "--transcript",
                transcriptPath,
                "--seed-from-transcript");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var editPlan = payload["editPlan"]!.AsObject();
            var timeline = editPlan["timeline"]!.AsObject();
            var videoTrack = timeline["tracks"]!.AsArray()[0]!.AsObject();
            var clips = videoTrack["clips"]!.AsArray();

            Assert.Equal(2, editPlan["schemaVersion"]!.GetValue<int>());
            Assert.True(editPlan["clips"]!.AsArray().Count == 0);
            Assert.Equal(2, clips.Count);
            Assert.Equal("00:00:01.2000000", clips[0]!["out"]!.GetValue<string>());
            Assert.Equal("00:00:01.2000000", clips[1]!["start"]!.GetValue<string>());
            Assert.Equal("brightness_contrast", clips[0]!["effects"]![0]!["type"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(transcriptPath), editPlan["transcript"]!["path"]!.GetValue<string>());
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
