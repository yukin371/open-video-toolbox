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
            var result = await ExecuteScaffoldTemplateAsync(
                inputPath!,
                templateId!,
                fullOutputDirectory,
                validateAfterWrite == true,
                checkFiles == true,
                options,
                pluginCatalog);

            WriteJson(result.Payload);
            return result.ExitCode;
        }
        catch (Exception ex)
        {
            return fail(ex.Message);
        }
    }

    public static async Task<int> RunScaffoldTemplateBatchAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--manifest", out var manifestPath, out error))
        {
            return fail(error!);
        }

        var jsonOutPath = GetOption(options, "--json-out");
        var pluginDir = GetOption(options, "--plugin-dir");
        var pluginCatalog = TemplatePluginCatalogLoader.Load(pluginDir);
        var fullManifestPath = Path.GetFullPath(manifestPath!);
        var manifestBaseDirectory = Path.GetDirectoryName(fullManifestPath)!;
        var summaryPath = Path.Combine(manifestBaseDirectory, "summary.json");

        try
        {
            var manifest = JsonSerializer.Deserialize<ScaffoldTemplateBatchManifest>(
                await File.ReadAllTextAsync(fullManifestPath),
                OpenVideoToolboxJson.Default)
                ?? throw new InvalidOperationException($"Failed to parse batch manifest '{fullManifestPath}'.");

            if (manifest.SchemaVersion != 1)
            {
                throw new InvalidOperationException(
                    $"Unsupported scaffold-template-batch manifest schema version '{manifest.SchemaVersion}'.");
            }

            if (manifest.Items.Count == 0)
            {
                throw new InvalidOperationException("Batch manifest must contain at least one item.");
            }

            var results = new List<object>();
            var succeededCount = 0;
            var failedCount = 0;

            for (var index = 0; index < manifest.Items.Count; index++)
            {
                var item = manifest.Items[index];
                try
                {
                    if (string.IsNullOrWhiteSpace(item.Id))
                    {
                        throw new InvalidOperationException($"Batch item at index {index} is missing required field 'id'.");
                    }

                    if (string.IsNullOrWhiteSpace(item.Input))
                    {
                        throw new InvalidOperationException($"Batch item '{item.Id}' is missing required field 'input'.");
                    }

                    if (string.IsNullOrWhiteSpace(item.Template))
                    {
                        throw new InvalidOperationException($"Batch item '{item.Id}' is missing required field 'template'.");
                    }

                    var resolvedInputPath = Path.GetFullPath(Path.Combine(manifestBaseDirectory, item.Input));
                    var resolvedWorkDirectory = ResolveBatchWorkDirectory(manifestBaseDirectory, item);
                    var itemOptions = BuildBatchScaffoldOptions(pluginDir);
                    var result = await ExecuteScaffoldTemplateAsync(
                        resolvedInputPath,
                        item.Template,
                        resolvedWorkDirectory,
                        item.Validate == true,
                        item.CheckFiles == true,
                        itemOptions,
                        pluginCatalog);

                    if (result.ExitCode == 0)
                    {
                        succeededCount++;
                        results.Add(new
                        {
                            index,
                            id = item.Id,
                            inputPath = resolvedInputPath,
                            templateId = item.Template,
                            workdir = resolvedWorkDirectory,
                            status = "succeeded",
                            result = result.Payload
                        });
                    }
                    else
                    {
                        failedCount++;
                        results.Add(new
                        {
                            index,
                            id = item.Id,
                            inputPath = resolvedInputPath,
                            templateId = item.Template,
                            workdir = resolvedWorkDirectory,
                            status = "failed",
                            result = result.Payload,
                            error = new
                            {
                                message = result.ErrorMessage ?? $"Scaffolded plan '{Path.Combine(resolvedWorkDirectory, "edit.json")}' failed validation."
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    results.Add(new
                    {
                        index,
                        id = item.Id,
                        templateId = item.Template,
                        status = "failed",
                        error = new
                        {
                            message = ex.Message
                        }
                    });
                }
            }

            var payload = new
            {
                manifestPath = fullManifestPath,
                manifestBaseDirectory,
                summaryPath,
                itemCount = manifest.Items.Count,
                succeededCount,
                failedCount,
                results
            };

            await File.WriteAllTextAsync(summaryPath, JsonSerializer.Serialize(payload, OpenVideoToolboxJson.Default));

            return WriteCommandEnvelope(
                "scaffold-template-batch",
                preview: false,
                payload,
                jsonOutPath,
                exitCode: failedCount == 0 ? 0 : 2);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return WriteCommandEnvelope("scaffold-template-batch", preview: false, new
            {
                manifestPath = fullManifestPath,
                summaryPath,
                error = new
                {
                    message = ex.Message
                }
            }, jsonOutPath, exitCode: 1);
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
            var artifactCommands = TemplateCommandPresentation.BuildTemplateArtifactCommands(template, supportingSignals, requiresPluginDir);

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

    private static async Task<ScaffoldTemplateOperationResult> ExecuteScaffoldTemplateAsync(
        string inputPath,
        string templateId,
        string fullOutputDirectory,
        bool validateAfterWrite,
        bool checkFiles,
        IReadOnlyDictionary<string, string> options,
        TemplatePluginCatalog pluginCatalog)
    {
        var fullPlanOutputPath = Path.Combine(fullOutputDirectory, "edit.json");
        var build = await TemplatePlanBuildSupport.BuildEditPlanFromTemplateAsync(inputPath, templateId, fullPlanOutputPath, options);
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
        var artifactCommands = TemplateCommandPresentation.BuildTemplateArtifactCommands(template, supportingSignals, requiresPluginDir);
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
        if (validateAfterWrite)
        {
            validation = await TemplatePlanValidationSupport.ValidatePlanFileAsync(fullPlanOutputPath, checkFiles, pluginCatalog);
        }

        return new ScaffoldTemplateOperationResult
        {
            Payload = new
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
                validated = validateAfterWrite,
                validation = validation is null
                    ? null
                    : new
                    {
                        checkFiles,
                        isValid = validation.IsValid,
                        issues = validation.Issues
                    },
                editPlan = build.Plan
            },
            ExitCode = validation?.IsValid == false ? 1 : 0,
            ErrorMessage = validation?.IsValid == false
                ? $"Scaffolded plan '{fullPlanOutputPath}' failed validation."
                : null
        };
    }

    private static string ResolveBatchWorkDirectory(string manifestBaseDirectory, ScaffoldTemplateBatchItem item)
    {
        var workdir = string.IsNullOrWhiteSpace(item.Workdir)
            ? Path.Combine("tasks", item.Id)
            : item.Workdir;

        return Path.GetFullPath(Path.Combine(manifestBaseDirectory, workdir));
    }

    private static IReadOnlyDictionary<string, string> BuildBatchScaffoldOptions(string? pluginDir)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(pluginDir))
        {
            options["--plugin-dir"] = pluginDir;
        }

        return options;
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
