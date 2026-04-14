using OpenVideoToolbox.Core;

namespace OpenVideoToolbox.Core.Execution;

public sealed record CommandPlan
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public required string ToolName { get; init; }

    public required string ExecutablePath { get; init; }

    public string? WorkingDirectory { get; init; }

    public IReadOnlyList<string> Arguments { get; init; } = [];

    public required string CommandLine { get; init; }

    public string? ResponseFilePath { get; init; }
}
