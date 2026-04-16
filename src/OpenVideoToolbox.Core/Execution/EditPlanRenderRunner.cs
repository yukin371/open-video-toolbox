using OpenVideoToolbox.Core.Editing;

namespace OpenVideoToolbox.Core.Execution;

public sealed class EditPlanRenderRunner
{
    private readonly FfmpegEditPlanRenderCommandBuilder _commandBuilder;
    private readonly IProcessRunner _processRunner;

    public EditPlanRenderRunner(FfmpegEditPlanRenderCommandBuilder commandBuilder, IProcessRunner processRunner)
    {
        _commandBuilder = commandBuilder;
        _processRunner = processRunner;
    }

    public async Task<ExecutionResult> RunAsync(
        EditPlanRenderRequest request,
        string executablePath = "ffmpeg",
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = _commandBuilder.Build(request, executablePath);
        var producedPaths = BuildProducedPaths(request.Plan);
        var result = await _processRunner.ExecuteAsync(
            new ProcessExecutionRequest
            {
                CommandPlan = plan,
                Timeout = timeout,
                ProducedPaths = producedPaths
            },
            cancellationToken).ConfigureAwait(false);

        if (result.Status != ExecutionStatus.Succeeded || request.Plan.Subtitles?.Mode != SubtitleMode.Sidecar)
        {
            return result with { ProducedPaths = producedPaths };
        }

        try
        {
            CopySubtitleSidecar(request.Plan, request.OverwriteExisting);
            return result with { ProducedPaths = producedPaths };
        }
        catch (Exception ex)
        {
            return result with
            {
                Status = ExecutionStatus.Failed,
                ErrorMessage = ex.Message,
                ProducedPaths = producedPaths
            };
        }
    }

    public static IReadOnlyList<string> BuildProducedPaths(EditPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var producedPaths = new List<string> { plan.Output.Path };
        foreach (var effect in BuildSideEffects(plan))
        {
            if (!string.IsNullOrWhiteSpace(effect.DestinationPath))
            {
                producedPaths.Add(effect.DestinationPath);
            }
        }

        return producedPaths;
    }

    public static IReadOnlyList<ExecutionSideEffect> BuildSideEffects(EditPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.Subtitles?.Mode != SubtitleMode.Sidecar)
        {
            return [];
        }

        return
        [
            new ExecutionSideEffect
            {
                Type = "copy-subtitle-sidecar",
                SourcePath = Path.GetFullPath(plan.Subtitles.Path),
                DestinationPath = GetSidecarSubtitleOutputPath(plan)
            }
        ];
    }

    private static void CopySubtitleSidecar(EditPlan plan, bool overwriteExisting)
    {
        var sourcePath = Path.GetFullPath(plan.Subtitles!.Path);
        var destinationPath = GetSidecarSubtitleOutputPath(plan);

        if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(sourcePath, destinationPath, overwriteExisting);
    }

    private static string GetSidecarSubtitleOutputPath(EditPlan plan)
    {
        var extension = Path.GetExtension(plan.Subtitles!.Path);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".srt";
        }

        return Path.ChangeExtension(plan.Output.Path, extension);
    }
}
