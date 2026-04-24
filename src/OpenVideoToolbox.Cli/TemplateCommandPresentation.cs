using System.Text;
using System.Text.Json;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Serialization;

namespace OpenVideoToolbox.Cli;

internal static class TemplateCommandPresentation
{
    public static IReadOnlyList<EditPlanTemplateDefinition> BuildAvailableTemplates(TemplatePluginCatalog pluginCatalog)
    {
        var templates = BuiltInEditPlanTemplateCatalog.GetAll()
            .Concat(pluginCatalog.LoadedTemplates.Select(item => item.Template))
            .ToArray();

        EnsureUniqueTemplateIds(templates);
        return templates;
    }

    public static object BuildTemplateSource(EditPlanTemplateDefinition template, TemplatePluginCatalog pluginCatalog)
    {
        var pluginTemplate = FindPluginTemplate(template, pluginCatalog);

        if (pluginTemplate is null)
        {
            return new
            {
                kind = "builtIn"
            };
        }

        return new
        {
            kind = "plugin",
            pluginId = pluginTemplate.Plugin.Id,
            pluginVersion = pluginTemplate.Plugin.Version,
            pluginDisplayName = pluginTemplate.Plugin.DisplayName,
            pluginDirectory = pluginTemplate.Plugin.Directory
        };
    }

    public static EditTemplateSourceReference BuildPersistedTemplateSource(
        EditPlanTemplateDefinition template,
        TemplatePluginCatalog pluginCatalog)
    {
        var pluginTemplate = FindPluginTemplate(template, pluginCatalog);
        if (pluginTemplate is null)
        {
            return new EditTemplateSourceReference
            {
                Kind = EditTemplateSourceKinds.BuiltIn
            };
        }

        return new EditTemplateSourceReference
        {
            Kind = EditTemplateSourceKinds.Plugin,
            PluginId = pluginTemplate.Plugin.Id,
            PluginVersion = pluginTemplate.Plugin.Version
        };
    }

    public static TemplatePluginLoadedTemplate? FindPluginTemplate(
        EditPlanTemplateDefinition template,
        TemplatePluginCatalog pluginCatalog)
    {
        return pluginCatalog.LoadedTemplates.FirstOrDefault(item =>
            string.Equals(item.Template.Id, template.Id, StringComparison.OrdinalIgnoreCase));
    }

    public static TemplateExampleWriteResult WriteTemplateExamples(
        EditPlanTemplateDefinition template,
        object source,
        bool requiresPluginDir,
        string outputDirectory,
        IReadOnlyDictionary<string, string> artifactsExample,
        IReadOnlyDictionary<string, string> templateParamsExample,
        IReadOnlyList<EditPlanTemplatePreview> previewPlans,
        IReadOnlyList<EditPlanSupportingSignalExample> supportingSignals,
        IReadOnlyList<string> commands,
        IReadOnlyList<TemplateSeedCommand> seedCommands,
        IReadOnlyList<TemplateSignalInstruction> signalInstructions,
        IReadOnlyList<string> signalCommands,
        IReadOnlyList<string> artifactCommands)
    {
        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);
        var writtenFiles = new List<string>();

        var artifactsPath = Path.Combine(fullOutputDirectory, "artifacts.json");
        File.WriteAllText(
            artifactsPath,
            JsonSerializer.Serialize(artifactsExample, OpenVideoToolboxJson.Default),
            Encoding.UTF8);
        writtenFiles.Add(artifactsPath);

        var templateParamsPath = Path.Combine(fullOutputDirectory, "template-params.json");
        File.WriteAllText(
            templateParamsPath,
            JsonSerializer.Serialize(templateParamsExample, OpenVideoToolboxJson.Default),
            Encoding.UTF8);
        writtenFiles.Add(templateParamsPath);

        foreach (var preview in previewPlans)
        {
            var fileName = $"preview-{preview.Mode.ToString().ToLowerInvariant()}.edit.json";
            var previewPath = Path.Combine(fullOutputDirectory, fileName);
            File.WriteAllText(
                previewPath,
                JsonSerializer.Serialize(preview.EditPlan, OpenVideoToolboxJson.Default),
                Encoding.UTF8);
            writtenFiles.Add(previewPath);
        }

        var templatePath = Path.Combine(fullOutputDirectory, "template.json");
        File.WriteAllText(
            templatePath,
            JsonSerializer.Serialize(template, OpenVideoToolboxJson.Default),
            Encoding.UTF8);
        writtenFiles.Add(templatePath);

