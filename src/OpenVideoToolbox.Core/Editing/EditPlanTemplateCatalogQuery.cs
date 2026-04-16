namespace OpenVideoToolbox.Core.Editing;

public sealed record EditPlanTemplateCatalogQuery
{
    public string? Category { get; init; }

    public EditPlanSeedMode? SeedMode { get; init; }

    public string? OutputContainer { get; init; }

    public string? ArtifactKind { get; init; }

    public bool? HasArtifacts { get; init; }

    public bool? HasSubtitles { get; init; }
}
