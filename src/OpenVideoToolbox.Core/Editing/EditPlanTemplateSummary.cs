namespace OpenVideoToolbox.Core.Editing;

public sealed record EditPlanTemplateSummary
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string Category { get; init; }

    public EditPlanTemplatePlanModel PlanModel { get; init; } = EditPlanTemplatePlanModel.V1;

    public required string OutputContainer { get; init; }

    public required IReadOnlyList<EditPlanSeedMode> RecommendedSeedModes { get; init; }

    public required IReadOnlyList<string> ArtifactKinds { get; init; }

    public bool HasArtifacts { get; init; }

    public bool HasSubtitles { get; init; }

    public IReadOnlyList<TranscriptSeedStrategy> RecommendedTranscriptSeedStrategies { get; init; } = [];

    public IReadOnlyList<EditPlanSupportingSignalKind> SupportingSignals { get; init; } = [];
}
