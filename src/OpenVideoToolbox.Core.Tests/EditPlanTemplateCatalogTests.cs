using OpenVideoToolbox.Core.Editing;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class EditPlanTemplateCatalogTests
{
    [Fact]
    public void GetRequired_ThrowsWhenTemplateIdsAreDuplicated()
    {
        var templates = new[]
        {
            CreateTemplate("plugin-captioned", "Plugin Captioned"),
            CreateTemplate("plugin-captioned", "Plugin Captioned Copy")
        };

        var exception = Assert.Throws<InvalidOperationException>(() => EditPlanTemplateCatalog.GetRequired(templates, "plugin-captioned"));

        Assert.Contains("Duplicate edit plan template id 'plugin-captioned'.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetSummaries_CanFilterArbitraryTemplateCollections()
    {
        var templates = new[]
        {
            CreateTemplate(
                "plugin-captioned",
                "Plugin Captioned",
                subtitles: true,
                artifactKind: "subtitle",
                supportingSignals: [EditPlanSupportingSignalKind.Transcript]),
            CreateTemplate(
                "plugin-bgm",
                "Plugin Bgm",
                seedModes: [EditPlanSeedMode.Manual, EditPlanSeedMode.Beats],
                artifactKind: "audio",
                supportingSignals: [EditPlanSupportingSignalKind.Beats, EditPlanSupportingSignalKind.Stems])
        };

        var summary = Assert.Single(EditPlanTemplateCatalog.GetSummaries(
            templates,
            new EditPlanTemplateCatalogQuery
            {
                Category = "plugin",
                HasSubtitles = true,
                SeedMode = EditPlanSeedMode.Transcript
            }));

        Assert.Equal("plugin-captioned", summary.Id);
        Assert.True(summary.HasArtifacts);
        Assert.True(summary.HasSubtitles);
        Assert.Contains("subtitle", summary.ArtifactKinds);
        Assert.True(summary.SupportingSignals.SequenceEqual([EditPlanSupportingSignalKind.Transcript]));
    }

    private static EditPlanTemplateDefinition CreateTemplate(
        string id,
        string displayName,
        bool subtitles = false,
        string? artifactKind = null,
        IReadOnlyList<EditPlanSeedMode>? seedModes = null,
        IReadOnlyList<EditPlanSupportingSignalKind>? supportingSignals = null)
    {
        return new EditPlanTemplateDefinition
        {
            Id = id,
            DisplayName = displayName,
            Description = $"Description for {displayName}",
            Category = "plugin",
            OutputContainer = "mp4",
            DefaultSubtitleMode = subtitles ? SubtitleMode.Sidecar : null,
            RecommendedSeedModes = seedModes ?? [EditPlanSeedMode.Manual, EditPlanSeedMode.Transcript],
            ArtifactSlots = artifactKind is null
                ? []
                :
                [
                    new EditPlanArtifactSlot
                    {
                        Id = artifactKind,
                        Kind = artifactKind,
                        Description = $"{artifactKind} artifact",
                        Required = false
                    }
                ],
            SupportingSignals = supportingSignals?.Select(kind => new EditPlanSupportingSignalHint
            {
                Kind = kind,
                Reason = $"Reason for {kind}"
            }).ToArray() ?? []
        };
    }
}
