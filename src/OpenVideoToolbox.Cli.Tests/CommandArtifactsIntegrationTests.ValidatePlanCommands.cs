using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task ValidatePlan_ReturnsSuccessJsonForMinimalValidPlan()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-validate-valid-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var inputPath = Path.Combine(outputDirectory, "input.mp4");
        var planPath = Path.Combine(outputDirectory, "valid.edit.json");
        await File.WriteAllTextAsync(inputPath, "fake-media");
        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": {
                "inputPath": "input.mp4"
              },
              "clips": [],
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
            var result = await RunCliAsync("validate-plan", "--plan", planPath);

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("validate-plan", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            Assert.True(payload["isValid"]!.GetValue<bool>());
            Assert.False(payload["checkFiles"]!.GetValue<bool>());
            Assert.Equal(Path.GetFullPath(planPath), payload["planPath"]!.GetValue<string>());
            Assert.Equal(outputDirectory, payload["resolvedBaseDirectory"]!.GetValue<string>());
            Assert.Empty(payload["issues"]!.AsArray());
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
    public async Task ValidatePlanCheckFiles_ReturnsFailureJsonForMissingSource()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-validate-missing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "invalid.edit.json");
        var jsonOutPath = Path.Combine(outputDirectory, "validation.json");
        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": {
                "inputPath": "missing.mp4"
              },
              "clips": [],
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
            var result = await RunCliAsync("validate-plan", "--plan", planPath, "--check-files", "--json-out", jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();

            Assert.Equal(stdout.ToJsonString(), file.ToJsonString());
            Assert.Equal("validate-plan", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());

            var payload = stdout["payload"]!.AsObject();
            Assert.False(payload["isValid"]!.GetValue<bool>());
            Assert.True(payload["checkFiles"]!.GetValue<bool>());

            var issueNode = Assert.Single(payload["issues"]!.AsArray());
            var issue = Assert.IsType<JsonObject>(issueNode);
            Assert.Equal("error", issue["severity"]!.GetValue<string>());
            Assert.Equal("source.inputPath", issue["path"]!.GetValue<string>());
            Assert.Equal("source.inputPath.missing", issue["code"]!.GetValue<string>());
            Assert.Contains("missing.mp4", issue["message"]!.GetValue<string>(), StringComparison.Ordinal);
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
