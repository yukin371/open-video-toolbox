namespace OpenVideoToolbox.Core.Execution;

public sealed record AudioExtractRequest
{
    public required string InputPath { get; init; }

    public required string OutputPath { get; init; }

    public int TrackIndex { get; init; }

    public bool OverwriteExisting { get; init; }

    public bool CopyCodec { get; init; } = true;
}
