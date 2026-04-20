using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task TemplatesSummary_WithPluginDir_IncludesPluginMetadataAndTemplates()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"ovt-template-plugin-summary-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);

        try
        {
            var pluginDirectory = await CreateTemplatePluginAsync(
                workingDirectory,
                pluginId: "community-pack",
                pluginDisplayName: "Community Pack",
                template: CreatePluginTemplateDefinition("plugin-captioned", "Plugin Captioned"));

            var result = await RunCliAsync("templates", "--summary", "--plugin-dir", pluginDirectory);

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var plugins = payload["plugins"]!.AsArray();
            var plugin = Assert.IsType<JsonObject>(Assert.Single(plugins));
            Assert.Equal("community-pack", plugin["id"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(pluginDirectory), plugin["directory"]!.GetValue<string>());

            var templates = payload["templates"]!.AsArray();
            Assert.Contains(
                templates,
                node => node is JsonObject templateNode
                    && templateNode["id"]!.GetValue<string>() == "plugin-captioned");
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
    public async Task TemplateGuide_WithPluginDir_ReturnsPluginSourceAndWritesExamples()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"ovt-template-plugin-guide-{Guid.NewGuid():N}");
        var outputDirectory = Path.Combine(workingDirectory, "examples");
        Directory.CreateDirectory(workingDirectory);

        try
        {
            var pluginDirectory = await CreateTemplatePluginAsync(
                workingDirectory,
                pluginId: "community-pack",
                pluginDisplayName: "Community Pack",
                template: CreatePluginTemplateDefinition("plugin-captioned", "Plugin Captioned"));

            var result = await RunCliAsync(
                "templates",
                "plugin-captioned",
                "--plugin-dir",
                pluginDirectory,
                "--write-examples",
                outputDirectory);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(Path.Combine(outputDirectory, "guide.json")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "template.json")));

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("plugin", payload["source"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", payload["source"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("plugin-captioned", payload["template"]!["id"]!.GetValue<string>());

            var guide = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "guide.json")))!.AsObject();
            Assert.Equal("plugin", guide["source"]!["kind"]!.GetValue<string>());
            var previewPlan = guide["examples"]!["previewPlans"]![0]!["editPlan"]!.AsObject();
            Assert.Equal("plugin", previewPlan["template"]!["source"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", previewPlan["template"]!["source"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", previewPlan["template"]!["source"]!["pluginVersion"]!.GetValue<string>());

            var template = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "template.json")))!.AsObject();
            Assert.Equal("plugin-captioned", template["id"]!.GetValue<string>());

            var commands = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.json")))!.AsObject();
            Assert.Equal("<plugin-dir>", commands["variables"]!["pluginDir"]!.GetValue<string>());
            Assert.Contains(
                commands["initPlanCommands"]!.AsArray(),
                node => node!.GetValue<string>().Contains("--plugin-dir <plugin-dir>", StringComparison.Ordinal));
            Assert.Contains(
                commands["workflowCommands"]!.AsArray(),
                node => node!.GetValue<string>() == "ovt validate-plan --plan edit.json --plugin-dir <plugin-dir>");

            var powerShellScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.ps1"));
            Assert.Contains("$PluginDir = \"<plugin-dir>\"", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("ovt validate-plan --plan edit.json --plugin-dir $PluginDir", powerShellScript, StringComparison.Ordinal);
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
    public async Task TemplatesSummary_WithPluginDir_FailsWhenTemplateIdConflictsWithBuiltInCatalog()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"ovt-template-plugin-duplicate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);

        try
        {
            var pluginDirectory = await CreateTemplatePluginAsync(
                workingDirectory,
                pluginId: "community-pack",
                pluginDisplayName: "Community Pack",
                template: CreatePluginTemplateDefinition("shorts-captioned", "Conflicting Plugin Template"));

            var result = await RunCliAsync("templates", "--summary", "--plugin-dir", pluginDirectory);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("Duplicate edit plan template id 'shorts-captioned'.", result.StdErr, StringComparison.Ordinal);
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
