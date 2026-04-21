using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
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
}
