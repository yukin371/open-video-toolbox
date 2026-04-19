using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using OpenVideoToolbox.Cli;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Serialization;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
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

}
