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
        return Create(
            BuiltInEditPlanTemplateCatalog.GetRequired(templateId),
            request,
            CreateBuiltInTemplateSource());
    }

    public EditPlan Create(
        EditPlanTemplateDefinition template,
        EditPlanTemplateRequest request,
        EditTemplateSourceReference? templateSource = null)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(request);

        var artifactBindings = BuildArtifactBindings(template, request);
        var subtitleMode = request.DisableSubtitles
            ? null
            : request.SubtitleModeOverride ?? template.DefaultSubtitleMode;
        var container = Path.GetExtension(request.RenderOutputPath);
        if (container.StartsWith(".", StringComparison.Ordinal))
        {
            container = container[1..];
        }

        var resolvedTemplateSource = templateSource ?? CreateBuiltInTemplateSource();
        var templateParameters = BuildTemplateParameters(template, request);
        var artifacts = BuildArtifacts(artifactBindings);
        var transcript = BuildTranscriptPlan(request);
        var beats = BuildBeatTrackPlan(request);
        var subtitles = BuildSubtitlePlan(request, artifactBindings, subtitleMode);
        var output = new EditOutputPlan
        {
            Path = request.RenderOutputPath,
            Container = string.IsNullOrWhiteSpace(container) ? template.OutputContainer : container
        };

        return template.PlanModel switch
        {
            EditPlanTemplatePlanModel.V2Timeline => CreateV2TimelinePlan(
                template,
                request,
                resolvedTemplateSource,
                templateParameters,
                artifactBindings,
                artifacts,
                transcript,
                beats,
                subtitles,
                output),
            _ => CreateV1Plan(
                template,
                request,
                resolvedTemplateSource,
                templateParameters,
                artifactBindings,
                artifacts,
                transcript,
                beats,
                subtitles,
                output)
        };
    }

    private static EditPlan CreateV1Plan(
        EditPlanTemplateDefinition template,
        EditPlanTemplateRequest request,
        EditTemplateSourceReference templateSource,
        IReadOnlyDictionary<string, string> templateParameters,
        IReadOnlyDictionary<EditPlanArtifactSlot, string> artifactBindings,
        IReadOnlyList<EditArtifactReference> artifacts,
        EditTranscriptPlan? transcript,
        EditBeatTrackPlan? beats,
        EditSubtitlePlan? subtitles,
        EditOutputPlan output)
    {
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
                Source = templateSource,
                Parameters = templateParameters
            },
            Clips = BuildSeedClips(request),
            AudioTracks = BuildAudioTracks(request, artifactBindings),
            Artifacts = artifacts,
            Transcript = transcript,
            Beats = beats,
            Subtitles = subtitles,
            Output = output,
            Extensions = BuildExtensions(template)
        };
    }

    private static EditPlan CreateV2TimelinePlan(
        EditPlanTemplateDefinition template,
        EditPlanTemplateRequest request,
        EditTemplateSourceReference templateSource,
        IReadOnlyDictionary<string, string> templateParameters,
        IReadOnlyDictionary<EditPlanArtifactSlot, string> artifactBindings,
        IReadOnlyList<EditArtifactReference> artifacts,
        EditTranscriptPlan? transcript,
        EditBeatTrackPlan? beats,
        EditSubtitlePlan? subtitles,
        EditOutputPlan output)
    {
        return new EditPlan
        {
            SchemaVersion = SchemaVersions.V2,
            Source = new EditPlanSource
            {
                InputPath = request.InputPath
            },
            Template = new EditTemplateReference
            {
                Id = template.Id,
                Version = template.Version,
                Source = templateSource,
                Parameters = templateParameters
            },
            Clips = [],
            AudioTracks = [],
            Artifacts = artifacts,
            Transcript = transcript,
            Beats = beats,
            Subtitles = subtitles,
            Timeline = BuildTimeline(request, artifactBindings),
            Output = output,
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
        var bgmPath = ResolveBgmPath(request, artifactBindings);

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

    private static EditPlanTimeline BuildTimeline(
        EditPlanTemplateRequest request,
        IReadOnlyDictionary<EditPlanArtifactSlot, string> artifactBindings)
    {
        var fallbackDuration = request.SourceDuration is { } sourceDuration && sourceDuration > TimeSpan.Zero
            ? sourceDuration
            : TimeSpan.FromSeconds(6);
        var shouldUseSeededLayout = request.SeedClipsFromTranscript || request.SeedClipsFromBeats;
        var seedClips = shouldUseSeededLayout ? BuildSeedClips(request) : [];
        var manualVideoClips = BuildTimelineVideoClips(fallbackDuration.TotalSeconds);
        var defaultClipEffects = manualVideoClips.FirstOrDefault()?.Effects ?? [];
        var videoClips = shouldUseSeededLayout
            ? BuildTimelineVideoClips(seedClips, defaultClipEffects)
            : manualVideoClips;
        var totalDuration = shouldUseSeededLayout
            ? ResolveTimelineDuration(videoClips, request.SourceDuration)
            : fallbackDuration;

        var videoTrack = new TimelineTrack
        {
            Id = "main",
            Kind = TrackKind.Video,
            Effects =
            [
                CreateEffect(
                    "scale",
                    ("width", 1920),
                    ("height", 1080),
                    ("flags", "lanczos"))
            ],
            Clips = videoClips
        };

        var tracks = new List<TimelineTrack>
        {
            videoTrack
        };

        var bgmPath = ResolveBgmPath(request, artifactBindings);
        if (!string.IsNullOrWhiteSpace(bgmPath))
        {
            tracks.Add(new TimelineTrack
            {
                Id = "bgm",
                Kind = TrackKind.Audio,
                Slot = new TrackSlot
                {
                    Name = "bgm",
                    DefaultAsset = bgmPath,
                    Required = false
                },
                Clips =
                [
                    new TimelineClip
                    {
                        Id = "bgm-001",
                        Src = bgmPath,
                        Start = TimeSpan.Zero,
                        Duration = totalDuration,
                        Effects =
                        [
                            CreateEffect("volume", ("gainDb", -10))
                        ]
                    }
                ]
            });
        }

        return new EditPlanTimeline
        {
            Duration = totalDuration,
            Resolution = new TimelineResolution
            {
                W = 1920,
                H = 1080
            },
            FrameRate = 30,
            Tracks = tracks
        };
    }

    private static IReadOnlyList<TimelineClip> BuildTimelineVideoClips(double totalSeconds)
    {
        if (totalSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalSeconds), "Timeline duration must be greater than zero.");
        }

        if (totalSeconds < 2)
        {
            return
            [
                new TimelineClip
                {
                    Id = "clip-001",
                    Start = TimeSpan.Zero,
                    InPoint = TimeSpan.Zero,
                    OutPoint = TimeSpan.FromSeconds(totalSeconds),
                    Effects =
                    [
                        CreateEffect(
                            "brightness_contrast",
                            ("brightness", 0.06),
                            ("contrast", 1.05))
                    ]
                }
            ];
        }

        var transitionDuration = Math.Min(0.5, Math.Round(totalSeconds / 12d, 3, MidpointRounding.AwayFromZero));
        transitionDuration = Math.Max(0.2, transitionDuration);

        var firstEnd = Math.Round(totalSeconds * 0.6d, 3, MidpointRounding.AwayFromZero);
        if (firstEnd >= totalSeconds)
        {
            firstEnd = Math.Round(totalSeconds - transitionDuration, 3, MidpointRounding.AwayFromZero);
        }

        var secondStart = Math.Round(Math.Max(firstEnd - transitionDuration, 0.2d), 3, MidpointRounding.AwayFromZero);
        if (secondStart >= totalSeconds)
        {
            secondStart = Math.Round(Math.Max(totalSeconds - 0.2d, 0d), 3, MidpointRounding.AwayFromZero);
        }

        return
        [
            new TimelineClip
            {
                Id = "clip-001",
                Start = TimeSpan.Zero,
                InPoint = TimeSpan.Zero,
                OutPoint = TimeSpan.FromSeconds(firstEnd),
                Effects =
                [
                    CreateEffect(
                        "brightness_contrast",
                        ("brightness", 0.06),
                        ("contrast", 1.05))
                ],
                Transitions = new ClipTransitions
                {
                    Out = new Transition
                    {
                        Type = "fade",
                        Duration = transitionDuration
                    }
                }
            },
            new TimelineClip
            {
                Id = "clip-002",
                Start = TimeSpan.FromSeconds(secondStart),
                InPoint = TimeSpan.FromSeconds(secondStart),
                OutPoint = TimeSpan.FromSeconds(totalSeconds),
                Transitions = new ClipTransitions
                {
                    In = new Transition
                    {
                        Type = "fade",
                        Duration = transitionDuration
                    }
                }
            }
        ];
    }

    private static IReadOnlyList<TimelineClip> BuildTimelineVideoClips(
        IReadOnlyList<EditClip> seedClips,
        IReadOnlyList<TimelineEffect> defaultEffects)
    {
        if (seedClips.Count == 0)
        {
            return [];
        }

        var timelineClips = new List<TimelineClip>(seedClips.Count);
        var cursor = TimeSpan.Zero;

        foreach (var clip in seedClips)
        {
            if (clip.OutPoint <= clip.InPoint)
            {
                continue;
            }

            timelineClips.Add(new TimelineClip
            {
                Id = clip.Id,
                Start = cursor,
                InPoint = clip.InPoint,
                OutPoint = clip.OutPoint,
                Effects = defaultEffects
            });

            cursor += clip.OutPoint - clip.InPoint;
        }

        return timelineClips;
    }

    private static TimeSpan ResolveTimelineDuration(
        IReadOnlyList<TimelineClip> clips,
        TimeSpan? sourceDuration)
    {
        if (clips.Count == 0)
        {
            return sourceDuration.GetValueOrDefault(TimeSpan.Zero);
        }

        return clips
            .Select(GetTimelineClipDuration)
            .Aggregate(TimeSpan.Zero, (current, duration) => current + duration);
    }

    private static TimeSpan GetTimelineClipDuration(TimelineClip clip)
    {
        if (clip.Duration is { } duration)
        {
            return duration;
        }

        if (clip.InPoint is { } inPoint && clip.OutPoint is { } outPoint)
        {
            return outPoint - inPoint;
        }

        return TimeSpan.Zero;
    }

    private static string? ResolveBgmPath(
        EditPlanTemplateRequest request,
        IReadOnlyDictionary<EditPlanArtifactSlot, string> artifactBindings)
    {
        return request.BgmPath
            ?? artifactBindings
            .FirstOrDefault(binding => string.Equals(binding.Key.Id, "bgm", StringComparison.OrdinalIgnoreCase)
                || string.Equals(binding.Key.Kind, "audio", StringComparison.OrdinalIgnoreCase))
            .Value;
    }

    private static TimelineEffect CreateEffect(string type, params (string Name, object Value)[] parameters)
    {
        Dictionary<string, JsonElement>? extensions = null;

        if (parameters.Length > 0)
        {
            extensions = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, value) in parameters)
            {
                extensions[name] = JsonSerializer.SerializeToElement(value, OpenVideoToolboxJson.Shared);
            }
        }

        return new TimelineEffect
        {
            Type = type,
            Extensions = extensions
        };
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

    private static EditTemplateSourceReference CreateBuiltInTemplateSource()
    {
        return new EditTemplateSourceReference
        {
            Kind = EditTemplateSourceKinds.BuiltIn
        };
    }
}
