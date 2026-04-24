using System.Text.Json;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Serialization;
using static OpenVideoToolbox.Cli.CliCommandOutput;
using static OpenVideoToolbox.Cli.CliOptionParsing;

namespace OpenVideoToolbox.Cli;

internal static class RenderCommandHandlers
{
    public static async Task<int> RunRenderAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--plan", out var planPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--preview", out var previewOnly, out error))
        {
            return fail(error!);
        }

        var jsonOutPath = GetOption(options, "--json-out");
        var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);

        try
        {
            var fullPlanPath = Path.GetFullPath(planPath!);
            var plan = await LoadEditPlanAsync(fullPlanPath, GetOption(options, "--output"));
            var result = await ExecuteRenderAsync(
                fullPlanPath,
                plan,
                previewOnly == true,
                ffmpegPath,
                timeout,
                overwriteExisting: GetOption(options, "--overwrite") == "true");

            if (result.ExitCode != 0 && result.ErrorMessage is not null)
            {
                Console.Error.WriteLine(result.ErrorMessage);
            }

            return WriteCommandEnvelope("render", previewOnly == true, result.Payload, jsonOutPath, result.ExitCode);
        }
        catch (Exception ex)
        {
            return fail(ex.Message);
        }
    }

    public static async Task<int> RunRenderBatchAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--manifest", out var manifestPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--preview", out var previewOnly, out error))
        {
            return fail(error!);
        }

        var jsonOutPath = GetOption(options, "--json-out");
        var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
        var fullManifestPath = Path.GetFullPath(manifestPath!);
        var manifestBaseDirectory = Path.GetDirectoryName(fullManifestPath)!;
        var summaryPath = BatchCommandArtifacts.ResolveSummaryPath(manifestBaseDirectory);

        try
        {
            var manifest = JsonSerializer.Deserialize<RenderBatchManifest>(
                await File.ReadAllTextAsync(fullManifestPath),
                OpenVideoToolboxJson.Default)
                ?? throw new InvalidOperationException($"Failed to parse batch manifest '{fullManifestPath}'.");

            if (manifest.SchemaVersion != 1)
            {
                throw new InvalidOperationException(
                    $"Unsupported render-batch manifest schema version '{manifest.SchemaVersion}'.");
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

                    var fullPlanPath = Path.GetFullPath(Path.Combine(manifestBaseDirectory, item.Plan));
                    var outputOverridePath = string.IsNullOrWhiteSpace(item.Output)
                        ? null
                        : Path.GetFullPath(Path.Combine(manifestBaseDirectory, item.Output));
                    var plan = await LoadEditPlanAsync(fullPlanPath, outputOverridePath);
                    var result = await ExecuteRenderAsync(
                        fullPlanPath,
                        plan,
                        previewOnly == true,
                        ffmpegPath,
                        timeout,
                        overwriteExisting: item.Overwrite == true);
                    var resultPath = await BatchCommandArtifacts.WriteResultAsync(manifestBaseDirectory, item.Id, result.Payload);

                    if (result.ExitCode == 0)
                    {
                        succeededCount++;
                        results.Add(new
                        {
                            index,
                            id = item.Id,
                            planPath = fullPlanPath,
                            outputPath = plan.Output.Path,
                            resultPath,
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
                            planPath = fullPlanPath,
                            outputPath = plan.Output.Path,
                            resultPath,
                            status = "failed",
                            result = result.Payload,
                            error = new
                            {
                                message = result.ErrorMessage ?? "Render batch item failed."
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
                preview = previewOnly == true,
                manifestPath = fullManifestPath,
                manifestBaseDirectory,
                summaryPath,
                itemCount = manifest.Items.Count,
                succeededCount,
                failedCount,
                results
            };

            await BatchCommandArtifacts.WriteSummaryAsync(manifestBaseDirectory, payload);

            return WriteCommandEnvelope(
                "render-batch",
                previewOnly == true,
                payload,
                jsonOutPath,
                exitCode: failedCount == 0 ? 0 : 2);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return WriteCommandEnvelope(
                "render-batch",
                previewOnly == true,
                new
                {
                    preview = previewOnly == true,
                    manifestPath = fullManifestPath,
                    summaryPath,
                    error = new
                    {
                        message = ex.Message
                    }
                },
                jsonOutPath,
                exitCode: 1);
        }
    }

    public static async Task<int> RunMixAudioAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--plan", out var planPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--preview", out var previewOnly, out error))
        {
            return fail(error!);
        }

        var jsonOutPath = GetOption(options, "--json-out");
        var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
        var fullPlanPath = Path.GetFullPath(planPath!);
        EditPlan? plan = null;
        string? resolvedOutputPath = null;
        object? preview = null;

        try
        {
            var planDirectory = Path.GetDirectoryName(fullPlanPath)!;
            plan = await LoadEditPlanAsync(fullPlanPath, outputOverridePath: null);
            resolvedOutputPath = EditPlanPathResolver.ResolvePath(planDirectory, outputPath!);
            Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);

            var request = new EditPlanAudioMixRequest
            {
                Plan = plan,
                OutputPath = resolvedOutputPath,
                OverwriteExisting = GetOption(options, "--overwrite") == "true"
            };

            var builder = new FfmpegEditPlanAudioMixCommandBuilder();
            var previewBuilder = new EditPlanExecutionPreviewBuilder(new FfmpegEditPlanRenderCommandBuilder(), builder);
            preview = previewBuilder.BuildAudioMixPreview(request, ffmpegPath);
            if (previewOnly == true)
            {
                return WriteCommandEnvelope(
                    "mix-audio",
                    preview: true,
                    new
                    {
                        mixAudio = new
                        {
                            planPath = fullPlanPath,
                            request.OutputPath
                        },
                        templateSource = BuildPlanTemplateSourcePayload(plan),
                        executionPreview = preview
                    },
                    jsonOutPath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);

            var processRunner = new DefaultProcessRunner();
            var runner = new EditPlanAudioMixRunner(builder, processRunner);
            var result = await runner.RunAsync(request, ffmpegPath, timeout);

            if (result.Status != ExecutionStatus.Succeeded)
            {
                var message = BuildExecutionFailureMessage(result);
                return FailWithCommandEnvelope(
                    "mix-audio",
                    preview: false,
                    BuildFailedPlanCommandPayload(
                        "mixAudio",
                        new
                        {
                            planPath = fullPlanPath,
                            outputPath = resolvedOutputPath
                        },
                        plan,
                        preview,
                        message,
                        result),
                    message,
                    jsonOutPath,
                    exitCode: 2);
            }

            return WriteCommandEnvelope(
                "mix-audio",
                preview: false,
                new
                {
                    mixAudio = new
                    {
                        planPath = fullPlanPath,
                        request.OutputPath
                    },
                    templateSource = BuildPlanTemplateSourcePayload(plan),
                    executionPreview = preview,
                    execution = result
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            if (plan is null || string.IsNullOrWhiteSpace(resolvedOutputPath))
            {
                return fail(ex.Message);
            }

            return FailWithCommandEnvelope(
                "mix-audio",
                previewOnly == true,
                BuildFailedPlanCommandPayload(
                    "mixAudio",
                    new
                    {
                        planPath = fullPlanPath,
                        outputPath = resolvedOutputPath
                    },
                    plan,
                    preview,
                    ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    private static async Task<RenderOperationResult> ExecuteRenderAsync(
        string fullPlanPath,
        EditPlan plan,
        bool previewOnly,
        string ffmpegPath,
        TimeSpan? timeout,
        bool overwriteExisting)
    {
        object? preview = null;

        try
        {
            var request = new EditPlanRenderRequest
            {
                Plan = plan,
                OverwriteExisting = overwriteExisting
            };

            var builder = new FfmpegEditPlanRenderCommandBuilder();
            var previewBuilder = new EditPlanExecutionPreviewBuilder(builder, new FfmpegEditPlanAudioMixCommandBuilder());
            preview = previewBuilder.BuildRenderPreview(request, ffmpegPath);
            if (previewOnly)
            {
                return new RenderOperationResult
                {
                    Payload = new
                    {
                        render = request.Plan,
                        templateSource = BuildPlanTemplateSourcePayload(request.Plan),
                        executionPreview = preview
                    },
                    ExitCode = 0
                };
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(plan.Output.Path))!);

            var processRunner = new DefaultProcessRunner();
            var runner = new EditPlanRenderRunner(builder, processRunner);
            var result = await runner.RunAsync(request, ffmpegPath, timeout);

            if (result.Status != ExecutionStatus.Succeeded)
            {
                var message = BuildExecutionFailureMessage(result);
                return new RenderOperationResult
                {
                    Payload = BuildFailedPlanCommandPayload("render", request.Plan, request.Plan, preview, message, result),
                    ExitCode = 2,
                    ErrorMessage = message
                };
            }

            return new RenderOperationResult
            {
                Payload = new
                {
                    render = request.Plan,
                    templateSource = BuildPlanTemplateSourcePayload(request.Plan),
                    executionPreview = preview,
                    execution = result
                },
                ExitCode = 0
            };
        }
        catch (Exception ex)
        {
            return new RenderOperationResult
            {
                Payload = BuildFailedPlanCommandPayload("render", plan, plan, preview, ex.Message),
                ExitCode = 1,
                ErrorMessage = ex.Message
            };
        }
    }

    private static async Task<EditPlan> LoadEditPlanAsync(string planPath, string? outputOverridePath)
    {
        var fullPlanPath = Path.GetFullPath(planPath);
        var content = await File.ReadAllTextAsync(fullPlanPath);
        var plan = JsonSerializer.Deserialize<EditPlan>(content, OpenVideoToolboxJson.Default)
            ?? throw new InvalidOperationException($"Failed to parse edit plan '{planPath}'.");

        if (plan.SchemaVersion != 1)
        {
            throw new InvalidOperationException($"Unsupported edit plan schema version '{plan.SchemaVersion}'.");
        }

        var resolvedPlan = EditPlanPathResolver.ResolvePaths(plan, Path.GetDirectoryName(fullPlanPath)!);
        if (string.IsNullOrWhiteSpace(outputOverridePath))
        {
            return resolvedPlan;
        }

        var resolvedOutputPath = EditPlanPathResolver.ResolvePath(Path.GetDirectoryName(fullPlanPath)!, outputOverridePath);
        var container = Path.GetExtension(resolvedOutputPath);
        if (container.StartsWith(".", StringComparison.Ordinal))
        {
            container = container[1..];
        }

        return resolvedPlan with
        {
            Output = resolvedPlan.Output with
            {
                Path = resolvedOutputPath,
                Container = string.IsNullOrWhiteSpace(container) ? resolvedPlan.Output.Container : container
            }
        };
    }
}
