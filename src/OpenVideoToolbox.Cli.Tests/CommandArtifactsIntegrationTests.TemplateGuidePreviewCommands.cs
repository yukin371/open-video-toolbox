using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task TemplateGuide_ForShortsCaptioned_ReturnsStableSeedModesAndPreviewShapes()
    {
        var result = await RunCliAsync("templates", "shorts-captioned");

        Assert.Equal(0, result.ExitCode);

        var payload = JsonNode.Parse(result.StdOut)!.AsObject();
        var template = payload["template"]!.AsObject();
        var examples = payload["examples"]!.AsObject();

        Assert.Equal("shorts-captioned", template["id"]!.GetValue<string>());
        Assert.True(template["recommendedSeedModes"]!.AsArray()
            .Select(node => node!.GetValue<string>())
            .SequenceEqual(["manual", "transcript", "beats"]));
        Assert.True(template["supportingSignals"]!.AsArray()
            .Select(node => node!["kind"]!.GetValue<string>())
            .SequenceEqual(["transcript", "beats", "silence"]));

        Assert.Equal("subtitles.srt", examples["artifacts"]!["subtitles"]!.GetValue<string>());
        Assert.Equal("burn-later", examples["templateParams"]!["captionStyle"]!.GetValue<string>());
        var supportingSignals = examples["supportingSignals"]!.AsArray();
        Assert.Equal(3, supportingSignals.Count);
        Assert.Contains(supportingSignals, node => node!["kind"]!.GetValue<string>() == "silence");
        Assert.Contains(supportingSignals, node => node!["command"]!.GetValue<string>().Contains("detect-silence", StringComparison.Ordinal));
        Assert.Contains(examples["signalCommands"]!.AsArray(), node => node!.GetValue<string>().Contains("beat-track", StringComparison.Ordinal));
        Assert.Contains(examples["artifactCommands"]!.AsArray(), node => node!.GetValue<string>() == "ovt subtitle <input> --transcript transcript.json --format srt --output subtitles.srt");

        var seedCommands = examples["seedCommands"]!.AsArray();
        Assert.Equal(3, seedCommands.Count);
        Assert.Contains(seedCommands, node => node!["mode"]!.GetValue<string>() == "transcript");
        Assert.Contains(seedCommands, node => node!["mode"]!.GetValue<string>() == "beats");

        var transcriptSeed = seedCommands
            .Single(node => node!["mode"]!.GetValue<string>() == "transcript")!
            .AsObject();
        var transcriptVariants = transcriptSeed["variants"]!.AsArray();
        Assert.Equal(3, transcriptVariants.Count);
        Assert.Equal("grouped", transcriptVariants[0]!["key"]!.GetValue<string>());
        Assert.True(transcriptVariants[0]!["recommended"]!.GetValue<bool>());
        Assert.Equal("max-gap", transcriptVariants[1]!["key"]!.GetValue<string>());
        Assert.True(transcriptVariants[1]!["recommended"]!.GetValue<bool>());
        Assert.Equal("min-duration", transcriptVariants[2]!["key"]!.GetValue<string>());
        Assert.False(transcriptVariants[2]!["recommended"]!.GetValue<bool>());
        Assert.Contains(transcriptVariants, node => node!["command"]!.GetValue<string>().Contains("--transcript-segment-group-size 2", StringComparison.Ordinal));
        Assert.Contains(transcriptVariants, node => node!["command"]!.GetValue<string>().Contains("--min-transcript-segment-duration-ms 500", StringComparison.Ordinal));
        Assert.Contains(transcriptVariants, node => node!["command"]!.GetValue<string>().Contains("--max-transcript-gap-ms 200", StringComparison.Ordinal));

        var previewPlans = examples["previewPlans"]!.AsArray();
        Assert.Equal(3, previewPlans.Count);

        var transcriptPreviewNode = GetPreview(previewPlans, "transcript");
        var transcriptPreview = Assert.IsType<JsonObject>(transcriptPreviewNode["editPlan"]);
        Assert.NotNull(transcriptPreview["transcript"]);
        Assert.Equal(2, transcriptPreview["clips"]!.AsArray().Count);
        var strategyVariants = transcriptPreviewNode["strategyVariants"]!.AsArray();
        Assert.Equal(3, strategyVariants.Count);
        Assert.Equal("grouped", strategyVariants[0]!["key"]!.GetValue<string>());
        Assert.True(strategyVariants[0]!["isRecommended"]!.GetValue<bool>());
        Assert.Equal("max-gap", strategyVariants[1]!["key"]!.GetValue<string>());
        Assert.True(strategyVariants[1]!["isRecommended"]!.GetValue<bool>());
        Assert.Equal("min-duration", strategyVariants[2]!["key"]!.GetValue<string>());
        Assert.False(strategyVariants[2]!["isRecommended"]!.GetValue<bool>());

        var beatsPreview = GetPreviewPlan(previewPlans, "beats");
        Assert.NotNull(beatsPreview["beats"]);
        Assert.Equal("sidecar", beatsPreview["subtitles"]!["mode"]!.GetValue<string>());
    }

    [Fact]
    public async Task TemplateGuide_ForBeatMontage_ReturnsBeatFirstExamplesWithoutSubtitleShape()
    {
        var result = await RunCliAsync("templates", "beat-montage");

        Assert.Equal(0, result.ExitCode);

        var payload = JsonNode.Parse(result.StdOut)!.AsObject();
        var template = payload["template"]!.AsObject();
        var examples = payload["examples"]!.AsObject();

        Assert.Equal("beat-montage", template["id"]!.GetValue<string>());
        Assert.True(template["recommendedSeedModes"]!.AsArray()
            .Select(node => node!.GetValue<string>())
            .SequenceEqual(["manual", "beats"]));
        Assert.True(template["supportingSignals"]!.AsArray()
            .Select(node => node!["kind"]!.GetValue<string>())
            .SequenceEqual(["beats", "stems"]));

        Assert.Equal("stems/htdemucs/input/no_vocals.wav", examples["artifacts"]!["bgm"]!.GetValue<string>());
        Assert.Equal("sync-cut", examples["templateParams"]!["pace"]!.GetValue<string>());
        Assert.Contains(examples["signalCommands"]!.AsArray(), node => node!.GetValue<string>().Contains("separate-audio", StringComparison.Ordinal));
        Assert.Contains(examples["supportingSignals"]!.AsArray(), node => node!["kind"]!.GetValue<string>() == "stems");
        Assert.Contains(
            examples["supportingSignals"]!.AsArray(),
            node => node!["kind"]!.GetValue<string>() == "stems"
                && node["consumption"]!.GetValue<string>().Contains("artifacts.json", StringComparison.Ordinal));
        Assert.Empty(examples["artifactCommands"]!.AsArray());

        var previewPlans = examples["previewPlans"]!.AsArray();
        Assert.Equal(2, previewPlans.Count);

        var manualPreview = GetPreviewPlan(previewPlans, "manual");
        Assert.Null(manualPreview["subtitles"]);
        Assert.Single(manualPreview["audioTracks"]!.AsArray());

        var beatsPreview = GetPreviewPlan(previewPlans, "beats");
        Assert.NotNull(beatsPreview["beats"]);
        Assert.Single(beatsPreview["clips"]!.AsArray());
        Assert.Equal("stems/htdemucs/input/no_vocals.wav", beatsPreview["audioTracks"]![0]!["path"]!.GetValue<string>());
    }
}
