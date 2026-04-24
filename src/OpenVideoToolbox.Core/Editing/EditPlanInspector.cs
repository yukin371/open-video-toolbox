namespace OpenVideoToolbox.Core.Editing;

public sealed class EditPlanInspector
{
    public EditPlanInspectionResult Inspect(
        EditPlan plan,
        string baseDirectory,
        bool checkReferencedFiles = false,
        IReadOnlyList<EditPlanTemplateDefinition>? availableTemplates = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        var resolvedPlan = EditPlanPathResolver.ResolvePaths(plan, baseDirectory);
        var validation = new EditPlanValidator().Validate(resolvedPlan, checkReferencedFiles, availableTemplates);
        var template = EditPlanTemplateResolution.ResolveTemplate(plan, availableTemplates);
        var templateSlots = template?.ArtifactSlots.ToDictionary(slot => slot.Id, StringComparer.OrdinalIgnoreCase);
        var materials = BuildMaterials(plan, resolvedPlan, templateSlots);
        var replaceTargets = BuildReplaceTargets(plan);
        var missingBindings = BuildMissingBindings(plan, materials, template, checkReferencedFiles);
        var signals = BuildSignalStatuses(plan, resolvedPlan, template, checkReferencedFiles);

        return new EditPlanInspectionResult
        {
            Template = plan.Template is null
                ? null
                : new EditPlanInspectionTemplate
                {
                    Id = plan.Template.Id,
                    Version = plan.Template.Version,
                    Source = plan.Template.Source
                },
            Summary = new EditPlanInspectionSummary
            {
                ClipCount = plan.Clips.Count,
                AudioTrackCount = plan.AudioTracks.Count,
                ArtifactCount = plan.Artifacts.Count,
                HasTranscript = plan.Transcript is not null,
                HasBeats = plan.Beats is not null,
                HasSubtitles = plan.Subtitles is not null
            },
            Materials = materials,
            ReplaceTargets = replaceTargets,
            Signals = signals,
            MissingBindings = missingBindings,
            Validation = validation
        };
    }

    private static IReadOnlyList<EditPlanInspectionMaterial> BuildMaterials(
        EditPlan plan,
        EditPlan resolvedPlan,
        IReadOnlyDictionary<string, EditPlanArtifactSlot>? templateSlots)
    {
        var materials = new List<EditPlanInspectionMaterial>
        {
            new()
            {
                TargetType = EditPlanInspectionTargetTypes.Source,
                TargetKey = EditPlanInspectionTargetKeys.SourceInput,
                DisplayName = "Source Video",
                Path = plan.Source.InputPath,
                ResolvedPath = resolvedPlan.Source.InputPath,
                Exists = File.Exists(resolvedPlan.Source.InputPath),
                Replaceable = true
            }
        };

        for (var index = 0; index < plan.AudioTracks.Count; index++)
        {
            var track = plan.AudioTracks[index];
            var resolvedTrack = resolvedPlan.AudioTracks[index];
            materials.Add(new EditPlanInspectionMaterial
            {
                TargetType = EditPlanInspectionTargetTypes.AudioTrack,
                TargetKey = BuildAudioTrackTargetKey(track, index),
                Id = track.Id,
                Role = track.Role,
                Path = track.Path,
                ResolvedPath = resolvedTrack.Path,
                Exists = File.Exists(resolvedTrack.Path),
                Replaceable = true
            });
        }

        for (var index = 0; index < plan.Artifacts.Count; index++)
        {
            var artifact = plan.Artifacts[index];
            var resolvedArtifact = resolvedPlan.Artifacts[index];
            EditPlanArtifactSlot? slot = null;
            if (!string.IsNullOrWhiteSpace(artifact.SlotId))
            {
                templateSlots?.TryGetValue(artifact.SlotId, out slot);
            }

            materials.Add(new EditPlanInspectionMaterial
            {
                TargetType = EditPlanInspectionTargetTypes.Artifact,
                TargetKey = BuildArtifactTargetKey(artifact, index),
                SlotId = artifact.SlotId,
                Kind = artifact.Kind,
                Required = slot?.Required,
                Path = artifact.Path,
                ResolvedPath = resolvedArtifact.Path,
                Exists = File.Exists(resolvedArtifact.Path),
                Replaceable = true
            });
        }

        if (plan.Transcript is not null && resolvedPlan.Transcript is not null)
        {
            materials.Add(new EditPlanInspectionMaterial
            {
                TargetType = EditPlanInspectionTargetTypes.Transcript,
                TargetKey = EditPlanInspectionTargetKeys.Transcript,
                Path = plan.Transcript.Path,
                ResolvedPath = resolvedPlan.Transcript.Path,
                Exists = File.Exists(resolvedPlan.Transcript.Path),
                Replaceable = true
            });
        }

        if (plan.Beats is not null && resolvedPlan.Beats is not null)
        {
            materials.Add(new EditPlanInspectionMaterial
            {
                TargetType = EditPlanInspectionTargetTypes.Beats,
                TargetKey = EditPlanInspectionTargetKeys.Beats,
                Path = plan.Beats.Path,
                ResolvedPath = resolvedPlan.Beats.Path,
                Exists = File.Exists(resolvedPlan.Beats.Path),
                Replaceable = true
            });
        }

        if (plan.Subtitles is not null && resolvedPlan.Subtitles is not null)
        {
            materials.Add(new EditPlanInspectionMaterial
            {
                TargetType = EditPlanInspectionTargetTypes.Subtitles,
                TargetKey = EditPlanInspectionTargetKeys.Subtitles,
                Mode = plan.Subtitles.Mode,
                Path = plan.Subtitles.Path,
                ResolvedPath = resolvedPlan.Subtitles.Path,
                Exists = File.Exists(resolvedPlan.Subtitles.Path),
                Replaceable = true
            });
        }

        return materials;
    }

