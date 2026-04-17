using OpenVideoToolbox.Core;

namespace OpenVideoToolbox.Core.Audio;

public sealed record AudioAnalysisDocument
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public required string InputPath { get; init; }

    public required AudioAnalysisMetrics Analysis { get; init; }
}

public sealed record AudioAnalysisMetrics
{
    public double? IntegratedLoudness { get; init; }

    public double? LoudnessRange { get; init; }

    public double? TruePeakDb { get; init; }

    public double? ThresholdDb { get; init; }

    public double? TargetOffset { get; init; }
}
