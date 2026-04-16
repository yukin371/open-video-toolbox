namespace OpenVideoToolbox.Core.Editing;

public sealed class EditPlanValidator
{
    public EditPlanValidationResult Validate(EditPlan plan, bool checkReferencedFiles = false)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var issues = new List<EditPlanValidationIssue>();
        var template = ResolveTemplate(plan, issues);

        ValidateSource(plan, issues, checkReferencedFiles);
        ValidateOutput(plan, issues);
        ValidateClips(plan, issues);
        ValidateAudioTracks(plan, issues, checkReferencedFiles);
        ValidateArtifacts(plan, template, issues, checkReferencedFiles);
        ValidateTranscript(plan, issues, checkReferencedFiles);
        ValidateBeats(plan, issues, checkReferencedFiles);
        ValidateSubtitles(plan, issues, checkReferencedFiles);

        return new EditPlanValidationResult
        {
            Issues = issues
        };
    }

    private static EditPlanTemplateDefinition? ResolveTemplate(EditPlan plan, ICollection<EditPlanValidationIssue> issues)
    {
        if (plan.Template is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(plan.Template.Id))
        {
            issues.Add(Error("template.id", "template.id.required", "Template id is required when template metadata is present."));
            return null;
        }

        try
        {
            return BuiltInEditPlanTemplateCatalog.GetRequired(plan.Template.Id);
        }
        catch (InvalidOperationException)
        {
            issues.Add(Error("template.id", "template.id.unknown", $"Unknown edit plan template '{plan.Template.Id}'."));
            return null;
        }
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
}

public sealed record EditPlanValidationResult
{
    public IReadOnlyList<EditPlanValidationIssue> Issues { get; init; } = [];

    public bool IsValid => Issues.All(issue => issue.Severity != EditPlanValidationSeverity.Error);
}

public sealed record EditPlanValidationIssue
{
    public EditPlanValidationSeverity Severity { get; init; }

    public required string Path { get; init; }

    public required string Code { get; init; }

    public required string Message { get; init; }
}

public enum EditPlanValidationSeverity
{
    Error,
    Warning
}
