using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task ScaffoldTemplate_WritesCommandArtifactsAlongsideEditPlan()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-scaffold-{Guid.NewGuid():N}");

        try
        {
            var result = await RunCliAsync("scaffold-template", "input.mp4", "--template", "shorts-captioned", "--dir", outputDirectory);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(Path.Combine(outputDirectory, "edit.json")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "commands.json")));

            var scaffold = JsonNode.Parse(result.StdOut)!.AsObject()["scaffold"]!.AsObject();
            var writtenFiles = scaffold["writtenFiles"]!.AsArray().Select(node => Path.GetFileName(node!.GetValue<string>())).ToArray();
            Assert.Contains("edit.json", writtenFiles);
            Assert.Contains("commands.json", writtenFiles);
            Assert.Contains("commands.ps1", writtenFiles);
            Assert.Contains("commands.cmd", writtenFiles);
            Assert.Contains("commands.sh", writtenFiles);
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
    public async Task ScaffoldTemplate_WithPluginDir_WritesPluginTemplateOutputs()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"ovt-scaffold-plugin-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);
        var outputDirectory = Path.Combine(workingDirectory, "scaffold");

        try
        {
            var pluginDirectory = await CreateTemplatePluginAsync(
                workingDirectory,
                pluginId: "community-pack",
                pluginDisplayName: "Community Pack",
                template: CreatePluginTemplateDefinition("plugin-captioned", "Plugin Captioned"));

            var result = await RunCliAsync(
                "scaffold-template",
                "input.mp4",
                "--template",
                "plugin-captioned",
                "--dir",
                outputDirectory,
                "--validate",
                "--plugin-dir",
                pluginDirectory);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(Path.Combine(outputDirectory, "edit.json")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "guide.json")));

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("plugin", payload["source"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", payload["source"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", payload["source"]!["pluginVersion"]!.GetValue<string>());
            Assert.Equal("plugin-captioned", payload["template"]!["id"]!.GetValue<string>());
            Assert.Contains(payload["scaffold"]!["writtenFiles"]!.AsArray(), node => Path.GetFileName(node!.GetValue<string>()) == "guide.json");
            Assert.True(payload["validated"]!.GetValue<bool>());
            Assert.True(payload["validation"]!["isValid"]!.GetValue<bool>());

            var guide = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "guide.json")))!.AsObject();
            Assert.Equal("plugin", guide["source"]!["kind"]!.GetValue<string>());
            var previewPlan = guide["examples"]!["previewPlans"]![0]!["editPlan"]!.AsObject();
            Assert.Equal("plugin", previewPlan["template"]!["source"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", previewPlan["template"]!["source"]!["pluginId"]!.GetValue<string>());

            var editPlan = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "edit.json")))!.AsObject();
            Assert.Equal("plugin", editPlan["template"]!["source"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", editPlan["template"]!["source"]!["pluginId"]!.GetValue<string>());

            var commands = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.json")))!.AsObject();
            Assert.Equal("<plugin-dir>", commands["variables"]!["pluginDir"]!.GetValue<string>());
            Assert.Contains(
                commands["workflowCommands"]!.AsArray(),
                node => node!.GetValue<string>() == "ovt inspect-plan --plan edit.json --check-files --plugin-dir <plugin-dir>");
            Assert.Contains(
                commands["workflowCommands"]!.AsArray(),
                node => node!.GetValue<string>() == "ovt validate-plan --plan edit.json --check-files --plugin-dir <plugin-dir>");
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
    public async Task ScaffoldTemplate_ForBeatMontage_WritesStemFirstArtifactsExample()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-scaffold-beat-montage-{Guid.NewGuid():N}");

        try
        {
            var result = await RunCliAsync("scaffold-template", "input.mp4", "--template", "beat-montage", "--dir", outputDirectory);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(Path.Combine(outputDirectory, "artifacts.json")));

            var artifacts = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "artifacts.json")))!.AsObject();
            Assert.Equal("stems/htdemucs/input/no_vocals.wav", artifacts["bgm"]!.GetValue<string>());
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
