namespace OpenVideoToolbox.Core.Execution;

public sealed class SilenceDetectionRunner
{
    private readonly FfmpegSilenceDetectionCommandBuilder _commandBuilder;
    private readonly IProcessRunner _processRunner;

    public SilenceDetectionRunner(FfmpegSilenceDetectionCommandBuilder commandBuilder, IProcessRunner processRunner)
    {
        _commandBuilder = commandBuilder;
        _processRunner = processRunner;
    }

    public async Task<ExecutionResult> RunAsync(
        SilenceDetectionRequest request,
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
                Timeout = timeout
            },
            cancellationToken).ConfigureAwait(false);
    }
}
