namespace OpenVideoToolbox.Core.Execution;

public sealed record SilenceDetectionRequest
{
    public required string InputPath { get; init; }

    public double NoiseDb { get; init; } = -30;

    public TimeSpan MinimumDuration { get; init; } = TimeSpan.FromSeconds(0.5);
}
