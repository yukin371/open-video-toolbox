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
    public async Task RenderPreview_CanWriteEnvelopeToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-preview-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var jsonOutPath = Path.Combine(outputDirectory, "render-preview.json");

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
            var result = await RunCliAsync("render", "--plan", planPath, "--preview", "--json-out", jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();

            Assert.Equal(stdout.ToJsonString(), file.ToJsonString());
            Assert.Equal("render", stdout["command"]!.GetValue<string>());
            Assert.True(stdout["preview"]!.GetValue<bool>());
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
    public async Task RenderPreview_AppliesOutputOverrideToExecutionPreview()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-override-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var overrideOutputPath = Path.Combine(outputDirectory, "custom-out.mp4");

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
            var result = await RunCliAsync("render", "--plan", planPath, "--output", "custom-out.mp4", "--preview");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("render", payload["command"]!.GetValue<string>());
            Assert.True(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            var executionPreview = envelope["executionPreview"]!.AsObject();
            var producedPath = Assert.Single(executionPreview["producedPaths"]!.AsArray())!.GetValue<string>();

            Assert.Equal(overrideOutputPath, envelope["render"]!["output"]!["path"]!.GetValue<string>());
            Assert.Equal(overrideOutputPath, producedPath);
            Assert.Contains(executionPreview["commandPlan"]!["arguments"]!.AsArray().Select(node => node!.GetValue<string>()), arg => arg == overrideOutputPath);
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
    public async Task RenderPreview_RejectsPlansWithoutClips()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-preview-empty-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "template": {
                "id": "plugin-captioned",
                "source": {
                  "kind": "plugin",
                  "pluginId": "community-pack",
                  "pluginVersion": "1.0.0"
                },
                "parameters": {}
              },
              "clips": [],
              "audioTracks": [],
              "artifacts": [],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync("render", "--plan", planPath, "--preview");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Render plan must contain at least one clip.", result.StdErr, StringComparison.Ordinal);
            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("render", payload["command"]!.GetValue<string>());
            Assert.True(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal("plugin", envelope["templateSource"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", envelope["templateSource"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", envelope["templateSource"]!["pluginVersion"]!.GetValue<string>());
            Assert.Equal("plugin-captioned", envelope["render"]!["template"]!["id"]!.GetValue<string>());
            Assert.Contains("Render plan must contain at least one clip.", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
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
