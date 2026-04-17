namespace OpenVideoToolbox.Core.Editing;

using OpenVideoToolbox.Core.Beats;
using OpenVideoToolbox.Core.Subtitles;

public static class EditPlanTemplateExampleBuilder
{
    public static IReadOnlyList<EditPlanSupportingSignalExample> BuildSupportingSignalExamples(EditPlanTemplateDefinition template)
    {
        ArgumentNullException.ThrowIfNull(template);

        return template.SupportingSignals
            .Select(signal => BuildSupportingSignalExample(signal))
            .ToArray();
    }

    public static IReadOnlyDictionary<string, string> BuildArtifactBindingsExample(EditPlanTemplateDefinition template)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (template.ArtifactSlots.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var supportsStems = template.SupportingSignals.Any(signal => signal.Kind == EditPlanSupportingSignalKind.Stems);
        var bindings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var slot in template.ArtifactSlots)
        {
            bindings[slot.Id] = BuildArtifactExamplePath(slot, supportsStems);
        }

        return bindings;
    }

    public static IReadOnlyDictionary<string, string> BuildTemplateParamsExample(EditPlanTemplateDefinition template)
    {
        ArgumentNullException.ThrowIfNull(template);

        return new Dictionary<string, string>(template.ParameterDefaults, StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<EditPlanTemplatePreview> BuildPreviewPlans(EditPlanTemplateDefinition template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var previews = new List<EditPlanTemplatePreview>();
        var artifacts = BuildArtifactBindingsExample(template);
        var templateParams = BuildTemplateParamsExample(template);
        var factory = new EditPlanTemplateFactory();

        foreach (var mode in template.RecommendedSeedModes.Distinct())
        {
            var transcript = mode == EditPlanSeedMode.Transcript ? BuildTranscriptExample() : null;
            var beats = mode == EditPlanSeedMode.Beats ? BuildBeatTrackExample() : null;
            var request = new EditPlanTemplateRequest
            {
                InputPath = "input.mp4",
                RenderOutputPath = $"final.{template.OutputContainer}",
                ParameterOverrides = templateParams,
                ArtifactBindings = artifacts,
                TranscriptPath = transcript is null ? null : "transcript.json",
                Transcript = transcript,
                SeedClipsFromTranscript = mode == EditPlanSeedMode.Transcript,
                BeatTrackPath = beats is null ? null : "beats.json",
                BeatTrack = beats,
                SeedClipsFromBeats = mode == EditPlanSeedMode.Beats,
                BeatGroupSize = 4
            };

            previews.Add(new EditPlanTemplatePreview
            {
                Mode = mode,
                EditPlan = factory.Create(template.Id, request),
                StrategyVariants = mode == EditPlanSeedMode.Transcript
                    ? BuildTranscriptStrategyVariantPreviews(template, artifacts, templateParams, factory)
                    : []
            });
        }

        return previews;
    }

    private static IReadOnlyList<EditPlanTemplateStrategyPreview> BuildTranscriptStrategyVariantPreviews(
        EditPlanTemplateDefinition template,
        IReadOnlyDictionary<string, string> artifacts,
        IReadOnlyDictionary<string, string> templateParams,
        EditPlanTemplateFactory factory)
    {
        EditPlanTemplateStrategyPreview[] variants =
        [
            new EditPlanTemplateStrategyPreview
            {
                Key = "grouped",
                Description = "Use a fixed transcript segment group size to merge adjacent lines into fewer seed clips.",
                Strategy = TranscriptSeedStrategy.Grouped,
                EditPlan = factory.Create(
                    template.Id,
                    new EditPlanTemplateRequest
                    {
                        InputPath = "input.mp4",
                        RenderOutputPath = $"final.{template.OutputContainer}",
                        ParameterOverrides = templateParams,
                        ArtifactBindings = artifacts,
                        TranscriptPath = "transcript.json",
                        Transcript = BuildGroupedTranscriptExample(),
                        SeedClipsFromTranscript = true,
                        TranscriptSegmentGroupSize = 2
                    })
            },
            new EditPlanTemplateStrategyPreview
            {
                Key = "min-duration",
                Description = "Filter out very short transcript segments before clip seeding to avoid noisy micro-cuts.",
                Strategy = TranscriptSeedStrategy.MinDuration,
                EditPlan = factory.Create(
                    template.Id,
                    new EditPlanTemplateRequest
                    {
                        InputPath = "input.mp4",
                        RenderOutputPath = $"final.{template.OutputContainer}",
                        ParameterOverrides = templateParams,
                        ArtifactBindings = artifacts,
                        TranscriptPath = "transcript.json",
                        Transcript = BuildMinDurationTranscriptExample(),
                        SeedClipsFromTranscript = true,
                        TranscriptSegmentGroupSize = 2,
                        MinTranscriptSegmentDuration = TimeSpan.FromMilliseconds(500)
                    })
            },
            new EditPlanTemplateStrategyPreview
            {
                Key = "max-gap",
                Description = "Split transcript seed clips when adjacent segments have a large silence gap.",
                Strategy = TranscriptSeedStrategy.MaxGap,
                EditPlan = factory.Create(
                    template.Id,
                    new EditPlanTemplateRequest
                    {
                        InputPath = "input.mp4",
                        RenderOutputPath = $"final.{template.OutputContainer}",
                        ParameterOverrides = templateParams,
                        ArtifactBindings = artifacts,
                        TranscriptPath = "transcript.json",
                        Transcript = BuildGapTranscriptExample(),
                        SeedClipsFromTranscript = true,
                        TranscriptSegmentGroupSize = 3,
                        MaxTranscriptGap = TimeSpan.FromMilliseconds(200)
                    })
            }
        ];

        return OrderTranscriptStrategyVariants(template, variants);
    }

    private static IReadOnlyList<EditPlanTemplateStrategyPreview> OrderTranscriptStrategyVariants(
        EditPlanTemplateDefinition template,
        IReadOnlyList<EditPlanTemplateStrategyPreview> variants)
    {
        if (template.RecommendedTranscriptSeedStrategies.Count == 0)
        {
            return variants;
        }

        var rankedStrategies = template.RecommendedTranscriptSeedStrategies
            .Select((strategy, index) => new { strategy, index })
            .ToDictionary(item => item.strategy, item => item.index);

        return variants
            .OrderBy(variant => rankedStrategies.TryGetValue(variant.Strategy, out var rank) ? rank : int.MaxValue)
            .ThenBy(variant => variant.Key, StringComparer.Ordinal)
            .Select(variant => variant with
            {
                IsRecommended = rankedStrategies.ContainsKey(variant.Strategy)
            })
            .ToArray();
    }

    private static TranscriptDocument BuildTranscriptExample()
    {
        return new TranscriptDocument
        {
            Language = "en",
            Segments =
            [
                new TranscriptSegment
                {
                    Id = "seg-001",
                    Start = TimeSpan.Zero,
                    End = TimeSpan.FromSeconds(1.5),
                    Text = "Example opening line"
                },
                new TranscriptSegment
                {
                    Id = "seg-002",
                    Start = TimeSpan.FromSeconds(1.5),
                    End = TimeSpan.FromSeconds(3),
                    Text = "Example follow-up line"
                }
            ]
        };
    }

    private static TranscriptDocument BuildGroupedTranscriptExample()
    {
        return new TranscriptDocument
        {
            Language = "en",
            Segments =
            [
                new TranscriptSegment
                {
                    Id = "seg-001",
                    Start = TimeSpan.Zero,
                    End = TimeSpan.FromSeconds(1),
                    Text = "Grouped example intro"
                },
                new TranscriptSegment
                {
                    Id = "seg-002",
                    Start = TimeSpan.FromSeconds(1),
                    End = TimeSpan.FromSeconds(2.5),
                    Text = "Grouped example detail"
                },
                new TranscriptSegment
                {
                    Id = "seg-003",
                    Start = TimeSpan.FromSeconds(2.5),
                    End = TimeSpan.FromSeconds(4),
                    Text = "Grouped example wrap"
                }
            ]
        };
    }

    private static TranscriptDocument BuildMinDurationTranscriptExample()
    {
        return new TranscriptDocument
        {
            Language = "en",
            Segments =
            [
                new TranscriptSegment
                {
                    Id = "seg-001",
                    Start = TimeSpan.Zero,
                    End = TimeSpan.FromMilliseconds(250),
                    Text = "Too short"
                },
                new TranscriptSegment
                {
                    Id = "seg-002",
                    Start = TimeSpan.FromMilliseconds(250),
                    End = TimeSpan.FromSeconds(1),
                    Text = "Keep"
                },
                new TranscriptSegment
                {
                    Id = "seg-003",
                    Start = TimeSpan.FromSeconds(1),
                    End = TimeSpan.FromSeconds(2),
                    Text = "Keep too"
                }
            ]
        };
    }

    private static TranscriptDocument BuildGapTranscriptExample()
    {
        return new TranscriptDocument
        {
            Language = "en",
            Segments =
            [
                new TranscriptSegment
                {
                    Id = "seg-001",
                    Start = TimeSpan.Zero,
                    End = TimeSpan.FromMilliseconds(600),
                    Text = "First"
                },
                new TranscriptSegment
                {
                    Id = "seg-002",
                    Start = TimeSpan.FromMilliseconds(750),
                    End = TimeSpan.FromSeconds(1.3),
                    Text = "Near"
                },
                new TranscriptSegment
                {
                    Id = "seg-003",
                    Start = TimeSpan.FromSeconds(2),
                    End = TimeSpan.FromSeconds(2.8),
                    Text = "Far"
                }
            ]
        };
    }

    private static BeatTrackDocument BuildBeatTrackExample()
    {
        return new BeatTrackDocument
        {
            SourcePath = "input.mp4",
            SampleRateHz = 16000,
            FrameDuration = TimeSpan.FromMilliseconds(50),
            EstimatedBpm = 120,
            Beats =
            [
                new BeatMarker { Index = 0, Time = TimeSpan.Zero, Strength = 0.9 },
                new BeatMarker { Index = 1, Time = TimeSpan.FromSeconds(1), Strength = 0.88 },
                new BeatMarker { Index = 2, Time = TimeSpan.FromSeconds(2), Strength = 0.91 },
                new BeatMarker { Index = 3, Time = TimeSpan.FromSeconds(3), Strength = 0.87 },
                new BeatMarker { Index = 4, Time = TimeSpan.FromSeconds(4), Strength = 0.9 }
            ]
        };
    }

    private static EditPlanSupportingSignalExample BuildSupportingSignalExample(EditPlanSupportingSignalHint signal)
    {
        return signal.Kind switch
        {
            EditPlanSupportingSignalKind.Transcript => new EditPlanSupportingSignalExample
            {
                Kind = signal.Kind,
                Reason = signal.Reason,
                OutputPath = "transcript.json",
                Command = "ovt transcribe <input> --model <path> --output transcript.json",
                Consumption = "Pass --transcript transcript.json to init-plan and enable --seed-from-transcript when dialogue should drive the first clip layout."
            },
            EditPlanSupportingSignalKind.Beats => new EditPlanSupportingSignalExample
            {
                Kind = signal.Kind,
                Reason = signal.Reason,
                OutputPath = "beats.json",
                Command = "ovt beat-track <input> --output beats.json",
                Consumption = "Pass --beats beats.json to init-plan and enable --seed-from-beats when rhythm should shape the initial clip pacing."
            },
            EditPlanSupportingSignalKind.Silence => new EditPlanSupportingSignalExample
            {
                Kind = signal.Kind,
                Reason = signal.Reason,
                OutputPath = "silence.json",
                Command = "ovt detect-silence <input> --output silence.json",
                Consumption = "Review silence.json before hand-tuning edit.json clip boundaries or transcript seed settings so long pauses do not survive into the first cut."
            },
            EditPlanSupportingSignalKind.Stems => new EditPlanSupportingSignalExample
            {
                Kind = signal.Kind,
                Reason = signal.Reason,
                OutputPath = "stems/",
                Command = "ovt separate-audio <input> --output-dir stems",
                Consumption = "After scaffold-template writes artifacts.json, point the bgm slot at stems/htdemucs/input/no_vocals.wav when the accompaniment stem should drive the mix; keep vocals.wav as a cleanup or reference stem."
            },
            _ => throw new InvalidOperationException($"Unsupported supporting signal kind '{signal.Kind}'.")
        };
    }

    private static string BuildArtifactExamplePath(EditPlanArtifactSlot slot, bool supportsStems)
    {
        return slot.Kind.ToLowerInvariant() switch
        {
            "subtitle" => "subtitles.srt",
            "audio" when supportsStems && string.Equals(slot.Id, "bgm", StringComparison.OrdinalIgnoreCase)
                => "stems/htdemucs/input/no_vocals.wav",
            "audio" => "audio/input.wav",
            _ => $"<{slot.Id}-path>"
        };
    }
}

public sealed record EditPlanTemplatePreview
{
    public required EditPlanSeedMode Mode { get; init; }

    public required EditPlan EditPlan { get; init; }

    public IReadOnlyList<EditPlanTemplateStrategyPreview> StrategyVariants { get; init; } = [];
}

public sealed record EditPlanTemplateStrategyPreview
{
    public required string Key { get; init; }

    public required string Description { get; init; }

    public required TranscriptSeedStrategy Strategy { get; init; }

    public bool IsRecommended { get; init; }

    public required EditPlan EditPlan { get; init; }
}

public sealed record EditPlanSupportingSignalExample
{
    public required EditPlanSupportingSignalKind Kind { get; init; }

    public required string Reason { get; init; }

    public required string OutputPath { get; init; }

    public required string Command { get; init; }

    public required string Consumption { get; init; }
}
