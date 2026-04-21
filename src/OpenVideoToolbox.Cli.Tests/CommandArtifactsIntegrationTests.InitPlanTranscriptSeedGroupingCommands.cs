using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task InitPlan_CanGroupTranscriptSegmentsIntoMergedSeedClips()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-transcript-group-{Guid.NewGuid():N}");
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
                { "id": "seg-001", "start": "00:00:00", "end": "00:00:01", "text": "Intro" },
                { "id": "seg-002", "start": "00:00:01", "end": "00:00:02.5000000", "text": "Detail" },
                { "id": "seg-003", "start": "00:00:02.5000000", "end": "00:00:04", "text": "Wrap" }
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
                "--seed-from-transcript",
                "--transcript-segment-group-size",
                "2");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var editPlan = payload["editPlan"]!.AsObject();
            var clips = editPlan["clips"]!.AsArray();

            Assert.Equal(2, clips.Count);
            Assert.Equal("seg-001..seg-002", clips[0]!["label"]!.GetValue<string>());
            Assert.Equal("00:00:02.5000000", clips[0]!["out"]!.GetValue<string>());
            Assert.Equal("seg-003", clips[1]!["label"]!.GetValue<string>());
            Assert.Equal("00:00:02.5000000", clips[1]!["in"]!.GetValue<string>());
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
    public async Task InitPlan_CanSplitTranscriptSeedGroupsWhenGapExceedsThreshold()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-transcript-gap-{Guid.NewGuid():N}");
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
                { "id": "seg-001", "start": "00:00:00", "end": "00:00:00.6000000", "text": "First" },
                { "id": "seg-002", "start": "00:00:00.7500000", "end": "00:00:01.3000000", "text": "Near" },
                { "id": "seg-003", "start": "00:00:02", "end": "00:00:02.8000000", "text": "Far" }
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
                "--seed-from-transcript",
                "--transcript-segment-group-size",
                "3",
                "--max-transcript-gap-ms",
                "200");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var editPlan = payload["editPlan"]!.AsObject();
            var clips = editPlan["clips"]!.AsArray();

            Assert.Equal(2, clips.Count);
            Assert.Equal("seg-001..seg-002", clips[0]!["label"]!.GetValue<string>());
            Assert.Equal("00:00:01.3000000", clips[0]!["out"]!.GetValue<string>());
            Assert.Equal("seg-003", clips[1]!["label"]!.GetValue<string>());
            Assert.Equal("00:00:02", clips[1]!["in"]!.GetValue<string>());
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
