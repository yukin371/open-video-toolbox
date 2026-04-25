using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenVideoToolbox.Core.Editing;

public sealed class EditPlanValidator
{
    public EditPlanValidationResult Validate(
        EditPlan plan,
        bool checkReferencedFiles = false,
        IReadOnlyList<EditPlanTemplateDefinition>? availableTemplates = null,
        EffectRegistry? effectRegistry = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var issues = new List<EditPlanValidationIssue>();
        var template = EditPlanTemplateResolution.ResolveTemplate(plan, availableTemplates, issues);

        ValidateSchemaVersion(plan, issues);
        ValidateSource(plan, issues, checkReferencedFiles);
        ValidateOutput(plan, issues);
        ValidateClips(plan, issues);
        ValidateAudioTracks(plan, issues, checkReferencedFiles);
        ValidateArtifacts(plan, template, issues, checkReferencedFiles);
        ValidateTranscript(plan, issues, checkReferencedFiles);
        ValidateBeats(plan, issues, checkReferencedFiles);
        ValidateSubtitles(plan, issues, checkReferencedFiles);
        ValidateTimeline(plan, issues, effectRegistry);

        return new EditPlanValidationResult
        {
            CheckMode = EditPlanValidationMode.Basic,
            Issues = issues
        };
    }

    private static void ValidateSchemaVersion(EditPlan plan, ICollection<EditPlanValidationIssue> issues)
    {
        if (plan.SchemaVersion is SchemaVersions.V1 or SchemaVersions.V2)
        {
            return;
        }

        issues.Add(Error(
            "schemaVersion",
            "plan.schemaVersion.unsupported",
            $"Schema version '{plan.SchemaVersion}' is not supported. Expected '{SchemaVersions.V1}' or '{SchemaVersions.V2}'."));
    }

    private static void ValidateSource(EditPlan plan, ICollection<EditPlanValidationIssue> issues, bool checkReferencedFiles)
    {
        if (string.IsNullOrWhiteSpace(plan.Source.InputPath))
        {
            issues.Add(Error("source.inputPath", "source.inputPath.required", "Source input path is required."));
            return;
        }

        if (checkReferencedFiles && !File.Exists(plan.Source.InputPath))
        {
            issues.Add(Error("source.inputPath", "source.inputPath.missing", $"Referenced source file does not exist: '{plan.Source.InputPath}'."));
        }
    }

    private static void ValidateOutput(EditPlan plan, ICollection<EditPlanValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(plan.Output.Path))
        {
            issues.Add(Error("output.path", "output.path.required", "Output path is required."));
        }

        if (string.IsNullOrWhiteSpace(plan.Output.Container))
        {
            issues.Add(Error("output.container", "output.container.required", "Output container is required."));
            return;
        }

        if (string.IsNullOrWhiteSpace(plan.Output.Path))
        {
            return;
        }

