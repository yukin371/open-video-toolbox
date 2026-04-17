using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using OpenVideoToolbox.Cli;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task TemplatesWriteExamples_WritesStableCommandArtifacts()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-template-guide-{Guid.NewGuid():N}");

        try
        {
            var result = await RunCliAsync("templates", "commentary-bgm", "--write-examples", outputDirectory);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(Path.Combine(outputDirectory, "commands.json")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "commands.ps1")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "commands.cmd")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "commands.sh")));

            var commands = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.json")))!.AsObject();
            Assert.Equal("<input>", commands["variables"]!["inputPath"]!.GetValue<string>());

            var initPlanCommands = commands["initPlanCommands"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray();
            Assert.Contains(initPlanCommands, command => command.Contains("--template commentary-bgm", StringComparison.Ordinal));

            var seedModes = commands["seedCommands"]!.AsArray()
                .Select(node => node!["mode"]!.GetValue<string>())
                .ToArray();
            Assert.True(seedModes.SequenceEqual(["manual", "transcript"]));

            var transcriptSeed = commands["seedCommands"]!.AsArray()
                .Single(node => node!["mode"]!.GetValue<string>() == "transcript")!
                .AsObject();
            var transcriptVariants = transcriptSeed["variants"]!.AsArray();
            Assert.Equal(3, transcriptVariants.Count);
            Assert.Equal("min-duration", transcriptVariants[0]!["key"]!.GetValue<string>());
            Assert.True(transcriptVariants[0]!["recommended"]!.GetValue<bool>());
            Assert.Equal("grouped", transcriptVariants[1]!["key"]!.GetValue<string>());
            Assert.False(transcriptVariants[1]!["recommended"]!.GetValue<bool>());
            Assert.Equal("max-gap", transcriptVariants[2]!["key"]!.GetValue<string>());

            var workflowCommands = commands["workflowCommands"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray();
            Assert.True(workflowCommands.SequenceEqual(
                [
                    "ovt validate-plan --plan edit.json",
                    "ovt render --plan edit.json --preview",
                    "ovt mix-audio --plan edit.json --output mixed.wav --preview"
                ]));
            var signalCommands = commands["signalCommands"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray();
            Assert.True(signalCommands.SequenceEqual(
                [
                    "ovt transcribe <input> --model <path> --output transcript.json",
                    "ovt detect-silence <input> --output silence.json"
                ]));
            Assert.Empty(commands["artifactCommands"]!.AsArray());

            var guide = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "guide.json")))!.AsObject();
            var commandFiles = guide["examples"]!["commandFiles"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray();
            Assert.True(commandFiles.SequenceEqual(["commands.json", "commands.ps1", "commands.cmd", "commands.sh"]));
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
    public async Task ScaffoldTemplate_WritesCommandArtifactsAlongsideEditPlan()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-scaffold-{Guid.NewGuid():N}");

        try
        {
            var result = await RunCliAsync("scaffold-template", "input.mp4", "--template", "shorts-captioned", "--dir", outputDirectory);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(Path.Combine(outputDirectory, "edit.json")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "commands.json")));

            var scaffold = JsonNode.Parse(result.StdOut)!.AsObject()["scaffold"]!.AsObject();
            var writtenFiles = scaffold["writtenFiles"]!.AsArray().Select(node => Path.GetFileName(node!.GetValue<string>())).ToArray();
            Assert.Contains("edit.json", writtenFiles);
            Assert.Contains("commands.json", writtenFiles);
            Assert.Contains("commands.ps1", writtenFiles);
            Assert.Contains("commands.cmd", writtenFiles);
            Assert.Contains("commands.sh", writtenFiles);
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

        Assert.Equal("audio/input.wav", examples["artifacts"]!["bgm"]!.GetValue<string>());
        Assert.Equal("sync-cut", examples["templateParams"]!["pace"]!.GetValue<string>());
        Assert.Contains(examples["signalCommands"]!.AsArray(), node => node!.GetValue<string>().Contains("separate-audio", StringComparison.Ordinal));
        Assert.Contains(examples["supportingSignals"]!.AsArray(), node => node!["kind"]!.GetValue<string>() == "stems");
        Assert.Empty(examples["artifactCommands"]!.AsArray());

        var previewPlans = examples["previewPlans"]!.AsArray();
        Assert.Equal(2, previewPlans.Count);

        var manualPreview = GetPreviewPlan(previewPlans, "manual");
        Assert.Null(manualPreview["subtitles"]);
        Assert.Single(manualPreview["audioTracks"]!.AsArray());

        var beatsPreview = GetPreviewPlan(previewPlans, "beats");
        Assert.NotNull(beatsPreview["beats"]);
        Assert.Single(beatsPreview["clips"]!.AsArray());
        Assert.Equal("audio/input.wav", beatsPreview["audioTracks"]![0]!["path"]!.GetValue<string>());
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
    public async Task RenderAndMixAudioPreview_ReturnUnifiedEnvelope()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-preview-envelope-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var inputPath = Path.Combine(outputDirectory, "input.mp4");
        var planPath = Path.Combine(outputDirectory, "edit.json");
        await File.WriteAllTextAsync(inputPath, "fake-media");
        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": {
                "inputPath": "input.mp4"
              },
              "clips": [
                {
                  "id": "clip-001",
                  "in": "00:00:00",
                  "out": "00:00:03"
                }
              ],
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
            var render = JsonNode.Parse((await RunCliAsync("render", "--plan", planPath, "--preview")).StdOut)!.AsObject();
            Assert.Equal("render", render["command"]!.GetValue<string>());
            Assert.True(render["preview"]!.GetValue<bool>());
            var renderPayload = render["payload"]!.AsObject();
            var renderPreview = renderPayload["executionPreview"]!.AsObject();
            Assert.Equal("render", renderPreview["operation"]!.GetValue<string>());
            Assert.True(renderPreview["isPreview"]!.GetValue<bool>());
            Assert.True(renderPreview["pathsResolved"]!.GetValue<bool>());

            var mixedOutputPath = Path.Combine(outputDirectory, "mixed.wav");
            var mixAudio = JsonNode.Parse((await RunCliAsync("mix-audio", "--plan", planPath, "--output", mixedOutputPath, "--preview")).StdOut)!.AsObject();
            Assert.Equal("mix-audio", mixAudio["command"]!.GetValue<string>());
            Assert.True(mixAudio["preview"]!.GetValue<bool>());
            var mixPayload = mixAudio["payload"]!.AsObject();
            var mixPreview = mixPayload["executionPreview"]!.AsObject();
            Assert.Equal("mix-audio", mixPreview["operation"]!.GetValue<string>());
            Assert.True(mixPreview["isPreview"]!.GetValue<bool>());
            Assert.True(mixPreview["pathsResolved"]!.GetValue<bool>());
            Assert.Equal(mixedOutputPath, mixPayload["mixAudio"]!["outputPath"]!.GetValue<string>());
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
    public async Task InitPlan_SeedsClipsFromTranscriptSegments()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-transcript-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(
            transcriptPath,
            """
            {
              "schemaVersion": 1,
              "language": "en",
              "segments": [
                { "id": "seg-001", "start": "00:00:00", "end": "00:00:01.2000000", "text": "Hello" },
                { "id": "seg-002", "start": "00:00:01.2000000", "end": "00:00:02.4000000", "text": "World" }
              ]
            }
            """);

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
                "--seed-from-transcript");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var editPlan = payload["editPlan"]!.AsObject();
            var clips = editPlan["clips"]!.AsArray();

            Assert.Equal(2, clips.Count);
            Assert.Equal("seg-001", clips[0]!["label"]!.GetValue<string>());
            Assert.Equal("00:00:01.2000000", clips[0]!["out"]!.GetValue<string>());
            Assert.Equal("seg-002", clips[1]!["label"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(transcriptPath), editPlan["transcript"]!["path"]!.GetValue<string>());
            Assert.Equal(2, editPlan["transcript"]!["segmentCount"]!.GetValue<int>());
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
    public async Task InitPlan_CanGroupTranscriptSegmentsIntoMergedSeedClips()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-transcript-group-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(
            transcriptPath,
            """
            {
              "schemaVersion": 1,
              "language": "en",
              "segments": [
                { "id": "seg-001", "start": "00:00:00", "end": "00:00:01", "text": "Intro" },
                { "id": "seg-002", "start": "00:00:01", "end": "00:00:02.5000000", "text": "Detail" },
                { "id": "seg-003", "start": "00:00:02.5000000", "end": "00:00:04", "text": "Wrap" }
              ]
            }
            """);

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
                "--transcript-segment-group-size",
                "2");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var editPlan = payload["editPlan"]!.AsObject();
            var clips = editPlan["clips"]!.AsArray();

            Assert.Equal(2, clips.Count);
            Assert.Equal("seg-001..seg-002", clips[0]!["label"]!.GetValue<string>());
            Assert.Equal("00:00:02.5000000", clips[0]!["out"]!.GetValue<string>());
            Assert.Equal("seg-003", clips[1]!["label"]!.GetValue<string>());
            Assert.Equal("00:00:02.5000000", clips[1]!["in"]!.GetValue<string>());
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
    public async Task InitPlan_CanSkipShortTranscriptSegmentsBeforeSeeding()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-transcript-min-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(
            transcriptPath,
            """
            {
              "schemaVersion": 1,
              "language": "en",
              "segments": [
                { "id": "seg-001", "start": "00:00:00", "end": "00:00:00.2500000", "text": "Too short" },
                { "id": "seg-002", "start": "00:00:00.2500000", "end": "00:00:01", "text": "Keep" },
                { "id": "seg-003", "start": "00:00:01", "end": "00:00:02", "text": "Keep too" }
              ]
            }
            """);

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
                "--transcript-segment-group-size",
                "2",
                "--min-transcript-segment-duration-ms",
                "500");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var editPlan = payload["editPlan"]!.AsObject();
            var clips = editPlan["clips"]!.AsArray();

            var clip = Assert.Single(clips);
            Assert.Equal("seg-002..seg-003", clip!["label"]!.GetValue<string>());
            Assert.Equal("00:00:00.2500000", clip["in"]!.GetValue<string>());
            Assert.Equal("00:00:02", clip["out"]!.GetValue<string>());
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
    public async Task InitPlan_CanSplitTranscriptSeedGroupsWhenGapExceedsThreshold()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-transcript-gap-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(
            transcriptPath,
            """
            {
              "schemaVersion": 1,
              "language": "en",
              "segments": [
                { "id": "seg-001", "start": "00:00:00", "end": "00:00:00.6000000", "text": "First" },
                { "id": "seg-002", "start": "00:00:00.7500000", "end": "00:00:01.3000000", "text": "Near" },
                { "id": "seg-003", "start": "00:00:02", "end": "00:00:02.8000000", "text": "Far" }
              ]
            }
            """);

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
                "--transcript-segment-group-size",
                "3",
                "--max-transcript-gap-ms",
                "200");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var editPlan = payload["editPlan"]!.AsObject();
            var clips = editPlan["clips"]!.AsArray();

            Assert.Equal(2, clips.Count);
            Assert.Equal("seg-001..seg-002", clips[0]!["label"]!.GetValue<string>());
            Assert.Equal("00:00:01.3000000", clips[0]!["out"]!.GetValue<string>());
            Assert.Equal("seg-003", clips[1]!["label"]!.GetValue<string>());
            Assert.Equal("00:00:02", clips[1]!["in"]!.GetValue<string>());
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
    public async Task InitPlan_SeedsClipsFromBeatGroups()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-beats-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var beatsPath = Path.Combine(outputDirectory, "beats.json");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(
            beatsPath,
            """
            {
              "schemaVersion": 1,
              "sourcePath": "input.mp4",
              "sampleRateHz": 16000,
              "frameDuration": "00:00:00.0500000",
              "estimatedBpm": 128,
              "beats": [
                { "index": 0, "time": "00:00:00", "strength": 0.9 },
                { "index": 1, "time": "00:00:01", "strength": 0.88 },
                { "index": 2, "time": "00:00:02", "strength": 0.9 },
                { "index": 3, "time": "00:00:03", "strength": 0.87 },
                { "index": 4, "time": "00:00:04", "strength": 0.92 }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync(
                "init-plan",
                "input.mp4",
                "--template",
                "beat-montage",
                "--output",
                planPath,
                "--render-output",
                "final.mp4",
                "--beats",
                beatsPath,
                "--seed-from-beats",
                "--beat-group-size",
                "2");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var editPlan = payload["editPlan"]!.AsObject();
            var clips = editPlan["clips"]!.AsArray();

            Assert.Equal(2, clips.Count);
            Assert.Equal("beat-group-001", clips[0]!["label"]!.GetValue<string>());
            Assert.Equal("00:00:02", clips[0]!["out"]!.GetValue<string>());
            Assert.Equal("beat-group-002", clips[1]!["label"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(beatsPath), editPlan["beats"]!["path"]!.GetValue<string>());
            Assert.Equal(128, editPlan["beats"]!["estimatedBpm"]!.GetValue<int>());
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

    [Fact]
    public async Task MixAudioPreview_ReturnsStableExecutionPreview()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-mix-preview-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var outputPath = Path.Combine(outputDirectory, "mixed.wav");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "clips": [
                { "id": "clip-001", "in": "00:00:00", "out": "00:00:02", "label": "intro" }
              ],
              "audioTracks": [],
              "artifacts": [],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync("mix-audio", "--plan", planPath, "--output", outputPath, "--preview");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("mix-audio", payload["command"]!.GetValue<string>());
            Assert.True(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            var executionPreview = envelope["executionPreview"]!.AsObject();
            var commandPlan = executionPreview["commandPlan"]!.AsObject();
            var producedPaths = executionPreview["producedPaths"]!.AsArray();

            Assert.Equal(Path.GetFullPath(planPath), envelope["mixAudio"]!["planPath"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(outputPath), envelope["mixAudio"]!["outputPath"]!.GetValue<string>());
            Assert.Equal("ffmpeg", commandPlan["toolName"]!.GetValue<string>());
            Assert.Equal("ffmpeg", commandPlan["executablePath"]!.GetValue<string>());
            Assert.Contains(commandPlan["arguments"]!.AsArray().Select(node => node!.GetValue<string>()), arg => arg == "-filter_complex");
            Assert.Contains(commandPlan["arguments"]!.AsArray().Select(node => node!.GetValue<string>()), arg => arg == "pcm_s16le");
            Assert.Equal(Path.GetFullPath(outputPath), Assert.Single(producedPaths)!.GetValue<string>());
            Assert.Empty(executionPreview["sideEffects"]!.AsArray());
            Assert.False(File.Exists(outputPath));
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
    public async Task MixAudioPreview_CanWriteEnvelopeToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-mix-preview-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var outputPath = Path.Combine(outputDirectory, "mixed.wav");
        var jsonOutPath = Path.Combine(outputDirectory, "mix-preview.json");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "clips": [
                { "id": "clip-001", "in": "00:00:00", "out": "00:00:02", "label": "intro" }
              ],
              "audioTracks": [],
              "artifacts": [],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync("mix-audio", "--plan", planPath, "--output", outputPath, "--preview", "--json-out", jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();

            Assert.Equal(stdout.ToJsonString(), file.ToJsonString());
            Assert.Equal("mix-audio", stdout["command"]!.GetValue<string>());
            Assert.True(stdout["preview"]!.GetValue<bool>());
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
    public async Task RenderPreview_ReturnsStableExecutionPreview()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-preview-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "clips": [
                { "id": "clip-001", "in": "00:00:00", "out": "00:00:02", "label": "intro" }
              ],
              "audioTracks": [],
              "artifacts": [],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync("render", "--plan", planPath, "--preview");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("render", payload["command"]!.GetValue<string>());
            Assert.True(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            var executionPreview = envelope["executionPreview"]!.AsObject();
            var commandPlan = executionPreview["commandPlan"]!.AsObject();
            var producedPaths = executionPreview["producedPaths"]!.AsArray();

            Assert.Equal(Path.Combine(outputDirectory, "input.mp4"), envelope["render"]!["source"]!["inputPath"]!.GetValue<string>());
            Assert.Equal(Path.Combine(outputDirectory, "final.mp4"), envelope["render"]!["output"]!["path"]!.GetValue<string>());
            Assert.Equal("ffmpeg", commandPlan["toolName"]!.GetValue<string>());
            Assert.Contains(commandPlan["arguments"]!.AsArray().Select(node => node!.GetValue<string>()), arg => arg == "-filter_complex");
            Assert.Contains(commandPlan["arguments"]!.AsArray().Select(node => node!.GetValue<string>()), arg => arg == "libx264");
            Assert.Contains(commandPlan["arguments"]!.AsArray().Select(node => node!.GetValue<string>()), arg => arg == "+faststart");
            Assert.Equal(Path.Combine(outputDirectory, "final.mp4"), Assert.Single(producedPaths)!.GetValue<string>());
            Assert.Empty(executionPreview["sideEffects"]!.AsArray());
            Assert.False(File.Exists(Path.Combine(outputDirectory, "final.mp4")));
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
    public async Task RenderPreview_CanWriteEnvelopeToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-preview-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var jsonOutPath = Path.Combine(outputDirectory, "render-preview.json");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "clips": [
                { "id": "clip-001", "in": "00:00:00", "out": "00:00:02", "label": "intro" }
              ],
              "audioTracks": [],
              "artifacts": [],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync("render", "--plan", planPath, "--preview", "--json-out", jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();

            Assert.Equal(stdout.ToJsonString(), file.ToJsonString());
            Assert.Equal("render", stdout["command"]!.GetValue<string>());
            Assert.True(stdout["preview"]!.GetValue<bool>());
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
    public async Task RenderPreview_AppliesOutputOverrideToExecutionPreview()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-override-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var overrideOutputPath = Path.Combine(outputDirectory, "custom-out.mp4");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "clips": [
                { "id": "clip-001", "in": "00:00:00", "out": "00:00:02", "label": "intro" }
              ],
              "audioTracks": [],
              "artifacts": [],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync("render", "--plan", planPath, "--output", "custom-out.mp4", "--preview");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("render", payload["command"]!.GetValue<string>());
            Assert.True(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            var executionPreview = envelope["executionPreview"]!.AsObject();
            var producedPath = Assert.Single(executionPreview["producedPaths"]!.AsArray())!.GetValue<string>();

            Assert.Equal(overrideOutputPath, envelope["render"]!["output"]!["path"]!.GetValue<string>());
            Assert.Equal(overrideOutputPath, producedPath);
            Assert.Contains(executionPreview["commandPlan"]!["arguments"]!.AsArray().Select(node => node!.GetValue<string>()), arg => arg == overrideOutputPath);
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
    public async Task MixAudioPreview_RejectsPlansWithoutClips()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-mix-preview-empty-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var outputPath = Path.Combine(outputDirectory, "mixed.wav");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "clips": [],
              "audioTracks": [],
              "artifacts": [],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync("mix-audio", "--plan", planPath, "--output", outputPath, "--preview");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Edit plan must contain at least one clip.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("mix-audio --plan <edit.json>", result.StdOut, StringComparison.Ordinal);
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
    public async Task RenderPreview_RejectsPlansWithoutClips()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-preview-empty-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "clips": [],
              "audioTracks": [],
              "artifacts": [],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync("render", "--plan", planPath, "--preview");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Render plan must contain at least one clip.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("render --plan <path>", result.StdOut, StringComparison.Ordinal);
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
    public async Task MixAudio_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-mix-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var outputPath = Path.Combine(outputDirectory, "mixed.wav");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "clips": [
                { "id": "clip-001", "in": "00:00:00", "out": "00:00:02", "label": "intro" }
              ],
              "audioTracks": [],
              "artifacts": [],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync("mix-audio", "--plan", planPath, "--output", outputPath, "--ffmpeg", "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("mix-audio --plan <edit.json>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task Render_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "clips": [
                { "id": "clip-001", "in": "00:00:00", "out": "00:00:02", "label": "intro" }
              ],
              "audioTracks": [],
              "artifacts": [],
              "output": { "path": "final.mp4", "container": "mp4" }
            }
            """);

        try
        {
            var result = await RunCliAsync("render", "--plan", planPath, "--ffmpeg", "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("render --plan <path>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(Path.Combine(outputDirectory, "final.mp4")));
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
    public async Task Cut_RejectsInvalidTimeRange()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-cut-range-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "clip.mp4");

        try
        {
            var result = await RunCliAsync(
                "cut",
                "input.mp4",
                "--from",
                "00:00:05",
                "--to",
                "00:00:04",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--to' must be greater than '--from'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("cut <input>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task Cut_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-cut-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "clip.mp4");

        try
        {
            var result = await RunCliAsync(
                "cut",
                "input.mp4",
                "--from",
                "00:00:00",
                "--to",
                "00:00:02",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("cut <input>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task Concat_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-concat-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var inputListPath = Path.Combine(outputDirectory, "clips.txt");
        var outputPath = Path.Combine(outputDirectory, "merged.mp4");
        await File.WriteAllTextAsync(inputListPath, "file 'a.mp4'");

        try
        {
            var result = await RunCliAsync(
                "concat",
                "--input-list",
                inputListPath,
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("concat --input-list <path>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task ExtractAudio_RequiresTrackOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-extract-track-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "voice.m4a");

        try
        {
            var result = await RunCliAsync(
                "extract-audio",
                "input.mp4",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--track' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("extract-audio <input> --track <n>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task ExtractAudio_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-extract-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "voice.m4a");

        try
        {
            var result = await RunCliAsync(
                "extract-audio",
                "input.mp4",
                "--track",
                "0",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("extract-audio <input> --track <n>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task AudioAnalyze_RequiresOutputOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-analyze-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("audio-analyze", "input.mp4");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("audio-analyze <input> --output <audio.json>", result.StdOut, StringComparison.Ordinal);
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
    public async Task AudioAnalyze_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-analyze-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "audio.json");

        try
        {
            var result = await RunCliAsync(
                "audio-analyze",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("audio-analyze <input> --output <audio.json>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task AudioGain_RequiresGainDbOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-gain-db-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "gain.wav");

        try
        {
            var result = await RunCliAsync(
                "audio-gain",
                "input.mp4",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--gain-db' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("audio-gain <input> --gain-db <n> --output <path>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task AudioGain_RejectsInvalidGainDb()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-gain-invalid-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "gain.wav");

        try
        {
            var result = await RunCliAsync(
                "audio-gain",
                "input.mp4",
                "--gain-db",
                "oops",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--gain-db' expects a numeric value.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("audio-gain <input> --gain-db <n> --output <path>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task AudioGain_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-gain-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "gain.wav");

        try
        {
            var result = await RunCliAsync(
                "audio-gain",
                "input.mp4",
                "--gain-db",
                "3",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("audio-gain <input> --gain-db <n> --output <path>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task Transcribe_RequiresModelOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-transcribe-model-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "transcript.json");

        try
        {
            var result = await RunCliAsync(
                "transcribe",
                "input.mp4",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--model' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("transcribe <input> --model <path> --output <transcript.json>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task Transcribe_RequiresOutputOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-transcribe-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync(
                "transcribe",
                "input.mp4",
                "--model",
                "ggml-base.bin");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("transcribe <input> --model <path> --output <transcript.json>", result.StdOut, StringComparison.Ordinal);
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
    public async Task Transcribe_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-transcribe-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "transcript.json");

        try
        {
            var result = await RunCliAsync(
                "transcribe",
                "input.mp4",
                "--model",
                "ggml-base.bin",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("transcribe <input> --model <path> --output <transcript.json>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task Transcribe_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-transcribe-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "transcript.json");
        var jsonOutPath = Path.Combine(outputDirectory, "transcribe.json");
        var modelPath = Path.Combine(outputDirectory, "ggml-base.bin");
        var ffmpegPath = Path.Combine(outputDirectory, "fake-ffmpeg.cmd");
        var whisperCliPath = Path.Combine(outputDirectory, "fake-whisper.cmd");
        await File.WriteAllTextAsync(modelPath, "model");
        WriteScript(
            ffmpegPath,
            """
            @echo off
            setlocal
            set "out="
            :loop
            if "%~1"=="" goto done
            set "out=%~1"
            shift
            goto loop
            :done
            if "%out%"=="" exit /b 2
            break> "%out%"
            exit /b 0
            """);
        WriteScript(
            whisperCliPath,
            """
            @echo off
            setlocal EnableDelayedExpansion
            set "prefix="
            :parse
            if "%~1"=="" goto done
            if "%~1"=="-of" (
              set "prefix=%~2"
              shift
            )
            shift
            goto parse
            :done
            if "%prefix%"=="" exit /b 2
            > "%prefix%.json" (
              echo {"result":{"language":"en"},"transcription":[{"text":"hello world","offsets":{"from":0,"to":1000}}]}
            )
            exit /b 0
            """);

        try
        {
            var result = await RunCliAsync(
                "transcribe",
                "input.mp4",
                "--model",
                modelPath,
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath,
                "--whisper-cli",
                whisperCliPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            var transcriptFile = JsonNode.Parse(await File.ReadAllTextAsync(outputPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal(Path.GetFullPath(outputPath), stdout["transcribe"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(modelPath), stdout["transcribe"]!["modelPath"]!.GetValue<string>());
            Assert.Equal("en", stdout["transcribe"]!["language"]!.GetValue<string>());
            Assert.Equal(1, stdout["transcribe"]!["segmentCount"]!.GetValue<int>());
            Assert.NotNull(stdout["transcript"]);
            Assert.True(JsonNode.DeepEquals(stdout["transcript"], transcriptFile));
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
    public async Task DetectSilence_RequiresOutputOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-detect-silence-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("detect-silence", "input.mp4");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("detect-silence <input> --output <silence.json>", result.StdOut, StringComparison.Ordinal);
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
    public async Task DetectSilence_RejectsInvalidNoiseDb()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-detect-silence-noise-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "silence.json");

        try
        {
            var result = await RunCliAsync(
                "detect-silence",
                "input.mp4",
                "--output",
                outputPath,
                "--noise-db",
                "oops");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--noise-db' expects a numeric value.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("detect-silence <input> --output <silence.json>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task DetectSilence_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-detect-silence-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "silence.json");

        try
        {
            var result = await RunCliAsync(
                "detect-silence",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("detect-silence <input> --output <silence.json>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task SeparateAudio_RequiresOutputDirectoryOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-separate-audio-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("separate-audio", "input.mp4");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output-dir' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("separate-audio <input> --output-dir <path>", result.StdOut, StringComparison.Ordinal);
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
    public async Task SeparateAudio_RejectsMissingDemucsExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-separate-audio-demucs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync(
                "separate-audio",
                "input.mp4",
                "--output-dir",
                outputDirectory,
                "--demucs",
                "missing-demucs");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-demucs", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("separate-audio <input> --output-dir <path>", result.StdOut, StringComparison.Ordinal);
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
    public async Task Subtitle_RejectsUnsupportedFormat()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-subtitle-format-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var outputPath = Path.Combine(outputDirectory, "subs.srt");
        await File.WriteAllTextAsync(transcriptPath, """{"schemaVersion":1,"language":"en","segments":[]}""");

        try
        {
            var result = await RunCliAsync(
                "subtitle",
                "input.mp4",
                "--transcript",
                transcriptPath,
                "--format",
                "vtt",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--format' expects one of: srt, ass.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("subtitle <input> --transcript <transcript.json> --format <srt|ass>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task Subtitle_RequiresTranscriptOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-subtitle-transcript-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "subs.srt");

        try
        {
            var result = await RunCliAsync(
                "subtitle",
                "input.mp4",
                "--format",
                "srt",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--transcript' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("subtitle <input> --transcript <transcript.json> --format <srt|ass>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task Subtitle_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-subtitle-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var outputPath = Path.Combine(outputDirectory, "subs.srt");
        var jsonOutPath = Path.Combine(outputDirectory, "subtitle.json");
        await File.WriteAllTextAsync(
            transcriptPath,
            """{"schemaVersion":1,"language":"en","segments":[{"id":"seg-001","start":"00:00:00","end":"00:00:01","text":"hello world"}]}""");

        try
        {
            var result = await RunCliAsync(
                "subtitle",
                "input.mp4",
                "--transcript",
                transcriptPath,
                "--format",
                "srt",
                "--output",
                outputPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal(Path.GetFullPath(outputPath), stdout["subtitle"]!["outputPath"]!.GetValue<string>());
            Assert.Equal("srt", stdout["subtitle"]!["format"]!.GetValue<string>());
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
    public async Task BeatTrack_RequiresOutputOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("beat-track", "input.mp4");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("beat-track <input> --output <beats.json>", result.StdOut, StringComparison.Ordinal);
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
    public async Task BeatTrack_RejectsInvalidSampleRate()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-sample-rate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "beats.json");

        try
        {
            var result = await RunCliAsync(
                "beat-track",
                "input.mp4",
                "--output",
                outputPath,
                "--sample-rate",
                "oops");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--sample-rate' expects an integer value.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("beat-track <input> --output <beats.json>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task BeatTrack_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "beats.json");

        try
        {
            var result = await RunCliAsync(
                "beat-track",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("beat-track <input> --output <beats.json>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task Subtitle_RequiresSupportedFormat()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-subtitle-format-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var outputPath = Path.Combine(outputDirectory, "subs.srt");
        await File.WriteAllTextAsync(transcriptPath, """{"schemaVersion":1,"language":"en","segments":[]}""");

        try
        {
            var result = await RunCliAsync(
                "subtitle",
                "input.mp4",
                "--transcript",
                transcriptPath,
                "--format",
                "vtt",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--format' expects one of: srt, ass.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("subtitle <input> --transcript <transcript.json> --format <srt|ass>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task Subtitle_RequiresTranscriptPath()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-subtitle-transcript-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "subs.srt");

        try
        {
            var result = await RunCliAsync(
                "subtitle",
                "input.mp4",
                "--format",
                "srt",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--transcript' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("subtitle <input> --transcript <transcript.json> --format <srt|ass>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task BeatTrack_RequiresOutputPath()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("beat-track", "input.mp4");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("beat-track <input> --output <beats.json>", result.StdOut, StringComparison.Ordinal);
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
    public async Task BeatTrack_RejectsNonIntegerSampleRate()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-sample-rate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "beats.json");

        try
        {
            var result = await RunCliAsync(
                "beat-track",
                "input.mp4",
                "--output",
                outputPath,
                "--sample-rate",
                "oops");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--sample-rate' expects an integer value.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("beat-track <input> --output <beats.json>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task BeatTrack_RejectsMissingFfmpegBinary()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "beats.json");

        try
        {
            var result = await RunCliAsync(
                "beat-track",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("beat-track <input> --output <beats.json>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task Probe_RejectsMissingFfprobeExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-probe-missing-ffprobe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync(
                "probe",
                "input.mp4",
                "--ffprobe",
                "missing-ffprobe");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffprobe", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("probe <input>", result.StdOut, StringComparison.Ordinal);
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
    public async Task Plan_RequiresInputPath()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-plan-input-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("plan");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Missing input file path.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("plan <input>", result.StdOut, StringComparison.Ordinal);
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
    public async Task Run_RejectsMissingFfprobeExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-run-missing-ffprobe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync(
                "run",
                "input.mp4",
                "--ffprobe",
                "missing-ffprobe");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffprobe", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("run <input>", result.StdOut, StringComparison.Ordinal);
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
    public async Task Doctor_ReturnsStructuredDependencyEnvelopeAndNonZeroWhenRequiredDependenciesMissing()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-doctor-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var modelPath = Path.Combine(outputDirectory, "model.gguf");
        await File.WriteAllTextAsync(modelPath, "model");

        try
        {
            var result = await RunCliAsync(
                "doctor",
                "--ffmpeg",
                "missing-ffmpeg",
                "--ffprobe",
                "missing-ffprobe",
                "--whisper-cli",
                "missing-whisper",
                "--demucs",
                "missing-demucs",
                "--whisper-model",
                modelPath);

            Assert.Equal(1, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("doctor", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            Assert.False(payload["isHealthy"]!.GetValue<bool>());
            Assert.Equal(2, payload["missingRequiredCount"]!.GetValue<int>());
            Assert.Equal(2, payload["missingOptionalCount"]!.GetValue<int>());

            var dependencies = payload["dependencies"]!.AsArray();
            Assert.Equal(5, dependencies.Count);
            Assert.Contains(
                dependencies,
                node => node!["id"]!.GetValue<string>() == "ffmpeg"
                    && node["source"]!.GetValue<string>() == "option"
                    && !node["isAvailable"]!.GetValue<bool>());
            Assert.Contains(
                dependencies,
                node => node!["id"]!.GetValue<string>() == "whisper-model"
                    && node["kind"]!.GetValue<string>() == "file"
                    && node["isAvailable"]!.GetValue<bool>());
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
    public async Task Doctor_CanWriteEnvelopeToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-doctor-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var jsonOutPath = Path.Combine(outputDirectory, "doctor.json");
        var modelPath = Path.Combine(outputDirectory, "model.gguf");
        await File.WriteAllTextAsync(modelPath, "model");

        try
        {
            var result = await RunCliAsync(
                "doctor",
                "--ffmpeg",
                "missing-ffmpeg",
                "--ffprobe",
                "missing-ffprobe",
                "--whisper-cli",
                "missing-whisper",
                "--demucs",
                "missing-demucs",
                "--whisper-model",
                modelPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(1, result.ExitCode);
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private static async Task<CliRunResult> RunCliAsync(params string[] args)
    {
        var cliAssemblyPath = typeof(TemplateCommandArtifactsBuilder).Assembly.Location;
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(cliAssemblyPath)!, "..", "..", "..", "..", ".."));

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add(cliAssemblyPath);
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new CliRunResult
        {
            ExitCode = process.ExitCode,
            StdOut = stdout,
            StdErr = stderr
        };
    }

    private static JsonObject GetPreviewPlan(JsonArray previews, string mode)
    {
        var preview = GetPreview(previews, mode);
        return Assert.IsType<JsonObject>(preview["editPlan"]);
    }

    private static JsonObject GetPreview(JsonArray previews, string mode)
    {
        var previewNode = previews.Single(node => node!["mode"]!.GetValue<string>() == mode);
        return Assert.IsType<JsonObject>(previewNode);
    }

    private sealed record CliRunResult
    {
        public required int ExitCode { get; init; }

        public required string StdOut { get; init; }

        public required string StdErr { get; init; }
    }

    private static void WriteScript(string path, string content)
    {
        File.WriteAllText(path, content.ReplaceLineEndings("\r\n"), new UTF8Encoding(false));
    }
}
