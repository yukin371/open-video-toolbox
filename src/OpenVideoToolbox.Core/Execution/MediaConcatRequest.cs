namespace OpenVideoToolbox.Core.Execution;

public sealed record MediaConcatRequest
{
    public required string InputListPath { get; init; }

    public required string OutputPath { get; init; }

    public bool OverwriteExisting { get; init; }

    public bool CopyStreams { get; init; } = true;
}
