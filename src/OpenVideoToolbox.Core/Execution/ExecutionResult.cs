using OpenVideoToolbox.Core;

namespace OpenVideoToolbox.Core.Execution;

public sealed record ExecutionResult
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public required ExecutionStatus Status { get; init; }

    public int? ExitCode { get; init; }

    public DateTimeOffset StartedAtUtc { get; init; }

    public DateTimeOffset FinishedAtUtc { get; init; }

    public TimeSpan Duration { get; init; }

    public required CommandPlan CommandPlan { get; init; }

    public IReadOnlyList<ProcessOutputLine> OutputLines { get; init; } = [];

    public string? ErrorMessage { get; init; }

    public IReadOnlyList<string> ProducedPaths { get; init; } = [];
}

public enum ExecutionStatus
{
    Succeeded,
    Failed,
    Cancelled,
    TimedOut
}

public sealed record ProcessOutputLine
{
    public DateTimeOffset TimestampUtc { get; init; }

    public ProcessOutputChannel Channel { get; init; }

    public bool IsError { get; init; }

    public required string Text { get; init; }
}

public enum ProcessOutputChannel
{
    StandardOutput,
    StandardError
}
