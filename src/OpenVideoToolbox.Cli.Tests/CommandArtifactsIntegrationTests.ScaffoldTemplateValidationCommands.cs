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

    [Fact]
    public async Task ScaffoldTemplate_RejectsConflictingTranscriptAndBeatSeedModes()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-scaffold-conflict-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var beatsPath = Path.Combine(outputDirectory, "beats.json");

        await File.WriteAllTextAsync(transcriptPath, """{"schemaVersion":1,"language":"en","segments":[]}""");
        await File.WriteAllTextAsync(beatsPath, """{"schemaVersion":1,"sourcePath":"input.mp4","sampleRateHz":16000,"frameDuration":"00:00:00.0500000","estimatedBpm":120,"beats":[]}""");

        try
        {
            var result = await RunCliAsync(
                "scaffold-template",
                "input.mp4",
                "--template",
                "shorts-basic",
                "--dir",
                outputDirectory,
                "--transcript",
                transcriptPath,
                "--seed-from-transcript",
                "--beats",
                beatsPath,
                "--seed-from-beats");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Options '--seed-from-transcript' and '--seed-from-beats' cannot be used together.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("scaffold-template <input>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(Path.Combine(outputDirectory, "edit.json")));
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
    public async Task ScaffoldTemplate_UsesUserArtifactsFileEvenWhenItMatchesCanonicalOutputPath()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-scaffold-artifacts-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var artifactsPath = Path.Combine(outputDirectory, "artifacts.json");

        await File.WriteAllTextAsync(artifactsPath, """{"bgm":"audio\\theme.wav"}""");

        try
        {
            var result = await RunCliAsync(
                "scaffold-template",
                "input.mp4",
                "--template",
                "shorts-basic",
                "--dir",
                outputDirectory,
                "--artifacts",
                artifactsPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Template 'shorts-basic' does not declare artifact slot 'bgm'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("scaffold-template <input>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(Path.Combine(outputDirectory, "edit.json")));
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
    public async Task ScaffoldTemplate_UsesUserTemplateParamsFileEvenWhenItMatchesCanonicalOutputPath()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-scaffold-params-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var templateParamsPath = Path.Combine(outputDirectory, "template-params.json");

        await File.WriteAllTextAsync(templateParamsPath, "[1,2,3]");

        try
        {
            var result = await RunCliAsync(
                "scaffold-template",
                "input.mp4",
                "--template",
                "shorts-basic",
                "--dir",
                outputDirectory,
                "--template-params",
                templateParamsPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Failed to parse template parameters", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("Expected a JSON object like {\"hookStyle\":\"hard-cut\"}.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("scaffold-template <input>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(Path.Combine(outputDirectory, "edit.json")));
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
