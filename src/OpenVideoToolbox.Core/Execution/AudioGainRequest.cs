namespace OpenVideoToolbox.Core.Execution;

public sealed record AudioGainRequest
{
    public required string InputPath { get; init; }

    public required string OutputPath { get; init; }

    public double GainDb { get; init; }

    public bool OverwriteExisting { get; init; }
}
