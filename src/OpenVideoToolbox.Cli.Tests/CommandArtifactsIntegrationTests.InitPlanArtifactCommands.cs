using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task InitPlan_AppliesArtifactBindingsAndTemplateParameterOverrides()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-artifacts-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var artifactsPath = Path.Combine(outputDirectory, "artifacts.json");
        var templateParamsPath = Path.Combine(outputDirectory, "template-params.json");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(artifactsPath, """{"subtitles":"subs\\captions.srt"}""");
        await File.WriteAllTextAsync(templateParamsPath, """{"hookStyle":"match-cut","captionStyle":"clean-sidecar"}""");

        try
        {
            var result = await RunCliAsync(
                "init-plan",
                "input.mp4",
                "--template",
                "shorts-captioned",
                "--output",
                planPath,
                "--render-output",
                "final.mp4",
                "--artifacts",
                artifactsPath,
                "--template-params",
                templateParamsPath);

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var editPlan = payload["editPlan"]!.AsObject();
            var artifacts = editPlan["artifacts"]!.AsArray();
            var artifactNode = Assert.Single(artifacts);
            var artifact = Assert.IsType<JsonObject>(artifactNode);

            Assert.Equal(Path.GetFullPath(planPath), payload["planPath"]!.GetValue<string>());
            Assert.False(payload["probed"]!.GetValue<bool>());
            Assert.Equal("match-cut", editPlan["template"]!["parameters"]!["hookStyle"]!.GetValue<string>());
            Assert.Equal("clean-sidecar", editPlan["template"]!["parameters"]!["captionStyle"]!.GetValue<string>());
            Assert.Equal("subtitles", artifact["slotId"]!.GetValue<string>());
            Assert.Equal("subs\\captions.srt", artifact["path"]!.GetValue<string>());
            Assert.Equal("subs\\captions.srt", editPlan["subtitles"]!["path"]!.GetValue<string>());
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
    public async Task InitPlan_TimelineEffectsStarter_WritesSchemaV2PlanThatRenderPreviewCanConsume()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-v2-template-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var artifactsPath = Path.Combine(outputDirectory, "artifacts.json");
        var planPath = Path.Combine(outputDirectory, "edit.v2.json");

        await File.WriteAllTextAsync(artifactsPath, """{"bgm":"audio\\bed.wav"}""");

        try
        {
            var initResult = await RunCliAsync(
                "init-plan",
                "input.mp4",
                "--template",
                "timeline-effects-starter",
                "--output",
                planPath,
                "--render-output",
                "timeline-final.mp4",
                "--artifacts",
                artifactsPath);

            Assert.Equal(0, initResult.ExitCode);

            var initPayload = JsonNode.Parse(initResult.StdOut)!.AsObject();
            var editPlan = initPayload["editPlan"]!.AsObject();

            Assert.Equal(Path.GetFullPath(planPath), initPayload["planPath"]!.GetValue<string>());
            Assert.Equal(2, editPlan["schemaVersion"]!.GetValue<int>());
            Assert.Equal("timeline-effects-starter", editPlan["template"]!["id"]!.GetValue<string>());
            Assert.Equal("builtIn", editPlan["template"]!["source"]!["kind"]!.GetValue<string>());
            Assert.NotNull(editPlan["timeline"]);
            Assert.True(editPlan["clips"]!.AsArray().Count == 0);
            Assert.Equal("audio\\bed.wav", editPlan["artifacts"]![0]!["path"]!.GetValue<string>());
            Assert.Equal("volume", editPlan["timeline"]!["tracks"]![1]!["clips"]![0]!["effects"]![0]!["type"]!.GetValue<string>());
            Assert.True(File.Exists(planPath));

            var renderResult = await RunCliAsync("render", "--plan", planPath, "--preview");

            Assert.Equal(0, renderResult.ExitCode);

            var renderPayload = JsonNode.Parse(renderResult.StdOut)!.AsObject();
            var envelope = renderPayload["payload"]!.AsObject();
            Assert.Equal(2, envelope["render"]!["schemaVersion"]!.GetValue<int>());
            Assert.Equal(2, envelope["executionPreview"]!["commandPlan"]!["schemaVersion"]!.GetValue<int>());
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
