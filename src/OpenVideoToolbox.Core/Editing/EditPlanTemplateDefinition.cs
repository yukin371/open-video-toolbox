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

    public IReadOnlyList<EditPlanArtifactSlot> ArtifactSlots { get; init; } = [];
}

public enum EditPlanSeedMode
{
    Manual,
    Transcript,
    Beats
}

public sealed record EditPlanArtifactSlot
{
    public required string Id { get; init; }

    public required string Kind { get; init; }

    public required string Description { get; init; }

    public bool Required { get; init; }
}
