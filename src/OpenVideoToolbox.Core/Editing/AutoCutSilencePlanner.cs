using System.Text.Json.Serialization;
using OpenVideoToolbox.Core.Audio;

namespace OpenVideoToolbox.Core.Editing;

public sealed record AutoCutSilenceRequest
{
    public required string SourcePath { get; init; }

    public required TimeSpan SourceDuration { get; init; }

    public required SilenceDetectionDocument Silence { get; init; }

    public TimeSpan Padding { get; init; } = TimeSpan.FromMilliseconds(200);

    public TimeSpan MergeGap { get; init; } = TimeSpan.FromMilliseconds(500);

    public TimeSpan MinClipDuration { get; init; } = TimeSpan.FromSeconds(1);

    public string? TemplateId { get; init; }

    public string? RenderOutputPath { get; init; }
}

public sealed record AutoCutSilenceResult
{
    public required string SourcePath { get; init; }

    public required TimeSpan SourceDuration { get; init; }

    public IReadOnlyList<EditClip> Clips { get; init; } = [];

    public IReadOnlyList<SilenceSegment> RemovedSegments { get; init; } = [];

    public required AutoCutSilenceStats Stats { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EditPlan? Plan { get; init; }
}

public sealed record AutoCutSilenceStats
{
    public required int GeneratedClipCount { get; init; }

    public required int RemovedSilenceCount { get; init; }

    public required TimeSpan RemovedSilenceDuration { get; init; }

    public required TimeSpan RetainedDuration { get; init; }
}

public sealed class AutoCutSilencePlanner
{
    public AutoCutSilenceResult BuildClips(AutoCutSilenceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Silence);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourcePath);

        if (request.SourceDuration <= TimeSpan.Zero)
        {
            throw new ArgumentException("Source duration must be greater than zero.", nameof(request));
        }

        if (request.Padding < TimeSpan.Zero)
        {
            throw new ArgumentException("Padding must be zero or greater.", nameof(request));
        }

        if (request.MergeGap < TimeSpan.Zero)
        {
            throw new ArgumentException("Merge gap must be zero or greater.", nameof(request));
        }

        if (request.MinClipDuration < TimeSpan.Zero)
        {
            throw new ArgumentException("Minimum clip duration must be zero or greater.", nameof(request));
        }

        var normalizedSilence = NormalizeSilenceSegments(request.Silence.Segments, request.SourceDuration);
        var intervals = InvertSilence(normalizedSilence, request.SourceDuration);
        var padded = ApplyPadding(intervals, request.Padding, request.SourceDuration);
        var merged = MergeIntervals(padded, request.MergeGap);
        var filtered = merged
            .Where(interval => interval.Duration >= request.MinClipDuration)
            .ToArray();

        var clips = filtered
            .Select((interval, index) => new EditClip
            {
                Id = $"clip-{index + 1:000}",
                InPoint = interval.Start,
                OutPoint = interval.End
            })
            .ToArray();

