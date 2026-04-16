namespace OpenVideoToolbox.Core.Execution;

public sealed record ExecutionPreview
{
    public required CommandPlan CommandPlan { get; init; }

    public IReadOnlyList<string> ProducedPaths { get; init; } = [];

    public IReadOnlyList<ExecutionSideEffect> SideEffects { get; init; } = [];
}

public sealed record ExecutionSideEffect
{
    public required string Type { get; init; }

    public string? SourcePath { get; init; }

    public string? DestinationPath { get; init; }
}
