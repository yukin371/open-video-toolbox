// v2 Schema Types — Design Draft

using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenVideoToolbox.Core.Editing;

public sealed record EditPlanTimeline
{
    public TimeSpan? Duration { get; init; }

    public TimelineResolution? Resolution { get; init; }

    public int? FrameRate { get; init; }

    public required IReadOnlyList<TimelineTrack> Tracks { get; init; }
}

public sealed record TimelineResolution
{
    public required int W { get; init; }

    public required int H { get; init; }
}

public sealed record TimelineTrack
{
    public required string Id { get; init; }

    public required TrackKind Kind { get; init; }

    public required IReadOnlyList<TimelineClip> Clips { get; init; }

    public IReadOnlyList<TimelineEffect> Effects { get; init; } = [];

    public bool Muted { get; init; }

    public TrackSlot? Slot { get; init; }
}

public enum TrackKind
{
    Video = 0,
    Audio = 1,
}

public sealed record TrackSlot
{
    public required string Name { get; init; }

    public bool Required { get; init; }

    public string? DefaultAsset { get; init; }
}

public sealed record TimelineClip
{
    public required string Id { get; init; }

    public string? Src { get; init; }

    public TimelineClipPlaceholder? Placeholder { get; init; }

    [JsonPropertyName("in")]
    public TimeSpan? InPoint { get; init; }

    [JsonPropertyName("out")]
    public TimeSpan? OutPoint { get; init; }

    public TimeSpan Start { get; init; }

    public TimeSpan? Duration { get; init; }

    public IReadOnlyList<TimelineEffect> Effects { get; init; } = [];

    public ClipTransitions? Transitions { get; init; }

    public string? Slot { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extensions { get; init; }
}

public sealed record TimelineClipPlaceholder
{
    public required string Kind { get; init; }

    public string? Color { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extensions { get; init; }
}

public sealed record ClipTransitions
{
    public Transition? In { get; init; }

    [JsonPropertyName("out")]
    public Transition? Out { get; init; }
}

public sealed record Transition
{
    public required string Type { get; init; }

    public required double Duration { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extensions { get; init; }
}

public sealed record TimelineEffect
{
    public required string Type { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extensions { get; init; }
}
