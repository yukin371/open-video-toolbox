using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task RenderBatch_Preview_WritesSummaryAndReturnsResults()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-batch-preview-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planAPath = Path.Combine(outputDirectory, "tasks", "a", "edit.json");
        var planBPath = Path.Combine(outputDirectory, "tasks", "b", "edit.json");
        var manifestPath = Path.Combine(outputDirectory, "batch.json");

        Directory.CreateDirectory(Path.GetDirectoryName(planAPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(planBPath)!);

        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "tasks", "a", "input.mp4"), "video-a");
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "tasks", "b", "input.mp4"), "video-b");
        await File.WriteAllTextAsync(
            planAPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "clips": [
                { "id": "clip-001", "in": "00:00:00", "out": "00:00:02", "label": "intro" }
              ],
              "audioTracks": [],
              "artifacts": [],
              "output": { "path": "final-a.mp4", "container": "mp4" }
            }
            """);
        await File.WriteAllTextAsync(
            planBPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "clips": [
                { "id": "clip-001", "in": "00:00:00", "out": "00:00:02", "label": "intro" }
              ],
              "audioTracks": [],
              "artifacts": [],
              "output": { "path": "final-b.mp4", "container": "mp4" }
            }
            """);
        await File.WriteAllTextAsync(
            manifestPath,
            """
            {
              "schemaVersion": 1,
              "items": [
                {
                  "id": "job-a",
                  "plan": "tasks/a/edit.json"
                },
                {
                  "id": "job-b",
                  "plan": "tasks/b/edit.json",
                  "output": "exports/final-b-custom.mp4"
                }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync("render-batch", "--manifest", manifestPath, "--preview");

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("render-batch", envelope["command"]!.GetValue<string>());
            Assert.True(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            Assert.True(payload["preview"]!.GetValue<bool>());
            Assert.Equal(2, payload["itemCount"]!.GetValue<int>());
            Assert.Equal(2, payload["succeededCount"]!.GetValue<int>());
            Assert.Equal(0, payload["failedCount"]!.GetValue<int>());
            Assert.True(File.Exists(payload["summaryPath"]!.GetValue<string>()));

            var results = payload["results"]!.AsArray();
            Assert.Equal("succeeded", results[0]!["status"]!.GetValue<string>());
            Assert.Equal("succeeded", results[1]!["status"]!.GetValue<string>());
            Assert.Equal(Path.Combine(outputDirectory, "tasks", "a", "final-a.mp4"), results[0]!["outputPath"]!.GetValue<string>());
            Assert.Equal(Path.Combine(outputDirectory, "exports", "final-b-custom.mp4"), results[1]!["outputPath"]!.GetValue<string>());
            Assert.NotNull(results[0]!["result"]!["executionPreview"]);
            Assert.True(File.Exists(results[0]!["resultPath"]!.GetValue<string>()));
            Assert.True(File.Exists(results[1]!["resultPath"]!.GetValue<string>()));
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
    public async Task RenderBatch_Preview_ReturnsPartialFailureSummary()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-batch-partial-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(outputDirectory, "tasks", "a"));
        var planPath = Path.Combine(outputDirectory, "tasks", "a", "edit.json");
        var manifestPath = Path.Combine(outputDirectory, "batch.json");

        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "tasks", "a", "input.mp4"), "video-a");
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
              "output": { "path": "final-a.mp4", "container": "mp4" }
            }
            """);
        await File.WriteAllTextAsync(
            manifestPath,
            """
            {
              "schemaVersion": 1,
              "items": [
                {
                  "id": "job-a",
                  "plan": "tasks/a/edit.json"
                },
                {
                  "id": "job-missing",
                  "plan": "tasks/missing/edit.json"
                }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync("render-batch", "--manifest", manifestPath, "--preview");

            Assert.Equal(2, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!["payload"]!.AsObject();
            Assert.Equal(2, payload["itemCount"]!.GetValue<int>());
            Assert.Equal(1, payload["succeededCount"]!.GetValue<int>());
            Assert.Equal(1, payload["failedCount"]!.GetValue<int>());
            Assert.Equal("failed", payload["results"]![1]!["status"]!.GetValue<string>());
            Assert.NotNull(payload["results"]![1]!["error"]);
            Assert.True(File.Exists(payload["results"]![1]!["resultPath"]!.GetValue<string>()));
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
