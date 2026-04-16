namespace OpenVideoToolbox.Core.Execution;

public sealed class AudioExtractRunner
{
    private readonly FfmpegAudioExtractCommandBuilder _commandBuilder;
    private readonly IProcessRunner _processRunner;

    public AudioExtractRunner(FfmpegAudioExtractCommandBuilder commandBuilder, IProcessRunner processRunner)
    {
        _commandBuilder = commandBuilder;
        _processRunner = processRunner;
    }

    public async Task<ExecutionResult> RunAsync(
        AudioExtractRequest request,
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
