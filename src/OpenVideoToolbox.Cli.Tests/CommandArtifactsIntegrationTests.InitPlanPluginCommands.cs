using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task InitPlan_WithPluginDir_CreatesPlanFromPluginTemplate()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-plugin-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);
        var planPath = Path.Combine(workingDirectory, "edit.json");

        try
        {
            var pluginDirectory = await CreateTemplatePluginAsync(
                workingDirectory,
                pluginId: "community-pack",
                pluginDisplayName: "Community Pack",
                template: CreatePluginTemplateDefinition("plugin-captioned", "Plugin Captioned"));

            var result = await RunCliAsync(
                "init-plan",
                "input.mp4",
                "--template",
                "plugin-captioned",
                "--output",
                planPath,
                "--render-output",
                "final.mp4",
                "--plugin-dir",
                pluginDirectory);

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("plugin", payload["source"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", payload["source"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", payload["source"]!["pluginVersion"]!.GetValue<string>());
            Assert.Equal("plugin-captioned", payload["template"]!["id"]!.GetValue<string>());
            Assert.Equal("plugin-captioned", payload["editPlan"]!["template"]!["id"]!.GetValue<string>());
            Assert.True(File.Exists(planPath));

            var editPlan = JsonNode.Parse(await File.ReadAllTextAsync(planPath))!.AsObject();
            Assert.Equal("plugin", editPlan["template"]!["source"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", editPlan["template"]!["source"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", editPlan["template"]!["source"]!["pluginVersion"]!.GetValue<string>());
        }
        finally
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
        }
    }
}
