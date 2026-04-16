namespace OpenVideoToolbox.Core.Editing;

public static class BuiltInEditPlanTemplateCatalog
{
    private static readonly IReadOnlyList<EditPlanTemplateDefinition> Templates =
    [
        new EditPlanTemplateDefinition
        {
            Id = "shorts-basic",
            DisplayName = "Shorts Basic",
            Description = "Single-track short-form cut with a clean clip skeleton and no subtitle requirement.",
            Category = "short-form",
            RecommendedSeedModes =
            [
                EditPlanSeedMode.Manual,
                EditPlanSeedMode.Transcript,
                EditPlanSeedMode.Beats
            ],
            ParameterDefaults = new Dictionary<string, string>
            {
                ["hookStyle"] = "hard-cut",
                ["pace"] = "fast"
            }
        },
        new EditPlanTemplateDefinition
        {
            Id = "shorts-captioned",
            DisplayName = "Shorts Captioned",
            Description = "Short-form cut with subtitle slot prepared for caption-driven publishing.",
            Category = "short-form",
            DefaultSubtitleMode = SubtitleMode.Sidecar,
            RecommendedSeedModes =
            [
                EditPlanSeedMode.Manual,
                EditPlanSeedMode.Transcript,
                EditPlanSeedMode.Beats
            ],
            ParameterDefaults = new Dictionary<string, string>
            {
                ["hookStyle"] = "hard-cut",
                ["captionStyle"] = "burn-later"
            },
            ArtifactSlots =
            [
                new EditPlanArtifactSlot
                {
                    Id = "subtitles",
                    Kind = "subtitle",
                    Description = "Optional subtitle file to attach or burn in later.",
                    Required = false
                }
            ]
        },
        new EditPlanTemplateDefinition
        {
            Id = "commentary-bgm",
            DisplayName = "Commentary With BGM",
            Description = "Talking-head or commentary cut with a prepared BGM slot for later mixing.",
            Category = "commentary",
            RecommendedSeedModes =
            [
                EditPlanSeedMode.Manual,
                EditPlanSeedMode.Transcript
            ],
            ParameterDefaults = new Dictionary<string, string>
            {
                ["narrationPriority"] = "high",
                ["bgmTargetGainDb"] = "-12"
            },
            ArtifactSlots =
            [
                new EditPlanArtifactSlot
                {
                    Id = "bgm",
                    Kind = "audio",
                    Description = "Optional background music file mixed under the main audio.",
                    Required = false
                }
            ]
        },
        new EditPlanTemplateDefinition
        {
            Id = "explainer-captioned",
            DisplayName = "Explainer Captioned",
            Description = "Transcript-friendly explainer cut with subtitle output prepared for tutorial or walkthrough delivery.",
            Category = "explainer",
            DefaultSubtitleMode = SubtitleMode.Sidecar,
            RecommendedSeedModes =
            [
                EditPlanSeedMode.Manual,
                EditPlanSeedMode.Transcript
            ],
            ParameterDefaults = new Dictionary<string, string>
            {
                ["structure"] = "problem-solution",
                ["captionStyle"] = "clean-sidecar"
            },
            ArtifactSlots =
            [
                new EditPlanArtifactSlot
                {
                    Id = "subtitles",
                    Kind = "subtitle",
                    Description = "Optional subtitle file for tutorial captions or localized sidecar delivery.",
                    Required = false
                }
            ]
        },
        new EditPlanTemplateDefinition
        {
            Id = "beat-montage",
            DisplayName = "Beat Montage",
            Description = "Rhythm-first montage template tuned for beat-group seeding and optional music replacement.",
            Category = "montage",
            RecommendedSeedModes =
            [
                EditPlanSeedMode.Manual,
                EditPlanSeedMode.Beats
            ],
            ParameterDefaults = new Dictionary<string, string>
            {
                ["pace"] = "sync-cut",
                ["bgmTargetGainDb"] = "-10"
            },
            ArtifactSlots =
            [
                new EditPlanArtifactSlot
                {
                    Id = "bgm",
                    Kind = "audio",
                    Description = "Optional music bed used to drive montage energy or replace source backing audio.",
                    Required = false
                }
            ]
        }
    ];

    public static IReadOnlyList<EditPlanTemplateDefinition> GetAll() => Templates;

    public static IReadOnlyList<EditPlanTemplateDefinition> GetAll(string? category, EditPlanSeedMode? seedMode)
        => GetAll(new EditPlanTemplateCatalogQuery
        {
            Category = category,
            SeedMode = seedMode
        });

    public static IReadOnlyList<EditPlanTemplateDefinition> GetAll(EditPlanTemplateCatalogQuery? query)
    {
        IEnumerable<EditPlanTemplateDefinition> candidates = Templates;

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

    public static IReadOnlyList<EditPlanTemplateSummary> GetSummaries(EditPlanTemplateCatalogQuery? query = null)
    {
        return GetAll(query)
            .Select(template => new EditPlanTemplateSummary
            {
                Id = template.Id,
                DisplayName = template.DisplayName,
                Category = template.Category,
                OutputContainer = template.OutputContainer,
                RecommendedSeedModes = template.RecommendedSeedModes.Distinct().ToArray(),
                ArtifactKinds = template.ArtifactSlots
                    .Select(slot => slot.Kind)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(kind => kind, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                HasArtifacts = template.ArtifactSlots.Count > 0,
                HasSubtitles = HasSubtitles(template)
            })
            .ToArray();
    }

    public static EditPlanTemplateDefinition GetRequired(string templateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);

        return Templates.FirstOrDefault(template => string.Equals(template.Id, templateId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Unknown edit plan template '{templateId}'.");
    }

    private static bool HasSubtitles(EditPlanTemplateDefinition template)
    {
        if (template.DefaultSubtitleMode is not null)
        {
            return true;
        }

        return template.ArtifactSlots.Any(slot =>
            string.Equals(slot.Kind, "subtitle", StringComparison.OrdinalIgnoreCase)
            || string.Equals(slot.Id, "subtitles", StringComparison.OrdinalIgnoreCase));
    }
}
