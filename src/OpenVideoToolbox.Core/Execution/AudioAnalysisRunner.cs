namespace OpenVideoToolbox.Core.Execution;

public sealed class AudioAnalysisRunner
{
    private readonly FfmpegAudioAnalysisCommandBuilder _commandBuilder;
    private readonly IProcessRunner _processRunner;

    public AudioAnalysisRunner(FfmpegAudioAnalysisCommandBuilder commandBuilder, IProcessRunner processRunner)
    {
        _commandBuilder = commandBuilder;
        _processRunner = processRunner;
    }

    public async Task<ExecutionResult> RunAsync(
        AudioAnalysisRequest request,
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