        var guide = BuildTemplateGuide(
            template,
            source,
            new TemplateExampleWriteResult
            {
                OutputDirectory = fullOutputDirectory,
                WrittenFiles = writtenFiles
            },
            artifactsExample,
            templateParamsExample,
            previewPlans,
            supportingSignals,
            commands,
            seedCommands,
            signalInstructions,
            signalCommands,
            artifactCommands);
        var guidePath = Path.Combine(fullOutputDirectory, "guide.json");
        File.WriteAllText(
            guidePath,
            JsonSerializer.Serialize(guide, OpenVideoToolboxJson.Default),
            Encoding.UTF8);
        writtenFiles.Add(guidePath);

        var commandBundle = TemplateCommandArtifactsBuilder.BuildCommandBundle(commands, seedCommands, signalInstructions, artifactCommands, requiresPluginDir);

        var commandsJsonPath = Path.Combine(fullOutputDirectory, "commands.json");
        File.WriteAllText(
            commandsJsonPath,
            JsonSerializer.Serialize(commandBundle, OpenVideoToolboxJson.Default),
            Encoding.UTF8);
        writtenFiles.Add(commandsJsonPath);

        var commandsPs1Path = Path.Combine(fullOutputDirectory, "commands.ps1");
        File.WriteAllText(
            commandsPs1Path,
            TemplateCommandArtifactsBuilder.BuildPowerShellCommandScript(commandBundle),
            Encoding.UTF8);
        writtenFiles.Add(commandsPs1Path);

        var commandsCmdPath = Path.Combine(fullOutputDirectory, "commands.cmd");
        File.WriteAllText(
            commandsCmdPath,
            TemplateCommandArtifactsBuilder.BuildBatchCommandScript(commandBundle),
            Encoding.UTF8);
        writtenFiles.Add(commandsCmdPath);

        var commandsShPath = Path.Combine(fullOutputDirectory, "commands.sh");
        File.WriteAllText(
            commandsShPath,
            TemplateCommandArtifactsBuilder.BuildShellCommandScript(commandBundle),
            Encoding.UTF8);
        writtenFiles.Add(commandsShPath);

