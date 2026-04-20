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
        EditPlan? plan = null;
        object? preview = null;

        try
        {
            plan = await LoadEditPlanAsync(planPath!, GetOption(options, "--output"));
            var request = new EditPlanRenderRequest
            {
                Plan = plan,
                OverwriteExisting = GetOption(options, "--overwrite") == "true"
            };

            var builder = new FfmpegEditPlanRenderCommandBuilder();
            var previewBuilder = new EditPlanExecutionPreviewBuilder(builder, new FfmpegEditPlanAudioMixCommandBuilder());
            preview = previewBuilder.BuildRenderPreview(request, ffmpegPath);
            if (previewOnly == true)
            {
                return WriteCommandEnvelope(
                    "render",
                    preview: true,
                    new
                    {
                        render = request.Plan,
                        templateSource = BuildPlanTemplateSourcePayload(request.Plan),
                        executionPreview = preview
                    },
                    jsonOutPath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(plan.Output.Path))!);

            var processRunner = new DefaultProcessRunner();
            var runner = new EditPlanRenderRunner(builder, processRunner);
            var result = await runner.RunAsync(request, ffmpegPath, timeout);

            if (result.Status != ExecutionStatus.Succeeded)
            {
                var message = BuildExecutionFailureMessage(result);
                return FailWithCommandEnvelope(
                    "render",
                    preview: false,
                    BuildFailedPlanCommandPayload("render", request.Plan, request.Plan, preview, message, result),
                    message,
                    jsonOutPath,
                    exitCode: 2);
            }

            return WriteCommandEnvelope(
                "render",
                preview: false,
                new
                {
                    render = request.Plan,
                    templateSource = BuildPlanTemplateSourcePayload(request.Plan),
                    executionPreview = preview,
                    execution = result
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            if (plan is null)
            {
                return fail(ex.Message);
            }

            return FailWithCommandEnvelope(
                "render",
                previewOnly == true,
                BuildFailedPlanCommandPayload("render", plan, plan, preview, ex.Message),
                ex.Message,
                jsonOutPath);
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