    private static IReadOnlyList<EditPlanInspectionReplaceTarget> BuildReplaceTargets(EditPlan plan)
    {
        var targets = new List<EditPlanInspectionReplaceTarget>
        {
            new()
            {
                TargetType = EditPlanInspectionTargetTypes.Source,
                TargetKey = EditPlanInspectionTargetKeys.SourceInput,
                Selector = new EditPlanInspectionTargetSelector
                {
                    Singleton = EditPlanInspectionTargetKeys.SourceInput
                }
            }
        };

        foreach (var track in plan.AudioTracks.Where(track => !string.IsNullOrWhiteSpace(track.Id)))
        {
            targets.Add(new EditPlanInspectionReplaceTarget
            {
                TargetType = EditPlanInspectionTargetTypes.AudioTrack,
                TargetKey = $"audioTrack:{track.Id}",
                Selector = new EditPlanInspectionTargetSelector
                {
                    AudioTrackId = track.Id
                }
            });
        }

        foreach (var artifact in plan.Artifacts.Where(artifact => !string.IsNullOrWhiteSpace(artifact.SlotId)))
        {
            targets.Add(new EditPlanInspectionReplaceTarget
            {
                TargetType = EditPlanInspectionTargetTypes.Artifact,
                TargetKey = $"artifact:{artifact.SlotId}",
                Selector = new EditPlanInspectionTargetSelector
                {
                    ArtifactSlot = artifact.SlotId
                }
            });
        }

        if (plan.Transcript is not null)
        {
            targets.Add(new EditPlanInspectionReplaceTarget
            {
                TargetType = EditPlanInspectionTargetTypes.Transcript,
                TargetKey = EditPlanInspectionTargetKeys.Transcript,
                Selector = new EditPlanInspectionTargetSelector
                {
                    Singleton = EditPlanInspectionTargetKeys.Transcript
                }
            });
        }

        if (plan.Beats is not null)
        {
            targets.Add(new EditPlanInspectionReplaceTarget
            {
                TargetType = EditPlanInspectionTargetTypes.Beats,
                TargetKey = EditPlanInspectionTargetKeys.Beats,
                Selector = new EditPlanInspectionTargetSelector
                {
                    Singleton = EditPlanInspectionTargetKeys.Beats
                }
            });
        }

        if (plan.Subtitles is not null)
        {
            targets.Add(new EditPlanInspectionReplaceTarget
            {
                TargetType = EditPlanInspectionTargetTypes.Subtitles,
                TargetKey = EditPlanInspectionTargetKeys.Subtitles,
                Selector = new EditPlanInspectionTargetSelector
                {
                    Singleton = EditPlanInspectionTargetKeys.Subtitles
                }
            });
        }

        return targets;
    }

