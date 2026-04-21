using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task PluginValidation_FailsWhenPluginDirectoryNotFound()
    {
        var result = await RunCliAsync("templates", "--plugin-dir", "nonexistent-dir");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("was not found", result.StdErr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PluginValidation_FailsWhenPluginJsonMissing()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ovt-plugin-no-manifest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        try
        {
            var result = await RunCliAsync("templates", "--plugin-dir", dir);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("plugin.json", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("was not found", result.StdErr, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task PluginValidation_FailsWhenPluginJsonInvalidJson()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ovt-plugin-bad-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "plugin.json"), "not json");

            var result = await RunCliAsync("templates", "--plugin-dir", dir);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("invalid JSON", result.StdErr, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task PluginValidation_FailsWhenPluginMissingId()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ovt-plugin-no-id-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "plugin.json"), """
                {
                  "schemaVersion": 1,
                  "displayName": "Test",
                  "version": "1.0.0",
                  "description": "Test",
                  "templates": []
                }
                """);

            var result = await RunCliAsync("templates", "--plugin-dir", dir);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("missing 'id'", result.StdErr, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task PluginValidation_FailsWhenPluginMissingTemplates()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ovt-plugin-no-templates-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "plugin.json"), """
                {
                  "schemaVersion": 1,
                  "id": "test-plugin",
                  "displayName": "Test",
                  "version": "1.0.0",
                  "description": "Test",
                  "templates": []
                }
                """);

            var result = await RunCliAsync("templates", "--plugin-dir", dir);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("at least one template", result.StdErr, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task PluginValidation_FailsWhenTemplateJsonMissing()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ovt-plugin-no-template-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "plugin.json"), """
                {
                  "schemaVersion": 1,
                  "id": "test-plugin",
                  "displayName": "Test",
                  "version": "1.0.0",
                  "description": "Test",
                  "templates": [{ "id": "my-template", "path": "templates/my-template" }]
                }
                """);

            var result = await RunCliAsync("templates", "--plugin-dir", dir);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("template.json", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("was not found", result.StdErr, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
