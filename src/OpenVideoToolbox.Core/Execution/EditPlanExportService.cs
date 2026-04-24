using System.Text;
using OpenVideoToolbox.Core;
using OpenVideoToolbox.Core.Editing;

namespace OpenVideoToolbox.Core.Execution;

public sealed class EditPlanExportService
{
    public async Task<ProjectExportResult> ExportAsync(
        ProjectExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Plan);

        if (request.Format != ProjectExportFormat.Edl)
        {
            throw new InvalidOperationException($"Unsupported export format '{request.Format}'.");
        }

        var outputPath = Path.GetFullPath(request.OutputPath);
        if (File.Exists(outputPath) && !request.Overwrite)
        {
            throw new InvalidOperationException(
                $"Output file '{outputPath}' already exists. Pass --overwrite to replace it.");
        }

        var warnings = new List<ProjectExportWarning>();
        var frameRate = ResolveFrameRate(request, warnings);
        var title = ResolveTitle(request, outputPath);
        var events = NormalizePlan(request.Plan, warnings);
        var content = BuildEdl(title, frameRate, events);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, content, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

        return new ProjectExportResult
        {
            Format = ProjectExportFormats.Edl,
            FidelityLevel = ProjectExportFidelityLevels.L1,
            OutputPath = outputPath,
            Title = title,
            FrameRate = frameRate,
            EventCount = events.Count,
            Warnings = warnings
        };
    }

    private static int ResolveFrameRate(ProjectExportRequest request, ICollection<ProjectExportWarning> warnings)
    {
        var frameRate = request.FrameRate ?? request.Plan.Timeline?.FrameRate;
        if (frameRate is null)
        {
            warnings.Add(new ProjectExportWarning
            {
                Code = ProjectExportWarningCodes.FrameRateDefaulted,
                Target = "frameRate",
                Message = "Frame rate was not provided by the request or plan; defaulted to 30 fps."
            });

            return 30;
        }

        if (frameRate.Value <= 0)
        {
            throw new InvalidOperationException("Frame rate must be greater than zero for export.");
        }

        return frameRate.Value;
    }

    private static string ResolveTitle(ProjectExportRequest request, string outputPath)
    {
        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            return request.Title.Trim();
        }

        var fileName = Path.GetFileNameWithoutExtension(outputPath);
        return string.IsNullOrWhiteSpace(fileName) ? "ovt-export" : fileName;
    }

    private static IReadOnlyList<NormalizedEdlEvent> NormalizePlan(
        EditPlan plan,
        ICollection<ProjectExportWarning> warnings)
    {
        if (plan.Timeline is not null)
        {
            return NormalizeTimelinePlan(plan, warnings);
        }

        if (plan.SchemaVersion == SchemaVersions.V2)
        {
            throw new InvalidOperationException("Schema v2 plan does not contain a timeline to export.");
        }

        return NormalizeV1Plan(plan, warnings);
    }

    private static IReadOnlyList<NormalizedEdlEvent> NormalizeV1Plan(
        EditPlan plan,
        ICollection<ProjectExportWarning> warnings)
    {
        AddWarningOnce(
            warnings,
            ProjectExportWarningCodes.V1Wrapped,
            "clips",
            "Schema v1 plan was wrapped as a single primary video track for EDL export.");

        if (plan.AudioTracks.Count > 0)
        {
            AddWarningOnce(
                warnings,
                ProjectExportWarningCodes.AudioIgnored,
                "audioTracks",
                "EDL L1 does not export audio tracks.");
        }

        if (plan.Clips.Count == 0)
        {
            throw new InvalidOperationException("Plan does not contain any clips to export.");
        }

        var sourcePath = Path.GetFullPath(plan.Source.InputPath);
        var reel = BuildReelName(sourcePath);
        var cursor = TimeSpan.Zero;
        var events = new List<NormalizedEdlEvent>(plan.Clips.Count);

        foreach (var clip in plan.Clips)
        {
            var sourceIn = clip.InPoint;
            var sourceOut = clip.OutPoint;
            var duration = sourceOut - sourceIn;
            if (duration <= TimeSpan.Zero)
            {
                throw new InvalidOperationException($"Clip '{clip.Id}' has non-positive duration.");
            }

            var recordIn = cursor;
            var recordOut = cursor + duration;
            events.Add(new NormalizedEdlEvent(
                reel,
                sourcePath,
                string.IsNullOrWhiteSpace(clip.Label) ? clip.Id : clip.Label!,
                sourceIn,
                sourceOut,
                recordIn,
                recordOut));

            cursor = recordOut;
        }

        return events;
    }

    private static IReadOnlyList<NormalizedEdlEvent> NormalizeTimelinePlan(
        EditPlan plan,
        ICollection<ProjectExportWarning> warnings)
    {
        var timeline = plan.Timeline!;
        var selectedTrack = timeline.Tracks.FirstOrDefault(track =>
                track.Kind == TrackKind.Video
                && string.Equals(track.Id, "main", StringComparison.OrdinalIgnoreCase))
            ?? timeline.Tracks.FirstOrDefault(track => track.Kind == TrackKind.Video);

        if (selectedTrack is null || selectedTrack.Clips.Count == 0)
        {
            throw new InvalidOperationException("Timeline does not contain an exportable primary video track.");
        }

        if (timeline.Tracks.Any(track => track.Kind == TrackKind.Audio && track.Clips.Count > 0))
        {
            AddWarningOnce(
                warnings,
                ProjectExportWarningCodes.AudioIgnored,
                "timeline.tracks",
                "EDL L1 ignores all audio tracks.");
        }

        if (timeline.Tracks.Any(track =>
                track.Kind == TrackKind.Video
                && !ReferenceEquals(track, selectedTrack)
                && track.Clips.Count > 0))
        {
            AddWarningOnce(
                warnings,
                ProjectExportWarningCodes.ExtraVideoTracksIgnored,
                "timeline.tracks",
                "EDL L1 exports only the primary video track.");
        }

        if (timeline.Tracks.Any(track =>
                track.Effects.Count > 0
                || track.Clips.Any(clip => clip.Effects.Count > 0)))
        {
            AddWarningOnce(
                warnings,
                ProjectExportWarningCodes.EffectsIgnored,
                "timeline.tracks",
                "EDL L1 does not export track or clip effects.");
        }

        if (timeline.Tracks.Any(track => track.Clips.Any(clip =>
                clip.Transitions?.In is not null
                || clip.Transitions?.Out is not null)))
        {
            AddWarningOnce(
                warnings,
                ProjectExportWarningCodes.TransitionsIgnored,
                "timeline.tracks",
                "EDL L1 does not export transitions.");
        }

        var events = selectedTrack.Clips
            .Select((clip, index) => new { clip, index })
            .OrderBy(item => item.clip.Start)
            .ThenBy(item => item.index)
            .Select(item => NormalizeTimelineClip(plan, selectedTrack, item.clip))
            .ToList();

        if (events.Count == 0)
        {
            throw new InvalidOperationException("Timeline does not contain any clips to export.");
        }

        return events;
    }

    private static NormalizedEdlEvent NormalizeTimelineClip(
        EditPlan plan,
        TimelineTrack track,
        TimelineClip clip)
    {
        var sourcePath = Path.GetFullPath(string.IsNullOrWhiteSpace(clip.Src) ? plan.Source.InputPath : clip.Src);
        var sourceIn = clip.InPoint ?? TimeSpan.Zero;
        var duration = clip.Duration;
        if (duration is null)
        {
            if (clip.InPoint is null || clip.OutPoint is null)
            {
                throw new InvalidOperationException(
                    $"Timeline clip '{clip.Id}' must provide either duration or both in/out points for export.");
            }

            duration = clip.OutPoint.Value - clip.InPoint.Value;
        }

        if (duration.Value <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"Timeline clip '{clip.Id}' on track '{track.Id}' has non-positive duration.");
        }

        var sourceOut = clip.OutPoint ?? (sourceIn + duration.Value);
        var recordIn = clip.Start;
        var recordOut = clip.Start + duration.Value;
        if (sourceOut <= sourceIn)
        {
            throw new InvalidOperationException($"Timeline clip '{clip.Id}' has invalid source range.");
        }

        return new NormalizedEdlEvent(
            BuildReelName(sourcePath),
            sourcePath,
            clip.Id,
            sourceIn,
            sourceOut,
            recordIn,
            recordOut);
    }

    private static void AddWarningOnce(
        ICollection<ProjectExportWarning> warnings,
        string code,
        string? target,
        string message)
    {
        if (warnings.Any(item => string.Equals(item.Code, code, StringComparison.Ordinal)))
        {
            return;
        }

        warnings.Add(new ProjectExportWarning
        {
            Code = code,
            Target = target,
            Message = message
        });
    }

    private static string BuildEdl(string title, int frameRate, IReadOnlyList<NormalizedEdlEvent> events)
    {
        var builder = new StringBuilder();
        builder.Append("TITLE: ").AppendLine(title);
        builder.AppendLine("FCM: NON-DROP FRAME");
        builder.AppendLine();

        for (var index = 0; index < events.Count; index++)
        {
            var item = events[index];
            builder.Append((index + 1).ToString("000"));
            builder.Append("  ");
            builder.Append(item.Reel.PadRight(8, ' '));
            builder.Append(" V     C        ");
            builder.Append(FormatTimecode(item.SourceIn, frameRate));
            builder.Append(' ');
            builder.Append(FormatTimecode(item.SourceOut, frameRate));
            builder.Append(' ');
            builder.Append(FormatTimecode(item.RecordIn, frameRate));
            builder.Append(' ');
            builder.AppendLine(FormatTimecode(item.RecordOut, frameRate));
            builder.Append("* FROM CLIP NAME: ").AppendLine(item.DisplayName);
            builder.Append("* SOURCE FILE: ").AppendLine(item.SourcePath);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string FormatTimecode(TimeSpan value, int frameRate)
    {
        var totalFrames = checked((long)Math.Round(
            value.TotalSeconds * frameRate,
            MidpointRounding.AwayFromZero));
        if (totalFrames < 0)
        {
            throw new InvalidOperationException("EDL timecode cannot be negative.");
        }

        var frames = totalFrames % frameRate;
        var totalSeconds = totalFrames / frameRate;
        var seconds = totalSeconds % 60;
        var totalMinutes = totalSeconds / 60;
        var minutes = totalMinutes % 60;
        var hours = totalMinutes / 60;

        return $"{hours:00}:{minutes:00}:{seconds:00}:{frames:00}";
    }

    private static string BuildReelName(string sourcePath)
    {
        var stem = Path.GetFileNameWithoutExtension(sourcePath);
        if (string.IsNullOrWhiteSpace(stem))
        {
            return "AX";
        }

        var characters = stem
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .Take(8)
            .ToArray();

        return characters.Length == 0 ? "AX" : new string(characters);
    }

    private sealed record NormalizedEdlEvent(
        string Reel,
        string SourcePath,
        string DisplayName,
        TimeSpan SourceIn,
        TimeSpan SourceOut,
        TimeSpan RecordIn,
        TimeSpan RecordOut);
}
