using System.Text.Json;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Media;
using OpenVideoToolbox.Core.Serialization;
using static OpenVideoToolbox.Cli.CliCommandOutput;
using static OpenVideoToolbox.Cli.CliOptionParsing;

namespace OpenVideoToolbox.Cli;

internal static class FoundationCommandHandlers
{
    public static int RunValidatePlugin(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--plugin-dir", out var pluginDirectory, out error))
        {
            return fail(error!);
        }

        var jsonOutPath = GetOption(options, "--json-out");
        var fullPluginDirectory = Path.GetFullPath(pluginDirectory!);

        try
        {
            var pluginCatalog = TemplatePluginCatalogLoader.Load(pluginDirectory);
            var availableTemplates = TemplateCommandPresentation.BuildAvailableTemplates(pluginCatalog);
            var pluginTemplates = pluginCatalog.LoadedTemplates
                .Select(item => item.Template)
                .ToArray();

            var report = new
            {
                pluginDirectory = fullPluginDirectory,
                isValid = true,
                issues = Array.Empty<object>(),
                plugins = pluginCatalog.Plugins,
                templates = EditPlanTemplateCatalog.GetSummaries(
                    availableTemplates,
                    new EditPlanTemplateCatalogQuery()).Where(summary =>
                        pluginTemplates.Any(template =>
                            string.Equals(template.Id, summary.Id, StringComparison.OrdinalIgnoreCase)))
                    .ToArray()
            };

            return WriteCommandEnvelope("validate-plugin", preview: false, report, jsonOutPath);
        }
        catch (Exception ex)
        {
            var report = new
            {
                pluginDirectory = fullPluginDirectory,
                isValid = false,
                issues = new[]
                {
                    new
                    {
                        severity = "error",
                        path = "$",
                        code = "plugin.validation.failed",
                        message = ex.Message
                    }
                },
                plugins = Array.Empty<object>(),
                templates = Array.Empty<object>()
            };

            return WriteCommandEnvelope("validate-plugin", preview: false, report, jsonOutPath, exitCode: 1);
        }
    }

    public static async Task<int> RunDoctorAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        var jsonOutPath = GetOption(options, "--json-out");
        TimeSpan? timeout = timeoutSeconds is null
            ? TimeSpan.FromSeconds(5)
            : TimeSpan.FromSeconds(timeoutSeconds.Value);

