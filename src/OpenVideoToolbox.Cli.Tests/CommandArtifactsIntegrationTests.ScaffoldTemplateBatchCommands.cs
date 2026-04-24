using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task ScaffoldTemplateBatch_WritesTaskWorkdirsAndSummary()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-scaffold-batch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(outputDirectory, "inputs"));
        var manifestPath = Path.Combine(outputDirectory, "batch.json");

        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "inputs", "a.mp4"), "video-a");
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "inputs", "b.mp4"), "video-b");
        await File.WriteAllTextAsync(
            manifestPath,
            """
            {
              "schemaVersion": 1,
              "items": [
                {
                  "id": "job-a",
                  "input": "inputs/a.mp4",
                  "template": "shorts-captioned"
                },
                {
                  "id": "job-b",
                  "input": "inputs/b.mp4",
                  "template": "beat-montage",
                  "workdir": "custom/job-b"
                }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync("scaffold-template-batch", "--manifest", manifestPath);

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("scaffold-template-batch", envelope["command"]!.GetValue<string>());
            var payload = envelope["payload"]!.AsObject();
            Assert.Equal(2, payload["itemCount"]!.GetValue<int>());
            Assert.Equal(2, payload["succeededCount"]!.GetValue<int>());
            Assert.Equal(0, payload["failedCount"]!.GetValue<int>());

            var summaryPath = payload["summaryPath"]!.GetValue<string>();
            Assert.True(File.Exists(summaryPath));

            var summary = JsonNode.Parse(await File.ReadAllTextAsync(summaryPath))!.AsObject();
            Assert.Equal(2, summary["itemCount"]!.GetValue<int>());

            var results = payload["results"]!.AsArray();
            Assert.Equal("job-a", results[0]!["id"]!.GetValue<string>());
            Assert.Equal("succeeded", results[0]!["status"]!.GetValue<string>());
            Assert.EndsWith(Path.Combine("tasks", "job-a"), results[0]!["workdir"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(Path.Combine(outputDirectory, "tasks", "job-a", "edit.json")));

            Assert.Equal("job-b", results[1]!["id"]!.GetValue<string>());
            Assert.Equal("succeeded", results[1]!["status"]!.GetValue<string>());
            Assert.EndsWith(Path.Combine("custom", "job-b"), results[1]!["workdir"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(Path.Combine(outputDirectory, "custom", "job-b", "edit.json")));
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
    public async Task ScaffoldTemplateBatch_ReturnsPartialFailureSummary()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-scaffold-batch-fail-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(outputDirectory, "inputs"));
        var manifestPath = Path.Combine(outputDirectory, "batch.json");

        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "inputs", "ok.mp4"), "video-ok");
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "inputs", "bad.mp4"), "video-bad");
        await File.WriteAllTextAsync(
            manifestPath,
            """
            {
              "schemaVersion": 1,
              "items": [
                {
                  "id": "ok-job",
                  "input": "inputs/ok.mp4",
                  "template": "shorts-captioned"
                },
                {
                  "id": "bad-job",
                  "input": "inputs/bad.mp4",
                  "template": "missing-template"
                }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync("scaffold-template-batch", "--manifest", manifestPath);

            Assert.Equal(2, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!["payload"]!.AsObject();
            Assert.Equal(2, payload["itemCount"]!.GetValue<int>());
            Assert.Equal(1, payload["succeededCount"]!.GetValue<int>());
            Assert.Equal(1, payload["failedCount"]!.GetValue<int>());
            Assert.Equal("failed", payload["results"]![1]!["status"]!.GetValue<string>());
            Assert.NotNull(payload["results"]![1]!["error"]);
            Assert.True(File.Exists(payload["summaryPath"]!.GetValue<string>()));
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