        return new TemplateExampleWriteResult
        {
            OutputDirectory = fullOutputDirectory,
            WrittenFiles = writtenFiles
        };
    }

    public static object BuildTemplateGuide(
        EditPlanTemplateDefinition template,
        object source,
        TemplateExampleWriteResult? writeResult,
        IReadOnlyDictionary<string, string> artifactsExample,
        IReadOnlyDictionary<string, string> templateParamsExample,
        IReadOnlyList<EditPlanTemplatePreview> previewPlans,
        IReadOnlyList<EditPlanSupportingSignalExample> supportingSignals,
        IReadOnlyList<string> commands,
        IReadOnlyList<TemplateSeedCommand> seedCommands,
        IReadOnlyList<TemplateSignalInstruction> signalInstructions,
        IReadOnlyList<string> signalCommands,
        IReadOnlyList<string> artifactCommands)
    {
        return new
        {
            source,
            template,
            examples = new
            {
                outputDirectory = writeResult?.OutputDirectory,
                writtenFiles = writeResult?.WrittenFiles,
                artifactsFileName = "artifacts.json",
                artifacts = artifactsExample,
                templateParamsFileName = "template-params.json",
                templateParams = templateParamsExample,
                seedModes = template.RecommendedSeedModes,
                supportingSignals,
                commandFiles = new[]
                {
                    "commands.json",
                    "commands.ps1",
                    "commands.cmd",
                    "commands.sh"
                },
                commands,
                signalInstructions,
                signalCommands,
                artifactCommands,
                seedCommands,
                previewPlans
            }
        };
    }

    public static IReadOnlyList<string> BuildTemplateArtifactCommands(
        EditPlanTemplateDefinition template,
        IReadOnlyList<EditPlanSupportingSignalExample> supportingSignals,
        bool requiresPluginDir)
    {
        var hasSubtitleOutput = template.DefaultSubtitleMode is not null
            || template.ArtifactSlots.Any(slot =>
                string.Equals(slot.Kind, "subtitle", StringComparison.OrdinalIgnoreCase)
                || string.Equals(slot.Id, "subtitles", StringComparison.OrdinalIgnoreCase));
        var hasTranscriptSignal = supportingSignals.Any(signal => signal.Kind == EditPlanSupportingSignalKind.Transcript);
        var pluginArg = requiresPluginDir ? " --plugin-dir <plugin-dir>" : string.Empty;

        if (!hasSubtitleOutput || !hasTranscriptSignal)
        {
            return [];
        }

        return
        [
            "ovt subtitle <input> --transcript transcript.json --format srt --output subtitles.srt",
            $"ovt attach-plan-material --plan edit.json --transcript --path transcript.json --check-files{pluginArg}",
            $"ovt attach-plan-material --plan edit.json --subtitles --path subtitles.srt --check-files{pluginArg}"
        ];
    }

    public static IReadOnlyList<string> BuildTemplateExampleCommands(
        EditPlanTemplateDefinition template,
        bool hasArtifacts,
        bool hasTemplateParams,
        bool requiresPluginDir)
    {
        var pluginArg = requiresPluginDir ? " --plugin-dir <plugin-dir>" : string.Empty;
        var commands = new List<string>
        {
            $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer}{pluginArg}"
        };

        if (hasArtifacts)
        {
            commands.Add(
                $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer}{pluginArg} --artifacts artifacts.json");
        }

        if (hasTemplateParams)
        {
            commands.Add(
                $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer}{pluginArg} --template-params template-params.json");
        }

        return commands;
    }

    public static IReadOnlyList<TemplateSeedCommand> BuildTemplateSeedCommands(EditPlanTemplateDefinition template, bool requiresPluginDir)
    {
        var pluginArg = requiresPluginDir ? " --plugin-dir <plugin-dir>" : string.Empty;
        var commands = new List<TemplateSeedCommand>();
        foreach (var mode in template.RecommendedSeedModes.Distinct())
        {
            commands.Add(new TemplateSeedCommand
            {
                Mode = mode.ToString().ToLowerInvariant(),
                Command = mode switch
                {
                    EditPlanSeedMode.Manual =>
                        $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer}{pluginArg}",
                    EditPlanSeedMode.Transcript =>
                        $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer}{pluginArg} --transcript transcript.json --seed-from-transcript",
                    EditPlanSeedMode.Beats =>
                        $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer}{pluginArg} --beats beats.json --seed-from-beats --beat-group-size 4",
                    _ => throw new InvalidOperationException($"Unsupported template seed mode '{mode}'.")
                },
                Variants = mode == EditPlanSeedMode.Transcript
                    ? BuildTranscriptSeedCommandVariants(template, requiresPluginDir)
                    : null
            });
        }

        return commands;
    }

    private static void EnsureUniqueTemplateIds(IReadOnlyList<EditPlanTemplateDefinition> templates)
    {
        var duplicate = templates
            .GroupBy(template => template.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Duplicate edit plan template id '{duplicate.Key}'.");
        }
    }

    private static IReadOnlyList<TemplateSeedVariant> BuildTranscriptSeedCommandVariants(EditPlanTemplateDefinition template, bool requiresPluginDir)
    {
        var pluginArg = requiresPluginDir ? " --plugin-dir <plugin-dir>" : string.Empty;
        var variants = new[]
        {
            new
            {
                strategy = TranscriptSeedStrategy.Grouped,
                key = "grouped",
                command = $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer}{pluginArg} --transcript transcript.json --seed-from-transcript --transcript-segment-group-size 2"
            },
            new
            {
                strategy = TranscriptSeedStrategy.MinDuration,
                key = "min-duration",
                command = $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer}{pluginArg} --transcript transcript.json --seed-from-transcript --min-transcript-segment-duration-ms 500"
            },
            new
            {
                strategy = TranscriptSeedStrategy.MaxGap,
                key = "max-gap",
                command = $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer}{pluginArg} --transcript transcript.json --seed-from-transcript --transcript-segment-group-size 3 --max-transcript-gap-ms 200"
            }
        };

        var rankedStrategies = template.RecommendedTranscriptSeedStrategies
            .Select((strategy, index) => new { strategy, index })
            .ToDictionary(item => item.strategy, item => item.index);

        return variants
            .OrderBy(variant => rankedStrategies.TryGetValue(variant.strategy, out var rank) ? rank : int.MaxValue)
            .ThenBy(variant => variant.key, StringComparer.Ordinal)
            .Select(variant => new TemplateSeedVariant
            {
                Key = variant.key,
                Command = variant.command,
                Recommended = rankedStrategies.ContainsKey(variant.strategy),
                Strategy = variant.strategy.ToString().ToLowerInvariant()
            })
            .ToArray();
    }
}
