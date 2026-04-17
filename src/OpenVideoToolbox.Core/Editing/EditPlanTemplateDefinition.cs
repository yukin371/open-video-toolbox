namespace OpenVideoToolbox.Core.Editing;

public sealed record EditPlanTemplateDefinition
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string Description { get; init; }

    public required string Category { get; init; }

    public string Version { get; init; } = "1.0.0";

    public string OutputContainer { get; init; } = "mp4";

    public SubtitleMode? DefaultSubtitleMode { get; init; }

    public IReadOnlyDictionary<string, string> ParameterDefaults { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<EditPlanSeedMode> RecommendedSeedModes { get; init; } = [EditPlanSeedMode.Manual];

    public IReadOnlyList<TranscriptSeedStrategy> RecommendedTranscriptSeedStrategies { get; init; } = [];

    public IReadOnlyList<EditPlanArtifactSlot> ArtifactSlots { get; init; } = [];

    public IReadOnlyList<EditPlanSupportingSignalHint> SupportingSignals { get; init; } = [];
}

public enum EditPlanSeedMode
{
    Manual,
    Transcript,
    Beats
}

public enum TranscriptSeedStrategy
{
    Grouped,
    MinDuration,
    MaxGap
}

public enum EditPlanSupportingSignalKind
{
    Transcript,
    Beats,
    Silence,
    Stems
}

public sealed record EditPlanSupportingSignalHint
{
    public required EditPlanSupportingSignalKind Kind { get; init; }

    public required string Reason { get; init; }
}

public sealed record EditPlanArtifactSlot
{
    public required string Id { get; init; }

    public required string Kind { get; init; }

    public required string Description { get; init; }

    public bool Required { get; init; }
}
