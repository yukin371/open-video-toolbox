namespace OpenVideoToolbox.Core.Execution;

public sealed class DemucsSeparationRunner
{
    private readonly DemucsCommandBuilder _commandBuilder;
    private readonly IProcessRunner _processRunner;

    public DemucsSeparationRunner(DemucsCommandBuilder commandBuilder, IProcessRunner processRunner)
    {
        _commandBuilder = commandBuilder;
        _processRunner = processRunner;
    }

    public async Task<ExecutionResult> RunAsync(
        DemucsExecutionRequest request,
        string executablePath = "demucs",
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = _commandBuilder.Build(request, executablePath);
        var trackDirectory = Path.Combine(
            Path.GetFullPath(request.OutputDirectory),
            request.Model,
            Path.GetFileNameWithoutExtension(request.InputPath));

        return await _processRunner.ExecuteAsync(
            new ProcessExecutionRequest
            {
                CommandPlan = plan,
                Timeout = timeout,
                ProducedPaths =
                [
                    Path.Combine(trackDirectory, "vocals.wav"),
                    Path.Combine(trackDirectory, "no_vocals.wav")
                ]
            },
            cancellationToken).ConfigureAwait(false);
    }
}
