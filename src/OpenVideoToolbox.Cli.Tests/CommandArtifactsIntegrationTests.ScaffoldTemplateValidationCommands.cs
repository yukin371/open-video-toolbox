using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task ScaffoldTemplateValidate_ReturnsValidationSummaryAndGeneratedPlan()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-scaffold-validate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync(
                "scaffold-template",
                "input.mp4",
                "--template",
                "shorts-captioned",
                "--dir",
                outputDirectory,
                "--validate");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var scaffold = payload["scaffold"]!.AsObject();
            var validation = payload["validation"]!.AsObject();
            var editPlan = payload["editPlan"]!.AsObject();

            Assert.True(payload["validated"]!.GetValue<bool>());
            Assert.False(payload["probed"]!.GetValue<bool>());
            Assert.Equal(outputDirectory, scaffold["outputDirectory"]!.GetValue<string>());
            Assert.Equal(Path.Combine(outputDirectory, "edit.json"), scaffold["planPath"]!.GetValue<string>());
            Assert.Contains(scaffold["writtenFiles"]!.AsArray(), node => Path.GetFileName(node!.GetValue<string>()) == "edit.json");
            Assert.Contains(scaffold["writtenFiles"]!.AsArray(), node => Path.GetFileName(node!.GetValue<string>()) == "commands.json");

            Assert.False(validation["checkFiles"]!.GetValue<bool>());
            Assert.True(validation["isValid"]!.GetValue<bool>());
            Assert.Empty(validation["issues"]!.AsArray());

            Assert.Equal("shorts-captioned", payload["template"]!["id"]!.GetValue<string>());
            Assert.Equal("shorts-captioned", editPlan["template"]!["id"]!.GetValue<string>());
            Assert.Equal("input.mp4", editPlan["source"]!["inputPath"]!.GetValue<string>());
            Assert.True(File.Exists(Path.Combine(outputDirectory, "edit.json")));
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
    public async Task ScaffoldTemplateCheckFiles_ReturnsFailureValidationWhenInputIsMissing()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-scaffold-check-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync(
                "scaffold-template",
                "input.mp4",
                "--template",
                "shorts-captioned",
                "--dir",
                outputDirectory,
                "--validate",
                "--check-files");

            Assert.Equal(1, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var validation = payload["validation"]!.AsObject();
            var issueNode = Assert.Single(validation["issues"]!.AsArray());
            var issue = Assert.IsType<JsonObject>(issueNode);

            Assert.True(payload["validated"]!.GetValue<bool>());
            Assert.True(validation["checkFiles"]!.GetValue<bool>());
            Assert.False(validation["isValid"]!.GetValue<bool>());
            Assert.Equal("source.inputPath", issue["path"]!.GetValue<string>());
            Assert.Equal("source.inputPath.missing", issue["code"]!.GetValue<string>());
            Assert.Contains("input.mp4", issue["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(outputDirectory, "edit.json")));
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