        return new AutoCutSilenceResult
        {
            SourcePath = request.SourcePath,
            SourceDuration = request.SourceDuration,
            Clips = clips,
            RemovedSegments = normalizedSilence
                .Select(interval => new SilenceSegment
                {
                    Start = interval.Start,
                    End = interval.End,
                    Duration = interval.Duration
                })
                .ToArray(),
            Stats = new AutoCutSilenceStats
            {
                GeneratedClipCount = clips.Length,
                RemovedSilenceCount = normalizedSilence.Count,
                RemovedSilenceDuration = TimeSpan.FromTicks(normalizedSilence.Sum(segment => segment.Duration.Ticks)),
                RetainedDuration = TimeSpan.FromTicks(clips.Sum(clip => (clip.OutPoint - clip.InPoint).Ticks))
            }
        };
    }

    public AutoCutSilenceResult BuildPlan(AutoCutSilenceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RenderOutputPath))
        {
            throw new ArgumentException("Render output path is required when generating a plan.", nameof(request));
        }

        var result = BuildClips(request);

        return result with
        {
            Plan = TryBuildV2TemplatePlan(request, result)
                ?? BuildSchemaV1Plan(request, result)
        };
    }

    private static EditPlan BuildSchemaV1Plan(AutoCutSilenceRequest request, AutoCutSilenceResult result)
    {
        var outputContainer = ResolveOutputContainer(request.RenderOutputPath!);

        return new EditPlan
        {
            Source = new EditPlanSource
            {
                InputPath = request.SourcePath
            },
            Template = string.IsNullOrWhiteSpace(request.TemplateId)
                ? null
                : new EditTemplateReference
                {
                    Id = request.TemplateId
                },
            Clips = result.Clips,
            Output = new EditOutputPlan
            {
                Path = request.RenderOutputPath!,
                Container = outputContainer
            }
        };
    }

    private static EditPlan? TryBuildV2TemplatePlan(AutoCutSilenceRequest request, AutoCutSilenceResult result)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateId))
        {
            return null;
        }

        var template = BuiltInEditPlanTemplateCatalog.GetAll()
            .FirstOrDefault(candidate => string.Equals(candidate.Id, request.TemplateId, StringComparison.OrdinalIgnoreCase));
        if (template is null || template.PlanModel != EditPlanTemplatePlanModel.V2Timeline)
        {
            return null;
        }

        var factory = new EditPlanTemplateFactory();
        var basePlan = factory.Create(
            template,
            new EditPlanTemplateRequest
            {
                InputPath = request.SourcePath,
                RenderOutputPath = request.RenderOutputPath!,
                SourceDuration = request.SourceDuration
            });

        if (basePlan.Timeline is null)
        {
            throw new InvalidOperationException(
                $"Template '{template.Id}' is marked as '{EditPlanTemplatePlanModel.V2Timeline}' but did not produce a timeline.");
        }

        var videoTrackIndex = basePlan.Timeline.Tracks
            .Select((track, index) => new { track, index })
            .FirstOrDefault(item => item.track.Kind == TrackKind.Video)?.index;
        if (videoTrackIndex is null)
        {
            throw new InvalidOperationException(
                $"Template '{template.Id}' did not produce a video track for auto-cut-silence.");
        }

        var videoTrack = basePlan.Timeline.Tracks[videoTrackIndex.Value];
        var clipEffects = videoTrack.Clips.FirstOrDefault()?.Effects ?? [];
        var updatedTracks = basePlan.Timeline.Tracks.ToArray();
        updatedTracks[videoTrackIndex.Value] = videoTrack with
        {
            Clips = BuildTimelineClips(result.Clips, clipEffects)
        };

        return basePlan with
        {
            Timeline = basePlan.Timeline with
            {
                Tracks = updatedTracks
            }
        };
    }

    private static IReadOnlyList<TimelineClip> BuildTimelineClips(
        IReadOnlyList<EditClip> clips,
        IReadOnlyList<TimelineEffect> clipEffects)
    {
        var timelineClips = new List<TimelineClip>(clips.Count);
        var cursor = TimeSpan.Zero;

        foreach (var clip in clips)
        {
            var inPoint = clip.InPoint;
            var outPoint = clip.OutPoint;
            if (outPoint <= inPoint)
            {
                continue;
            }

            timelineClips.Add(new TimelineClip
            {
                Id = clip.Id,
                Start = cursor,
                InPoint = inPoint,
                OutPoint = outPoint,
                Effects = clipEffects
            });

            cursor += outPoint - inPoint;
        }

        return timelineClips;
    }

    private static string ResolveOutputContainer(string renderOutputPath)
    {
        var extension = Path.GetExtension(renderOutputPath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException(
                $"Render output path '{renderOutputPath}' must include a file extension.",
                nameof(renderOutputPath));
        }

        return extension[1..];
    }

    private static List<TimeRange> NormalizeSilenceSegments(
        IReadOnlyList<SilenceSegment> segments,
        TimeSpan sourceDuration)
    {
        var normalized = segments
            .Select(segment => new TimeRange(
                Max(TimeSpan.Zero, segment.Start),
                Min(sourceDuration, segment.End)))
            .Where(segment => segment.End > segment.Start)
            .OrderBy(segment => segment.Start)
            .ThenBy(segment => segment.End)
            .ToList();

        if (normalized.Count == 0)
        {
            return [];
        }

        var merged = new List<TimeRange>
        {
            normalized[0]
        };

        for (var index = 1; index < normalized.Count; index++)
        {
            var current = normalized[index];
            var last = merged[^1];

            if (current.Start <= last.End)
            {
                merged[^1] = new TimeRange(last.Start, Max(last.End, current.End));
                continue;
            }

            merged.Add(current);
        }

        return merged;
    }

    private static List<TimeRange> InvertSilence(IReadOnlyList<TimeRange> silenceSegments, TimeSpan sourceDuration)
    {
        var intervals = new List<TimeRange>();
        var cursor = TimeSpan.Zero;

        foreach (var segment in silenceSegments)
        {
            if (segment.Start > cursor)
            {
                intervals.Add(new TimeRange(cursor, segment.Start));
            }

            cursor = Max(cursor, segment.End);
        }

        if (cursor < sourceDuration)
        {
            intervals.Add(new TimeRange(cursor, sourceDuration));
        }

        return intervals;
    }

    private static List<TimeRange> ApplyPadding(
        IReadOnlyList<TimeRange> intervals,
        TimeSpan padding,
        TimeSpan sourceDuration)
    {
        if (padding == TimeSpan.Zero)
        {
            return intervals.ToList();
        }

        return intervals
            .Select(interval => new TimeRange(
                Max(TimeSpan.Zero, interval.Start - padding),
                Min(sourceDuration, interval.End + padding)))
            .Where(interval => interval.End > interval.Start)
            .ToList();
    }

    private static IReadOnlyList<TimeRange> MergeIntervals(
        IReadOnlyList<TimeRange> intervals,
        TimeSpan mergeGap)
    {
        if (intervals.Count == 0)
        {
            return [];
        }

        var merged = new List<TimeRange>
        {
            intervals[0]
        };

        for (var index = 1; index < intervals.Count; index++)
        {
            var current = intervals[index];
            var last = merged[^1];
            if (current.Start - last.End <= mergeGap)
            {
                merged[^1] = new TimeRange(last.Start, Max(last.End, current.End));
                continue;
            }

            merged.Add(current);
        }

        return merged;
    }

    private static TimeSpan Min(TimeSpan left, TimeSpan right) => left <= right ? left : right;

    private static TimeSpan Max(TimeSpan left, TimeSpan right) => left >= right ? left : right;

    private readonly record struct TimeRange(TimeSpan Start, TimeSpan End)
    {
        public TimeSpan Duration => End - Start;
    }
}
