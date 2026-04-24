namespace OpenVideoToolbox.Core.Editing;

public static class EditPlanTemplateCatalog
{
    public static IReadOnlyList<EditPlanTemplateDefinition> Filter(
        IReadOnlyList<EditPlanTemplateDefinition> templates,
        EditPlanTemplateCatalogQuery? query = null)
    {
        ArgumentNullException.ThrowIfNull(templates);

        IEnumerable<EditPlanTemplateDefinition> candidates = templates;

        if (!string.IsNullOrWhiteSpace(query?.Category))
        {
            candidates = candidates.Where(template => string.Equals(template.Category, query.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (query?.SeedMode is not null)
        {
            candidates = candidates.Where(template => template.RecommendedSeedModes.Contains(query.SeedMode.Value));
        }

        if (!string.IsNullOrWhiteSpace(query?.OutputContainer))
        {
            candidates = candidates.Where(template => string.Equals(template.OutputContainer, query.OutputContainer, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query?.ArtifactKind))
        {
            candidates = candidates.Where(template => template.ArtifactSlots.Any(
                slot => string.Equals(slot.Kind, query.ArtifactKind, StringComparison.OrdinalIgnoreCase)));
        }

        if (query?.HasArtifacts is not null)
        {
            candidates = candidates.Where(template => template.ArtifactSlots.Count > 0 == query.HasArtifacts.Value);
        }

        if (query?.HasSubtitles is not null)
        {
            candidates = candidates.Where(template => HasSubtitles(template) == query.HasSubtitles.Value);
        }

        return candidates.ToArray();
    }

    public static IReadOnlyList<EditPlanTemplateSummary> GetSummaries(
        IReadOnlyList<EditPlanTemplateDefinition> templates,
        EditPlanTemplateCatalogQuery? query = null)
    {
        ArgumentNullException.ThrowIfNull(templates);

        return Filter(templates, query)
            .Select(template => new EditPlanTemplateSummary
            {
                Id = template.Id,
                DisplayName = template.DisplayName,
                Category = template.Category,
                PlanModel = template.PlanModel,
                OutputContainer = template.OutputContainer,
                RecommendedSeedModes = template.RecommendedSeedModes.Distinct().ToArray(),
                ArtifactKinds = template.ArtifactSlots
                    .Select(slot => slot.Kind)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(kind => kind, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                HasArtifacts = template.ArtifactSlots.Count > 0,
                HasSubtitles = HasSubtitles(template),
                RecommendedTranscriptSeedStrategies = template.RecommendedTranscriptSeedStrategies.ToArray(),
                SupportingSignals = template.SupportingSignals
                    .Select(signal => signal.Kind)
                    .Distinct()
                    .ToArray()
            })
            .ToArray();
    }

    public static EditPlanTemplateDefinition GetRequired(
        IReadOnlyList<EditPlanTemplateDefinition> templates,
        string templateId)
    {
        ArgumentNullException.ThrowIfNull(templates);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);

        var matches = templates
            .Where(template => string.Equals(template.Id, templateId, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();

        return matches.Length switch
        {
            0 => throw new InvalidOperationException($"Unknown edit plan template '{templateId}'."),
            > 1 => throw new InvalidOperationException($"Duplicate edit plan template id '{templateId}'."),
            _ => matches[0]
        };
    }

    public static bool HasSubtitles(EditPlanTemplateDefinition template)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (template.DefaultSubtitleMode is not null)
        {
            return true;
        }

        return template.ArtifactSlots.Any(slot =>
            string.Equals(slot.Kind, "subtitle", StringComparison.OrdinalIgnoreCase)
            || string.Equals(slot.Id, "subtitles", StringComparison.OrdinalIgnoreCase));
    }
}
