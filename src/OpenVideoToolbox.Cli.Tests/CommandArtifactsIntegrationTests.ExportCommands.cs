using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task Export_WritesEdlAndJsonEnvelope()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-export-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var jsonOutPath = Path.Combine(outputDirectory, "export-result.json");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "clips": [
                { "id": "clip-001", "in": "00:00:00", "out": "00:00:02", "label": "intro" }
              ],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync(
                "export",
                "--plan", planPath,
                "--format", "edl",
                "--output", "exports/plan.edl",
                "--json-out", jsonOutPath);

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            var savedEnvelope = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(envelope, savedEnvelope));
            Assert.Equal("export", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            var export = payload["export"]!.AsObject();
            var outputPath = Path.Combine(outputDirectory, "exports", "plan.edl");

            Assert.Equal(Path.GetFullPath(planPath), export["planPath"]!.GetValue<string>());
            Assert.Equal("edl", export["format"]!.GetValue<string>());
            Assert.Equal("L1", export["fidelityLevel"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(outputPath), export["outputPath"]!.GetValue<string>());
            Assert.Equal(30, export["frameRate"]!.GetValue<int>());
            Assert.Equal(1, export["eventCount"]!.GetValue<int>());
            Assert.Contains(
                export["warnings"]!.AsArray().Select(node => node!["code"]!.GetValue<string>()),
                code => code == "export.plan.v1Wrapped");
            Assert.Contains(
                export["warnings"]!.AsArray().Select(node => node!["code"]!.GetValue<string>()),
                code => code == "export.frameRate.defaulted");
            Assert.True(File.Exists(outputPath));
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
    public async Task Export_ReturnsStructuredFailureWhenOutputAlreadyExists()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-export-exists-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var outputPath = Path.Combine(outputDirectory, "existing.edl");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "clips": [
                { "id": "clip-001", "in": "00:00:00", "out": "00:00:01" }
              ],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);
        await File.WriteAllTextAsync(outputPath, "existing");

        try
        {
            var result = await RunCliAsync(
                "export",
                "--plan", planPath,
                "--format", "edl",
                "--output", outputPath);

            Assert.Equal(1, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            var payload = envelope["payload"]!.AsObject();
            Assert.Equal("export", envelope["command"]!.GetValue<string>());
            Assert.Contains("already exists", payload["error"]!["message"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
            Assert.Equal(Path.GetFullPath(outputPath), payload["export"]!["outputPath"]!.GetValue<string>());
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
    public async Task Export_ReturnsStructuredFailureForInvalidFormat()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-export-format-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "clips": [
                { "id": "clip-001", "in": "00:00:00", "out": "00:00:01" }
              ],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync(
                "export",
                "--plan", planPath,
                "--format", "premiere-xml",
                "--output", "plan.xml");

            Assert.Equal(1, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("export", envelope["command"]!.GetValue<string>());
            Assert.Equal("Option '--format' expects 'edl'.", envelope["payload"]!["error"]!["message"]!.GetValue<string>());
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
    public async Task Export_ReturnsStructuredFailureForInvalidFrameRate()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-export-fps-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "clips": [
                { "id": "clip-001", "in": "00:00:00", "out": "00:00:01" }
              ],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync(
                "export",
                "--plan", planPath,
                "--format", "edl",
                "--output", "plan.edl",
                "--frame-rate", "abc");

            Assert.Equal(1, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("export", envelope["command"]!.GetValue<string>());
            Assert.Equal(
                "Option '--frame-rate' expects an integer value.",
                envelope["payload"]!["error"]!["message"]!.GetValue<string>());
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
