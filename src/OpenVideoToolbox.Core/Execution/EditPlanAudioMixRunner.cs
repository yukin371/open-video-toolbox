namespace OpenVideoToolbox.Core.Execution;

public sealed class EditPlanAudioMixRunner
{
    private readonly FfmpegEditPlanAudioMixCommandBuilder _commandBuilder;
    private readonly IProcessRunner _processRunner;

    public EditPlanAudioMixRunner(FfmpegEditPlanAudioMixCommandBuilder commandBuilder, IProcessRunner processRunner)
    {
        _commandBuilder = commandBuilder;
        _processRunner = processRunner;
    }

    public async Task<ExecutionResult> RunAsync(
        EditPlanAudioMixRequest request,
        string executablePath = "ffmpeg",
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = _commandBuilder.Build(request, executablePath);
        return await _processRunner.ExecuteAsync(
            new ProcessExecutionRequest
            {
                CommandPlan = plan,
                Timeout = timeout,
                ProducedPaths = BuildProducedPaths(request)
            },
            cancellationToken).ConfigureAwait(false);
    }

    public static IReadOnlyList<string> BuildProducedPaths(EditPlanAudioMixRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputPath);

        return [request.OutputPath];
    }
}
