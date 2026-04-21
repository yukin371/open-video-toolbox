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
}
