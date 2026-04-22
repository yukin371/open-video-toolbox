namespace OpenVideoToolbox.Core.Execution;

public sealed record AudioNormalizeRequest
{
    public const double DefaultTargetLufs = -16;
    public const double DefaultLoudnessRangeTarget = 11;
    public const double DefaultTruePeakDb = -1.5;

    public required string InputPath { get; init; }

    public required string OutputPath { get; init; }

    public double TargetLufs { get; init; } = DefaultTargetLufs;

    public double LoudnessRangeTarget { get; init; } = DefaultLoudnessRangeTarget;

    public double TruePeakDb { get; init; } = DefaultTruePeakDb;

    public bool OverwriteExisting { get; init; }
}
