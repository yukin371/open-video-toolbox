namespace OpenVideoToolbox.Core.Execution;

public sealed class WhisperCppTranscriptionRunner
{
    private readonly WhisperCppCommandBuilder _commandBuilder;
    private readonly IProcessRunner _processRunner;

    public WhisperCppTranscriptionRunner(WhisperCppCommandBuilder commandBuilder, IProcessRunner processRunner)
    {
        _commandBuilder = commandBuilder;
        _processRunner = processRunner;
    }

    public async Task<ExecutionResult> RunAsync(
        WhisperCppExecutionRequest request,
        string executablePath = "whisper-cli",
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
                ProducedPaths = [$"{request.OutputFilePrefix}.json"]
            },
            cancellationToken).ConfigureAwait(false);
    }
}
