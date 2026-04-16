using OpenVideoToolbox.Core;

namespace OpenVideoToolbox.Core.Beats;

public sealed record BeatTrackDocument
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public required string SourcePath { get; init; }

    public int SampleRateHz { get; init; }

    public TimeSpan FrameDuration { get; init; }

    public double? EstimatedBpm { get; init; }

    public IReadOnlyList<BeatMarker> Beats { get; init; } = [];
}

public sealed record BeatMarker
{
    public int Index { get; init; }

    public TimeSpan Time { get; init; }

    public double Strength { get; init; }
}
