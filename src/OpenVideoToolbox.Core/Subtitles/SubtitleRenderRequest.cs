namespace OpenVideoToolbox.Core.Subtitles;

public sealed record SubtitleRenderRequest
{
    public required TranscriptDocument Transcript { get; init; }

    public required string OutputPath { get; init; }

    public SubtitleFormat Format { get; init; }

    public int MaxLineLength { get; init; } = 24;
}

public sealed record SubtitleRenderResult
{
    public required string OutputPath { get; init; }

    public required SubtitleFormat Format { get; init; }

    public required string Content { get; init; }

    public int SegmentCount { get; init; }

    public int MaxLineLength { get; init; }
}

public enum SubtitleFormat
{
    Srt,
    Ass
}
