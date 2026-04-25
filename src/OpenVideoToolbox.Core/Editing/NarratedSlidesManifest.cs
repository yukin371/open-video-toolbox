namespace OpenVideoToolbox.Core.Editing;

public sealed record NarratedSlidesManifest
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public IReadOnlyDictionary<string, string> Variables { get; init; } = new Dictionary<string, string>();

    public required NarratedSlidesVideoManifest Video { get; init; }

    public NarratedSlidesTemplateManifest? Template { get; init; }

    public NarratedSlidesSubtitleManifest? Subtitles { get; init; }

    public NarratedSlidesBgmManifest? Bgm { get; init; }

    public IReadOnlyList<NarratedSlidesSectionManifest> Sections { get; init; } = [];
}

public sealed record NarratedSlidesVideoManifest
{
    public string? Id { get; init; }

    public string? Title { get; init; }

    public string? AspectRatio { get; init; }

    public NarratedSlidesResolutionManifest? Resolution { get; init; }

    public int? FrameRate { get; init; }

    public string? Output { get; init; }

    public NarratedSlidesProgressBarManifest? ProgressBar { get; init; }
}

public sealed record NarratedSlidesResolutionManifest
{
    public required int W { get; init; }

    public required int H { get; init; }
}

public sealed record NarratedSlidesTemplateManifest
{
    public required string Id { get; init; }
}

public sealed record NarratedSlidesProgressBarManifest
{
    public bool Enabled { get; init; } = true;

    public int? Height { get; init; }

    public int? Margin { get; init; }

    public string? Color { get; init; }

    public string? BackgroundColor { get; init; }
}

public sealed record NarratedSlidesSubtitleManifest
{
    public required string Path { get; init; }

    public SubtitleMode Mode { get; init; } = SubtitleMode.Sidecar;
}

public sealed record NarratedSlidesBgmManifest
{
    public required string Path { get; init; }

    public double GainDb { get; init; } = -18;
}

public sealed record NarratedSlidesSectionManifest
{
    public required string Id { get; init; }

    public string? Title { get; init; }

    public required NarratedSlidesVisualManifest Visual { get; init; }

    public required NarratedSlidesVoiceManifest Voice { get; init; }
}

public sealed record NarratedSlidesVisualManifest
{
    public required string Kind { get; init; }

    public required string Path { get; init; }

    public int? DurationMs { get; init; }
}

public sealed record NarratedSlidesVoiceManifest
{
    public required string Path { get; init; }

    public int? DurationMs { get; init; }
}

public sealed record NarratedSlidesResolvedSection
{
    public required string Id { get; init; }

    public string? Title { get; init; }

    public required string VisualPath { get; init; }

    public required TimeSpan VisualDuration { get; init; }

    public required string VoicePath { get; init; }

    public required TimeSpan VoiceDuration { get; init; }
}

public sealed record NarratedSlidesPlanBuildRequest
{
    public required NarratedSlidesManifest Manifest { get; init; }

    public required string TemplateId { get; init; }

    public required string RenderOutputPath { get; init; }

    public required IReadOnlyList<NarratedSlidesResolvedSection> Sections { get; init; }

    public string? SubtitlePath { get; init; }

    public SubtitleMode? SubtitleMode { get; init; }

    public string? BgmPath { get; init; }

    public double BgmGainDb { get; init; } = -18;
}

public sealed record NarratedSlidesPlanBuildStats
{
    public required int SectionCount { get; init; }

    public required TimeSpan TotalDuration { get; init; }

    public required bool HasSubtitles { get; init; }

    public required bool HasBgm { get; init; }
}

public sealed record NarratedSlidesPlanBuildResult
{
    public required EditPlan Plan { get; init; }

    public required NarratedSlidesPlanBuildStats Stats { get; init; }
}
