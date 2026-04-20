using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task TemplatesSummary_ExposesNewTemplateCategoriesForDiscovery()
    {
        var result = await RunCliAsync("templates", "--summary");

        Assert.Equal(0, result.ExitCode);

        var payload = JsonNode.Parse(result.StdOut)!.AsObject();
        Assert.True(payload["summary"]!.GetValue<bool>());

        var templates = payload["templates"]!.AsArray()
            .Select(node => node!.AsObject())
            .ToArray();

        Assert.Contains(templates, template => template["id"]!.GetValue<string>() == "explainer-captioned");
        Assert.Contains(templates, template => template["id"]!.GetValue<string>() == "beat-montage");

        var explainer = templates.Single(template => template["id"]!.GetValue<string>() == "explainer-captioned");
        Assert.Equal("explainer", explainer["category"]!.GetValue<string>());
        Assert.True(explainer["hasSubtitles"]!.GetValue<bool>());
        Assert.True(explainer["recommendedTranscriptSeedStrategies"]!.AsArray()
            .Select(node => node!.GetValue<string>())
            .SequenceEqual(["grouped", "maxGap"]));

        var montage = templates.Single(template => template["id"]!.GetValue<string>() == "beat-montage");
        Assert.Equal("montage", montage["category"]!.GetValue<string>());
        Assert.True(montage["hasArtifacts"]!.GetValue<bool>());
        Assert.Empty(montage["recommendedTranscriptSeedStrategies"]!.AsArray());
    }

    [Fact]
    public async Task TemplatesFilters_ReturnStableExplainerAndMontageResults()
    {
        var explainerResult = await RunCliAsync(
            "templates",
            "--category",
            "explainer",
            "--seed-mode",
            "transcript",
            "--has-subtitles",
            "true",
            "--summary");

        Assert.Equal(0, explainerResult.ExitCode);

        var explainerTemplates = JsonNode.Parse(explainerResult.StdOut)!.AsObject()["templates"]!.AsArray();
        var explainerNode = Assert.Single(explainerTemplates);
        var explainer = Assert.IsType<JsonObject>(explainerNode);
        Assert.Equal("explainer-captioned", explainer["id"]!.GetValue<string>());

        var montageResult = await RunCliAsync(
            "templates",
            "--category",
            "montage",
            "--seed-mode",
            "beats",
            "--artifact-kind",
            "audio",
            "--summary");

        Assert.Equal(0, montageResult.ExitCode);

        var montageTemplates = JsonNode.Parse(montageResult.StdOut)!.AsObject()["templates"]!.AsArray();
        Assert.Equal(2, montageTemplates.Count);
        var montageIds = montageTemplates
            .Select(node => node!["id"]!.GetValue<string>())
            .ToArray();
        Assert.Contains("beat-montage", montageIds);
        Assert.Contains("music-captioned-montage", montageIds);
    }

    [Fact]
    public async Task TemplatesJsonOut_WritesSameSummaryPayloadToFile()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-templates-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var jsonOutPath = Path.Combine(outputDirectory, "templates-summary.json");

        try
        {
            var result = await RunCliAsync("templates", "--summary", "--json-out", jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();

            Assert.Equal(stdout.ToJsonString(), file.ToJsonString());
            Assert.True(file["summary"]!.GetValue<bool>());
            Assert.Contains(file["templates"]!.AsArray(), node => node!["id"]!.GetValue<string>() == "beat-montage");
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
