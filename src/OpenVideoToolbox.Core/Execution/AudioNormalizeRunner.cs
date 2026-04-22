namespace OpenVideoToolbox.Core.Execution;

public sealed class AudioNormalizeRunner
{
    private readonly FfmpegAudioNormalizeCommandBuilder _commandBuilder;
    private readonly IProcessRunner _processRunner;

    public AudioNormalizeRunner(FfmpegAudioNormalizeCommandBuilder commandBuilder, IProcessRunner processRunner)
    {
        _commandBuilder = commandBuilder;
        _processRunner = processRunner;
    }

    public async Task<ExecutionResult> RunAsync(
        AudioNormalizeRequest request,
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
                ProducedPaths = [request.OutputPath]
            },
            cancellationToken).ConfigureAwait(false);
    }
}
