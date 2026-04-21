using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task InitPlan_CanSkipShortTranscriptSegmentsBeforeSeeding()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-transcript-min-{Guid.NewGuid():N}");
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
                { "id": "seg-001", "start": "00:00:00", "end": "00:00:00.2500000", "text": "Too short" },
                { "id": "seg-002", "start": "00:00:00.2500000", "end": "00:00:01", "text": "Keep" },
                { "id": "seg-003", "start": "00:00:01", "end": "00:00:02", "text": "Keep too" }
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
                "2",
                "--min-transcript-segment-duration-ms",
                "500");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var editPlan = payload["editPlan"]!.AsObject();
            var clips = editPlan["clips"]!.AsArray();

            var clip = Assert.Single(clips);
            Assert.Equal("seg-002..seg-003", clip!["label"]!.GetValue<string>());
            Assert.Equal("00:00:00.2500000", clip["in"]!.GetValue<string>());
            Assert.Equal("00:00:02", clip["out"]!.GetValue<string>());
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
