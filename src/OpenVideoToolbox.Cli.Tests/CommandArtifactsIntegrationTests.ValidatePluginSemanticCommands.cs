using OpenVideoToolbox.Core.Editing;
using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task ValidatePlugin_FailsWhenTemplatePathEscapesPluginRoot()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"ovt-plugin-escape-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);

        try
        {
            var pluginDirectory = Path.Combine(workingDirectory, "escape-plugin");
            Directory.CreateDirectory(pluginDirectory);

            await File.WriteAllTextAsync(Path.Combine(pluginDirectory, "plugin.json"), """
                {
                  "schemaVersion": 1,
                  "id": "escape-plugin",
                  "displayName": "Escape Plugin",
                  "version": "1.0.0",
                  "description": "Invalid plugin",
                  "templates": [
                    {
                      "id": "escaped-template",
                      "path": "../outside-template"
                    }
                  ]
                }
                """);

            var result = await RunCliAsync("validate-plugin", "--plugin-dir", pluginDirectory);

            Assert.Equal(1, result.ExitCode);
            var message = JsonNode.Parse(result.StdOut)!["payload"]!["issues"]![0]!["message"]!.GetValue<string>();
            Assert.Contains("resolves outside plugin root", message, StringComparison.Ordinal);
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
    public async Task ValidatePlugin_FailsWhenArtifactSlotIdsDuplicate()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"ovt-plugin-duplicate-slot-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);

        try
        {
            var template = CreatePluginTemplateDefinition("duplicate-slot-template", "Duplicate Slot Template") with
            {
                ArtifactSlots =
                [
                    new EditPlanArtifactSlot
                    {
                        Id = "subtitles",
                        Kind = "subtitle",
                        Description = "Subtitle sidecar",
                        Required = false
                    },
                    new EditPlanArtifactSlot
                    {
                        Id = "subtitles",
                        Kind = "subtitle",
                        Description = "Duplicate subtitle sidecar",
                        Required = false
                    }
                ]
            };

            var pluginDirectory = await CreateTemplatePluginAsync(
                workingDirectory,
                pluginId: "duplicate-slot-plugin",
                pluginDisplayName: "Duplicate Slot Plugin",
                template: template);

            var result = await RunCliAsync("validate-plugin", "--plugin-dir", pluginDirectory);

            Assert.Equal(1, result.ExitCode);
            var message = JsonNode.Parse(result.StdOut)!["payload"]!["issues"]![0]!["message"]!.GetValue<string>();
            Assert.Contains("duplicate artifact slot id 'subtitles'", message, StringComparison.Ordinal);
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
