using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using OpenVideoToolbox.Cli;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Serialization;
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
                    "ovt transcribe <input> --model <whisper-model-path> --output transcript.json",
                    "ovt detect-silence <input> --output silence.json"
                ]));
            var signalInstructions = commands["signalInstructions"]!.AsArray();
            Assert.Equal(2, signalInstructions.Count);
            Assert.Contains(
                signalInstructions,
                node => node!["kind"]!.GetValue<string>() == "transcript"
                    && node["command"]!.GetValue<string>().Contains("<whisper-model-path>", StringComparison.Ordinal));
            Assert.Contains(
                signalInstructions,
                node => node!["kind"]!.GetValue<string>() == "silence"
                    && node["consumption"]!.GetValue<string>().Contains("edit.json clip boundaries", StringComparison.Ordinal));
            Assert.Empty(commands["artifactCommands"]!.AsArray());

            var guide = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "guide.json")))!.AsObject();
            var commandFiles = guide["examples"]!["commandFiles"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray();
            Assert.True(commandFiles.SequenceEqual(["commands.json", "commands.ps1", "commands.cmd", "commands.sh"]));
            Assert.Equal("<whisper-model-path>", commands["variables"]!["whisperModelPath"]!.GetValue<string>());
            var powerShellScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.ps1"));
            Assert.Contains("$WhisperModelPath = \"<whisper-model-path>\"", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("# transcript seed example", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("ovt init-plan $InputPath --template commentary-bgm --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("# transcript variant: min-duration (recommended)", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("ovt init-plan $InputPath --template commentary-bgm --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript --min-transcript-segment-duration-ms 500", powerShellScript, StringComparison.Ordinal);
            var batchScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.cmd"));
            Assert.Contains("REM Review silence.json before hand-tuning edit.json clip boundaries", batchScript, StringComparison.Ordinal);
            Assert.Contains("REM transcript variant: min-duration (recommended)", batchScript, StringComparison.Ordinal);
            var shellScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.sh"));
            Assert.Contains("# transcript variant: min-duration (recommended)", shellScript, StringComparison.Ordinal);
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
    public async Task TemplatesWriteExamples_ForCaptionedTemplate_WritesArtifactCommandsIntoAllScripts()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-template-captioned-{Guid.NewGuid():N}");

        try
        {
            var result = await RunCliAsync("templates", "shorts-captioned", "--write-examples", outputDirectory);

            Assert.Equal(0, result.ExitCode);

            var commands = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.json")))!.AsObject();
            Assert.Contains(
                commands["artifactCommands"]!.AsArray(),
                node => node!.GetValue<string>() == "ovt subtitle <input> --transcript transcript.json --format srt --output subtitles.srt");

            var powerShellScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.ps1"));
            var batchScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.cmd"));
            var shellScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.sh"));

            Assert.Contains("ovt subtitle $InputPath --transcript transcript.json --format srt --output subtitles.srt", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("ovt subtitle \"%INPUT_PATH%\" --transcript transcript.json --format srt --output subtitles.srt", batchScript, StringComparison.Ordinal);
            Assert.Contains("ovt subtitle \"$INPUT_PATH\" --transcript transcript.json --format srt --output subtitles.srt", shellScript, StringComparison.Ordinal);
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
    public async Task TemplatesWriteExamples_ForBeatMontage_WritesStemGuidanceIntoAllScripts()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-template-beat-scripts-{Guid.NewGuid():N}");

        try
        {
            var result = await RunCliAsync("templates", "beat-montage", "--write-examples", outputDirectory);

            Assert.Equal(0, result.ExitCode);

            var powerShellScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.ps1"));
            var batchScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.cmd"));
            var shellScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.sh"));

            Assert.Contains("After scaffold-template writes artifacts.json, point the bgm slot at stems/htdemucs/input/no_vocals.wav", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("ovt separate-audio $InputPath --output-dir stems", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("After scaffold-template writes artifacts.json, point the bgm slot at stems/htdemucs/input/no_vocals.wav", batchScript, StringComparison.Ordinal);
            Assert.Contains("ovt separate-audio \"%INPUT_PATH%\" --output-dir stems", batchScript, StringComparison.Ordinal);
            Assert.Contains("After scaffold-template writes artifacts.json, point the bgm slot at stems/htdemucs/input/no_vocals.wav", shellScript, StringComparison.Ordinal);
            Assert.Contains("ovt separate-audio \"$INPUT_PATH\" --output-dir stems", shellScript, StringComparison.Ordinal);
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
    public async Task TemplatesSummary_WithPluginDir_IncludesPluginMetadataAndTemplates()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"ovt-template-plugin-summary-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);

        try
        {
            var pluginDirectory = await CreateTemplatePluginAsync(
                workingDirectory,
                pluginId: "community-pack",
                pluginDisplayName: "Community Pack",
                template: CreatePluginTemplateDefinition("plugin-captioned", "Plugin Captioned"));

            var result = await RunCliAsync("templates", "--summary", "--plugin-dir", pluginDirectory);

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            var plugins = payload["plugins"]!.AsArray();
            var plugin = Assert.IsType<JsonObject>(Assert.Single(plugins));
            Assert.Equal("community-pack", plugin["id"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(pluginDirectory), plugin["directory"]!.GetValue<string>());

            var templates = payload["templates"]!.AsArray();
            Assert.Contains(
                templates,
                node => node is JsonObject templateNode
                    && templateNode["id"]!.GetValue<string>() == "plugin-captioned");
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
    public async Task TemplateGuide_WithPluginDir_ReturnsPluginSourceAndWritesExamples()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"ovt-template-plugin-guide-{Guid.NewGuid():N}");
        var outputDirectory = Path.Combine(workingDirectory, "examples");
        Directory.CreateDirectory(workingDirectory);

        try
        {
            var pluginDirectory = await CreateTemplatePluginAsync(
                workingDirectory,
                pluginId: "community-pack",
                pluginDisplayName: "Community Pack",
                template: CreatePluginTemplateDefinition("plugin-captioned", "Plugin Captioned"));

            var result = await RunCliAsync(
                "templates",
                "plugin-captioned",
                "--plugin-dir",
                pluginDirectory,
                "--write-examples",
                outputDirectory);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(Path.Combine(outputDirectory, "guide.json")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "template.json")));

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("plugin", payload["source"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", payload["source"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("plugin-captioned", payload["template"]!["id"]!.GetValue<string>());

            var guide = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "guide.json")))!.AsObject();
            Assert.Equal("plugin", guide["source"]!["kind"]!.GetValue<string>());
            var previewPlan = guide["examples"]!["previewPlans"]![0]!["editPlan"]!.AsObject();
            Assert.Equal("plugin", previewPlan["template"]!["source"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", previewPlan["template"]!["source"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", previewPlan["template"]!["source"]!["pluginVersion"]!.GetValue<string>());

            var template = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "template.json")))!.AsObject();
            Assert.Equal("plugin-captioned", template["id"]!.GetValue<string>());

            var commands = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.json")))!.AsObject();
            Assert.Equal("<plugin-dir>", commands["variables"]!["pluginDir"]!.GetValue<string>());
            Assert.Contains(
                commands["initPlanCommands"]!.AsArray(),
                node => node!.GetValue<string>().Contains("--plugin-dir <plugin-dir>", StringComparison.Ordinal));
            Assert.Contains(
                commands["workflowCommands"]!.AsArray(),
                node => node!.GetValue<string>() == "ovt validate-plan --plan edit.json --plugin-dir <plugin-dir>");

            var powerShellScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.ps1"));
            Assert.Contains("$PluginDir = \"<plugin-dir>\"", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("ovt validate-plan --plan edit.json --plugin-dir $PluginDir", powerShellScript, StringComparison.Ordinal);
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
    public async Task TemplatesSummary_WithPluginDir_FailsWhenTemplateIdConflictsWithBuiltInCatalog()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"ovt-template-plugin-duplicate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);

        try
        {
            var pluginDirectory = await CreateTemplatePluginAsync(
                workingDirectory,
                pluginId: "community-pack",
                pluginDisplayName: "Community Pack",
                template: CreatePluginTemplateDefinition("shorts-captioned", "Conflicting Plugin Template"));

            var result = await RunCliAsync("templates", "--summary", "--plugin-dir", pluginDirectory);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("Duplicate edit plan template id 'shorts-captioned'.", result.StdErr, StringComparison.Ordinal);
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

    [Fact]
    public async Task ScaffoldTemplate_WithPluginDir_WritesPluginTemplateOutputs()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"ovt-scaffold-plugin-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);
        var outputDirectory = Path.Combine(workingDirectory, "scaffold");

        try
        {
            var pluginDirectory = await CreateTemplatePluginAsync(
                workingDirectory,
                pluginId: "community-pack",
                pluginDisplayName: "Community Pack",
                template: CreatePluginTemplateDefinition("plugin-captioned", "Plugin Captioned"));

            var result = await RunCliAsync(
                "scaffold-template",
                "input.mp4",
                "--template",
                "plugin-captioned",
                "--dir",
                outputDirectory,
                "--validate",
                "--plugin-dir",
                pluginDirectory);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(Path.Combine(outputDirectory, "edit.json")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "guide.json")));

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("plugin", payload["source"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", payload["source"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", payload["source"]!["pluginVersion"]!.GetValue<string>());
            Assert.Equal("plugin-captioned", payload["template"]!["id"]!.GetValue<string>());
            Assert.Contains(payload["scaffold"]!["writtenFiles"]!.AsArray(), node => Path.GetFileName(node!.GetValue<string>()) == "guide.json");
            Assert.True(payload["validated"]!.GetValue<bool>());
            Assert.True(payload["validation"]!["isValid"]!.GetValue<bool>());

            var guide = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "guide.json")))!.AsObject();
            Assert.Equal("plugin", guide["source"]!["kind"]!.GetValue<string>());
            var previewPlan = guide["examples"]!["previewPlans"]![0]!["editPlan"]!.AsObject();
            Assert.Equal("plugin", previewPlan["template"]!["source"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", previewPlan["template"]!["source"]!["pluginId"]!.GetValue<string>());

            var editPlan = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "edit.json")))!.AsObject();
            Assert.Equal("plugin", editPlan["template"]!["source"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", editPlan["template"]!["source"]!["pluginId"]!.GetValue<string>());

            var commands = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.json")))!.AsObject();
            Assert.Equal("<plugin-dir>", commands["variables"]!["pluginDir"]!.GetValue<string>());
            Assert.Contains(
                commands["workflowCommands"]!.AsArray(),
                node => node!.GetValue<string>() == "ovt validate-plan --plan edit.json --plugin-dir <plugin-dir>");
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

    [Fact]
    public async Task ScaffoldTemplate_ForBeatMontage_WritesStemFirstArtifactsExample()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-scaffold-beat-montage-{Guid.NewGuid():N}");

        try
        {
            var result = await RunCliAsync("scaffold-template", "input.mp4", "--template", "beat-montage", "--dir", outputDirectory);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(Path.Combine(outputDirectory, "artifacts.json")));

            var artifacts = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "artifacts.json")))!.AsObject();
            Assert.Equal("stems/htdemucs/input/no_vocals.wav", artifacts["bgm"]!.GetValue<string>());
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
              "template": {
                "id": "plugin-captioned",
                "source": {
                  "kind": "plugin",
                  "pluginId": "community-pack",
                  "pluginVersion": "1.0.0"
                },
                "parameters": {}
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
            Assert.Equal("plugin", renderPayload["templateSource"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", renderPayload["templateSource"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", renderPayload["templateSource"]!["pluginVersion"]!.GetValue<string>());

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
            Assert.Equal("plugin", mixPayload["templateSource"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", mixPayload["templateSource"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", mixPayload["templateSource"]!["pluginVersion"]!.GetValue<string>());
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
              "template": {
                "id": "plugin-captioned",
                "source": {
                  "kind": "plugin",
                  "pluginId": "community-pack",
                  "pluginVersion": "1.0.0"
                },
                "parameters": {}
              },
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
            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("mix-audio", payload["command"]!.GetValue<string>());
            Assert.True(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal("plugin", envelope["templateSource"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", envelope["templateSource"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", envelope["templateSource"]!["pluginVersion"]!.GetValue<string>());
            Assert.Equal(outputPath, envelope["mixAudio"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("Edit plan must contain at least one clip.", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
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
              "template": {
                "id": "plugin-captioned",
                "source": {
                  "kind": "plugin",
                  "pluginId": "community-pack",
                  "pluginVersion": "1.0.0"
                },
                "parameters": {}
              },
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
            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("render", payload["command"]!.GetValue<string>());
            Assert.True(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal("plugin", envelope["templateSource"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", envelope["templateSource"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", envelope["templateSource"]!["pluginVersion"]!.GetValue<string>());
            Assert.Equal("plugin-captioned", envelope["render"]!["template"]!["id"]!.GetValue<string>());
            Assert.Contains("Render plan must contain at least one clip.", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
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
              "template": {
                "id": "plugin-captioned",
                "source": {
                  "kind": "plugin",
                  "pluginId": "community-pack",
                  "pluginVersion": "1.0.0"
                },
                "parameters": {}
              },
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
            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("mix-audio", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal("plugin", envelope["templateSource"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", envelope["templateSource"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", envelope["templateSource"]!["pluginVersion"]!.GetValue<string>());
            Assert.Equal(outputPath, envelope["mixAudio"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.NotNull(envelope["executionPreview"]);
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
              "template": {
                "id": "plugin-captioned",
                "source": {
                  "kind": "plugin",
                  "pluginId": "community-pack",
                  "pluginVersion": "1.0.0"
                },
                "parameters": {}
              },
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
            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("render", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal("plugin", envelope["templateSource"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", envelope["templateSource"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", envelope["templateSource"]!["pluginVersion"]!.GetValue<string>());
            Assert.Equal("plugin-captioned", envelope["render"]!["template"]!["id"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.NotNull(envelope["executionPreview"]);
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
    public async Task MixAudio_ReturnsFailureEnvelope_WhenExecutableExitsNonZero()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-mix-process-failed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var outputPath = Path.Combine(outputDirectory, "mixed.wav");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg-fail",
            """
            @echo off
            echo fake ffmpeg failure 1>&2
            exit /b 7
            """,
            """
            #!/bin/sh
            echo "fake ffmpeg failure" >&2
            exit 7
            """);

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "template": {
                "id": "plugin-captioned",
                "source": {
                  "kind": "plugin",
                  "pluginId": "community-pack",
                  "pluginVersion": "1.0.0"
                },
                "parameters": {}
              },
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
            var result = await RunCliAsync("mix-audio", "--plan", planPath, "--output", outputPath, "--ffmpeg", ffmpegPath);

            Assert.Equal(2, result.ExitCode);
            Assert.Contains("Process exited with code 7.", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("mix-audio", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal("plugin", envelope["templateSource"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", envelope["templateSource"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", envelope["templateSource"]!["pluginVersion"]!.GetValue<string>());
            Assert.Equal(outputPath, envelope["mixAudio"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("Process exited with code 7.", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.NotNull(envelope["executionPreview"]);
            Assert.Equal("failed", envelope["execution"]!["status"]!.GetValue<string>());
            Assert.Equal(7, envelope["execution"]!["exitCode"]!.GetValue<int>());
            Assert.Contains("Process exited with code 7.", envelope["execution"]!["errorMessage"]!.GetValue<string>(), StringComparison.Ordinal);
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
    public async Task Render_ReturnsFailureEnvelope_WhenExecutableExitsNonZero()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-process-failed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg-fail",
            """
            @echo off
            echo fake ffmpeg failure 1>&2
            exit /b 7
            """,
            """
            #!/bin/sh
            echo "fake ffmpeg failure" >&2
            exit 7
            """);

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": { "inputPath": "input.mp4" },
              "template": {
                "id": "plugin-captioned",
                "source": {
                  "kind": "plugin",
                  "pluginId": "community-pack",
                  "pluginVersion": "1.0.0"
                },
                "parameters": {}
              },
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
            var result = await RunCliAsync("render", "--plan", planPath, "--ffmpeg", ffmpegPath);

            Assert.Equal(2, result.ExitCode);
            Assert.Contains("Process exited with code 7.", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("render", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal("plugin", envelope["templateSource"]!["kind"]!.GetValue<string>());
            Assert.Equal("community-pack", envelope["templateSource"]!["pluginId"]!.GetValue<string>());
            Assert.Equal("1.0.0", envelope["templateSource"]!["pluginVersion"]!.GetValue<string>());
            Assert.Equal("plugin-captioned", envelope["render"]!["template"]!["id"]!.GetValue<string>());
            Assert.Contains("Process exited with code 7.", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.NotNull(envelope["executionPreview"]);
            Assert.Equal("failed", envelope["execution"]!["status"]!.GetValue<string>());
            Assert.Equal(7, envelope["execution"]!["exitCode"]!.GetValue<int>());
            Assert.Contains("Process exited with code 7.", envelope["execution"]!["errorMessage"]!.GetValue<string>(), StringComparison.Ordinal);
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

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("cut", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["cut"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Null(envelope["execution"]);
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
    public async Task Cut_ReturnsFailureEnvelope_WhenExecutableExitsNonZero()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-cut-process-failed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "clip.mp4");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg-fail",
            """
            @echo off
            echo fake ffmpeg failure 1>&2
            exit /b 7
            """,
            """
            #!/bin/sh
            echo "fake ffmpeg failure" >&2
            exit 7
            """);

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
                ffmpegPath);

            Assert.Equal(2, result.ExitCode);
            Assert.Contains("Process exited with code 7.", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("cut", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["cut"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("Process exited with code 7.", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Equal("failed", envelope["execution"]!["status"]!.GetValue<string>());
            Assert.Equal(7, envelope["execution"]!["exitCode"]!.GetValue<int>());
            Assert.Contains("Process exited with code 7.", envelope["execution"]!["errorMessage"]!.GetValue<string>(), StringComparison.Ordinal);
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

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("concat", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(inputListPath), envelope["concat"]!["inputListPath"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(outputPath), envelope["concat"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Null(envelope["execution"]);
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

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("extract-audio", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["extractAudio"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(0, envelope["extractAudio"]!["trackIndex"]!.GetValue<int>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Null(envelope["execution"]);
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

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("audio-analyze", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["audioAnalyze"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
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
    public async Task AudioAnalyze_ReturnsFailureEnvelope_WhenProcessExitsNonZero()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-analyze-failed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "audio.json");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg-fail",
            """
            @echo off
            1>&2 echo loudnorm failed
            exit /b 7
            """,
            """
            #!/bin/sh
            echo "loudnorm failed" >&2
            exit 7
            """);

        try
        {
            var result = await RunCliAsync(
                "audio-analyze",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("ffmpeg audio analysis failed", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("audio-analyze", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["audioAnalyze"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("ffmpeg audio analysis failed", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
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
    public async Task AudioAnalyze_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-analyze-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "audio.json");
        var jsonOutPath = Path.Combine(outputDirectory, "audio-analyze.json");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg",
            """
            @echo off
            1>&2 echo {
            1>&2 echo   "input_i" : "-16.40",
            1>&2 echo   "input_lra" : "3.10",
            1>&2 echo   "input_tp" : "-1.20",
            1>&2 echo   "input_thresh" : "-27.30",
            1>&2 echo   "target_offset" : "0.50"
            1>&2 echo }
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            cat >&2 <<'EOF'
            {
              "input_i" : "-16.40",
              "input_lra" : "3.10",
              "input_tp" : "-1.20",
              "input_thresh" : "-27.30",
              "target_offset" : "0.50"
            }
            EOF
            """);

        try
        {
            var result = await RunCliAsync(
                "audio-analyze",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            var analysisFile = JsonNode.Parse(await File.ReadAllTextAsync(outputPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("audio-analyze", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["audioAnalyze"]!["outputPath"]!.GetValue<string>());
            Assert.True(JsonNode.DeepEquals(envelope["analysis"], analysisFile));
            Assert.Equal(-16.4, envelope["analysis"]!["analysis"]!["integratedLoudness"]!.GetValue<double>());
            Assert.Equal(3.1, envelope["analysis"]!["analysis"]!["loudnessRange"]!.GetValue<double>());
            Assert.Equal(-1.2, envelope["analysis"]!["analysis"]!["truePeakDb"]!.GetValue<double>());
            Assert.Equal(-27.3, envelope["analysis"]!["analysis"]!["thresholdDb"]!.GetValue<double>());
            Assert.Equal(0.5, envelope["analysis"]!["analysis"]!["targetOffset"]!.GetValue<double>());
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

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("audio-gain", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["audioGain"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(3, envelope["audioGain"]!["gainDb"]!.GetValue<double>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Null(envelope["execution"]);
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
    public async Task AudioGain_ReturnsFailureEnvelope_WhenProcessExitsNonZero()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-gain-failed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "gain.wav");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg-fail",
            """
            @echo off
            echo volume failed 1>&2
            exit /b 7
            """,
            """
            #!/bin/sh
            echo "volume failed" >&2
            exit 7
            """);

        try
        {
            var result = await RunCliAsync(
                "audio-gain",
                "input.mp4",
                "--gain-db",
                "-6",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath);

            Assert.Equal(2, result.ExitCode);
            Assert.Contains("Process exited with code 7.", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("audio-gain", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["audioGain"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(-6, envelope["audioGain"]!["gainDb"]!.GetValue<double>());
            Assert.Contains("Process exited with code 7.", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Equal("failed", envelope["execution"]!["status"]!.GetValue<string>());
            Assert.Equal(7, envelope["execution"]!["exitCode"]!.GetValue<int>());
            Assert.Contains("Process exited with code 7.", envelope["execution"]!["errorMessage"]!.GetValue<string>(), StringComparison.Ordinal);
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
    public async Task AudioGain_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-gain-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "gain.wav");
        var jsonOutPath = Path.Combine(outputDirectory, "audio-gain.json");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg",
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
            """,
            """
            #!/usr/bin/env bash
            out=""
            for arg in "$@"; do
              out="$arg"
            done
            if [ -z "$out" ]; then
              exit 2
            fi
            : > "$out"
            """);

        try
        {
            var result = await RunCliAsync(
                "audio-gain",
                "input.mp4",
                "--gain-db",
                "-6",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("audio-gain", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["audioGain"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(-6, envelope["audioGain"]!["gainDb"]!.GetValue<double>());
            Assert.Equal("succeeded", envelope["execution"]!["status"]!.GetValue<string>());
            Assert.Contains(Path.GetFullPath(outputPath), envelope["execution"]!["producedPaths"]!.AsArray().Select(node => node!.GetValue<string>()));
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

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("transcribe", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["transcribe"]!["outputPath"]!.GetValue<string>());
            Assert.EndsWith("ggml-base.bin", envelope["transcribe"]!["modelPath"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
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
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg",
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
            """,
            """
            #!/usr/bin/env bash
            out=""
            for arg in "$@"; do
              out="$arg"
            done
            if [ -z "$out" ]; then
              exit 2
            fi
            : > "$out"
            """);
        var whisperCliPath = WriteExecutableScript(
            outputDirectory,
            "fake-whisper",
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
            """,
            """
            #!/usr/bin/env bash
            prefix=""
            while [ "$#" -gt 0 ]; do
              if [ "$1" = "-of" ]; then
                prefix="$2"
                shift 2
                continue
              fi
              shift
            done
            if [ -z "$prefix" ]; then
              exit 2
            fi
            cat > "${prefix}.json" <<'EOF'
            {"result":{"language":"en"},"transcription":[{"text":"hello world","offsets":{"from":0,"to":1000}}]}
            EOF
            """);
        await File.WriteAllTextAsync(modelPath, "model");

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
            Assert.Equal("transcribe", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["transcribe"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(modelPath), envelope["transcribe"]!["modelPath"]!.GetValue<string>());
            Assert.Equal("en", envelope["transcribe"]!["language"]!.GetValue<string>());
            Assert.Equal(1, envelope["transcribe"]!["segmentCount"]!.GetValue<int>());
            Assert.NotNull(envelope["transcript"]);
            Assert.True(JsonNode.DeepEquals(envelope["transcript"], transcriptFile));
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

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("detect-silence", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["detectSilence"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
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
    public async Task DetectSilence_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-detect-silence-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "silence.json");
        var jsonOutPath = Path.Combine(outputDirectory, "detect-silence.json");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg",
            """
            @echo off
            1>&2 echo silence_start: 1.25
            1>&2 echo silence_end: 2.75 ^| silence_duration: 1.50
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'silence_start: 1.25\n' >&2
            printf 'silence_end: 2.75 | silence_duration: 1.50\n' >&2
            """);

        try
        {
            var result = await RunCliAsync(
                "detect-silence",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            var silenceFile = JsonNode.Parse(await File.ReadAllTextAsync(outputPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("detect-silence", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["detectSilence"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(1, envelope["detectSilence"]!["segmentCount"]!.GetValue<int>());
            Assert.True(JsonNode.DeepEquals(envelope["silence"], silenceFile));
            Assert.Equal("00:00:01.2500000", envelope["silence"]!["segments"]![0]!["start"]!.GetValue<string>());
            Assert.Equal("00:00:02.7500000", envelope["silence"]!["segments"]![0]!["end"]!.GetValue<string>());
            Assert.Equal("00:00:01.5000000", envelope["silence"]!["segments"]![0]!["duration"]!.GetValue<string>());
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

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("separate-audio", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputDirectory), envelope["separateAudio"]!["outputDirectory"]!.GetValue<string>());
            Assert.Contains("missing-demucs", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
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
    public async Task SeparateAudio_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-separate-audio-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var jsonOutPath = Path.Combine(outputDirectory, "separate-audio.json");
        var demucsPath = WriteExecutableScript(
            outputDirectory,
            "fake-demucs",
            """
            @echo off
            setlocal
            set "out="
            set "model=htdemucs"
            set "input="
            :parse
            if "%~1"=="" goto done
            if "%~1"=="-o" (
              set "out=%~2"
              shift
            ) else if "%~1"=="-n" (
              set "model=%~2"
              shift
            ) else (
              set "input=%~1"
            )
            shift
            goto parse
            :done
            if "%out%"=="" exit /b 2
            if "%input%"=="" exit /b 2
            set "name=%~n1"
            if not defined name (
              for %%I in ("%input%") do set "name=%%~nI"
            )
            set "trackdir=%out%\%model%\%name%"
            mkdir "%trackdir%" >nul 2>nul
            break> "%trackdir%\vocals.wav"
            break> "%trackdir%\no_vocals.wav"
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            out=""
            model="htdemucs"
            input=""
            while [ "$#" -gt 0 ]; do
              case "$1" in
                -o)
                  out="$2"
                  shift 2
                  ;;
                -n)
                  model="$2"
                  shift 2
                  ;;
                *)
                  input="$1"
                  shift
                  ;;
              esac
            done
            if [ -z "$out" ] || [ -z "$input" ]; then
              exit 2
            fi
            name="$(basename "$input")"
            name="${name%.*}"
            trackdir="$out/$model/$name"
            mkdir -p "$trackdir"
            : > "$trackdir/vocals.wav"
            : > "$trackdir/no_vocals.wav"
            """);

        try
        {
            var result = await RunCliAsync(
                "separate-audio",
                "input.mp4",
                "--output-dir",
                outputDirectory,
                "--demucs",
                demucsPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("separate-audio", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputDirectory), envelope["separateAudio"]!["outputDirectory"]!.GetValue<string>());
            Assert.Equal("htdemucs", envelope["separateAudio"]!["model"]!.GetValue<string>());
            Assert.EndsWith(Path.Combine("htdemucs", "input", "vocals.wav"), envelope["stems"]!["vocals"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(Path.Combine("htdemucs", "input", "no_vocals.wav"), envelope["stems"]!["accompaniment"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
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

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("beat-track", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["beatTrack"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Null(envelope["extraction"]);
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
    public async Task BeatTrack_ReturnsFailureEnvelope_WhenExtractionExitsNonZero()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-process-failed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "beats.json");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg-fail",
            """
            @echo off
            echo fake ffmpeg failure 1>&2
            exit /b 7
            """,
            """
            #!/bin/sh
            echo "fake ffmpeg failure" >&2
            exit 7
            """);

        try
        {
            var result = await RunCliAsync(
                "beat-track",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath);

            Assert.Equal(2, result.ExitCode);
            Assert.Contains("Process exited with code 7.", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("beat-track", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["beatTrack"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("Process exited with code 7.", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Equal("failed", envelope["extraction"]!["status"]!.GetValue<string>());
            Assert.Equal(7, envelope["extraction"]!["exitCode"]!.GetValue<int>());
            Assert.Contains("Process exited with code 7.", envelope["extraction"]!["errorMessage"]!.GetValue<string>(), StringComparison.Ordinal);
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
    public async Task BeatTrack_CanWriteEnvelopeToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "beats.json");
        var jsonOutPath = Path.Combine(outputDirectory, "beat-track.json");
        var sourceWavePath = Path.Combine(outputDirectory, "source.wav");
        WriteMonoPcmWave(sourceWavePath, sampleRateHz: 16000, sampleCount: 16000);
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg-copy-wave",
            $"""
            @echo off
            set "out=%~1"
            :loop
            if "%~2"=="" goto done
            shift
            set "out=%~1"
            goto loop
            :done
            copy /Y "{sourceWavePath}" "%out%" >nul
            exit /b 0
            """,
            $"""
            #!/bin/sh
            out=""
            for arg in "$@"; do
              out="$arg"
            done
            cp "{sourceWavePath.Replace("\\", "/")}" "$out"
            """);

        try
        {
            var result = await RunCliAsync(
                "beat-track",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            var beatsFile = JsonNode.Parse(await File.ReadAllTextAsync(outputPath))!.AsObject();

            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("beat-track", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());

            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["beatTrack"]!["outputPath"]!.GetValue<string>());
            Assert.True(envelope["beatTrack"]!["beatCount"]!.GetValue<int>() >= 0);
            Assert.Equal("succeeded", envelope["extraction"]!["status"]!.GetValue<string>());
            Assert.Equal(1, beatsFile["schemaVersion"]!.GetValue<int>());
            Assert.Equal("input.mp4", beatsFile["sourcePath"]!.GetValue<string>());
            Assert.Equal(envelope["beatTrack"]!["beatCount"]!.GetValue<int>(), beatsFile["beats"]!.AsArray().Count);
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

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("beat-track", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["beatTrack"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Null(envelope["extraction"]);
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

    [Fact]
    public async Task Doctor_UsesEnvironmentFallbackForOptionalDependencies()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-doctor-env-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg",
            """
            @echo off
            echo ffmpeg version n-test
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'ffmpeg version n-test\n'
            """);
        var ffprobePath = WriteExecutableScript(
            outputDirectory,
            "fake-ffprobe",
            """
            @echo off
            echo ffprobe version n-test
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'ffprobe version n-test\n'
            """);
        var whisperCliPath = WriteExecutableScript(
            outputDirectory,
            "fake-whisper-cli",
            """
            @echo off
            echo usage: whisper-cli
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'usage: whisper-cli\n'
            """);
        var demucsPath = WriteExecutableScript(
            outputDirectory,
            "fake-demucs",
            """
            @echo off
            echo usage: demucs
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'usage: demucs\n'
            """);
        var modelPath = Path.Combine(outputDirectory, "model.gguf");
        await File.WriteAllTextAsync(modelPath, "model");

        try
        {
            var result = await CliTestProcessHelper.RunCliAsync(
                new Dictionary<string, string?>
                {
                    ["OVT_WHISPER_CLI_PATH"] = whisperCliPath,
                    ["OVT_DEMUCS_PATH"] = demucsPath,
                    ["OVT_WHISPER_MODEL_PATH"] = modelPath
                },
                "doctor",
                "--ffmpeg",
                ffmpegPath,
                "--ffprobe",
                ffprobePath);

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            var payload = envelope["payload"]!.AsObject();
            Assert.True(payload["isHealthy"]!.GetValue<bool>());

            var dependencies = payload["dependencies"]!.AsArray();
            Assert.Contains(
                dependencies,
                node => node!["id"]!.GetValue<string>() == "whisper-cli"
                    && node["source"]!.GetValue<string>() == "environment"
                    && node["isAvailable"]!.GetValue<bool>());
            Assert.Contains(
                dependencies,
                node => node!["id"]!.GetValue<string>() == "demucs"
                    && node["source"]!.GetValue<string>() == "environment"
                    && node["isAvailable"]!.GetValue<bool>());
            Assert.Contains(
                dependencies,
                node => node!["id"]!.GetValue<string>() == "whisper-model"
                    && node["source"]!.GetValue<string>() == "environment"
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
    public async Task Doctor_OptionValuesOverrideEnvironmentFallback()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-doctor-option-precedence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg",
            """
            @echo off
            echo ffmpeg version n-test
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'ffmpeg version n-test\n'
            """);
        var ffprobePath = WriteExecutableScript(
            outputDirectory,
            "fake-ffprobe",
            """
            @echo off
            echo ffprobe version n-test
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'ffprobe version n-test\n'
            """);
        var whisperCliPath = WriteExecutableScript(
            outputDirectory,
            "fake-whisper-cli",
            """
            @echo off
            echo usage: whisper-cli
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'usage: whisper-cli\n'
            """);
        var demucsPath = WriteExecutableScript(
            outputDirectory,
            "fake-demucs",
            """
            @echo off
            echo usage: demucs
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'usage: demucs\n'
            """);
        var modelPath = Path.Combine(outputDirectory, "model.gguf");
        await File.WriteAllTextAsync(modelPath, "model");

        try
        {
            var result = await CliTestProcessHelper.RunCliAsync(
                new Dictionary<string, string?>
                {
                    ["OVT_WHISPER_CLI_PATH"] = whisperCliPath,
                    ["OVT_DEMUCS_PATH"] = demucsPath,
                    ["OVT_WHISPER_MODEL_PATH"] = modelPath
                },
                "doctor",
                "--ffmpeg",
                ffmpegPath,
                "--ffprobe",
                ffprobePath,
                "--whisper-cli",
                "missing-whisper",
                "--demucs",
                "missing-demucs",
                "--whisper-model",
                "missing-model.gguf");

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            var payload = envelope["payload"]!.AsObject();
            Assert.True(payload["isHealthy"]!.GetValue<bool>());

            var dependencies = payload["dependencies"]!.AsArray();
            Assert.Contains(
                dependencies,
                node => node!["id"]!.GetValue<string>() == "whisper-cli"
                    && node["source"]!.GetValue<string>() == "option"
                    && node["resolvedValue"]!.GetValue<string>() == "missing-whisper"
                    && node["detail"]!.GetValue<string>().Contains("missing-whisper", StringComparison.Ordinal)
                    && !node["isAvailable"]!.GetValue<bool>());
            Assert.Contains(
                dependencies,
                node => node!["id"]!.GetValue<string>() == "demucs"
                    && node["source"]!.GetValue<string>() == "option"
                    && node["resolvedValue"]!.GetValue<string>() == "missing-demucs"
                    && node["detail"]!.GetValue<string>().Contains("missing-demucs", StringComparison.Ordinal)
                    && !node["isAvailable"]!.GetValue<bool>());
            Assert.Contains(
                dependencies,
                node => node!["id"]!.GetValue<string>() == "whisper-model"
                    && node["source"]!.GetValue<string>() == "option"
                    && node["resolvedValue"]!.GetValue<string>().EndsWith("missing-model.gguf", StringComparison.OrdinalIgnoreCase)
                    && node["detail"]!.GetValue<string>().Contains("missing-model.gguf", StringComparison.OrdinalIgnoreCase)
                    && !node["isAvailable"]!.GetValue<bool>());
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
    public async Task Doctor_UsesDefaultAndUnsetSourcesWhenOptionalDependenciesAreNotConfigured()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-doctor-default-unset-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg",
            """
            @echo off
            echo ffmpeg version n-test
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'ffmpeg version n-test\n'
            """);
        var ffprobePath = WriteExecutableScript(
            outputDirectory,
            "fake-ffprobe",
            """
            @echo off
            echo ffprobe version n-test
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'ffprobe version n-test\n'
            """);

        try
        {
            var result = await CliTestProcessHelper.RunCliAsync(
                new Dictionary<string, string?>
                {
                    ["OVT_WHISPER_CLI_PATH"] = null,
                    ["OVT_DEMUCS_PATH"] = null,
                    ["OVT_WHISPER_MODEL_PATH"] = null
                },
                "doctor",
                "--ffmpeg",
                ffmpegPath,
                "--ffprobe",
                ffprobePath);

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            var payload = envelope["payload"]!.AsObject();

            var dependencies = payload["dependencies"]!.AsArray();
            Assert.Contains(
                dependencies,
                node => node!["id"]!.GetValue<string>() == "whisper-cli"
                    && node["source"]!.GetValue<string>() == "default"
                    && node["resolvedValue"]!.GetValue<string>() == "whisper-cli");
            Assert.Contains(
                dependencies,
                node => node!["id"]!.GetValue<string>() == "demucs"
                    && node["source"]!.GetValue<string>() == "default"
                    && node["resolvedValue"]!.GetValue<string>() == "demucs");
            Assert.Contains(
                dependencies,
                node => node!["id"]!.GetValue<string>() == "whisper-model"
                    && node["source"]!.GetValue<string>() == "unset"
                    && node["resolvedValue"] is null
                    && node["detail"]!.GetValue<string>() == "Dependency path is not configured.");
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
        return await CliTestProcessHelper.RunCliAsync(args);
    }

    private static async Task<string> CreateTemplatePluginAsync(
        string workingDirectory,
        string pluginId,
        string pluginDisplayName,
        EditPlanTemplateDefinition template)
    {
        var pluginDirectory = Path.Combine(workingDirectory, pluginId);
        var templateDirectory = Path.Combine(pluginDirectory, "templates", template.Id);
        Directory.CreateDirectory(templateDirectory);

        await File.WriteAllTextAsync(
            Path.Combine(pluginDirectory, "plugin.json"),
            System.Text.Json.JsonSerializer.Serialize(
                new
                {
                    schemaVersion = 1,
                    id = pluginId,
                    displayName = pluginDisplayName,
                    version = "1.0.0",
                    description = $"{pluginDisplayName} plugin",
                    templates = new[]
                    {
                        new
                        {
                            id = template.Id,
                            path = Path.Combine("templates", template.Id).Replace('\\', '/')
                        }
                    }
                },
                OpenVideoToolboxJson.Default));

        await File.WriteAllTextAsync(
            Path.Combine(templateDirectory, "template.json"),
            System.Text.Json.JsonSerializer.Serialize(template, OpenVideoToolboxJson.Default));

        return pluginDirectory;
    }

    private static EditPlanTemplateDefinition CreatePluginTemplateDefinition(string id, string displayName)
    {
        return new EditPlanTemplateDefinition
        {
            Id = id,
            DisplayName = displayName,
            Description = $"{displayName} description",
            Category = "plugin",
            OutputContainer = "mp4",
            DefaultSubtitleMode = SubtitleMode.Sidecar,
            RecommendedSeedModes = [EditPlanSeedMode.Manual, EditPlanSeedMode.Transcript],
            RecommendedTranscriptSeedStrategies = [TranscriptSeedStrategy.Grouped],
            ArtifactSlots =
            [
                new EditPlanArtifactSlot
                {
                    Id = "subtitles",
                    Kind = "subtitle",
                    Description = "Subtitle sidecar",
                    Required = false
                }
            ],
            SupportingSignals =
            [
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Transcript,
                    Reason = "Use transcript guidance before generating subtitles."
                }
            ]
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

    private static string WriteExecutableScript(string directory, string baseName, string windowsContent, string unixContent)
    {
        return CliTestProcessHelper.WriteExecutableScript(directory, baseName, windowsContent, unixContent);
    }

    private static void WriteMonoPcmWave(string path, int sampleRateHz, int sampleCount)
    {
        const short bitsPerSample = 16;
        const short channels = 1;
        var blockAlign = (short)(channels * bitsPerSample / 8);
        var byteRate = sampleRateHz * blockAlign;
        var dataSize = sampleCount * blockAlign;

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRateHz);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);

        for (var i = 0; i < sampleCount; i++)
        {
            var sample = (short)((i % 200) < 100 ? short.MaxValue / 8 : short.MinValue / 8);
            writer.Write(sample);
        }
    }
}
