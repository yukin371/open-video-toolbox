using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task ValidatePlugin_ReturnsStructuredSuccessReport()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"ovt-validate-plugin-success-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);

        try
        {
            var pluginDirectory = await CreateTemplatePluginAsync(
                workingDirectory,
                pluginId: "community-pack",
                pluginDisplayName: "Community Pack",
                template: CreatePluginTemplateDefinition("plugin-captioned", "Plugin Captioned"));

            var result = await RunCliAsync("validate-plugin", "--plugin-dir", pluginDirectory);

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("validate-plugin", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            Assert.True(payload["isValid"]!.GetValue<bool>());
            Assert.Equal(Path.GetFullPath(pluginDirectory), payload["pluginDirectory"]!.GetValue<string>());
            Assert.Empty(payload["issues"]!.AsArray());

            var plugins = payload["plugins"]!.AsArray();
            Assert.Single(plugins);
            Assert.Equal("community-pack", plugins[0]!["id"]!.GetValue<string>());

            var templates = payload["templates"]!.AsArray();
            Assert.Single(templates);
            Assert.Equal("plugin-captioned", templates[0]!["id"]!.GetValue<string>());
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
    public async Task ValidatePlugin_ReturnsStructuredFailureReport()
    {
        var pluginDirectory = Path.Combine(Path.GetTempPath(), $"ovt-validate-plugin-missing-{Guid.NewGuid():N}");

        var result = await RunCliAsync("validate-plugin", "--plugin-dir", pluginDirectory);

        Assert.Equal(1, result.ExitCode);

        var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
        Assert.Equal("validate-plugin", envelope["command"]!.GetValue<string>());

        var payload = envelope["payload"]!.AsObject();
        Assert.False(payload["isValid"]!.GetValue<bool>());
        Assert.Equal(Path.GetFullPath(pluginDirectory), payload["pluginDirectory"]!.GetValue<string>());
        Assert.Empty(payload["plugins"]!.AsArray());
        Assert.Empty(payload["templates"]!.AsArray());

        var issue = payload["issues"]!.AsArray().Single()!.AsObject();
        Assert.Equal("plugin.validation.failed", issue["code"]!.GetValue<string>());
        Assert.Contains("was not found", issue["message"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidatePlugin_WritesJsonOut()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"ovt-validate-plugin-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);
        var jsonOutPath = Path.Combine(workingDirectory, "validate-plugin.json");

        try
        {
            var pluginDirectory = await CreateTemplatePluginAsync(
                workingDirectory,
                pluginId: "community-pack",
                pluginDisplayName: "Community Pack",
                template: CreatePluginTemplateDefinition("plugin-captioned", "Plugin Captioned"));

            var result = await RunCliAsync("validate-plugin", "--plugin-dir", pluginDirectory, "--json-out", jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut);
            var written = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath));
            Assert.Equal(stdout!.ToJsonString(), written!.ToJsonString());
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
