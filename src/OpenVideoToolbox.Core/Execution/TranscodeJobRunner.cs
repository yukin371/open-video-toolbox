using OpenVideoToolbox.Core.Jobs;

namespace OpenVideoToolbox.Core.Execution;

public sealed class TranscodeJobRunner
{
    private readonly FfmpegCommandBuilder _commandBuilder;
    private readonly IProcessRunner _processRunner;

    public TranscodeJobRunner(FfmpegCommandBuilder commandBuilder, IProcessRunner processRunner)
    {
        _commandBuilder = commandBuilder;
        _processRunner = processRunner;
    }

    public async Task<ExecutionResult> RunAsync(
        JobDefinition job,
        string executablePath = "ffmpeg",
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        var plan = _commandBuilder.Build(job, executablePath);
        return await _processRunner.ExecuteAsync(
            new ProcessExecutionRequest
            {
                CommandPlan = plan,
                Timeout = timeout,
                ProducedPaths = [FfmpegCommandBuilder.BuildOutputPath(job.Output)]
            },
            cancellationToken).ConfigureAwait(false);
    }
}

