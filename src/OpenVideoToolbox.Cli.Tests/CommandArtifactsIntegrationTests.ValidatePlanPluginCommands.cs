using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task ValidatePlan_WithPluginDir_AllowsPluginTemplatePlan()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"ovt-validate-plugin-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);
        var planPath = Path.Combine(workingDirectory, "edit.json");

        try
        {
            var pluginDirectory = await CreateTemplatePluginAsync(
                workingDirectory,
                pluginId: "community-pack",
                pluginDisplayName: "Community Pack",
                template: CreatePluginTemplateDefinition("plugin-captioned", "Plugin Captioned"));

            var initResult = await RunCliAsync(
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

            Assert.Equal(0, initResult.ExitCode);

            var result = await RunCliAsync(
                "validate-plan",
                "--plan",
                planPath,
                "--plugin-dir",
                pluginDirectory);

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject()["payload"]!.AsObject();
            Assert.True(payload["isValid"]!.GetValue<bool>());
            Assert.Empty(payload["issues"]!.AsArray());
        }
        finally
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ValidatePlan_WithoutPluginDir_ReportsPluginCatalogRequirement()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"ovt-validate-plugin-missing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);
        var planPath = Path.Combine(workingDirectory, "edit.json");

        try
        {
            var pluginDirectory = await CreateTemplatePluginAsync(
                workingDirectory,
                pluginId: "community-pack",
                pluginDisplayName: "Community Pack",
                template: CreatePluginTemplateDefinition("plugin-captioned", "Plugin Captioned"));

            var initResult = await RunCliAsync(
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

            Assert.Equal(0, initResult.ExitCode);

            var result = await RunCliAsync("validate-plan", "--plan", planPath);

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject()["payload"]!.AsObject();
            Assert.False(payload["isValid"]!.GetValue<bool>());
            Assert.Contains(
                payload["issues"]!.AsArray(),
                node => node!["code"]!.GetValue<string>() == "template.source.catalog.required");
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
