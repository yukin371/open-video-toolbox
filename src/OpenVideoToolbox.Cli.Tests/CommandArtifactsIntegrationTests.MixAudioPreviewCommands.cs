using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task RenderAndMixAudioPreview_ReturnUnifiedEnvelope()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-preview-envelope-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var inputPath = Path.Combine(outputDirectory, "input.mp4");
        var planPath = Path.Combine(outputDirectory, "edit.json");
        await File.WriteAllTextAsync(inputPath, "fake-media");
        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": {
                "inputPath": "input.mp4"
              },
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
                {
                  "id": "clip-001",
                  "in": "00:00:00",
                  "out": "00:00:03"
                }
              ],
              "audioTracks": [],
              "artifacts": [],
              "output": {
                "path": "final.mp4",
                "container": "mp4"
              }
            }
            """);

        try
        {
            var render = JsonNode.Parse((await RunCliAsync("render", "--plan", planPath, "--preview")).StdOut)!.AsObject();
            Assert.Equal("render", render["command"]!.GetValue<string>());
            Assert.True(render["preview"]!.GetValue<bool>());
            var renderPayload = render["payload"]!.AsObject();
            var renderPreview = renderPayload["executionPreview"]!.AsObject();
            Assert.Equal("render", renderPreview["operation"]!.GetValue<string>());
            Assert.True(renderPreview["isPreview"]!.GetValue<bool>());
            Assert.True(renderPreview["pathsResolved"]!.GetValue<bool>());
            Assert.Equal("plugin", renderPayload["templateSource"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", renderPayload["templateSource"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", renderPayload["templateSource"]!["pluginVersion"]!.GetValue<string>());

            var mixedOutputPath = Path.Combine(outputDirectory, "mixed.wav");
            var mixAudio = JsonNode.Parse((await RunCliAsync("mix-audio", "--plan", planPath, "--output", mixedOutputPath, "--preview")).StdOut)!.AsObject();
            Assert.Equal("mix-audio", mixAudio["command"]!.GetValue<string>());
            Assert.True(mixAudio["preview"]!.GetValue<bool>());
            var mixPayload = mixAudio["payload"]!.AsObject();
            var mixPreview = mixPayload["executionPreview"]!.AsObject();
            Assert.Equal("mix-audio", mixPreview["operation"]!.GetValue<string>());
            Assert.True(mixPreview["isPreview"]!.GetValue<bool>());
            Assert.True(mixPreview["pathsResolved"]!.GetValue<bool>());
            Assert.Equal(mixedOutputPath, mixPayload["mixAudio"]!["outputPath"]!.GetValue<string>());
            Assert.Equal("plugin", mixPayload["templateSource"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", mixPayload["templateSource"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", mixPayload["templateSource"]!["pluginVersion"]!.GetValue<string>());
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
}
