using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task InitPlan_AppliesArtifactBindingsAndTemplateParameterOverrides()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-artifacts-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var artifactsPath = Path.Combine(outputDirectory, "artifacts.json");
        var templateParamsPath = Path.Combine(outputDirectory, "template-params.json");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(artifactsPath, """{"subtitles":"subs\\captions.srt"}""");
        await File.WriteAllTextAsync(templateParamsPath, """{"hookStyle":"match-cut","captionStyle":"clean-sidecar"}""");

        try
        {
            var result = await RunCliAsync(
                "init-plan",
                "input.mp4",
                "--template",
                "shorts-captioned",
                "--output",
                planPath,
                "--render-output",
                "final.mp4",
                "--artifacts",
                artifactsPath,
                "--template-params",
                templateParamsPath);

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var editPlan = payload["editPlan"]!.AsObject();
            var artifacts = editPlan["artifacts"]!.AsArray();
            var artifactNode = Assert.Single(artifacts);
            var artifact = Assert.IsType<JsonObject>(artifactNode);

            Assert.Equal(Path.GetFullPath(planPath), payload["planPath"]!.GetValue<string>());
            Assert.False(payload["probed"]!.GetValue<bool>());
            Assert.Equal("match-cut", editPlan["template"]!["parameters"]!["hookStyle"]!.GetValue<string>());
            Assert.Equal("clean-sidecar", editPlan["template"]!["parameters"]!["captionStyle"]!.GetValue<string>());
            Assert.Equal("subtitles", artifact["slotId"]!.GetValue<string>());
            Assert.Equal("subs\\captions.srt", artifact["path"]!.GetValue<string>());
            Assert.Equal("subs\\captions.srt", editPlan["subtitles"]!["path"]!.GetValue<string>());
            Assert.True(File.Exists(planPath));
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
    public async Task InitPlan_RejectsConflictingTranscriptAndBeatSeedModes()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-conflict-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var beatsPath = Path.Combine(outputDirectory, "beats.json");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(transcriptPath, """{"schemaVersion":1,"language":"en","segments":[]}""");
        await File.WriteAllTextAsync(beatsPath, """{"schemaVersion":1,"sourcePath":"input.mp4","sampleRateHz":16000,"frameDuration":"00:00:00.0500000","estimatedBpm":120,"beats":[]}""");

        try
        {
            var result = await RunCliAsync(
                "init-plan",
                "input.mp4",
                "--template",
                "shorts-basic",
                "--output",
                planPath,
                "--render-output",
                "final.mp4",
                "--transcript",
                transcriptPath,
                "--seed-from-transcript",
                "--beats",
                beatsPath,
                "--seed-from-beats");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Options '--seed-from-transcript' and '--seed-from-beats' cannot be used together.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("Open Video Toolbox CLI", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("init-plan <input>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(planPath));
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
    public async Task InitPlan_RejectsArtifactBindingsForUndeclaredSlots()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-bad-artifact-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var artifactsPath = Path.Combine(outputDirectory, "artifacts.json");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(artifactsPath, """{"bgm":"audio\\theme.wav"}""");

        try
        {
            var result = await RunCliAsync(
                "init-plan",
                "input.mp4",
                "--template",
                "shorts-basic",
                "--output",
                planPath,
                "--render-output",
                "final.mp4",
                "--artifacts",
                artifactsPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Template 'shorts-basic' does not declare artifact slot 'bgm'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("Open Video Toolbox CLI", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("init-plan <input>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(planPath));
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
    public async Task InitPlan_RejectsNonObjectTemplateParamsJson()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-bad-params-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var templateParamsPath = Path.Combine(outputDirectory, "template-params.json");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(templateParamsPath, "[1,2,3]");

        try
        {
            var result = await RunCliAsync(
                "init-plan",
                "input.mp4",
                "--template",
                "shorts-basic",
                "--output",
                planPath,
                "--render-output",
                "final.mp4",
                "--template-params",
                templateParamsPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Failed to parse template parameters", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("Expected a JSON object like {\"hookStyle\":\"hard-cut\"}.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("Open Video Toolbox CLI", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("init-plan <input>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(planPath));
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
    public async Task InitPlan_WithPluginDir_CreatesPlanFromPluginTemplate()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-plugin-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);
        var planPath = Path.Combine(workingDirectory, "edit.json");

        try
        {
            var pluginDirectory = await CreateTemplatePluginAsync(
                workingDirectory,
                pluginId: "community-pack",
                pluginDisplayName: "Community Pack",
                template: CreatePluginTemplateDefinition("plugin-captioned", "Plugin Captioned"));

            var result = await RunCliAsync(
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

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("plugin", payload["source"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", payload["source"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", payload["source"]!["pluginVersion"]!.GetValue<string>());
            Assert.Equal("plugin-captioned", payload["template"]!["id"]!.GetValue<string>());
            Assert.Equal("plugin-captioned", payload["editPlan"]!["template"]!["id"]!.GetValue<string>());
            Assert.True(File.Exists(planPath));

            var editPlan = JsonNode.Parse(await File.ReadAllTextAsync(planPath))!.AsObject();
            Assert.Equal("plugin", editPlan["template"]!["source"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", editPlan["template"]!["source"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", editPlan["template"]!["source"]!["pluginVersion"]!.GetValue<string>());
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