    private static IReadOnlyList<EditPlanInspectionMissingBinding> BuildMissingBindings(
        EditPlan plan,
        IReadOnlyList<EditPlanInspectionMaterial> materials,
        EditPlanTemplateDefinition? template,
        bool checkReferencedFiles)
    {
        var missingBindings = new List<EditPlanInspectionMissingBinding>();

        if (checkReferencedFiles)
        {
            missingBindings.AddRange(materials
                .Where(material => !material.Exists)
                .Select(material => new EditPlanInspectionMissingBinding
                {
                    TargetType = material.TargetType,
                    TargetKey = material.TargetKey,
                    SlotId = material.SlotId,
                    Reason = EditPlanInspectionMissingBindingReasons.PathMissing,
                    Path = material.Path,
                    ResolvedPath = material.ResolvedPath
                }));
        }

        if (template is null)
        {
            return missingBindings;
        }

        foreach (var requiredSlot in template.ArtifactSlots.Where(slot => slot.Required))
        {
            if (plan.Artifacts.Any(artifact => string.Equals(artifact.SlotId, requiredSlot.Id, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            missingBindings.Add(new EditPlanInspectionMissingBinding
            {
                TargetType = EditPlanInspectionTargetTypes.Artifact,
                TargetKey = $"artifact:{requiredSlot.Id}",
                SlotId = requiredSlot.Id,
                Kind = requiredSlot.Kind,
                Reason = EditPlanInspectionMissingBindingReasons.RequiredSlotUnbound
            });
        }

        return missingBindings;
    }

    private static IReadOnlyList<EditPlanInspectionSignalStatus> BuildSignalStatuses(
        EditPlan plan,
        EditPlan resolvedPlan,
        EditPlanTemplateDefinition? template,
        bool checkReferencedFiles)
    {
        return
        [
            BuildTranscriptSignalStatus(plan, resolvedPlan, template, checkReferencedFiles),
            BuildBeatsSignalStatus(plan, resolvedPlan, template, checkReferencedFiles),
            BuildSubtitlesSignalStatus(plan, resolvedPlan, template, checkReferencedFiles)
        ];
    }

    private static EditPlanInspectionSignalStatus BuildTranscriptSignalStatus(
        EditPlan plan,
        EditPlan resolvedPlan,
        EditPlanTemplateDefinition? template,
        bool checkReferencedFiles)
    {
        return BuildSignalStatus(
            kind: EditPlanInspectionSignalKinds.Transcript,
            targetKey: EditPlanInspectionTargetKeys.Transcript,
            expectedByTemplate: template?.SupportingSignals.Any(signal => signal.Kind == EditPlanSupportingSignalKind.Transcript) == true,
            attachedPath: plan.Transcript?.Path,
            resolvedPath: resolvedPlan.Transcript?.Path,
            checkReferencedFiles,
            mode: null);
    }

    private static EditPlanInspectionSignalStatus BuildBeatsSignalStatus(
        EditPlan plan,
        EditPlan resolvedPlan,
        EditPlanTemplateDefinition? template,
        bool checkReferencedFiles)
    {
        return BuildSignalStatus(
            kind: EditPlanInspectionSignalKinds.Beats,
            targetKey: EditPlanInspectionTargetKeys.Beats,
            expectedByTemplate: template?.SupportingSignals.Any(signal => signal.Kind == EditPlanSupportingSignalKind.Beats) == true,
            attachedPath: plan.Beats?.Path,
            resolvedPath: resolvedPlan.Beats?.Path,
            checkReferencedFiles,
            mode: null);
    }

    private static EditPlanInspectionSignalStatus BuildSubtitlesSignalStatus(
        EditPlan plan,
        EditPlan resolvedPlan,
        EditPlanTemplateDefinition? template,
        bool checkReferencedFiles)
    {
        var expectedByTemplate = template is not null && (
            template.DefaultSubtitleMode is not null
            || template.ArtifactSlots.Any(slot =>
                string.Equals(slot.Kind, "subtitle", StringComparison.OrdinalIgnoreCase)
                || string.Equals(slot.Id, "subtitles", StringComparison.OrdinalIgnoreCase)));

        return BuildSignalStatus(
            kind: EditPlanInspectionSignalKinds.Subtitles,
            targetKey: EditPlanInspectionTargetKeys.Subtitles,
            expectedByTemplate,
            attachedPath: plan.Subtitles?.Path,
            resolvedPath: resolvedPlan.Subtitles?.Path,
            checkReferencedFiles,
            mode: plan.Subtitles?.Mode);
    }

    private static EditPlanInspectionSignalStatus BuildSignalStatus(
        string kind,
        string targetKey,
        bool expectedByTemplate,
        string? attachedPath,
        string? resolvedPath,
        bool checkReferencedFiles,
        SubtitleMode? mode)
    {
        var attached = !string.IsNullOrWhiteSpace(attachedPath);
        bool? exists = null;
        var fileStatus = EditPlanInspectionSignalFileStatuses.Unbound;

        if (attached)
        {
            if (checkReferencedFiles)
            {
                exists = File.Exists(resolvedPath!);
                fileStatus = exists == true
                    ? EditPlanInspectionSignalFileStatuses.Present
                    : EditPlanInspectionSignalFileStatuses.Missing;
            }
            else
            {
                fileStatus = EditPlanInspectionSignalFileStatuses.NotChecked;
            }
        }

        return new EditPlanInspectionSignalStatus
        {
            Kind = kind,
            TargetKey = targetKey,
            ExpectedByTemplate = expectedByTemplate,
            Attached = attached,
            BindingStatus = attached
                ? EditPlanInspectionSignalBindingStatuses.Attached
                : EditPlanInspectionSignalBindingStatuses.Unbound,
            FileStatus = fileStatus,
            Mode = mode,
            Path = attached ? attachedPath : null,
            ResolvedPath = attached ? resolvedPath : null,
            Exists = exists
        };
    }

    private static string BuildAudioTrackTargetKey(AudioTrackMix track, int index)
    {
        return string.IsNullOrWhiteSpace(track.Id)
            ? $"audioTrack[{index}]"
            : $"audioTrack:{track.Id}";
    }

    private static string BuildArtifactTargetKey(EditArtifactReference artifact, int index)
    {
        return string.IsNullOrWhiteSpace(artifact.SlotId)
            ? $"artifact[{index}]"
            : $"artifact:{artifact.SlotId}";
    }
}

public sealed record EditPlanInspectionResult
{
    public EditPlanInspectionTemplate? Template { get; init; }

    public required EditPlanInspectionSummary Summary { get; init; }

    public IReadOnlyList<EditPlanInspectionMaterial> Materials { get; init; } = [];

    public IReadOnlyList<EditPlanInspectionReplaceTarget> ReplaceTargets { get; init; } = [];

    public IReadOnlyList<EditPlanInspectionSignalStatus> Signals { get; init; } = [];

    public IReadOnlyList<EditPlanInspectionMissingBinding> MissingBindings { get; init; } = [];

    public required EditPlanValidationResult Validation { get; init; }
}

public sealed record EditPlanInspectionTemplate
{
    public required string Id { get; init; }

    public string? Version { get; init; }

    public EditTemplateSourceReference? Source { get; init; }
}

public sealed record EditPlanInspectionSummary
{
    public int ClipCount { get; init; }

    public int AudioTrackCount { get; init; }

    public int ArtifactCount { get; init; }

    public bool HasTranscript { get; init; }

    public bool HasBeats { get; init; }

    public bool HasSubtitles { get; init; }
}

public sealed record EditPlanInspectionMaterial
{
    public required string TargetType { get; init; }

    public required string TargetKey { get; init; }

    public string? DisplayName { get; init; }

    public string? Id { get; init; }

    public AudioTrackRole? Role { get; init; }

    public string? SlotId { get; init; }

    public string? Kind { get; init; }

    public bool? Required { get; init; }

    public SubtitleMode? Mode { get; init; }

    public required string Path { get; init; }

    public required string ResolvedPath { get; init; }

    public bool Exists { get; init; }

    public bool Replaceable { get; init; }
}

public sealed record EditPlanInspectionReplaceTarget
{
    public required string TargetType { get; init; }

    public required string TargetKey { get; init; }

    public required EditPlanInspectionTargetSelector Selector { get; init; }
}

public sealed record EditPlanInspectionTargetSelector
{
    public string? Singleton { get; init; }

    public string? AudioTrackId { get; init; }

    public string? ArtifactSlot { get; init; }
}

public sealed record EditPlanInspectionMissingBinding
{
    public required string TargetType { get; init; }

    public required string TargetKey { get; init; }

    public string? SlotId { get; init; }

    public string? Kind { get; init; }

    public required string Reason { get; init; }

    public string? Path { get; init; }

    public string? ResolvedPath { get; init; }
}

public sealed record EditPlanInspectionSignalStatus
{
    public required string Kind { get; init; }

    public required string TargetKey { get; init; }

    public bool ExpectedByTemplate { get; init; }

    public bool Attached { get; init; }

    public required string BindingStatus { get; init; }

    public required string FileStatus { get; init; }

    public SubtitleMode? Mode { get; init; }

    public string? Path { get; init; }

    public string? ResolvedPath { get; init; }

    public bool? Exists { get; init; }
}

public static class EditPlanInspectionTargetTypes
{
    public const string Source = "source";

    public const string AudioTrack = "audioTrack";

    public const string Artifact = "artifact";

    public const string Transcript = "transcript";

    public const string Beats = "beats";

    public const string Subtitles = "subtitles";
}

public static class EditPlanInspectionTargetKeys
{
    public const string SourceInput = "source.inputPath";

    public const string Transcript = "transcript";

    public const string Beats = "beats";

    public const string Subtitles = "subtitles";
}

public static class EditPlanInspectionMissingBindingReasons
{
    public const string PathMissing = "pathMissing";

    public const string RequiredSlotUnbound = "requiredSlotUnbound";
}

public static class EditPlanInspectionSignalKinds
{
    public const string Transcript = "transcript";

    public const string Beats = "beats";

    public const string Subtitles = "subtitles";
}

public static class EditPlanInspectionSignalBindingStatuses
{
    public const string Attached = "attached";

    public const string Unbound = "unbound";
}

public static class EditPlanInspectionSignalFileStatuses
{
    public const string Present = "present";

    public const string Missing = "missing";

    public const string NotChecked = "notChecked";

    public const string Unbound = "unbound";
}