        var extension = Path.GetExtension(plan.Output.Path);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return;
        }

        var normalizedExtension = extension.StartsWith(".", StringComparison.Ordinal) ? extension[1..] : extension;
        if (!string.Equals(normalizedExtension, plan.Output.Container, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error(
                "output.container",
                "output.container.mismatch",
                $"Output container '{plan.Output.Container}' does not match output path extension '{normalizedExtension}'."));
        }
    }

    private static void ValidateClips(EditPlan plan, ICollection<EditPlanValidationIssue> issues)
    {
        ValidateUniqueValues(
            plan.Clips.Select((clip, index) => ((string?)clip.Id, $"clips[{index}].id")),
            "clips.id.duplicate",
            "Clip id",
            issues);

        for (var index = 0; index < plan.Clips.Count; index++)
        {
            var clip = plan.Clips[index];
            if (string.IsNullOrWhiteSpace(clip.Id))
            {
                issues.Add(Error($"clips[{index}].id", "clips.id.required", "Clip id is required."));
            }

            if (clip.OutPoint <= clip.InPoint)
            {
                issues.Add(Error(
                    $"clips[{index}]",
                    "clips.range.invalid",
                    $"Clip '{clip.Id}' must end after it starts."));
            }
        }
    }

    private static void ValidateAudioTracks(EditPlan plan, ICollection<EditPlanValidationIssue> issues, bool checkReferencedFiles)
    {
        ValidateUniqueValues(
            plan.AudioTracks.Select((track, index) => ((string?)track.Id, $"audioTracks[{index}].id")),
            "audioTracks.id.duplicate",
            "Audio track id",
            issues);

        for (var index = 0; index < plan.AudioTracks.Count; index++)
        {
            var track = plan.AudioTracks[index];
            if (string.IsNullOrWhiteSpace(track.Id))
            {
                issues.Add(Error($"audioTracks[{index}].id", "audioTracks.id.required", "Audio track id is required."));
            }

            if (string.IsNullOrWhiteSpace(track.Path))
            {
                issues.Add(Error($"audioTracks[{index}].path", "audioTracks.path.required", "Audio track path is required."));
                continue;
            }

            if (checkReferencedFiles && !File.Exists(track.Path))
            {
                issues.Add(Error(
                    $"audioTracks[{index}].path",
                    "audioTracks.path.missing",
                    $"Referenced audio track file does not exist: '{track.Path}'."));
            }
        }
    }

    private static void ValidateArtifacts(
        EditPlan plan,
        EditPlanTemplateDefinition? template,
        ICollection<EditPlanValidationIssue> issues,
        bool checkReferencedFiles)
    {
        ValidateUniqueValues(
            plan.Artifacts.Select((artifact, index) => ((string?)artifact.SlotId, $"artifacts[{index}].slotId")),
            "artifacts.slotId.duplicate",
            "Artifact slot id",
            issues);

        var templateSlots = template?.ArtifactSlots.ToDictionary(slot => slot.Id, StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < plan.Artifacts.Count; index++)
        {
            var artifact = plan.Artifacts[index];
            if (string.IsNullOrWhiteSpace(artifact.SlotId))
            {
                issues.Add(Error($"artifacts[{index}].slotId", "artifacts.slotId.required", "Artifact slot id is required."));
            }

            if (string.IsNullOrWhiteSpace(artifact.Kind))
            {
                issues.Add(Error($"artifacts[{index}].kind", "artifacts.kind.required", "Artifact kind is required."));
            }

            if (string.IsNullOrWhiteSpace(artifact.Path))
            {
                issues.Add(Error($"artifacts[{index}].path", "artifacts.path.required", "Artifact path is required."));
            }
            else if (checkReferencedFiles && !File.Exists(artifact.Path))
            {
                issues.Add(Error(
                    $"artifacts[{index}].path",
                    "artifacts.path.missing",
                    $"Referenced artifact file does not exist: '{artifact.Path}'."));
            }

            if (templateSlots is null || string.IsNullOrWhiteSpace(artifact.SlotId))
            {
                continue;
            }

            if (!templateSlots.TryGetValue(artifact.SlotId, out var declaredSlot))
            {
                issues.Add(Error(
                    $"artifacts[{index}].slotId",
                    "artifacts.slotId.undeclared",
                    $"Template '{template!.Id}' does not declare artifact slot '{artifact.SlotId}'."));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(artifact.Kind)
                && !string.Equals(artifact.Kind, declaredSlot.Kind, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Error(
                    $"artifacts[{index}].kind",
                    "artifacts.kind.mismatch",
                    $"Artifact slot '{artifact.SlotId}' expects kind '{declaredSlot.Kind}' but got '{artifact.Kind}'."));
            }
        }

        if (templateSlots is null)
        {
            return;
        }

        foreach (var requiredSlot in template!.ArtifactSlots.Where(slot => slot.Required))
        {
            if (plan.Artifacts.Any(artifact => string.Equals(artifact.SlotId, requiredSlot.Id, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            issues.Add(Error(
                "artifacts",
                "artifacts.slot.required",
                $"Template '{template.Id}' requires artifact slot '{requiredSlot.Id}'."));
        }
    }

    private static void ValidateTranscript(EditPlan plan, ICollection<EditPlanValidationIssue> issues, bool checkReferencedFiles)
    {
        if (plan.Transcript is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(plan.Transcript.Path))
        {
            issues.Add(Error("transcript.path", "transcript.path.required", "Transcript path is required when transcript metadata is present."));
        }
        else if (checkReferencedFiles && !File.Exists(plan.Transcript.Path))
        {
            issues.Add(Error("transcript.path", "transcript.path.missing", $"Referenced transcript file does not exist: '{plan.Transcript.Path}'."));
        }

        if (plan.Transcript.SegmentCount is < 0)
        {
            issues.Add(Error("transcript.segmentCount", "transcript.segmentCount.invalid", "Transcript segment count cannot be negative."));
        }
    }

    private static void ValidateBeats(EditPlan plan, ICollection<EditPlanValidationIssue> issues, bool checkReferencedFiles)
    {
        if (plan.Beats is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(plan.Beats.Path))
        {
            issues.Add(Error("beats.path", "beats.path.required", "Beat track path is required when beat metadata is present."));
        }
        else if (checkReferencedFiles && !File.Exists(plan.Beats.Path))
        {
            issues.Add(Error("beats.path", "beats.path.missing", $"Referenced beat track file does not exist: '{plan.Beats.Path}'."));
        }

        if (plan.Beats.EstimatedBpm is <= 0)
        {
            issues.Add(Error("beats.estimatedBpm", "beats.estimatedBpm.invalid", "Estimated BPM must be greater than zero when provided."));
        }
    }

    private static void ValidateSubtitles(EditPlan plan, ICollection<EditPlanValidationIssue> issues, bool checkReferencedFiles)
    {
        if (plan.Subtitles is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(plan.Subtitles.Path))
        {
            issues.Add(Error("subtitles.path", "subtitles.path.required", "Subtitle path is required when subtitle metadata is present."));
            return;
        }

        if (checkReferencedFiles && !File.Exists(plan.Subtitles.Path))
        {
            issues.Add(Error("subtitles.path", "subtitles.path.missing", $"Referenced subtitle file does not exist: '{plan.Subtitles.Path}'."));
        }
    }

    private static void ValidateTimeline(
        EditPlan plan,
        ICollection<EditPlanValidationIssue> issues,
        EffectRegistry? effectRegistry)
    {
        if (plan.SchemaVersion == SchemaVersions.V2 && plan.Timeline is null)
        {
            issues.Add(Error(
                "timeline",
                "timeline.required",
                "Timeline is required when schemaVersion is 2."));
            return;
        }

        if (plan.Timeline is null)
        {
            return;
        }

        if (plan.SchemaVersion != SchemaVersions.V2)
        {
            issues.Add(Error(
                "timeline",
                "timeline.schemaVersion.mismatch",
                "Timeline is only supported when schemaVersion is 2."));
        }

        if (plan.Timeline.Duration.HasValue && plan.Timeline.Duration.Value <= TimeSpan.Zero)
        {
            issues.Add(Error(
                "timeline.duration",
                "timeline.duration.invalid",
                "Timeline duration must be greater than zero when provided."));
        }

        if (plan.Timeline.Resolution is { W: <= 0 } || plan.Timeline.Resolution is { H: <= 0 })
        {
            issues.Add(Error(
                "timeline.resolution",
                "timeline.resolution.invalid",
                "Timeline resolution width and height must both be greater than zero."));
        }

        if (plan.Timeline.FrameRate is <= 0)
        {
            issues.Add(Error(
                "timeline.frameRate",
                "timeline.frameRate.invalid",
                "Timeline frameRate must be greater than zero when provided."));
        }

        if (plan.Timeline.Tracks.Count == 0)
        {
            issues.Add(Error(
                "timeline.tracks",
                "timeline.tracks.required",
                "Timeline must contain at least one track."));
            return;
        }

        ValidateUniqueValues(
            plan.Timeline.Tracks.Select((track, index) => ((string?)track.Id, $"timeline.tracks[{index}].id")),
            "timeline.track.id.duplicate",
            "Timeline track id",
            issues);

        ValidateUniqueValues(
            plan.Timeline.Tracks.SelectMany((track, trackIndex) => track.Clips.Select(
                (clip, clipIndex) => ((string?)clip.Id, $"timeline.tracks[{trackIndex}].clips[{clipIndex}].id"))),
            "timeline.clip.id.duplicate",
            "Timeline clip id",
            issues);

        var trackIds = plan.Timeline.Tracks
            .Where(track => !string.IsNullOrWhiteSpace(track.Id))
            .Select(track => track.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (plan.Timeline.Tracks
            .SelectMany(track => track.Clips)
            .Any(clip => clip.Placeholder is not null)
            && plan.Timeline.Resolution is null)
        {
            issues.Add(Error(
                "timeline.resolution",
                "timeline.placeholder.resolution.required",
                "Timeline resolution is required when placeholder video clips are present."));
        }

        for (var trackIndex = 0; trackIndex < plan.Timeline.Tracks.Count; trackIndex++)
        {
            var track = plan.Timeline.Tracks[trackIndex];
            var trackPath = $"timeline.tracks[{trackIndex}]";

            if (string.IsNullOrWhiteSpace(track.Id))
            {
                issues.Add(Error(
                    $"{trackPath}.id",
                    "timeline.track.id.required",
                    "Timeline track id is required."));
            }

            ValidateTimelineEffects(track.Effects, $"{trackPath}.effects", trackIds, effectRegistry, issues);

            for (var clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
            {
                var clip = track.Clips[clipIndex];
                var clipPath = $"{trackPath}.clips[{clipIndex}]";

                if (string.IsNullOrWhiteSpace(clip.Id))
                {
                    issues.Add(Error(
                        $"{clipPath}.id",
                        "timeline.clip.id.required",
                        "Timeline clip id is required."));
                }

                if (clip.Start < TimeSpan.Zero)
                {
                    issues.Add(Error(
                        $"{clipPath}.start",
                        "timeline.clip.start.invalid",
                        "Timeline clip start must be zero or greater."));
                }

                if (clip.Duration.HasValue && clip.Duration.Value <= TimeSpan.Zero)
                {
                    issues.Add(Error(
                        $"{clipPath}.duration",
                        "timeline.clip.duration.invalid",
                        "Timeline clip duration must be greater than zero when provided."));
                }

                ValidatePlaceholderClip(track, clip, clipPath, issues);

                if (track.Kind == TrackKind.Video)
                {
                    if (clip.Placeholder is not null)
                    {
                        if (clip.Duration is null)
                        {
                            issues.Add(Error(
                                $"{clipPath}.duration",
                                "timeline.clip.placeholder.duration.required",
                                "Placeholder video clips must specify duration."));
                        }
                    }
                    else if (clip.InPoint is null || clip.OutPoint is null)
                    {
                        issues.Add(Error(
                            clipPath,
                            "timeline.clip.video.range.required",
                            "Video timeline clips must specify both in and out."));
                    }
                    else if (clip.OutPoint <= clip.InPoint)
                    {
                        issues.Add(Error(
                            clipPath,
                            "timeline.clip.video.range.invalid",
                            "Video timeline clip out must be greater than in."));
                    }
                }

                ValidateTransitions(clip, clipPath, issues);
                ValidateTimelineEffects(clip.Effects, $"{clipPath}.effects", trackIds, effectRegistry, issues);
            }
        }
    }

    private static void ValidatePlaceholderClip(
        TimelineTrack track,
        TimelineClip clip,
        string clipPath,
        ICollection<EditPlanValidationIssue> issues)
    {
        var placeholder = clip.Placeholder;
        if (placeholder is null)
        {
            return;
        }

        if (track.Kind != TrackKind.Video)
        {
            issues.Add(Error(
                $"{clipPath}.placeholder",
                "timeline.clip.placeholder.track.unsupported",
                "Placeholder clips are only supported on video tracks."));
        }

        if (!string.IsNullOrWhiteSpace(clip.Src))
        {
            issues.Add(Error(
                $"{clipPath}.src",
                "timeline.clip.placeholder.src.conflict",
                "Placeholder clips cannot specify src."));
        }

        if (clip.InPoint is not null || clip.OutPoint is not null)
        {
            issues.Add(Error(
                clipPath,
                "timeline.clip.placeholder.range.conflict",
                "Placeholder clips cannot specify in/out points."));
        }

        if (!string.Equals(placeholder.Kind, "color", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Error(
                $"{clipPath}.placeholder.kind",
                "timeline.clip.placeholder.kind.unsupported",
                $"Placeholder kind '{placeholder.Kind}' is not supported. Expected 'color'."));
            return;
        }

        if (string.IsNullOrWhiteSpace(placeholder.Color))
        {
            issues.Add(Error(
                $"{clipPath}.placeholder.color",
                "timeline.clip.placeholder.color.required",
                "Color placeholder clips must specify a non-empty color."));
        }
    }

    private static void ValidateTransitions(
        TimelineClip clip,
        string clipPath,
        ICollection<EditPlanValidationIssue> issues)
    {
        ValidateTransition(clip.Transitions?.In, $"{clipPath}.transitions.in", clip, issues);
        ValidateTransition(clip.Transitions?.Out, $"{clipPath}.transitions.out", clip, issues);
    }

    private static void ValidateTransition(
        Transition? transition,
        string transitionPath,
        TimelineClip clip,
        ICollection<EditPlanValidationIssue> issues)
    {
        if (transition is null)
        {
            return;
        }

        if (transition.Duration <= 0)
        {
            issues.Add(Error(
                $"{transitionPath}.duration",
                "timeline.transition.duration.invalid",
                "Timeline transition duration must be greater than zero."));
            return;
        }

        var clipDuration = GetTimelineClipDuration(clip);
        if (clipDuration is not null && TimeSpan.FromSeconds(transition.Duration) > clipDuration.Value)
        {
            issues.Add(Error(
                $"{transitionPath}.duration",
                "timeline.transition.duration.exceedsClip",
                "Timeline transition duration cannot exceed the clip duration."));
        }
    }

    private static void ValidateTimelineEffects(
        IReadOnlyList<TimelineEffect> effects,
        string effectsPath,
        IReadOnlySet<string> trackIds,
        EffectRegistry? effectRegistry,
        ICollection<EditPlanValidationIssue> issues)
    {
        for (var effectIndex = 0; effectIndex < effects.Count; effectIndex++)
        {
            var effect = effects[effectIndex];
            var effectPath = $"{effectsPath}[{effectIndex}]";

            if (string.IsNullOrWhiteSpace(effect.Type))
            {
                issues.Add(Error(
                    $"{effectPath}.type",
                    "timeline.effect.type.required",
                    "Timeline effect type is required."));
                continue;
            }

            if (effectRegistry is not null && effectRegistry.Get(effect.Type) is null)
            {
                issues.Add(Warning(
                    $"{effectPath}.type",
                    "timeline.effect.type.unknown",
                    $"Timeline effect type '{effect.Type}' is not registered."));
            }

            if (!string.Equals(effect.Type, "auto_ducking", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!TryGetStringExtension(effect.Extensions, "reference", out var referenceTrackId)
                || string.IsNullOrWhiteSpace(referenceTrackId)
                || !trackIds.Contains(referenceTrackId))
            {
                issues.Add(Error(
                    $"{effectPath}.reference",
                    "timeline.effect.reference.track.missing",
                    "auto_ducking effect reference must point to an existing timeline track id."));
            }
        }
    }

    private static bool TryGetStringExtension(
        IDictionary<string, JsonElement>? extensions,
        string key,
        out string? value)
    {
        value = null;
        if (extensions is null || !extensions.TryGetValue(key, out var element))
        {
            return false;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = element.GetString();
        return true;
    }

    private static TimeSpan? GetTimelineClipDuration(TimelineClip clip)
    {
        if (clip.Duration is not null)
        {
            return clip.Duration;
        }

        if (clip.InPoint is not null && clip.OutPoint is not null && clip.OutPoint > clip.InPoint)
        {
            return clip.OutPoint - clip.InPoint;
        }

        return null;
    }

    private static void ValidateUniqueValues(
        IEnumerable<(string? Value, string Path)> values,
        string code,
        string label,
        ICollection<EditPlanValidationIssue> issues)
    {
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (value, path) in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (seen.TryGetValue(value, out var firstPath))
            {
                issues.Add(Error(path, code, $"{label} '{value}' is duplicated. First seen at '{firstPath}'."));
                continue;
            }

            seen[value] = path;
        }
    }

    private static EditPlanValidationIssue Error(string path, string code, string message)
        => new()
        {
            Severity = EditPlanValidationSeverity.Error,
            Path = path,
            Code = code,
            Message = message
        };

    private static EditPlanValidationIssue Warning(string path, string code, string message)
        => new()
        {
            Severity = EditPlanValidationSeverity.Warning,
            Path = path,
            Code = code,
            Message = message
        };
}

public sealed record EditPlanValidationResult
{
    public EditPlanValidationMode CheckMode { get; init; } = EditPlanValidationMode.Basic;

    public IReadOnlyList<EditPlanValidationIssue> Issues { get; init; } = [];

    public bool IsValid => Issues.All(issue => issue.Severity != EditPlanValidationSeverity.Error);

    public EditPlanValidationStats Stats => EditPlanValidationStats.FromIssues(Issues);
}

public sealed record EditPlanValidationStats
{
    public required int TotalIssues { get; init; }

    public required int ErrorCount { get; init; }

    public required int WarningCount { get; init; }

    public required IReadOnlyDictionary<string, int> BySeverity { get; init; }

    public required IReadOnlyDictionary<string, int> ByCode { get; init; }

    public static EditPlanValidationStats FromIssues(IReadOnlyList<EditPlanValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        var errorCount = issues.Count(issue => issue.Severity == EditPlanValidationSeverity.Error);
        var warningCount = issues.Count - errorCount;
        var byCode = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var issue in issues)
        {
            byCode[issue.Code] = byCode.TryGetValue(issue.Code, out var count) ? count + 1 : 1;
        }

        return new EditPlanValidationStats
        {
            TotalIssues = issues.Count,
            ErrorCount = errorCount,
            WarningCount = warningCount,
            BySeverity = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["error"] = errorCount,
                ["warning"] = warningCount
            },
            ByCode = byCode
        };
    }
}

public sealed record EditPlanValidationIssue
{
    public EditPlanValidationSeverity Severity { get; init; }

    public required string Path { get; init; }

    public required string Code { get; init; }

    public required string Message { get; init; }

    public string Category => EditPlanValidationIssueMetadata.ResolveCategory(Code);

    public string CheckStage => EditPlanValidationIssueMetadata.ResolveCheckStage(Code);

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Suggestion => EditPlanValidationIssueMetadata.ResolveSuggestion(Code);
}

public enum EditPlanValidationSeverity
{
    Error,
    Warning
}

public enum EditPlanValidationMode
{
    Basic,
    Deep
}

internal static class EditPlanValidationIssueMetadata
{
    public static string ResolveCategory(string code)
        => code switch
        {
            var value when value.StartsWith("source.", StringComparison.OrdinalIgnoreCase) => "source",
            var value when value.StartsWith("output.", StringComparison.OrdinalIgnoreCase) => "output",
            var value when value.StartsWith("clips.", StringComparison.OrdinalIgnoreCase) => "clips",
            var value when value.StartsWith("audioTracks.", StringComparison.OrdinalIgnoreCase) => "audioTracks",
            var value when value.StartsWith("artifacts.", StringComparison.OrdinalIgnoreCase) => "artifacts",
            var value when value.StartsWith("transcript.", StringComparison.OrdinalIgnoreCase) => "transcript",
            var value when value.StartsWith("beats.", StringComparison.OrdinalIgnoreCase) => "beats",
            var value when value.StartsWith("subtitles.", StringComparison.OrdinalIgnoreCase) => "subtitles",
            var value when value.StartsWith("timeline.", StringComparison.OrdinalIgnoreCase) => "timeline",
            var value when value.StartsWith("template.", StringComparison.OrdinalIgnoreCase) => "template",
            var value when value.StartsWith("plan.", StringComparison.OrdinalIgnoreCase) => "plan",
            _ => "plan"
        };

    public static string ResolveCheckStage(string code)
        => code switch
        {
            "plan.parse.failed" => "load",
            "template.source.catalog.required" => "template",
            "template.id.unknown" => "template",
            "artifacts.slot.required" => "template",
            "artifacts.slotId.undeclared" => "template",
            "artifacts.kind.mismatch" => "template",
            var value when value.StartsWith("timeline.", StringComparison.OrdinalIgnoreCase) => "structure",
            var value when value.StartsWith("template.", StringComparison.OrdinalIgnoreCase) => "template",
            var value when value.EndsWith(".missing", StringComparison.OrdinalIgnoreCase) => "files",
            _ => "structure"
        };

    public static string? ResolveSuggestion(string code)
        => code switch
        {
            "plan.parse.failed" => "Fix edit.json syntax or schema version issues, then re-run validate-plan.",
            "source.inputPath.required" => "Set source.inputPath to the primary input media path.",
            "source.inputPath.missing" => "Fix source.inputPath so it points to an existing media file, or rerun without --check-files.",
            "output.path.required" => "Set output.path to the target rendered file path.",
            "output.container.required" => "Set output.container to the desired output container, such as 'mp4'.",
            "output.container.mismatch" => "Align output.container with the extension used by output.path.",
            "clips.id.required" => "Give each clip a stable non-empty id.",
            "clips.id.duplicate" => "Rename duplicated clip ids so each clip id is unique.",
            "clips.range.invalid" => "Adjust clip out so it is greater than clip in.",
            "audioTracks.id.required" => "Give each audio track a stable non-empty id.",
            "audioTracks.id.duplicate" => "Rename duplicated audio track ids so each audio track id is unique.",
            "audioTracks.path.required" => "Set audioTracks[].path to the referenced audio file.",
            "audioTracks.path.missing" => "Fix audioTracks[].path so it points to an existing audio file, or rerun without --check-files.",
            "artifacts.slotId.required" => "Set artifacts[].slotId to a declared template slot id.",
            "artifacts.slotId.duplicate" => "Keep one artifact binding per slot id or rename the duplicated slot id.",
            "artifacts.kind.required" => "Set artifacts[].kind to the declared artifact kind.",
            "artifacts.path.required" => "Set artifacts[].path to the artifact file path.",
            "artifacts.path.missing" => "Fix artifacts[].path so it points to an existing file, or rerun without --check-files.",
            "artifacts.slotId.undeclared" => "Use a slot id declared by the selected template, or remove the artifact binding.",
            "artifacts.kind.mismatch" => "Change artifacts[].kind so it matches the template slot's declared kind.",
            "artifacts.slot.required" => "Provide the required artifact slot, or choose a template that does not require it.",
            "transcript.path.required" => "Set transcript.path when transcript metadata is present.",
            "transcript.path.missing" => "Fix transcript.path so it points to an existing transcript file, or rerun without --check-files.",
            "transcript.segmentCount.invalid" => "Set transcript.segmentCount to zero or a positive integer.",
            "beats.path.required" => "Set beats.path when beat metadata is present.",
            "beats.path.missing" => "Fix beats.path so it points to an existing beat file, or rerun without --check-files.",
            "beats.estimatedBpm.invalid" => "Set beats.estimatedBpm to a value greater than zero, or omit it.",
            "subtitles.path.required" => "Set subtitles.path when subtitle metadata is present.",
            "subtitles.path.missing" => "Fix subtitles.path so it points to an existing subtitle file, or rerun without --check-files.",
            "plan.schemaVersion.unsupported" => "Use schemaVersion 1 for the current v1 model, or schemaVersion 2 together with timeline.",
            "template.id.required" => "Set template.id when template metadata is present.",
            "template.id.unknown" => "Use a built-in template id or provide the matching plugin catalog with --plugin-dir.",
            "template.source.catalog.required" => "Re-run validate-plan with --plugin-dir pointing at the plugin catalog that provides this template.",
            "template.source.kind.required" => "Set template.source.kind to 'builtIn' or 'plugin'.",
            "template.source.kind.invalid" => "Use a supported template.source.kind value: 'builtIn' or 'plugin'.",
            "template.source.pluginId.required" => "Set template.source.pluginId to the plugin that owns this template.",
            "template.source.pluginId.unexpected" => "Remove template.source.pluginId when template.source.kind is 'builtIn'.",
            "timeline.required" => "Add timeline when schemaVersion is 2, or switch schemaVersion back to 1.",
            "timeline.schemaVersion.mismatch" => "Set schemaVersion to 2 when using timeline.",
            "timeline.duration.invalid" => "Set timeline.duration to a value greater than zero, or omit it.",
            "timeline.resolution.invalid" => "Set timeline.resolution.w and timeline.resolution.h to values greater than zero.",
            "timeline.frameRate.invalid" => "Set timeline.frameRate to a value greater than zero, or omit it.",
            "timeline.tracks.required" => "Add at least one timeline track when timeline is present.",
            "timeline.track.id.required" => "Give each timeline track a stable non-empty id.",
            "timeline.track.id.duplicate" => "Rename duplicated timeline track ids so each track id is unique.",
            "timeline.clip.id.required" => "Give each timeline clip a stable non-empty id.",
            "timeline.clip.id.duplicate" => "Rename duplicated timeline clip ids so each clip id is unique across the whole timeline.",
            "timeline.clip.start.invalid" => "Set timeline clip start to zero or a positive value.",
            "timeline.clip.duration.invalid" => "Set timeline clip duration to a value greater than zero, or omit it.",
            "timeline.clip.video.range.required" => "Video timeline clips must provide both in and out.",
            "timeline.clip.video.range.invalid" => "Adjust timeline clip out so it is greater than clip in.",
            "timeline.transition.duration.invalid" => "Set transition duration to a value greater than zero.",
            "timeline.transition.duration.exceedsClip" => "Reduce transition duration so it does not exceed the clip duration.",
            "timeline.effect.type.required" => "Set each timeline effect type to a registered effect name.",
            "timeline.effect.type.unknown" => "Register the effect type before using it, or change it to a known effect name.",
            "timeline.effect.reference.track.missing" => "Set auto_ducking.reference to an existing timeline track id.",
            _ => null
        };
}
