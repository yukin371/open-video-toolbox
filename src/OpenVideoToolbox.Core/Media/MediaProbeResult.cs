using OpenVideoToolbox.Core;

namespace OpenVideoToolbox.Core.Media;

public sealed record MediaProbeResult
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public required string SourcePath { get; init; }

    public required string FileName { get; init; }

    public required MediaFormatInfo Format { get; init; }

    public IReadOnlyList<MediaStreamInfo> Streams { get; init; } = [];
}

public sealed record MediaFormatInfo
{
    public string? ContainerName { get; init; }

    public string? ContainerLongName { get; init; }

    public TimeSpan? Duration { get; init; }

    public long? SizeBytes { get; init; }

    public long? Bitrate { get; init; }
}

public sealed record MediaStreamInfo
{
    public int Index { get; init; }

    public MediaStreamKind Kind { get; init; }

    public string? CodecName { get; init; }

    public string? CodecLongName { get; init; }

    public string? Language { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public double? FrameRate { get; init; }

    public int? Channels { get; init; }

    public int? SampleRate { get; init; }

    public string? ChannelLayout { get; init; }

    public long? Bitrate { get; init; }

    public TimeSpan? Duration { get; init; }
}

public enum MediaStreamKind
{
    Unknown,
    Video,
    Audio,
    Subtitle,
    Data,
    Attachment
}
