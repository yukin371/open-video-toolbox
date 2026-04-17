using System.Text.Json;
using OpenVideoToolbox.Core.Beats;
using OpenVideoToolbox.Core.Serialization;
using OpenVideoToolbox.Core.Subtitles;

namespace OpenVideoToolbox.Core.Editing;

public sealed class EditPlanTemplateFactory
{
    public EditPlan Create(string templateId, EditPlanTemplateRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);
        ArgumentNullException.ThrowIfNull(request);

        var template = BuiltInEditPlanTemplateCatalog.GetRequired(templateId);
        var artifactBindings = BuildArtifactBindings(template, request);
        var subtitleMode = request.DisableSubtitles
            ? null
            : request.SubtitleModeOverride ?? template.DefaultSubtitleMode;
        var container = Path.GetExtension(request.RenderOutputPath);
        if (container.StartsWith(".", StringComparison.Ordinal))
        {
            container = container[1..];
        }

        return new EditPlan
        {
            Source = new EditPlanSource
            {
                InputPath = request.InputPath
            },
            Template = new EditTemplateReference
            {
                Id = template.Id,
                Version = template.Version,
                Parameters = BuildTemplateParameters(template, request)
            },
            Clips = BuildSeedClips(request),
            AudioTracks = BuildAudioTracks(request, artifactBindings),
            Artifacts = BuildArtifacts(artifactBindings),
            Transcript = BuildTranscriptPlan(request),
            Beats = BuildBeatTrackPlan(request),
            Subtitles = BuildSubtitlePlan(request, artifactBindings, subtitleMode),
            Output = new EditOutputPlan
            {
                Path = request.RenderOutputPath,
                Container = string.IsNullOrWhiteSpace(container) ? template.OutputContainer : container
            },
            Extensions = BuildExtensions(template)
        };
    }

    private static IReadOnlyList<EditClip> BuildSeedClips(EditPlanTemplateRequest request)
    {
        if (request.TranscriptSegmentGroupSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Transcript segment group size must be greater than zero.");
        }

        if (request.MinTranscriptSegmentDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Minimum transcript segment duration must be zero or greater.");
        }

        if (request.MaxTranscriptGap.HasValue && request.MaxTranscriptGap.Value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Maximum transcript gap must be zero or greater.");
        }

        if (request.BeatGroupSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Beat group size must be greater than zero.");
        }

        if (request.SeedClipsFromTranscript && request.Transcript is { Segments.Count: > 0 })
        {
            return BuildTranscriptSeedClips(
                request.Transcript,
                request.TranscriptSegmentGroupSize,
                request.MinTranscriptSegmentDuration,
                request.MaxTranscriptGap);
        }

        if (request.SeedClipsFromBeats && request.BeatTrack is { Beats.Count: > 1 })
        {
            return BuildBeatSeedClips(request.BeatTrack, request.BeatGroupSize);
        }

        if (request.SourceDuration is null || request.SourceDuration <= TimeSpan.Zero)
        {
            return [];
        }

        return
        [
            new EditClip
            {
                Id = "clip-001",
                InPoint = TimeSpan.Zero,
                OutPoint = request.SourceDuration.Value,
                Label = "full-source"
            }
        ];
    }

    private static IReadOnlyList<EditClip> BuildTranscriptSeedClips(
        TranscriptDocument transcript,
        int transcriptSegmentGroupSize,
        TimeSpan minTranscriptSegmentDuration,
        TimeSpan? maxTranscriptGap)
    {
        var validSegments = transcript.Segments
            .Where(segment => segment.End > segment.Start)
            .Where(segment => segment.End - segment.Start >= minTranscriptSegmentDuration)
            .ToArray();

        if (validSegments.Length == 0)
        {
            return [];
        }

        var clips = new List<EditClip>();
        var clipNumber = 1;
        for (var index = 0; index < validSegments.Length;)
        {
            var segmentGroup = new List<TranscriptSegment> { validSegments[index] };
            var nextIndex = index + 1;

            while (nextIndex < validSegments.Length && segmentGroup.Count < transcriptSegmentGroupSize)
            {
                var previousSegment = validSegments[nextIndex - 1];
                var currentSegment = validSegments[nextIndex];
                var gap = currentSegment.Start - previousSegment.End;
                if (gap < TimeSpan.Zero)
                {
                    gap = TimeSpan.Zero;
                }

                if (maxTranscriptGap is not null && gap > maxTranscriptGap.Value)
                {
                    break;
                }

                segmentGroup.Add(currentSegment);
                nextIndex++;
            }

            var firstSegment = segmentGroup[0];
            var lastSegment = segmentGroup[^1];

            clips.Add(new EditClip
            {
                Id = $"clip-{clipNumber:000}",
                InPoint = firstSegment.Start,
                OutPoint = lastSegment.End,
                Label = BuildTranscriptGroupLabel(segmentGroup, clipNumber)
            });

            clipNumber++;
            index = nextIndex;
        }

        return clips;
    }

    private static string BuildTranscriptGroupLabel(IReadOnlyList<TranscriptSegment> segmentGroup, int clipNumber)
    {
        if (segmentGroup.Count == 1)
        {
            var segment = segmentGroup[0];
            return string.IsNullOrWhiteSpace(segment.Id)
                ? $"transcript-segment-{clipNumber:000}"
                : segment.Id;
        }

        var firstId = segmentGroup[0].Id;
        var lastId = segmentGroup[^1].Id;
        if (!string.IsNullOrWhiteSpace(firstId) && !string.IsNullOrWhiteSpace(lastId))
        {
            return string.Equals(firstId, lastId, StringComparison.Ordinal)
                ? firstId
                : $"{firstId}..{lastId}";
        }

        return $"transcript-group-{clipNumber:000}";
    }

    private static IReadOnlyDictionary<string, string> BuildTemplateParameters(
        EditPlanTemplateDefinition template,
        EditPlanTemplateRequest request)
    {
        var parameters = new Dictionary<string, string>(template.ParameterDefaults, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in request.ParameterOverrides)
        {
            parameters[pair.Key] = pair.Value;
        }

        return parameters;
    }

    private static IReadOnlyList<EditClip> BuildBeatSeedClips(BeatTrackDocument beatTrack, int beatGroupSize)
    {
        var clips = new List<EditClip>();
        var clipNumber = 1;

        for (var index = 0; index + beatGroupSize < beatTrack.Beats.Count; index += beatGroupSize)
        {
            var start = beatTrack.Beats[index].Time;
            var end = beatTrack.Beats[index + beatGroupSize].Time;
            if (end <= start)
            {
                continue;
            }

            clips.Add(new EditClip
            {
                Id = $"clip-{clipNumber:000}",
                InPoint = start,
                OutPoint = end,
                Label = $"beat-group-{clipNumber:000}"
            });
            clipNumber++;
        }

        return clips;
    }

    private static IReadOnlyDictionary<EditPlanArtifactSlot, string> BuildArtifactBindings(
        EditPlanTemplateDefinition template,
        EditPlanTemplateRequest request)
    {
        var slotMap = template.ArtifactSlots.ToDictionary(slot => slot.Id, StringComparer.OrdinalIgnoreCase);
        var bindings = new Dictionary<EditPlanArtifactSlot, string>();

        foreach (var binding in request.ArtifactBindings)
        {
            if (!slotMap.TryGetValue(binding.Key, out var slot))
            {
                throw new InvalidOperationException(
                    $"Template '{template.Id}' does not declare artifact slot '{binding.Key}'.");
            }

            bindings[slot] = binding.Value;
        }

        var subtitlePath = request.SubtitlePath;
        if (!string.IsNullOrWhiteSpace(subtitlePath))
        {
            var subtitleSlot = FindSlot(template, preferredId: "subtitles", kind: "subtitle");
            if (subtitleSlot is not null)
            {
                bindings[subtitleSlot] = subtitlePath;
            }
        }

        var bgmPath = request.BgmPath;
        if (!string.IsNullOrWhiteSpace(bgmPath))
        {
            var bgmSlot = FindSlot(template, preferredId: "bgm", kind: "audio");
            if (bgmSlot is not null)
            {
                bindings[bgmSlot] = bgmPath;
            }
        }

        return bindings;
    }

    private static EditPlanArtifactSlot? FindSlot(
        EditPlanTemplateDefinition template,
        string preferredId,
        string kind)
    {
        return template.ArtifactSlots.FirstOrDefault(slot => string.Equals(slot.Id, preferredId, StringComparison.OrdinalIgnoreCase))
            ?? template.ArtifactSlots.FirstOrDefault(slot => string.Equals(slot.Kind, kind, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<AudioTrackMix> BuildAudioTracks(
        EditPlanTemplateRequest request,
        IReadOnlyDictionary<EditPlanArtifactSlot, string> artifactBindings)
    {
        var bgmPath = request.BgmPath
            ?? artifactBindings
                .FirstOrDefault(binding => string.Equals(binding.Key.Id, "bgm", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(binding.Key.Kind, "audio", StringComparison.OrdinalIgnoreCase))
                .Value;

        if (string.IsNullOrWhiteSpace(bgmPath))
        {
            return [];
        }

        return
        [
            new AudioTrackMix
            {
                Id = "bgm-01",
                Role = AudioTrackRole.Bgm,
                Path = bgmPath,
                Start = TimeSpan.Zero,
                GainDb = -12
            }
        ];
    }

    private static IReadOnlyList<EditArtifactReference> BuildArtifacts(
        IReadOnlyDictionary<EditPlanArtifactSlot, string> artifactBindings)
    {
        if (artifactBindings.Count == 0)
        {
            return [];
        }

        return artifactBindings
            .Select(binding => new EditArtifactReference
            {
                SlotId = binding.Key.Id,
                Kind = binding.Key.Kind,
                Path = binding.Value
            })
            .ToArray();
    }

    private static EditSubtitlePlan? BuildSubtitlePlan(
        EditPlanTemplateRequest request,
        IReadOnlyDictionary<EditPlanArtifactSlot, string> artifactBindings,
        SubtitleMode? subtitleMode)
    {
        var subtitlePath = request.SubtitlePath
            ?? artifactBindings
                .FirstOrDefault(binding => string.Equals(binding.Key.Id, "subtitles", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(binding.Key.Kind, "subtitle", StringComparison.OrdinalIgnoreCase))
                .Value;

        if (string.IsNullOrWhiteSpace(subtitlePath))
        {
            return null;
        }

        subtitleMode ??= SubtitleMode.Sidecar;

        if (subtitleMode is null)
        {
            return null;
        }

        return new EditSubtitlePlan
        {
            Path = string.IsNullOrWhiteSpace(subtitlePath) ? "subtitles.srt" : subtitlePath,
            Mode = subtitleMode.Value
        };
    }

    private static EditBeatTrackPlan? BuildBeatTrackPlan(EditPlanTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BeatTrackPath))
        {
            return null;
        }

        return new EditBeatTrackPlan
        {
            Path = request.BeatTrackPath,
            EstimatedBpm = request.BeatTrack?.EstimatedBpm
        };
    }

    private static EditTranscriptPlan? BuildTranscriptPlan(EditPlanTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TranscriptPath))
        {
            return null;
        }

        return new EditTranscriptPlan
        {
            Path = request.TranscriptPath,
            Language = request.Transcript?.Language,
            SegmentCount = request.Transcript?.Segments.Count
        };
    }

    private static IDictionary<string, JsonElement>? BuildExtensions(EditPlanTemplateDefinition template)
    {
        if (template.ArtifactSlots.Count == 0)
        {
            return null;
        }

        var slots = template.ArtifactSlots
            .Select(slot => new
            {
                slot.Id,
                slot.Kind,
                slot.Description,
                slot.Required
            })
            .ToArray();

        return new Dictionary<string, JsonElement>
        {
            ["x-template"] = JsonSerializer.SerializeToElement(new
            {
                slots
            }, OpenVideoToolboxJson.Shared)
        };
    }
}
