using System.Text.Json;
using System.Text.Json.Serialization;
using OpenVideoToolbox.Core;

namespace OpenVideoToolbox.Core.Editing;

public sealed record EditPlan
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public required EditPlanSource Source { get; init; }

    public EditTemplateReference? Template { get; init; }

    public IReadOnlyList<EditClip> Clips { get; init; } = [];

    public IReadOnlyList<AudioTrackMix> AudioTracks { get; init; } = [];

    public IReadOnlyList<EditArtifactReference> Artifacts { get; init; } = [];

    public EditTranscriptPlan? Transcript { get; init; }

    public EditBeatTrackPlan? Beats { get; init; }

    public EditSubtitlePlan? Subtitles { get; init; }

    // v2: 结构化时间线（可选，存在时渲染引擎走 v2 路径）
    public EditPlanTimeline? Timeline { get; init; }

    public required EditOutputPlan Output { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extensions { get; init; }
}

public sealed record EditPlanSource
{
    public required string InputPath { get; init; }
}

public sealed record EditTemplateReference
{
    public required string Id { get; init; }

    public string? Version { get; init; }

    public EditTemplateSourceReference? Source { get; init; }

    public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();
}

public sealed record EditTemplateSourceReference
{
    public required string Kind { get; init; }

    public string? PluginId { get; init; }

    public string? PluginVersion { get; init; }
}

public static class EditTemplateSourceKinds
{
    public const string BuiltIn = "builtIn";

    public const string Plugin = "plugin";
}

public sealed record EditClip
{
    public required string Id { get; init; }

    [JsonPropertyName("in")]
    public TimeSpan InPoint { get; init; }

    [JsonPropertyName("out")]
    public TimeSpan OutPoint { get; init; }

    public string? Label { get; init; }
}

public sealed record AudioTrackMix
{
    public required string Id { get; init; }

    public AudioTrackRole Role { get; init; }

    public required string Path { get; init; }

    public TimeSpan Start { get; init; }

    public double? GainDb { get; init; }
}

public sealed record EditArtifactReference
{
    public required string SlotId { get; init; }

    public required string Kind { get; init; }

    public required string Path { get; init; }
}

public sealed record EditTranscriptPlan
{
    public required string Path { get; init; }

    public string? Language { get; init; }

    public int? SegmentCount { get; init; }
}

public sealed record EditBeatTrackPlan
{
    public required string Path { get; init; }

    public double? EstimatedBpm { get; init; }
}

public enum AudioTrackRole
{
    Original,
    Voice,
    Bgm,
    Effects
}

public sealed record EditSubtitlePlan
{
    public required string Path { get; init; }

    public SubtitleMode Mode { get; init; } = SubtitleMode.Sidecar;
}

public enum SubtitleMode
{
    Sidecar,
    BurnIn
}

public sealed record EditOutputPlan
{
    public required string Path { get; init; }

    public required string Container { get; init; }
}
