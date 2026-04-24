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
            RecommendedTranscriptSeedStrategies =
            [
                TranscriptSeedStrategy.Grouped,
                TranscriptSeedStrategy.MinDuration
            ],
            SupportingSignals =
            [
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Transcript,
                    Reason = "Use transcript segments when spoken hooks should drive the first short-form cut."
                },
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Beats,
                    Reason = "Use beat markers when the edit should lean on music pacing instead of spoken structure."
                },
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Silence,
                    Reason = "Use silence markers to trim dead air before turning a rough short into a publishable pass."
                }
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
            RecommendedTranscriptSeedStrategies =
            [
                TranscriptSeedStrategy.Grouped,
                TranscriptSeedStrategy.MaxGap
            ],
            SupportingSignals =
            [
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Transcript,
                    Reason = "Use transcript segments to line up caption-first hooks with spoken lines."
                },
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Beats,
                    Reason = "Use beat markers when the short needs rhythm-first pacing but should still keep caption outputs ready."
                },
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Silence,
                    Reason = "Use silence markers to catch long pauses before locking subtitle timing and clip boundaries."
                }
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
            RecommendedTranscriptSeedStrategies =
            [
                TranscriptSeedStrategy.MinDuration
            ],
            SupportingSignals =
            [
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Transcript,
                    Reason = "Use transcript segments to keep commentary cuts aligned with spoken beats instead of ambient pauses."
                },
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Silence,
                    Reason = "Use silence markers to remove dead air before adding background music under the voice track."
                }
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
            Id = "commentary-captioned",
            DisplayName = "Commentary Captioned",
            Description = "Commentary or talking-head cut with captions ready for social publishing and optional BGM support.",
            Category = "commentary",
            DefaultSubtitleMode = SubtitleMode.Sidecar,
            RecommendedSeedModes =
            [
                EditPlanSeedMode.Manual,
                EditPlanSeedMode.Transcript
            ],
            RecommendedTranscriptSeedStrategies =
            [
                TranscriptSeedStrategy.MinDuration,
                TranscriptSeedStrategy.MaxGap
            ],
            SupportingSignals =
            [
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Transcript,
                    Reason = "Use transcript segments when commentary structure and caption timing should stay in sync."
                },
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Silence,
                    Reason = "Use silence markers to collapse awkward pauses before captions and BGM are finalized."
                }
            ],
            ParameterDefaults = new Dictionary<string, string>
            {
                ["narrationPriority"] = "high",
                ["captionStyle"] = "clean-sidecar",
                ["bgmTargetGainDb"] = "-14"
            },
            ArtifactSlots =
            [
                new EditPlanArtifactSlot
                {
                    Id = "subtitles",
                    Kind = "subtitle",
                    Description = "Optional subtitle file for caption-first commentary delivery.",
                    Required = false
                },
                new EditPlanArtifactSlot
                {
                    Id = "bgm",
                    Kind = "audio",
                    Description = "Optional background music file mixed under the commentary track.",
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
            RecommendedTranscriptSeedStrategies =
            [
                TranscriptSeedStrategy.Grouped,
                TranscriptSeedStrategy.MaxGap
            ],
            SupportingSignals =
            [
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Transcript,
                    Reason = "Use transcript segments to preserve explainer structure and keep scene transitions tied to spoken ideas."
                },
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Silence,
                    Reason = "Use silence markers to spot natural chapter breaks or pauses before tutorial captions are rendered."
                }
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
            SupportingSignals =
            [
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Beats,
                    Reason = "Use beat markers as the primary cut scaffold for rhythm-first montage timing."
                },
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Stems,
                    Reason = "Use separated stems when the source music bed needs cleanup or replacement before montage mixing."
                }
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
        },
        new EditPlanTemplateDefinition
        {
            Id = "music-captioned-montage",
            DisplayName = "Music Captioned Montage",
            Description = "Beat-driven montage template with optional lyric or hook captions and a prepared music bed slot.",
            Category = "montage",
            DefaultSubtitleMode = SubtitleMode.Sidecar,
            RecommendedSeedModes =
            [
                EditPlanSeedMode.Manual,
                EditPlanSeedMode.Beats
            ],
            SupportingSignals =
            [
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Beats,
                    Reason = "Use beat markers to keep montage pacing locked to the driving track."
                },
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Stems,
                    Reason = "Use separated stems when lyric captions or hook overlays need a cleaner accompaniment bed."
                }
            ],
            ParameterDefaults = new Dictionary<string, string>
            {
                ["pace"] = "sync-cut",
                ["captionStyle"] = "punchy-hook",
                ["bgmTargetGainDb"] = "-8"
            },
            ArtifactSlots =
            [
                new EditPlanArtifactSlot
                {
                    Id = "subtitles",
                    Kind = "subtitle",
                    Description = "Optional subtitle or lyric file for overlay-ready montage captions.",
                    Required = false
                },
                new EditPlanArtifactSlot
                {
                    Id = "bgm",
                    Kind = "audio",
                    Description = "Optional music bed used as the driving track for the montage.",
                    Required = false
                }
            ]
        },
        new EditPlanTemplateDefinition
        {
            Id = "timeline-effects-starter",
            DisplayName = "Timeline Effects Starter",
            Description = "First schema v2 timeline starter with real clip effects, optional transcript/beat seeding, and an optional BGM track.",
            Category = "timeline",
            PlanModel = EditPlanTemplatePlanModel.V2Timeline,
            RecommendedSeedModes =
            [
                EditPlanSeedMode.Manual,
                EditPlanSeedMode.Transcript,
                EditPlanSeedMode.Beats
            ],
            RecommendedTranscriptSeedStrategies =
            [
                TranscriptSeedStrategy.Grouped,
                TranscriptSeedStrategy.MaxGap
            ],
            SupportingSignals =
            [
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Transcript,
                    Reason = "Use transcript segments when the first v2 timeline pass should follow spoken beats instead of the default demo skeleton."
                },
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Beats,
                    Reason = "Use beat markers when the first v2 timeline pass should inherit rhythm-first pacing from music or motion."
                }
            ],
            ParameterDefaults = new Dictionary<string, string>
            {
                ["look"] = "clean-contrast",
                ["bgmTargetGainDb"] = "-10"
            },
            ArtifactSlots =
            [
                new EditPlanArtifactSlot
                {
                    Id = "bgm",
                    Kind = "audio",
                    Description = "Optional background music file placed on a dedicated v2 timeline audio track.",
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
        => EditPlanTemplateCatalog.Filter(Templates, query);

    public static IReadOnlyList<EditPlanTemplateSummary> GetSummaries(EditPlanTemplateCatalogQuery? query = null)
        => EditPlanTemplateCatalog.GetSummaries(Templates, query);

    public static EditPlanTemplateDefinition GetRequired(string templateId)
        => EditPlanTemplateCatalog.GetRequired(Templates, templateId);
}
