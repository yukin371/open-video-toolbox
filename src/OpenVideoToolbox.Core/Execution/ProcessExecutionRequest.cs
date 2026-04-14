namespace OpenVideoToolbox.Core.Execution;

public sealed record ProcessExecutionRequest
{
    public required CommandPlan CommandPlan { get; init; }

    public TimeSpan? Timeout { get; init; }

    public IReadOnlyList<string> ProducedPaths { get; init; } = [];
}

public interface IProcessRunner
{
    Task<ExecutionResult> ExecuteAsync(ProcessExecutionRequest request, CancellationToken cancellationToken = default);
}

