using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task RenderPreview_ReturnsStableExecutionPreview()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-preview-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");

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
            var result = await RunCliAsync("render", "--plan", planPath, "--preview");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("render", payload["command"]!.GetValue<string>());
            Assert.True(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            var executionPreview = envelope["executionPreview"]!.AsObject();
            var commandPlan = executionPreview["commandPlan"]!.AsObject();
            var producedPaths = executionPreview["producedPaths"]!.AsArray();

            Assert.Equal(Path.Combine(outputDirectory, "input.mp4"), envelope["render"]!["source"]!["inputPath"]!.GetValue<string>());
            Assert.Equal(Path.Combine(outputDirectory, "final.mp4"), envelope["render"]!["output"]!["path"]!.GetValue<string>());
            Assert.Equal("ffmpeg", commandPlan["toolName"]!.GetValue<string>());
            Assert.Contains(commandPlan["arguments"]!.AsArray().Select(node => node!.GetValue<string>()), arg => arg == "-filter_complex");
            Assert.Contains(commandPlan["arguments"]!.AsArray().Select(node => node!.GetValue<string>()), arg => arg == "libx264");
            Assert.Contains(commandPlan["arguments"]!.AsArray().Select(node => node!.GetValue<string>()), arg => arg == "+faststart");
            Assert.Equal(Path.Combine(outputDirectory, "final.mp4"), Assert.Single(producedPaths)!.GetValue<string>());
            Assert.Empty(executionPreview["sideEffects"]!.AsArray());
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
    public async Task RenderPreview_AcceptsSchemaV2Timeline()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-preview-v2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.v2.json");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 2,
              "source": { "inputPath": "input.mp4" },
              "timeline": {
                "duration": "00:00:03",
                "resolution": { "w": 1920, "h": 1080 },
                "frameRate": 30,
                "tracks": [
                  {
                    "id": "main",
                    "kind": "video",
                    "clips": [
                      { "id": "clip-001", "start": "00:00:00", "in": "00:00:00", "out": "00:00:03" }
                    ]
                  }
                ]
              },
              "output": { "path": "final-v2.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync("render", "--plan", planPath, "--preview");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("render", payload["command"]!.GetValue<string>());
            Assert.True(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            var executionPreview = envelope["executionPreview"]!.AsObject();
            var commandPlan = executionPreview["commandPlan"]!.AsObject();

            Assert.Equal(2, commandPlan["schemaVersion"]!.GetValue<int>());
            Assert.Equal(Path.Combine(outputDirectory, "input.mp4"), envelope["render"]!["source"]!["inputPath"]!.GetValue<string>());
            Assert.Equal(Path.Combine(outputDirectory, "final-v2.mp4"), envelope["render"]!["output"]!["path"]!.GetValue<string>());
            Assert.Contains(commandPlan["arguments"]!.AsArray().Select(node => node!.GetValue<string>()), arg => arg == "[v_out]");
            Assert.Contains(commandPlan["arguments"]!.AsArray().Select(node => node!.GetValue<string>()), arg => arg == "-an");
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
