namespace OpenVideoToolbox.Core.Execution;

public sealed record MediaCutRequest
{
    public required string InputPath { get; init; }

    public required string OutputPath { get; init; }

    public TimeSpan Start { get; init; }

    public TimeSpan End { get; init; }

    public bool OverwriteExisting { get; init; }

    public bool CopyStreams { get; init; } = true;
}
