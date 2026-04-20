using System.Text.Json;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Serialization;
using static OpenVideoToolbox.Cli.CliCommandOutput;
using static OpenVideoToolbox.Cli.CliOptionParsing;

namespace OpenVideoToolbox.Cli;

internal static class TemplateCommandHandlers
{
    public static async Task<int> RunInitPlanAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--template", out var templateId, out error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--output", out var planOutputPath, out error))
        {
            return fail(error!);
        }

        var fullPlanOutputPath = Path.GetFullPath(planOutputPath!);

        try
        {
            var build = await TemplatePlanBuildSupport.BuildEditPlanFromTemplateAsync(inputPath!, templateId!, fullPlanOutputPath, options);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPlanOutputPath)!);
            await File.WriteAllTextAsync(fullPlanOutputPath, JsonSerializer.Serialize(build.Plan, OpenVideoToolboxJson.Default));

            WriteJson(new
            {
                source = build.TemplateSource,
                template = build.Template,
                planPath = fullPlanOutputPath,
                probed = build.Probe is not null,
                editPlan = build.Plan
            });

            return 0;
        }
        catch (Exception ex)
        {
            return fail(ex.Message);
        }
    }

    public static async Task<int> RunScaffoldTemplateAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--template", out var templateId, out error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--dir", out var outputDirectory, out error))
        {
            return fail(error!);
        }

        var fullOutputDirectory = Path.GetFullPath(outputDirectory!);
        var fullPlanOutputPath = Path.Combine(fullOutputDirectory, "edit.json");
        if (!TryGetBoolOption(options, "--validate", out var validateAfterWrite, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--check-files", out var checkFiles, out error))
        {
            return fail(error!);
        }

        var pluginCatalog = TemplatePluginCatalogLoader.Load(GetOption(options, "--plugin-dir"));

        try
        {
            var build = await TemplatePlanBuildSupport.BuildEditPlanFromTemplateAsync(inputPath!, templateId!, fullPlanOutputPath, options);
            var template = build.Template;
            var artifactsExample = EditPlanTemplateExampleBuilder.BuildArtifactBindingsExample(template);
            var templateParamsExample = EditPlanTemplateExampleBuilder.BuildTemplateParamsExample(template);
            var previewPlans = EditPlanTemplateExampleBuilder.BuildPreviewPlans(
                template,
                TemplateCommandPresentation.BuildPersistedTemplateSource(template, pluginCatalog));
            var supportingSignals = EditPlanTemplateExampleBuilder.BuildSupportingSignalExamples(template);
            var requiresPluginDir = TemplateCommandPresentation.FindPluginTemplate(template, pluginCatalog) is not null;
            var commands = TemplateCommandPresentation.BuildTemplateExampleCommands(template, artifactsExample.Count > 0, templateParamsExample.Count > 0, requiresPluginDir);
            var seedCommands = TemplateCommandPresentation.BuildTemplateSeedCommands(template, requiresPluginDir);
            var signalCommands = supportingSignals.Select(signal => signal.Command).ToArray();
            var signalInstructions = BuildSignalInstructions(supportingSignals);
            var artifactCommands = TemplateCommandPresentation.BuildTemplateArtifactCommands(template, supportingSignals);
            var exampleWriteResult = TemplateCommandPresentation.WriteTemplateExamples(
                template,
                build.TemplateSource,
                requiresPluginDir,
                fullOutputDirectory,
                artifactsExample,
                templateParamsExample,
                previewPlans,
                supportingSignals,
                commands,
                seedCommands,
                signalInstructions,
                signalCommands,
                artifactCommands);

            await File.WriteAllTextAsync(fullPlanOutputPath, JsonSerializer.Serialize(build.Plan, OpenVideoToolboxJson.Default));

            var canonicalFiles = new List<string>(exampleWriteResult.WrittenFiles)
            {
                fullPlanOutputPath
            };

            if (build.ArtifactBindings.Count > 0)
            {
                var artifactsPath = Path.Combine(fullOutputDirectory, "artifacts.json");
                await File.WriteAllTextAsync(artifactsPath, JsonSerializer.Serialize(build.ArtifactBindings, OpenVideoToolboxJson.Default));
                canonicalFiles.Add(artifactsPath);
            }

            if (build.ParameterOverrides.Count > 0)
            {
                var templateParamsPath = Path.Combine(fullOutputDirectory, "template-params.json");
                await File.WriteAllTextAsync(templateParamsPath, JsonSerializer.Serialize(build.ParameterOverrides, OpenVideoToolboxJson.Default));
                canonicalFiles.Add(templateParamsPath);
            }

            EditPlanValidationResult? validation = null;
            if (validateAfterWrite == true)
            {
                validation = await TemplatePlanValidationSupport.ValidatePlanFileAsync(fullPlanOutputPath, checkFiles == true, pluginCatalog);
            }

            WriteJson(new
            {
                source = build.TemplateSource,
                template = build.Template,
                scaffold = new
                {
                    outputDirectory = fullOutputDirectory,
                    planPath = fullPlanOutputPath,
                    writtenFiles = canonicalFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                },
                probed = build.Probe is not null,
                validated = validateAfterWrite == true,
                validation = validation is null
                    ? null
                    : new
                    {
                        checkFiles = checkFiles == true,
                        isValid = validation.IsValid,
                        issues = validation.Issues
                    },
                editPlan = build.Plan
            });

            return validation?.IsValid == false ? 1 : 0;
        }
        catch (Exception ex)
        {
            return fail(ex.Message);
        }
    }

    public static int RunTemplates(string[] args, Func<string, int> fail)
    {
        string? templateId = null;
        IReadOnlyDictionary<string, string> options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal))
        {
            templateId = args[0];
            if (!TryParseOptions(args.Skip(1).ToArray(), out var parsedOptions, out var error))
            {
                return fail(error!);
            }

            options = parsedOptions;
        }
        else
        {
            if (!TryParseOptions(args, out var parsedOptions, out var error))
            {
                return fail(error!);
            }

            options = parsedOptions;
            templateId = GetOption(options, "--template");
        }

        try
        {
            if (!TryParseSeedModeOption(GetOption(options, "--seed-mode"), out var seedMode, out var error))
            {
                return fail(error!);
            }

            if (!TryGetBoolOption(options, "--has-artifacts", out var hasArtifacts, out error))
            {
                return fail(error!);
            }

            if (!TryGetBoolOption(options, "--has-subtitles", out var hasSubtitles, out error))
            {
                return fail(error!);
            }

            if (!TryGetBoolOption(options, "--summary", out var summaryOnly, out error))
            {
                return fail(error!);
            }

            var query = new EditPlanTemplateCatalogQuery
            {
                Category = GetOption(options, "--category"),
                SeedMode = seedMode,
                OutputContainer = GetOption(options, "--output-container"),
                ArtifactKind = GetOption(options, "--artifact-kind"),
                HasArtifacts = hasArtifacts,
                HasSubtitles = hasSubtitles
            };
            var jsonOutPath = GetOption(options, "--json-out");
            var pluginCatalog = TemplatePluginCatalogLoader.Load(GetOption(options, "--plugin-dir"));
            var availableTemplates = TemplateCommandPresentation.BuildAvailableTemplates(pluginCatalog);

            if (string.IsNullOrWhiteSpace(templateId))
            {
                object templates = summaryOnly == true
                    ? EditPlanTemplateCatalog.GetSummaries(availableTemplates, query)
                    : EditPlanTemplateCatalog.Filter(availableTemplates, query);

                return WriteResult(new
                {
                    filters = query,
                    summary = summaryOnly == true,
                    plugins = pluginCatalog.Plugins,
                    templates
                }, jsonOutPath);
            }

            var template = EditPlanTemplateCatalog.GetRequired(availableTemplates, templateId!);
            var templateSource = TemplateCommandPresentation.BuildTemplateSource(template, pluginCatalog);
            var artifactsExample = EditPlanTemplateExampleBuilder.BuildArtifactBindingsExample(template);
            var templateParamsExample = EditPlanTemplateExampleBuilder.BuildTemplateParamsExample(template);
            var previewPlans = EditPlanTemplateExampleBuilder.BuildPreviewPlans(
                template,
                TemplateCommandPresentation.BuildPersistedTemplateSource(template, pluginCatalog));
            var supportingSignals = EditPlanTemplateExampleBuilder.BuildSupportingSignalExamples(template);
            var writeExamplesDirectory = GetOption(options, "--write-examples");
            TemplateExampleWriteResult? writeResult = null;
            var requiresPluginDir = TemplateCommandPresentation.FindPluginTemplate(template, pluginCatalog) is not null;
            var commands = TemplateCommandPresentation.BuildTemplateExampleCommands(template, artifactsExample.Count > 0, templateParamsExample.Count > 0, requiresPluginDir);
            var seedCommands = TemplateCommandPresentation.BuildTemplateSeedCommands(template, requiresPluginDir);
            var signalCommands = supportingSignals.Select(signal => signal.Command).ToArray();
            var signalInstructions = BuildSignalInstructions(supportingSignals);
            var artifactCommands = TemplateCommandPresentation.BuildTemplateArtifactCommands(template, supportingSignals);

            if (!string.IsNullOrWhiteSpace(writeExamplesDirectory))
            {
                writeResult = TemplateCommandPresentation.WriteTemplateExamples(
                    template,
                    templateSource,
                    requiresPluginDir,
                    writeExamplesDirectory,
                    artifactsExample,
                    templateParamsExample,
                    previewPlans,
                    supportingSignals,
                    commands,
                    seedCommands,
                    signalInstructions,
                    signalCommands,
                    artifactCommands);
            }

            return WriteResult(TemplateCommandPresentation.BuildTemplateGuide(
                template,
                templateSource,
                writeResult,
                artifactsExample,
                templateParamsExample,
                previewPlans,
                supportingSignals,
                commands,
                seedCommands,
                signalInstructions,
                signalCommands,
                artifactCommands), jsonOutPath);
        }
        catch (Exception ex)
        {
            return fail(ex.Message);
        }
    }

    private static IReadOnlyList<TemplateSignalInstruction> BuildSignalInstructions(
        IReadOnlyList<EditPlanSupportingSignalExample> supportingSignals)
    {
        return supportingSignals
            .Select(signal => new TemplateSignalInstruction
            {
                Kind = signal.Kind.ToString().ToLowerInvariant(),
                Command = signal.Command,
                Consumption = signal.Consumption
            })
            .ToArray();
    }
}
