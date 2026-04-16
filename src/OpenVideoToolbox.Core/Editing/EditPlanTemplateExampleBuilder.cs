namespace OpenVideoToolbox.Core.Editing;

using OpenVideoToolbox.Core.Beats;
using OpenVideoToolbox.Core.Subtitles;

public static class EditPlanTemplateExampleBuilder
{
    public static IReadOnlyDictionary<string, string> BuildArtifactBindingsExample(EditPlanTemplateDefinition template)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (template.ArtifactSlots.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var bindings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var slot in template.ArtifactSlots)
        {
            bindings[slot.Id] = BuildArtifactExamplePath(slot);
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
                EditPlan = factory.Create(template.Id, request)
            });
        }

        return previews;
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

    private static string BuildArtifactExamplePath(EditPlanArtifactSlot slot)
    {
        return slot.Kind.ToLowerInvariant() switch
        {
            "subtitle" => "subtitles.srt",
            "audio" => "audio/input.wav",
            _ => $"<{slot.Id}-path>"
        };
    }
}

public sealed record EditPlanTemplatePreview
{
    public required EditPlanSeedMode Mode { get; init; }

    public required EditPlan EditPlan { get; init; }
}
