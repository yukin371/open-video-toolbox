using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task ReplacePlanMaterialBatch_WritesSummaryAndResultArtifacts()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-replace-batch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(outputDirectory, "tasks", "job-a", "audio"));

        var planPath = Path.Combine(outputDirectory, "tasks", "job-a", "edit.json");
        var replacementPath = Path.Combine(outputDirectory, "tasks", "job-a", "audio", "updated.wav");
        var manifestPath = Path.Combine(outputDirectory, "batch.json");

        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "tasks", "job-a", "input.mp4"), "video");
        await File.WriteAllTextAsync(replacementPath, "audio");
        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "audioTracks": [
                { "id": "voice-main", "role": "voice", "path": "dub.wav" }
              ],
              "output": { "path": "final.mp4", "container": "mp4" }
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
                  "plan": "tasks/job-a/edit.json",
                  "path": "tasks/job-a/audio/updated.wav",
                  "audioTrackId": "voice-main",
                  "checkFiles": true,
                  "pathStyle": "relative"
                }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync("replace-plan-material-batch", "--manifest", manifestPath);

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("replace-plan-material-batch", envelope["command"]!.GetValue<string>());

            var payload = envelope["payload"]!.AsObject();
            Assert.Equal(1, payload["itemCount"]!.GetValue<int>());
            Assert.Equal(1, payload["succeededCount"]!.GetValue<int>());
            Assert.Equal(0, payload["failedCount"]!.GetValue<int>());
            Assert.True(File.Exists(payload["summaryPath"]!.GetValue<string>()));

            var item = payload["results"]![0]!.AsObject();
            Assert.Equal("job-a", item["id"]!.GetValue<string>());
            Assert.Equal("succeeded", item["status"]!.GetValue<string>());
            Assert.True(File.Exists(item["resultPath"]!.GetValue<string>()));

            var updatedPlan = JsonNode.Parse(await File.ReadAllTextAsync(planPath))!.AsObject();
            Assert.Equal(Path.Combine("audio", "updated.wav"), updatedPlan["audioTracks"]![0]!["path"]!.GetValue<string>());
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
    public async Task ReplacePlanMaterialBatch_ReturnsPartialFailureSummary()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-replace-batch-partial-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(outputDirectory, "tasks", "job-a", "audio"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "tasks", "job-b"));

        var validPlanPath = Path.Combine(outputDirectory, "tasks", "job-a", "edit.json");
        var invalidPlanPath = Path.Combine(outputDirectory, "tasks", "job-b", "edit.json");
        var replacementPath = Path.Combine(outputDirectory, "tasks", "job-a", "audio", "updated.wav");
        var manifestPath = Path.Combine(outputDirectory, "batch.json");

        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "tasks", "job-a", "input.mp4"), "video-a");
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "tasks", "job-b", "input.mp4"), "video-b");
        await File.WriteAllTextAsync(replacementPath, "audio");
        await File.WriteAllTextAsync(
            validPlanPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "audioTracks": [
                { "id": "voice-main", "role": "voice", "path": "dub.wav" }
              ],
              "output": { "path": "final-a.mp4", "container": "mp4" }
            }
            """);
        await File.WriteAllTextAsync(
            invalidPlanPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "audioTracks": [
                { "id": "voice-main", "role": "voice", "path": "dub.wav" }
              ],
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
                  "plan": "tasks/job-a/edit.json",
                  "path": "tasks/job-a/audio/updated.wav",
                  "audioTrackId": "voice-main",
                  "checkFiles": true,
                  "pathStyle": "relative"
                },
                {
                  "id": "job-b",
                  "plan": "tasks/job-b/edit.json",
                  "path": "tasks/job-b/missing/updated.wav",
                  "audioTrackId": "voice-main",
                  "writeTo": "outputs/job-b.edit.json",
                  "checkFiles": true,
                  "requireValid": true
                }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync("replace-plan-material-batch", "--manifest", manifestPath);

            Assert.Equal(2, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!["payload"]!.AsObject();
            Assert.Equal(2, payload["itemCount"]!.GetValue<int>());
            Assert.Equal(1, payload["succeededCount"]!.GetValue<int>());
            Assert.Equal(1, payload["failedCount"]!.GetValue<int>());
            Assert.True(File.Exists(payload["summaryPath"]!.GetValue<string>()));

            var failedItem = payload["results"]![1]!.AsObject();
            Assert.Equal("failed", failedItem["status"]!.GetValue<string>());
            Assert.NotNull(failedItem["error"]);
            Assert.True(File.Exists(failedItem["resultPath"]!.GetValue<string>()));
            Assert.False(File.Exists(Path.Combine(outputDirectory, "outputs", "job-b.edit.json")));
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
