using OpenVideoToolbox.Core.Beats;

namespace OpenVideoToolbox.Core.Editing;

using OpenVideoToolbox.Core.Subtitles;

public sealed record EditPlanTemplateRequest
{
    public required string InputPath { get; init; }

    public required string RenderOutputPath { get; init; }

    public TimeSpan? SourceDuration { get; init; }

    public IReadOnlyDictionary<string, string> ParameterOverrides { get; init; } = new Dictionary<string, string>();

    public string? TranscriptPath { get; init; }

    public TranscriptDocument? Transcript { get; init; }

    public bool SeedClipsFromTranscript { get; init; }

    public string? SubtitlePath { get; init; }

    public SubtitleMode? SubtitleModeOverride { get; init; }

    public bool DisableSubtitles { get; init; }

    public string? BeatTrackPath { get; init; }

    public BeatTrackDocument? BeatTrack { get; init; }

    public bool SeedClipsFromBeats { get; init; }

    public int BeatGroupSize { get; init; } = 4;

    public IReadOnlyDictionary<string, string> ArtifactBindings { get; init; } = new Dictionary<string, string>();

    public string? BgmPath { get; init; }
}