        try
        {
            var inspector = new ExternalDependencyInspector(new DefaultProcessRunner());
            var report = await inspector.InspectAsync(FoundationCommandSupport.BuildDoctorDependencyDefinitions(options, timeout));

            return WriteCommandEnvelope(
                "doctor",
                preview: false,
                report,
                jsonOutPath,
                exitCode: report.IsHealthy ? 0 : 1);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "doctor",
                preview: false,
                BuildFailedCommandPayload(
                    "doctor",
                    new { },
                    ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    public static async Task<int> RunProbeAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        var jsonOutPath = GetOption(options, "--json-out");
        var ffprobePath = GetOption(options, "--ffprobe") ?? "ffprobe";
        var processRunner = new DefaultProcessRunner();
        var probeService = new FfprobeMediaProbeService(processRunner, new FfprobeJsonParser());

        try
        {
            var result = await probeService.ProbeAsync(inputPath!, ffprobePath);

            return WriteCommandEnvelope(
                "probe",
                preview: false,
                new
                {
                    probe = new
                    {
                        inputPath,
                        ffprobePath
                    },
                    result
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "probe",
                preview: false,
                BuildFailedCommandPayload(
                    "probe",
                    new
                    {
                        inputPath,
                        ffprobePath
                    },
                    ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    public static async Task<int> RunValidatePlanAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--plan", out var planPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--check-files", out var checkFiles, out error))
        {
            return fail(error!);
        }

        var jsonOutPath = GetOption(options, "--json-out");
        var pluginCatalog = TemplatePluginCatalogLoader.Load(GetOption(options, "--plugin-dir"));
        var fullPlanPath = Path.GetFullPath(planPath!);
        var planDirectory = Path.GetDirectoryName(fullPlanPath)!;

        try
        {
            var validation = await TemplatePlanValidationSupport.ValidatePlanFileAsync(fullPlanPath, checkFiles == true, pluginCatalog);
            var report = new
            {
                planPath = fullPlanPath,
                resolvedBaseDirectory = planDirectory,
                checkFiles = checkFiles == true,
                isValid = validation.IsValid,
                issues = validation.Issues
            };

            return WriteCommandEnvelope("validate-plan", preview: false, report, jsonOutPath);
        }
        catch (Exception ex)
        {
            var report = new
            {
                planPath = fullPlanPath,
                resolvedBaseDirectory = planDirectory,
                checkFiles = checkFiles == true,
                isValid = false,
                issues = new[]
                {
                    new EditPlanValidationIssue
                    {
                        Severity = EditPlanValidationSeverity.Error,
                        Path = "$",
                        Code = "plan.parse.failed",
                        Message = ex.Message
                    }
                }
            };

            return WriteCommandEnvelope("validate-plan", preview: false, report, jsonOutPath);
        }
    }

    public static async Task<int> RunInspectPlanAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--plan", out var planPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--check-files", out var checkFiles, out error))
        {
            return fail(error!);
        }

        var jsonOutPath = GetOption(options, "--json-out");
        var pluginCatalog = TemplatePluginCatalogLoader.Load(GetOption(options, "--plugin-dir"));
        var fullPlanPath = Path.GetFullPath(planPath!);
        var planDirectory = Path.GetDirectoryName(fullPlanPath)!;

        try
        {
            var planContext = await TemplatePlanValidationSupport.LoadPlanContextAsync(fullPlanPath, pluginCatalog);
            var inspection = new EditPlanInspector().Inspect(
                planContext.Plan,
                planContext.BaseDirectory,
                checkFiles == true,
                planContext.ValidationTemplates);

            var report = new
            {
                planPath = fullPlanPath,
                resolvedBaseDirectory = planDirectory,
                checkFiles = checkFiles == true,
                template = inspection.Template,
                summary = inspection.Summary,
                materials = inspection.Materials,
                replaceTargets = inspection.ReplaceTargets,
                signals = inspection.Signals,
                missingBindings = inspection.MissingBindings,
                validation = inspection.Validation
            };

            return WriteCommandEnvelope("inspect-plan", preview: false, report, jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "inspect-plan",
                preview: false,
                BuildFailedCommandPayload(
                    "inspectPlan",
                    new
                    {
                        planPath = fullPlanPath,
                        resolvedBaseDirectory = planDirectory,
                        checkFiles = checkFiles == true
                    },
                    ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    public static async Task<int> RunReplacePlanMaterialAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--plan", out var planPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--path", out var replacementPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--check-files", out var checkFiles, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--require-valid", out var requireValid, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--in-place", out var inPlace, out error))
        {
            return fail(error!);
        }

        if (!TryParseEditPlanPathWriteStyleOption(GetOption(options, "--path-style"), out var pathStyle, out error))
        {
            return fail(error!);
        }

        if (!TryResolveReplacementTarget(options, out var targetSelector, out error))
        {
            return fail(error!);
        }

        var subtitleModeRaw = GetOption(options, "--subtitle-mode");
        if (subtitleModeRaw is not null
            && !string.Equals(targetSelector.Singleton, EditPlanInspectionTargetKeys.Subtitles, StringComparison.Ordinal))
        {
            return fail("Option '--subtitle-mode' can only be used with '--subtitles'.");
        }

        SubtitleMode? subtitleMode = null;
        if (subtitleModeRaw is not null)
        {
            if (!TryParseRequiredSubtitleMode(subtitleModeRaw, out var parsedSubtitleMode, out error))
            {
                return fail(error!);
            }

            subtitleMode = parsedSubtitleMode;
        }

        if (GetOption(options, "--audio-track-role") is not null)
        {
            return fail("Option '--audio-track-role' can only be used with 'attach-plan-material --audio-track-id <id>'.");
        }

        var jsonOutPath = GetOption(options, "--json-out");
        var pluginCatalog = TemplatePluginCatalogLoader.Load(GetOption(options, "--plugin-dir"));
        var fullPlanPath = Path.GetFullPath(planPath!);
        var outputPlanPath = ResolveOutputPlanPath(fullPlanPath, inPlace, GetOption(options, "--write-to"), out error);
        if (outputPlanPath is null)
        {
            return fail(error!);
        }

        var outputBaseDirectory = Path.GetDirectoryName(outputPlanPath)!;
        var resolvedReplacementPath = Path.GetFullPath(replacementPath!);

        try
        {
            var result = await ExecuteReplacePlanMaterialAsync(
                new ReplacePlanMaterialOperationRequest
                {
                    FullPlanPath = fullPlanPath,
                    OutputPlanPath = outputPlanPath,
                    ResolvedReplacementPath = resolvedReplacementPath,
                    TargetSelector = targetSelector,
                    PathStyle = pathStyle,
                    CheckFiles = checkFiles == true,
                    RequireValid = requireValid == true,
                    SubtitleMode = subtitleMode
                },
                pluginCatalog);

            if (result.ErrorMessage is not null)
            {
                Console.Error.WriteLine(result.ErrorMessage);
                return WriteCommandEnvelope("replace-plan-material", preview: false, new
                {
                    replacePlanMaterial = result.Report,
                    error = new
                    {
                        message = result.ErrorMessage
                    }
                }, jsonOutPath, exitCode: result.ExitCode);
            }

            return WriteCommandEnvelope("replace-plan-material", preview: false, result.Report!, jsonOutPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return WriteCommandEnvelope("replace-plan-material", preview: false, new
            {
                replacePlanMaterial = new
                {
                    planPath = fullPlanPath,
                    outputPlanPath,
                    checkFiles = checkFiles == true,
                    requireValid = requireValid == true,
                    target = targetSelector,
                    replacementPath = resolvedReplacementPath
                },
                error = new
                {
                    message = ex.Message
                }
            }, jsonOutPath, exitCode: 1);
        }
    }

    public static async Task<int> RunReplacePlanMaterialBatchAsync(string[] args, Func<string, int> fail)
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
        var pluginCatalog = TemplatePluginCatalogLoader.Load(GetOption(options, "--plugin-dir"));
        var fullManifestPath = Path.GetFullPath(manifestPath!);
        var manifestBaseDirectory = Path.GetDirectoryName(fullManifestPath)!;
        var summaryPath = Path.Combine(manifestBaseDirectory, "summary.json");

        try
        {
            var manifest = JsonSerializer.Deserialize<ReplacePlanMaterialBatchManifest>(
                await File.ReadAllTextAsync(fullManifestPath),
                OpenVideoToolboxJson.Default)
                ?? throw new InvalidOperationException($"Failed to parse batch manifest '{fullManifestPath}'.");

            if (manifest.SchemaVersion != 1)
            {
                throw new InvalidOperationException(
                    $"Unsupported replace-plan-material-batch manifest schema version '{manifest.SchemaVersion}'.");
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

                    if (string.IsNullOrWhiteSpace(item.Plan))
                    {
                        throw new InvalidOperationException($"Batch item '{item.Id}' is missing required field 'plan'.");
                    }

                    if (string.IsNullOrWhiteSpace(item.Path))
                    {
                        throw new InvalidOperationException($"Batch item '{item.Id}' is missing required field 'path'.");
                    }

                    if (!TryResolveReplacementTarget(item, out var targetSelector, out error))
                    {
                        throw new InvalidOperationException($"Batch item '{item.Id}': {error}");
                    }

                    if (item.SubtitleMode is not null
                        && !string.Equals(targetSelector.Singleton, EditPlanInspectionTargetKeys.Subtitles, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Batch item '{item.Id}': field 'subtitleMode' can only be used with target 'subtitles'.");
                    }

                    var fullPlanPath = Path.GetFullPath(Path.Combine(manifestBaseDirectory, item.Plan));
                    var resolvedReplacementPath = Path.GetFullPath(Path.Combine(manifestBaseDirectory, item.Path));
                    var outputPlanPath = string.IsNullOrWhiteSpace(item.WriteTo)
                        ? fullPlanPath
                        : Path.GetFullPath(Path.Combine(manifestBaseDirectory, item.WriteTo));
                    var result = await ExecuteReplacePlanMaterialAsync(
                        new ReplacePlanMaterialOperationRequest
                        {
                            FullPlanPath = fullPlanPath,
                            OutputPlanPath = outputPlanPath,
                            ResolvedReplacementPath = resolvedReplacementPath,
                            TargetSelector = targetSelector,
                            PathStyle = item.PathStyle ?? EditPlanPathWriteStyle.Auto,
                            CheckFiles = item.CheckFiles == true,
                            RequireValid = item.RequireValid == true,
                            SubtitleMode = item.SubtitleMode
                        },
                        pluginCatalog);
                    var resultPath = await BatchCommandArtifacts.WriteResultAsync(manifestBaseDirectory, item.Id, result.Report);

                    if (result.ErrorMessage is null)
                    {
                        succeededCount++;
                        results.Add(new
                        {
                            index,
                            id = item.Id,
                            planPath = fullPlanPath,
                            outputPlanPath,
                            resultPath,
                            status = "succeeded",
                            result = result.Report
                        });
                    }
                    else
                    {
                        failedCount++;
                        results.Add(new
                        {
                            index,
                            id = item.Id,
                            planPath = fullPlanPath,
                            outputPlanPath,
                            resultPath,
                            status = "failed",
                            result = result.Report,
                            error = new
                            {
                                message = result.ErrorMessage
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    string? resultPath = null;
                    if (!string.IsNullOrWhiteSpace(item.Id))
                    {
                        resultPath = await BatchCommandArtifacts.WriteResultAsync(manifestBaseDirectory, item.Id, new
                        {
                            index,
                            id = item.Id,
                            status = "failed",
                            error = new
                            {
                                message = ex.Message
                            }
                        });
                    }

                    results.Add(new
                    {
                        index,
                        id = item.Id,
                        resultPath,
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
                "replace-plan-material-batch",
                preview: false,
                payload,
                jsonOutPath,
                exitCode: failedCount == 0 ? 0 : 2);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return WriteCommandEnvelope("replace-plan-material-batch", preview: false, new
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

    public static async Task<int> RunAttachPlanMaterialAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--plan", out var planPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--path", out var attachmentPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--check-files", out var checkFiles, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--require-valid", out var requireValid, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--in-place", out var inPlace, out error))
        {
            return fail(error!);
        }

        if (!TryParseEditPlanPathWriteStyleOption(GetOption(options, "--path-style"), out var pathStyle, out error))
        {
            return fail(error!);
        }

        if (!TryResolveAttachmentTarget(options, out var targetSelector, out error))
        {
            return fail(error!);
        }

        var subtitleModeRaw = GetOption(options, "--subtitle-mode");
        if (subtitleModeRaw is not null
            && !string.Equals(targetSelector.Singleton, EditPlanInspectionTargetKeys.Subtitles, StringComparison.Ordinal))
        {
            return fail("Option '--subtitle-mode' can only be used with '--subtitles'.");
        }

        SubtitleMode? subtitleMode = null;
        if (subtitleModeRaw is not null)
        {
            if (!TryParseRequiredSubtitleMode(subtitleModeRaw, out var parsedSubtitleMode, out error))
            {
                return fail(error!);
            }

            subtitleMode = parsedSubtitleMode;
        }

        if (!TryParseAudioTrackRole(GetOption(options, "--audio-track-role"), out var audioTrackRole, out error))
        {
            return fail(error!);
        }

        if (audioTrackRole is not null && string.IsNullOrWhiteSpace(targetSelector.AudioTrackId))
        {
            return fail("Option '--audio-track-role' can only be used with '--audio-track-id <id>'.");
        }

        var jsonOutPath = GetOption(options, "--json-out");
        var pluginCatalog = TemplatePluginCatalogLoader.Load(GetOption(options, "--plugin-dir"));
        var fullPlanPath = Path.GetFullPath(planPath!);
        var outputPlanPath = ResolveOutputPlanPath(fullPlanPath, inPlace, GetOption(options, "--write-to"), out error);
        if (outputPlanPath is null)
        {
            return fail(error!);
        }

        var outputBaseDirectory = Path.GetDirectoryName(outputPlanPath)!;
        var resolvedAttachmentPath = Path.GetFullPath(attachmentPath!);

        try
        {
            var result = await ExecuteAttachPlanMaterialAsync(
                new AttachPlanMaterialOperationRequest
                {
                    FullPlanPath = fullPlanPath,
                    OutputPlanPath = outputPlanPath,
                    ResolvedAttachmentPath = resolvedAttachmentPath,
                    TargetSelector = targetSelector,
                    PathStyle = pathStyle,
                    CheckFiles = checkFiles == true,
                    RequireValid = requireValid == true,
                    SubtitleMode = subtitleMode,
                    AudioTrackRole = audioTrackRole
                },
                pluginCatalog);

            if (result.ErrorMessage is not null)
            {
                Console.Error.WriteLine(result.ErrorMessage);
                return WriteCommandEnvelope("attach-plan-material", preview: false, new
                {
                    attachPlanMaterial = result.Report,
                    error = new
                    {
                        message = result.ErrorMessage
                    }
                }, jsonOutPath, exitCode: result.ExitCode);
            }

            return WriteCommandEnvelope("attach-plan-material", preview: false, result.Report!, jsonOutPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return WriteCommandEnvelope("attach-plan-material", preview: false, new
            {
                attachPlanMaterial = new
                {
                    planPath = fullPlanPath,
                    outputPlanPath,
                    checkFiles = checkFiles == true,
                    requireValid = requireValid == true,
                    target = targetSelector,
                    attachmentPath = resolvedAttachmentPath
                },
                error = new
                {
                    message = ex.Message
                }
            }, jsonOutPath, exitCode: 1);
        }
    }

    public static async Task<int> RunAttachPlanMaterialBatchAsync(string[] args, Func<string, int> fail)
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
        var pluginCatalog = TemplatePluginCatalogLoader.Load(GetOption(options, "--plugin-dir"));
        var fullManifestPath = Path.GetFullPath(manifestPath!);
        var manifestBaseDirectory = Path.GetDirectoryName(fullManifestPath)!;
        var summaryPath = Path.Combine(manifestBaseDirectory, "summary.json");

        try
        {
            var manifest = JsonSerializer.Deserialize<AttachPlanMaterialBatchManifest>(
                await File.ReadAllTextAsync(fullManifestPath),
                OpenVideoToolboxJson.Default)
                ?? throw new InvalidOperationException($"Failed to parse batch manifest '{fullManifestPath}'.");

            if (manifest.SchemaVersion != 1)
            {
                throw new InvalidOperationException(
                    $"Unsupported attach-plan-material-batch manifest schema version '{manifest.SchemaVersion}'.");
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

                    if (string.IsNullOrWhiteSpace(item.Plan))
                    {
                        throw new InvalidOperationException($"Batch item '{item.Id}' is missing required field 'plan'.");
                    }

                    if (string.IsNullOrWhiteSpace(item.Path))
                    {
                        throw new InvalidOperationException($"Batch item '{item.Id}' is missing required field 'path'.");
                    }

                    if (!TryResolveAttachmentTarget(item, out var targetSelector, out error))
                    {
                        throw new InvalidOperationException($"Batch item '{item.Id}': {error}");
                    }

                    if (item.SubtitleMode is not null
                        && !string.Equals(targetSelector.Singleton, EditPlanInspectionTargetKeys.Subtitles, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Batch item '{item.Id}': field 'subtitleMode' can only be used with target 'subtitles'.");
                    }

                    if (item.AudioTrackRole is not null && string.IsNullOrWhiteSpace(targetSelector.AudioTrackId))
                    {
                        throw new InvalidOperationException(
                            $"Batch item '{item.Id}': field 'audioTrackRole' can only be used with target 'audioTrackId'.");
                    }

                    var fullPlanPath = Path.GetFullPath(Path.Combine(manifestBaseDirectory, item.Plan));
                    var resolvedAttachmentPath = Path.GetFullPath(Path.Combine(manifestBaseDirectory, item.Path));
                    var outputPlanPath = string.IsNullOrWhiteSpace(item.WriteTo)
                        ? fullPlanPath
                        : Path.GetFullPath(Path.Combine(manifestBaseDirectory, item.WriteTo));
                    var result = await ExecuteAttachPlanMaterialAsync(
                        new AttachPlanMaterialOperationRequest
                        {
                            FullPlanPath = fullPlanPath,
                            OutputPlanPath = outputPlanPath,
                            ResolvedAttachmentPath = resolvedAttachmentPath,
                            TargetSelector = targetSelector,
                            PathStyle = item.PathStyle ?? EditPlanPathWriteStyle.Auto,
                            CheckFiles = item.CheckFiles == true,
                            RequireValid = item.RequireValid == true,
                            SubtitleMode = item.SubtitleMode,
                            AudioTrackRole = item.AudioTrackRole
                        },
                        pluginCatalog);
                    var resultPath = await BatchCommandArtifacts.WriteResultAsync(manifestBaseDirectory, item.Id, result.Report);

                    if (result.ErrorMessage is null)
                    {
                        succeededCount++;
                        results.Add(new
                        {
                            index,
                            id = item.Id,
                            planPath = fullPlanPath,
                            outputPlanPath,
                            resultPath,
                            status = "succeeded",
                            result = result.Report
                        });
                    }
                    else
                    {
                        failedCount++;
                        results.Add(new
                        {
                            index,
                            id = item.Id,
                            planPath = fullPlanPath,
                            outputPlanPath,
                            resultPath,
                            status = "failed",
                            result = result.Report,
                            error = new
                            {
                                message = result.ErrorMessage
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    string? resultPath = null;
                    if (!string.IsNullOrWhiteSpace(item.Id))
                    {
                        resultPath = await BatchCommandArtifacts.WriteResultAsync(manifestBaseDirectory, item.Id, new
                        {
                            index,
                            id = item.Id,
                            status = "failed",
                            error = new
                            {
                                message = ex.Message
                            }
                        });
                    }

                    results.Add(new
                    {
                        index,
                        id = item.Id,
                        resultPath,
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
                "attach-plan-material-batch",
                preview: false,
                payload,
                jsonOutPath,
                exitCode: failedCount == 0 ? 0 : 2);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return WriteCommandEnvelope("attach-plan-material-batch", preview: false, new
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

    public static async Task<int> RunBindVoiceTrackAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--plan", out var planPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--path", out var voicePath, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--check-files", out var checkFiles, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--require-valid", out var requireValid, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--in-place", out var inPlace, out error))
        {
            return fail(error!);
        }

        if (!TryParseEditPlanPathWriteStyleOption(GetOption(options, "--path-style"), out var pathStyle, out error))
        {
            return fail(error!);
        }

        if (!TryParseAudioTrackRole(GetOption(options, "--role"), out var requestedRole, out error))
        {
            return fail(error!);
        }

        var jsonOutPath = GetOption(options, "--json-out");
        var pluginCatalog = TemplatePluginCatalogLoader.Load(GetOption(options, "--plugin-dir"));
        var fullPlanPath = Path.GetFullPath(planPath!);
        var outputPlanPath = ResolveOutputPlanPath(fullPlanPath, inPlace, GetOption(options, "--write-to"), out error);
        if (outputPlanPath is null)
        {
            return fail(error!);
        }

        var outputBaseDirectory = Path.GetDirectoryName(outputPlanPath)!;
        var resolvedVoicePath = Path.GetFullPath(voicePath!);
        var trackId = GetOption(options, "--track-id") ?? "voice-main";

        try
        {
            var result = await ExecuteBindVoiceTrackAsync(
                new BindVoiceTrackOperationRequest
                {
                    FullPlanPath = fullPlanPath,
                    OutputPlanPath = outputPlanPath,
                    ResolvedVoicePath = resolvedVoicePath,
                    TrackId = trackId,
                    RequestedRole = requestedRole,
                    PathStyle = pathStyle,
                    CheckFiles = checkFiles == true,
                    RequireValid = requireValid == true
                },
                pluginCatalog);

            if (result.ErrorMessage is not null)
            {
                Console.Error.WriteLine(result.ErrorMessage);
                return WriteCommandEnvelope("bind-voice-track", preview: false, new
                {
                    bindVoiceTrack = result.Report,
                    error = new
                    {
                        message = result.ErrorMessage
                    }
                }, jsonOutPath, exitCode: result.ExitCode);
            }

            return WriteCommandEnvelope("bind-voice-track", preview: false, result.Report!, jsonOutPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return WriteCommandEnvelope("bind-voice-track", preview: false, new
            {
                bindVoiceTrack = new
                {
                    planPath = fullPlanPath,
                    outputPlanPath,
                    checkFiles = checkFiles == true,
                    requireValid = requireValid == true,
                    trackId,
                    replacementPath = resolvedVoicePath
                },
                error = new
                {
                    message = ex.Message
                }
            }, jsonOutPath, exitCode: 1);
        }
    }

    public static async Task<int> RunBindVoiceTrackBatchAsync(string[] args, Func<string, int> fail)
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
        var pluginCatalog = TemplatePluginCatalogLoader.Load(GetOption(options, "--plugin-dir"));
        var fullManifestPath = Path.GetFullPath(manifestPath!);
        var manifestBaseDirectory = Path.GetDirectoryName(fullManifestPath)!;
        var summaryPath = Path.Combine(manifestBaseDirectory, "summary.json");

        try
        {
            var manifest = JsonSerializer.Deserialize<BindVoiceTrackBatchManifest>(
                await File.ReadAllTextAsync(fullManifestPath),
                OpenVideoToolboxJson.Default)
                ?? throw new InvalidOperationException($"Failed to parse batch manifest '{fullManifestPath}'.");

            if (manifest.SchemaVersion != 1)
            {
                throw new InvalidOperationException(
                    $"Unsupported bind-voice-track-batch manifest schema version '{manifest.SchemaVersion}'.");
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
                    if (string.IsNullOrWhiteSpace(item.Plan))
                    {
                        throw new InvalidOperationException($"Batch item at index {index} is missing required field 'plan'.");
                    }

                    if (string.IsNullOrWhiteSpace(item.Path))
                    {
                        throw new InvalidOperationException(
                            $"Batch item at index {index} is missing required field 'path'.");
                    }

                    var itemId = string.IsNullOrWhiteSpace(item.Id)
                        ? $"item-{index + 1:000}"
                        : item.Id;
                    var fullPlanPath = Path.GetFullPath(Path.Combine(manifestBaseDirectory, item.Plan));
                    var resolvedVoicePath = Path.GetFullPath(Path.Combine(manifestBaseDirectory, item.Path));
                    var outputPlanPath = item.WriteTo is null
                        ? fullPlanPath
                        : Path.GetFullPath(Path.Combine(manifestBaseDirectory, item.WriteTo));

                    var result = await ExecuteBindVoiceTrackAsync(
                        new BindVoiceTrackOperationRequest
                        {
                            FullPlanPath = fullPlanPath,
                            OutputPlanPath = outputPlanPath,
                            ResolvedVoicePath = resolvedVoicePath,
                            TrackId = item.TrackId ?? "voice-main",
                            RequestedRole = item.Role,
                            PathStyle = item.PathStyle ?? EditPlanPathWriteStyle.Auto,
                            CheckFiles = item.CheckFiles == true,
                            RequireValid = item.RequireValid == true
                        },
                        pluginCatalog);
                    var resultPath = await BatchCommandArtifacts.WriteResultAsync(manifestBaseDirectory, itemId, result.Report);

                    if (result.ErrorMessage is null)
                    {
                        succeededCount++;
                        results.Add(new
                        {
                            index,
                            id = itemId,
                            planPath = fullPlanPath,
                            outputPlanPath,
                            resultPath,
                            status = "succeeded",
                            result = result.Report
                        });
                    }
                    else
                    {
                        failedCount++;
                        results.Add(new
                        {
                            index,
                            id = itemId,
                            planPath = fullPlanPath,
                            outputPlanPath,
                            resultPath,
                            status = "failed",
                            result = result.Report,
                            error = new
                            {
                                message = result.ErrorMessage
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    var itemId = string.IsNullOrWhiteSpace(item.Id)
                        ? $"item-{index + 1:000}"
                        : item.Id;
                    var resultPath = await BatchCommandArtifacts.WriteResultAsync(manifestBaseDirectory, itemId, new
                    {
                        index,
                        id = itemId,
                        status = "failed",
                        error = new
                        {
                            message = ex.Message
                        }
                    });
                    results.Add(new
                    {
                        index,
                        id = itemId,
                        resultPath,
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
                "bind-voice-track-batch",
                preview: false,
                payload,
                jsonOutPath,
                exitCode: failedCount == 0 ? 0 : 2);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return WriteCommandEnvelope("bind-voice-track-batch", preview: false, new
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

    public static Task<int> RunPlanAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return Task.FromResult(fail(error!));
        }

        var jsonOutPath = GetOption(options, "--json-out");

        try
        {
            var job = FoundationCommandSupport.BuildJob(inputPath!, options, probeSnapshot: null);
            var plan = new FfmpegCommandBuilder().Build(job, GetOption(options, "--ffmpeg") ?? "ffmpeg");

            return Task.FromResult(WriteCommandEnvelope(
                "plan",
                preview: false,
                new
                {
                    plan = new
                    {
                        inputPath
                    },
                    job,
                    commandPlan = plan
                },
                jsonOutPath));
        }
        catch (Exception ex)
        {
            return Task.FromResult(FailWithCommandEnvelope(
                "plan",
                preview: false,
                BuildFailedCommandPayload(
                    "plan",
                    new
                    {
                        inputPath
                    },
                    ex.Message),
                ex.Message,
                jsonOutPath));
        }
    }

    public static async Task<int> RunTranscodeAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        var jsonOutPath = GetOption(options, "--json-out");
        var ffprobePath = GetOption(options, "--ffprobe") ?? "ffprobe";
        var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);

        var processRunner = new DefaultProcessRunner();
        var probeService = new FfprobeMediaProbeService(processRunner, new FfprobeJsonParser());
        var runner = new TranscodeJobRunner(new FfmpegCommandBuilder(), processRunner);

        try
        {
            var probeResult = await probeService.ProbeAsync(inputPath!, ffprobePath, timeout);
            var job = FoundationCommandSupport.BuildJob(inputPath!, options, probeResult);
            Directory.CreateDirectory(job.Output.OutputDirectory);

            var result = await runner.RunAsync(job, ffmpegPath, timeout);

            if (result.Status != ExecutionStatus.Succeeded)
            {
                var message = BuildExecutionFailureMessage(result);
                return FailWithCommandEnvelope(
                    "run",
                    preview: false,
                    BuildFailedCommandPayload("run", job, message, result),
                    message,
                    jsonOutPath,
                    exitCode: 2);
            }

            return WriteCommandEnvelope(
                "run",
                preview: false,
                new
                {
                    run = new
                    {
                        inputPath,
                        ffprobePath,
                        ffmpegPath
                    },
                    job,
                    execution = result
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "run",
                preview: false,
                BuildFailedCommandPayload(
                    "run",
                    new
                    {
                        inputPath,
                        ffprobePath,
                        ffmpegPath
                    },
                    ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    private static string? ResolveOutputPlanPath(
        string fullPlanPath,
        bool? inPlace,
        string? writeTo,
        out string? error)
    {
        error = null;

        if (inPlace == false && string.IsNullOrWhiteSpace(writeTo))
        {
            error = "Option '--write-to' is required when '--in-place false' is specified.";
            return null;
        }

        if (inPlace == true && !string.IsNullOrWhiteSpace(writeTo))
        {
            error = "Options '--write-to' and '--in-place true' cannot be used together.";
            return null;
        }

        if (!string.IsNullOrWhiteSpace(writeTo))
        {
            return Path.GetFullPath(writeTo);
        }

        return fullPlanPath;
    }

    private static bool TryResolveReplacementTarget(
        IReadOnlyDictionary<string, string> options,
        out EditPlanInspectionTargetSelector selector,
        out string? error)
    {
        selector = new EditPlanInspectionTargetSelector();
        error = null;

        var sourceInput = options.ContainsKey("--source-input");
        var transcript = options.ContainsKey("--transcript");
        var beats = options.ContainsKey("--beats");
        var subtitles = options.ContainsKey("--subtitles");
        var audioTrackId = GetOption(options, "--audio-track-id");
        var artifactSlot = GetOption(options, "--artifact-slot");

        var selectionCount = 0;
        selectionCount += sourceInput ? 1 : 0;
        selectionCount += transcript ? 1 : 0;
        selectionCount += beats ? 1 : 0;
        selectionCount += subtitles ? 1 : 0;
        selectionCount += string.IsNullOrWhiteSpace(audioTrackId) ? 0 : 1;
        selectionCount += string.IsNullOrWhiteSpace(artifactSlot) ? 0 : 1;

        if (selectionCount != 1)
        {
            error = "Exactly one replacement target is required: '--source-input', '--audio-track-id <id>', '--artifact-slot <slotId>', '--transcript', '--beats', or '--subtitles'.";
            return false;
        }

        if (sourceInput)
        {
            selector = new EditPlanInspectionTargetSelector
            {
                Singleton = EditPlanInspectionTargetKeys.SourceInput
            };
            return true;
        }

        if (transcript)
        {
            selector = new EditPlanInspectionTargetSelector
            {
                Singleton = EditPlanInspectionTargetKeys.Transcript
            };
            return true;
        }

        if (beats)
        {
            selector = new EditPlanInspectionTargetSelector
            {
                Singleton = EditPlanInspectionTargetKeys.Beats
            };
            return true;
        }

        if (subtitles)
        {
            selector = new EditPlanInspectionTargetSelector
            {
                Singleton = EditPlanInspectionTargetKeys.Subtitles
            };
            return true;
        }

        if (!string.IsNullOrWhiteSpace(audioTrackId))
        {
            selector = new EditPlanInspectionTargetSelector
            {
                AudioTrackId = audioTrackId
            };
            return true;
        }

        selector = new EditPlanInspectionTargetSelector
        {
            ArtifactSlot = artifactSlot
        };
        return true;
    }

    private static bool TryResolveReplacementTarget(
        ReplacePlanMaterialBatchItem item,
        out EditPlanInspectionTargetSelector selector,
        out string? error)
    {
        selector = new EditPlanInspectionTargetSelector();
        error = null;

        var selectionCount = 0;
        selectionCount += item.SourceInput ? 1 : 0;
        selectionCount += item.Transcript ? 1 : 0;
        selectionCount += item.Beats ? 1 : 0;
        selectionCount += item.Subtitles ? 1 : 0;
        selectionCount += string.IsNullOrWhiteSpace(item.AudioTrackId) ? 0 : 1;
        selectionCount += string.IsNullOrWhiteSpace(item.ArtifactSlot) ? 0 : 1;

        if (selectionCount != 1)
        {
            error = "Exactly one replacement target is required: 'sourceInput', 'audioTrackId', 'artifactSlot', 'transcript', 'beats', or 'subtitles'.";
            return false;
        }

        if (item.SourceInput)
        {
            selector = new EditPlanInspectionTargetSelector
            {
                Singleton = EditPlanInspectionTargetKeys.SourceInput
            };
            return true;
        }

        if (item.Transcript)
        {
            selector = new EditPlanInspectionTargetSelector
            {
                Singleton = EditPlanInspectionTargetKeys.Transcript
            };
            return true;
        }

        if (item.Beats)
        {
            selector = new EditPlanInspectionTargetSelector
            {
                Singleton = EditPlanInspectionTargetKeys.Beats
            };
            return true;
        }

        if (item.Subtitles)
        {
            selector = new EditPlanInspectionTargetSelector
            {
                Singleton = EditPlanInspectionTargetKeys.Subtitles
            };
            return true;
        }

        if (!string.IsNullOrWhiteSpace(item.AudioTrackId))
        {
            selector = new EditPlanInspectionTargetSelector
            {
                AudioTrackId = item.AudioTrackId
            };
            return true;
        }

        selector = new EditPlanInspectionTargetSelector
        {
            ArtifactSlot = item.ArtifactSlot
        };
        return true;
    }

    private static bool TryResolveAttachmentTarget(
        IReadOnlyDictionary<string, string> options,
        out EditPlanInspectionTargetSelector selector,
        out string? error)
    {
        selector = new EditPlanInspectionTargetSelector();
        error = null;

        var transcript = options.ContainsKey("--transcript");
        var beats = options.ContainsKey("--beats");
        var subtitles = options.ContainsKey("--subtitles");
        var audioTrackId = GetOption(options, "--audio-track-id");
        var artifactSlot = GetOption(options, "--artifact-slot");

        var selectionCount = 0;
        selectionCount += transcript ? 1 : 0;
        selectionCount += beats ? 1 : 0;
        selectionCount += subtitles ? 1 : 0;
        selectionCount += string.IsNullOrWhiteSpace(audioTrackId) ? 0 : 1;
        selectionCount += string.IsNullOrWhiteSpace(artifactSlot) ? 0 : 1;

        if (selectionCount != 1)
        {
            error = "Exactly one attachment target is required: '--transcript', '--beats', '--subtitles', '--audio-track-id <id>', or '--artifact-slot <slotId>'.";
            return false;
        }

        if (transcript)
        {
            selector = new EditPlanInspectionTargetSelector
            {
                Singleton = EditPlanInspectionTargetKeys.Transcript
            };
            return true;
        }

        if (beats)
        {
            selector = new EditPlanInspectionTargetSelector
            {
                Singleton = EditPlanInspectionTargetKeys.Beats
            };
            return true;
        }

        if (subtitles)
        {
            selector = new EditPlanInspectionTargetSelector
            {
                Singleton = EditPlanInspectionTargetKeys.Subtitles
            };
            return true;
        }

        if (!string.IsNullOrWhiteSpace(audioTrackId))
        {
            selector = new EditPlanInspectionTargetSelector
            {
                AudioTrackId = audioTrackId
            };
            return true;
        }

        selector = new EditPlanInspectionTargetSelector
        {
            ArtifactSlot = artifactSlot
        };
        return true;
    }

    private static bool TryResolveAttachmentTarget(
        AttachPlanMaterialBatchItem item,
        out EditPlanInspectionTargetSelector selector,
        out string? error)
    {
        selector = new EditPlanInspectionTargetSelector();
        error = null;

        var selectionCount = 0;
        selectionCount += item.Transcript ? 1 : 0;
        selectionCount += item.Beats ? 1 : 0;
        selectionCount += item.Subtitles ? 1 : 0;
        selectionCount += string.IsNullOrWhiteSpace(item.AudioTrackId) ? 0 : 1;
        selectionCount += string.IsNullOrWhiteSpace(item.ArtifactSlot) ? 0 : 1;

        if (selectionCount != 1)
        {
            error = "Exactly one attachment target is required: 'transcript', 'beats', 'subtitles', 'audioTrackId', or 'artifactSlot'.";
            return false;
        }

        if (item.Transcript)
        {
            selector = new EditPlanInspectionTargetSelector
            {
                Singleton = EditPlanInspectionTargetKeys.Transcript
            };
            return true;
        }

        if (item.Beats)
        {
            selector = new EditPlanInspectionTargetSelector
            {
                Singleton = EditPlanInspectionTargetKeys.Beats
            };
            return true;
        }

        if (item.Subtitles)
        {
            selector = new EditPlanInspectionTargetSelector
            {
                Singleton = EditPlanInspectionTargetKeys.Subtitles
            };
            return true;
        }

        if (!string.IsNullOrWhiteSpace(item.AudioTrackId))
        {
            selector = new EditPlanInspectionTargetSelector
            {
                AudioTrackId = item.AudioTrackId
            };
            return true;
        }

        selector = new EditPlanInspectionTargetSelector
        {
            ArtifactSlot = item.ArtifactSlot
        };
        return true;
    }

    private static async Task<AttachPlanMaterialOperationResult> ExecuteAttachPlanMaterialAsync(
        AttachPlanMaterialOperationRequest request,
        TemplatePluginCatalog? pluginCatalog)
    {
        var outputBaseDirectory = Path.GetDirectoryName(request.OutputPlanPath)!;
        var planContext = await TemplatePlanValidationSupport.LoadPlanContextAsync(request.FullPlanPath, pluginCatalog);
        var attachment = new EditPlanMaterialAttacher().Attach(
            planContext.Plan,
            outputBaseDirectory,
            new EditPlanMaterialAttachmentRequest
            {
                Target = request.TargetSelector,
                ResolvedPath = request.ResolvedAttachmentPath,
                PathStyle = request.PathStyle,
                SubtitleMode = request.SubtitleMode,
                AudioTrackRole = request.AudioTrackRole
            },
            planContext.ValidationTemplates);

        var validation = new EditPlanValidator().Validate(
            EditPlanPathResolver.ResolvePaths(attachment.UpdatedPlan, outputBaseDirectory),
            request.CheckFiles,
            planContext.ValidationTemplates);

        var report = new
        {
            planPath = request.FullPlanPath,
            outputPlanPath = request.OutputPlanPath,
            checkFiles = request.CheckFiles,
            requireValid = request.RequireValid,
            target = new
            {
                attachment.Target.TargetType,
                attachment.Target.TargetKey,
                attachment.Target.Selector,
                previousPath = attachment.PreviousPath,
                nextPath = attachment.NextPath,
                pathStyleApplied = attachment.PathStyleApplied,
                previousSubtitleMode = attachment.PreviousSubtitleMode,
                nextSubtitleMode = attachment.NextSubtitleMode,
                previousAudioTrackRole = attachment.PreviousAudioTrackRole,
                nextAudioTrackRole = attachment.NextAudioTrackRole
            },
            added = attachment.Added,
            validation
        };

        if (request.RequireValid && !validation.IsValid)
        {
            return new AttachPlanMaterialOperationResult
            {
                Report = report,
                ErrorMessage = "Updated plan failed validation and '--require-valid' was specified.",
                ExitCode = 1
            };
        }

        Directory.CreateDirectory(outputBaseDirectory);
        await File.WriteAllTextAsync(request.OutputPlanPath, JsonSerializer.Serialize(attachment.UpdatedPlan, OpenVideoToolboxJson.Default));

        return new AttachPlanMaterialOperationResult
        {
            Report = report,
            ExitCode = 0
        };
    }

    private static async Task<ReplacePlanMaterialOperationResult> ExecuteReplacePlanMaterialAsync(
        ReplacePlanMaterialOperationRequest request,
        TemplatePluginCatalog? pluginCatalog)
    {
        var outputBaseDirectory = Path.GetDirectoryName(request.OutputPlanPath)!;
        var planContext = await TemplatePlanValidationSupport.LoadPlanContextAsync(request.FullPlanPath, pluginCatalog);
        var replacement = new EditPlanMaterialReplacer().Replace(
            planContext.Plan,
            outputBaseDirectory,
            new EditPlanMaterialReplacementRequest
            {
                Target = request.TargetSelector,
                ResolvedPath = request.ResolvedReplacementPath,
                PathStyle = request.PathStyle,
                SubtitleMode = request.SubtitleMode
            });

        var validation = new EditPlanValidator().Validate(
            EditPlanPathResolver.ResolvePaths(replacement.UpdatedPlan, outputBaseDirectory),
            request.CheckFiles,
            planContext.ValidationTemplates);

        var report = new
        {
            planPath = request.FullPlanPath,
            outputPlanPath = request.OutputPlanPath,
            checkFiles = request.CheckFiles,
            requireValid = request.RequireValid,
            target = new
            {
                replacement.Target.TargetType,
                replacement.Target.TargetKey,
                replacement.Target.Selector,
                previousPath = replacement.PreviousPath,
                nextPath = replacement.NextPath,
                pathStyleApplied = replacement.PathStyleApplied,
                previousSubtitleMode = replacement.PreviousSubtitleMode,
                nextSubtitleMode = replacement.NextSubtitleMode
            },
            changed = replacement.Changed,
            validation
        };

        if (request.RequireValid && !validation.IsValid)
        {
            return new ReplacePlanMaterialOperationResult
            {
                Report = report,
                ErrorMessage = "Updated plan failed validation and '--require-valid' was specified.",
                ExitCode = 1
            };
        }

        Directory.CreateDirectory(outputBaseDirectory);
        await File.WriteAllTextAsync(request.OutputPlanPath, JsonSerializer.Serialize(replacement.UpdatedPlan, OpenVideoToolboxJson.Default));

        return new ReplacePlanMaterialOperationResult
        {
            Report = report,
            ExitCode = 0
        };
    }

    private static async Task<BindVoiceTrackOperationResult> ExecuteBindVoiceTrackAsync(
        BindVoiceTrackOperationRequest request,
        TemplatePluginCatalog? pluginCatalog)
    {
        var outputBaseDirectory = Path.GetDirectoryName(request.OutputPlanPath)!;
        var planContext = await TemplatePlanValidationSupport.LoadPlanContextAsync(request.FullPlanPath, pluginCatalog);
        var existingTrack = planContext.Plan.AudioTracks
            .FirstOrDefault(track => string.Equals(track.Id, request.TrackId, StringComparison.OrdinalIgnoreCase));
        var roleApplied = request.RequestedRole ?? existingTrack?.Role ?? AudioTrackRole.Voice;

        var attachment = new EditPlanMaterialAttacher().Attach(
            planContext.Plan,
            outputBaseDirectory,
            new EditPlanMaterialAttachmentRequest
            {
                Target = new EditPlanInspectionTargetSelector
                {
                    AudioTrackId = request.TrackId
                },
                ResolvedPath = request.ResolvedVoicePath,
                PathStyle = request.PathStyle,
                AudioTrackRole = roleApplied
            },
            planContext.ValidationTemplates);

        var validation = new EditPlanValidator().Validate(
            EditPlanPathResolver.ResolvePaths(attachment.UpdatedPlan, outputBaseDirectory),
            request.CheckFiles,
            planContext.ValidationTemplates);

        var report = new
        {
            planPath = request.FullPlanPath,
            outputPlanPath = request.OutputPlanPath,
            checkFiles = request.CheckFiles,
            requireValid = request.RequireValid,
            voiceTrack = new
            {
                trackId = request.TrackId,
                roleApplied,
                added = attachment.Added,
                previousPath = attachment.PreviousPath,
                nextPath = attachment.NextPath,
                pathStyleApplied = attachment.PathStyleApplied,
                previousAudioTrackRole = attachment.PreviousAudioTrackRole,
                nextAudioTrackRole = attachment.NextAudioTrackRole
            },
            validation
        };

        if (request.RequireValid && !validation.IsValid)
        {
            return new BindVoiceTrackOperationResult
            {
                Report = report,
                ErrorMessage = "Updated plan failed validation and '--require-valid' was specified.",
                ExitCode = 1
            };
        }

        Directory.CreateDirectory(outputBaseDirectory);
        await File.WriteAllTextAsync(request.OutputPlanPath, JsonSerializer.Serialize(attachment.UpdatedPlan, OpenVideoToolboxJson.Default));

        return new BindVoiceTrackOperationResult
        {
            Report = report,
            ExitCode = 0
        };
    }

    private sealed record AttachPlanMaterialOperationRequest
    {
        public required string FullPlanPath { get; init; }

        public required string OutputPlanPath { get; init; }

        public required string ResolvedAttachmentPath { get; init; }

        public required EditPlanInspectionTargetSelector TargetSelector { get; init; }

        public EditPlanPathWriteStyle PathStyle { get; init; }

        public bool CheckFiles { get; init; }

        public bool RequireValid { get; init; }

        public SubtitleMode? SubtitleMode { get; init; }

        public AudioTrackRole? AudioTrackRole { get; init; }
    }

    private sealed record AttachPlanMaterialOperationResult
    {
        public required object Report { get; init; }

        public string? ErrorMessage { get; init; }

        public int ExitCode { get; init; }
    }

    private sealed record ReplacePlanMaterialOperationRequest
    {
        public required string FullPlanPath { get; init; }

        public required string OutputPlanPath { get; init; }

        public required string ResolvedReplacementPath { get; init; }

        public required EditPlanInspectionTargetSelector TargetSelector { get; init; }

        public EditPlanPathWriteStyle PathStyle { get; init; }

        public bool CheckFiles { get; init; }

        public bool RequireValid { get; init; }

        public SubtitleMode? SubtitleMode { get; init; }
    }

    private sealed record ReplacePlanMaterialOperationResult
    {
        public required object Report { get; init; }

        public string? ErrorMessage { get; init; }

        public int ExitCode { get; init; }
    }

    private sealed record BindVoiceTrackOperationRequest
    {
        public required string FullPlanPath { get; init; }

        public required string OutputPlanPath { get; init; }

        public required string ResolvedVoicePath { get; init; }

        public required string TrackId { get; init; }

        public AudioTrackRole? RequestedRole { get; init; }

        public EditPlanPathWriteStyle PathStyle { get; init; }

        public bool CheckFiles { get; init; }

        public bool RequireValid { get; init; }
    }

    private sealed record BindVoiceTrackOperationResult
    {
        public required object Report { get; init; }

        public string? ErrorMessage { get; init; }

        public int ExitCode { get; init; }
    }
}
