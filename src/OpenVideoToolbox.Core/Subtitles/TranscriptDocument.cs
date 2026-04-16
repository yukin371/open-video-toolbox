using OpenVideoToolbox.Core;

namespace OpenVideoToolbox.Core.Subtitles;

public sealed record TranscriptDocument
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public string? Language { get; init; }

    public IReadOnlyList<TranscriptSegment> Segments { get; init; } = [];
}

public sealed record TranscriptSegment
{
    public string? Id { get; init; }

    public TimeSpan Start { get; init; }

    public TimeSpan End { get; init; }

    public required string Text { get; init; }

    public string? Speaker { get; init; }
}
