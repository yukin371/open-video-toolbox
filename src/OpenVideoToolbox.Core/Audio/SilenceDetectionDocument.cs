using OpenVideoToolbox.Core;

namespace OpenVideoToolbox.Core.Audio;

public sealed record SilenceDetectionDocument
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public required string InputPath { get; init; }

    public IReadOnlyList<SilenceSegment> Segments { get; init; } = [];
}

public sealed record SilenceSegment
{
    public TimeSpan Start { get; init; }

    public TimeSpan End { get; init; }

    public TimeSpan Duration { get; init; }
}
