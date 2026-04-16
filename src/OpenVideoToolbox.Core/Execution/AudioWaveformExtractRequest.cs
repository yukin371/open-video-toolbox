namespace OpenVideoToolbox.Core.Execution;

public sealed record AudioWaveformExtractRequest
{
    public required string InputPath { get; init; }

    public required string OutputPath { get; init; }

    public int SampleRateHz { get; init; } = 16000;

    public bool OverwriteExisting { get; init; }
}
