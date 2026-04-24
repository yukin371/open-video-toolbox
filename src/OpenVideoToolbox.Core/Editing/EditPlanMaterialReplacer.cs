namespace OpenVideoToolbox.Core.Editing;

public sealed class EditPlanMaterialReplacer
{
    public EditPlanMaterialReplacementResult Replace(
        EditPlan plan,
        string outputBaseDirectory,
        EditPlanMaterialReplacementRequest request)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputBaseDirectory);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ResolvedPath);

        return request.Target switch
        {
            { Singleton: EditPlanInspectionTargetKeys.SourceInput } => ReplaceSource(plan, outputBaseDirectory, request),
            { Singleton: EditPlanInspectionTargetKeys.Transcript } => ReplaceTranscript(plan, outputBaseDirectory, request),
            { Singleton: EditPlanInspectionTargetKeys.Beats } => ReplaceBeats(plan, outputBaseDirectory, request),
            { Singleton: EditPlanInspectionTargetKeys.Subtitles } => ReplaceSubtitles(plan, outputBaseDirectory, request),
            { AudioTrackId: { } audioTrackId } => ReplaceAudioTrack(plan, outputBaseDirectory, request, audioTrackId),
            { ArtifactSlot: { } artifactSlot } => ReplaceArtifact(plan, outputBaseDirectory, request, artifactSlot),
            _ => throw new InvalidOperationException("Replacement target is required.")
        };
    }

    private static EditPlanMaterialReplacementResult ReplaceSource(
        EditPlan plan,
        string outputBaseDirectory,
        EditPlanMaterialReplacementRequest request)
    {
        var previousPath = plan.Source.InputPath;
        var nextPath = EditPlanPathWriteStyleResolver.FormatPath(
            outputBaseDirectory,
            request.ResolvedPath,
            previousPath,
            request.PathStyle,
            out var pathStyleApplied);

        return new EditPlanMaterialReplacementResult
        {
            UpdatedPlan = plan with
            {
                Source = plan.Source with
                {
                    InputPath = nextPath
                }
            },
            Target = new EditPlanInspectionReplaceTarget
            {
                TargetType = EditPlanInspectionTargetTypes.Source,
                TargetKey = EditPlanInspectionTargetKeys.SourceInput,
                Selector = request.Target
            },
            PreviousPath = previousPath,
            NextPath = nextPath,
            PathStyleApplied = pathStyleApplied,
            Changed = !string.Equals(previousPath, nextPath, StringComparison.Ordinal)
        };
    }

    private static EditPlanMaterialReplacementResult ReplaceTranscript(
        EditPlan plan,
        string outputBaseDirectory,
        EditPlanMaterialReplacementRequest request)
    {
        if (plan.Transcript is null)
        {
            throw new InvalidOperationException("Plan does not contain a transcript target to replace.");
        }

        var previousPath = plan.Transcript.Path;
        var nextPath = EditPlanPathWriteStyleResolver.FormatPath(
            outputBaseDirectory,
            request.ResolvedPath,
            previousPath,
            request.PathStyle,
            out var pathStyleApplied);

        return new EditPlanMaterialReplacementResult
        {
            UpdatedPlan = plan with
            {
                Transcript = plan.Transcript with
                {
                    Path = nextPath
                }
            },
            Target = new EditPlanInspectionReplaceTarget
            {
                TargetType = EditPlanInspectionTargetTypes.Transcript,
                TargetKey = EditPlanInspectionTargetKeys.Transcript,
                Selector = request.Target
            },
            PreviousPath = previousPath,
            NextPath = nextPath,
            PathStyleApplied = pathStyleApplied,
            Changed = !string.Equals(previousPath, nextPath, StringComparison.Ordinal)
        };
    }

    private static EditPlanMaterialReplacementResult ReplaceBeats(
        EditPlan plan,
        string outputBaseDirectory,
        EditPlanMaterialReplacementRequest request)
    {
        if (plan.Beats is null)
        {
            throw new InvalidOperationException("Plan does not contain a beats target to replace.");
        }

        var previousPath = plan.Beats.Path;
        var nextPath = EditPlanPathWriteStyleResolver.FormatPath(
            outputBaseDirectory,
            request.ResolvedPath,
            previousPath,
            request.PathStyle,
            out var pathStyleApplied);

        return new EditPlanMaterialReplacementResult
        {
            UpdatedPlan = plan with
            {
                Beats = plan.Beats with
                {
                    Path = nextPath
                }
            },
            Target = new EditPlanInspectionReplaceTarget
            {
                TargetType = EditPlanInspectionTargetTypes.Beats,
                TargetKey = EditPlanInspectionTargetKeys.Beats,
                Selector = request.Target
            },
            PreviousPath = previousPath,
            NextPath = nextPath,
            PathStyleApplied = pathStyleApplied,
            Changed = !string.Equals(previousPath, nextPath, StringComparison.Ordinal)
        };
    }

    private static EditPlanMaterialReplacementResult ReplaceSubtitles(
        EditPlan plan,
        string outputBaseDirectory,
        EditPlanMaterialReplacementRequest request)
    {
        if (plan.Subtitles is null)
        {
            throw new InvalidOperationException("Plan does not contain a subtitles target to replace.");
        }

        var previousPath = plan.Subtitles.Path;
        var nextPath = EditPlanPathWriteStyleResolver.FormatPath(
            outputBaseDirectory,
            request.ResolvedPath,
            previousPath,
            request.PathStyle,
            out var pathStyleApplied);
        var nextMode = request.SubtitleMode ?? plan.Subtitles.Mode;
        var changed =
            !string.Equals(previousPath, nextPath, StringComparison.Ordinal)
            || plan.Subtitles.Mode != nextMode;

        return new EditPlanMaterialReplacementResult
        {
            UpdatedPlan = plan with
            {
                Subtitles = plan.Subtitles with
                {
                    Path = nextPath,
                    Mode = nextMode
                }
            },
            Target = new EditPlanInspectionReplaceTarget
            {
                TargetType = EditPlanInspectionTargetTypes.Subtitles,
                TargetKey = EditPlanInspectionTargetKeys.Subtitles,
                Selector = request.Target
            },
            PreviousPath = previousPath,
            NextPath = nextPath,
            PathStyleApplied = pathStyleApplied,
            PreviousSubtitleMode = plan.Subtitles.Mode,
            NextSubtitleMode = nextMode,
            Changed = changed
        };
    }

    private static EditPlanMaterialReplacementResult ReplaceAudioTrack(
        EditPlan plan,
        string outputBaseDirectory,
        EditPlanMaterialReplacementRequest request,
        string audioTrackId)
    {
        var index = plan.AudioTracks
            .Select((track, idx) => new { track, idx })
            .FirstOrDefault(item => string.Equals(item.track.Id, audioTrackId, StringComparison.OrdinalIgnoreCase))
            ?.idx ?? -1;

        if (index < 0)
        {
            throw new InvalidOperationException($"Plan does not contain an audio track with id '{audioTrackId}'.");
        }

        var track = plan.AudioTracks[index];
        var previousPath = track.Path;
        var nextPath = EditPlanPathWriteStyleResolver.FormatPath(
            outputBaseDirectory,
            request.ResolvedPath,
            previousPath,
            request.PathStyle,
            out var pathStyleApplied);
        var updatedTracks = plan.AudioTracks.ToArray();
        updatedTracks[index] = track with
        {
            Path = nextPath
        };

        return new EditPlanMaterialReplacementResult
        {
            UpdatedPlan = plan with
            {
                AudioTracks = updatedTracks
            },
            Target = new EditPlanInspectionReplaceTarget
            {
                TargetType = EditPlanInspectionTargetTypes.AudioTrack,
                TargetKey = $"audioTrack:{track.Id}",
                Selector = request.Target
            },
            PreviousPath = previousPath,
            NextPath = nextPath,
            PathStyleApplied = pathStyleApplied,
            Changed = !string.Equals(previousPath, nextPath, StringComparison.Ordinal)
        };
    }

    private static EditPlanMaterialReplacementResult ReplaceArtifact(
        EditPlan plan,
        string outputBaseDirectory,
        EditPlanMaterialReplacementRequest request,
        string artifactSlot)
    {
        var index = plan.Artifacts
            .Select((artifact, idx) => new { artifact, idx })
            .FirstOrDefault(item => string.Equals(item.artifact.SlotId, artifactSlot, StringComparison.OrdinalIgnoreCase))
            ?.idx ?? -1;

        if (index < 0)
        {
            throw new InvalidOperationException($"Plan does not contain an artifact slot '{artifactSlot}' to replace.");
        }

        var artifact = plan.Artifacts[index];
        var previousPath = artifact.Path;
        var nextPath = EditPlanPathWriteStyleResolver.FormatPath(
            outputBaseDirectory,
            request.ResolvedPath,
            previousPath,
            request.PathStyle,
            out var pathStyleApplied);
        var updatedArtifacts = plan.Artifacts.ToArray();
        updatedArtifacts[index] = artifact with
        {
            Path = nextPath
        };

        return new EditPlanMaterialReplacementResult
        {
            UpdatedPlan = plan with
            {
                Artifacts = updatedArtifacts
            },
            Target = new EditPlanInspectionReplaceTarget
            {
                TargetType = EditPlanInspectionTargetTypes.Artifact,
                TargetKey = $"artifact:{artifact.SlotId}",
                Selector = request.Target
            },
            PreviousPath = previousPath,
            NextPath = nextPath,
            PathStyleApplied = pathStyleApplied,
            Changed = !string.Equals(previousPath, nextPath, StringComparison.Ordinal)
        };
    }
}

public sealed class EditPlanMaterialAttacher
{
    public EditPlanMaterialAttachmentResult Attach(
        EditPlan plan,
        string outputBaseDirectory,
        EditPlanMaterialAttachmentRequest request,
        IReadOnlyList<EditPlanTemplateDefinition>? availableTemplates = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputBaseDirectory);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ResolvedPath);

        return request.Target switch
        {
            { AudioTrackId: { } audioTrackId } => UpsertAudioTrack(plan, outputBaseDirectory, request, audioTrackId),
            { Singleton: EditPlanInspectionTargetKeys.Transcript } => AttachTranscript(plan, outputBaseDirectory, request),
            { Singleton: EditPlanInspectionTargetKeys.Beats } => AttachBeats(plan, outputBaseDirectory, request),
            { Singleton: EditPlanInspectionTargetKeys.Subtitles } => AttachSubtitles(plan, outputBaseDirectory, request),
            { ArtifactSlot: { } artifactSlot } => UpsertArtifact(plan, outputBaseDirectory, request, artifactSlot, availableTemplates),
            _ => throw new InvalidOperationException("Attachment target is required.")
        };
    }

    private static EditPlanMaterialAttachmentResult UpsertAudioTrack(
        EditPlan plan,
        string outputBaseDirectory,
        EditPlanMaterialAttachmentRequest request,
        string audioTrackId)
    {
        var existingIndex = plan.AudioTracks
            .Select((track, idx) => new { track, idx })
            .FirstOrDefault(item => string.Equals(item.track.Id, audioTrackId, StringComparison.OrdinalIgnoreCase))
            ?.idx ?? -1;
        var previousPath = existingIndex >= 0 ? plan.AudioTracks[existingIndex].Path : null;
        var previousRole = existingIndex >= 0 ? (AudioTrackRole?)plan.AudioTracks[existingIndex].Role : null;

        if (existingIndex < 0 && request.AudioTrackRole is null)
        {
            throw new InvalidOperationException(
                $"Attaching a new audio track with id '{audioTrackId}' requires an explicit audio track role.");
        }

        var nextPath = EditPlanPathWriteStyleResolver.FormatPath(
            outputBaseDirectory,
            request.ResolvedPath,
            previousPath,
            request.PathStyle,
            out var pathStyleApplied);
        var nextRole = request.AudioTrackRole ?? previousRole ?? throw new InvalidOperationException(
            $"Audio track '{audioTrackId}' could not determine a role.");
        var updatedTracks = plan.AudioTracks.ToList();

        if (existingIndex >= 0)
        {
            updatedTracks[existingIndex] = updatedTracks[existingIndex] with
            {
                Role = nextRole,
                Path = nextPath
            };
        }
        else
        {
            updatedTracks.Add(new AudioTrackMix
            {
                Id = audioTrackId,
                Role = nextRole,
                Path = nextPath
            });
        }

        return new EditPlanMaterialAttachmentResult
        {
            UpdatedPlan = plan with
            {
                AudioTracks = updatedTracks
            },
            Target = new EditPlanInspectionReplaceTarget
            {
                TargetType = EditPlanInspectionTargetTypes.AudioTrack,
                TargetKey = $"audioTrack:{audioTrackId}",
                Selector = request.Target
            },
            Added = existingIndex < 0,
            PreviousPath = previousPath,
            NextPath = nextPath,
            PathStyleApplied = pathStyleApplied,
            PreviousAudioTrackRole = previousRole,
            NextAudioTrackRole = nextRole
        };
    }

    private static EditPlanMaterialAttachmentResult AttachTranscript(
        EditPlan plan,
        string outputBaseDirectory,
        EditPlanMaterialAttachmentRequest request)
    {
        if (plan.Transcript is not null)
        {
            throw new InvalidOperationException("Plan already contains a transcript target. Use replace-plan-material instead.");
        }

        var nextPath = EditPlanPathWriteStyleResolver.FormatPath(
            outputBaseDirectory,
            request.ResolvedPath,
            existingPath: null,
            request.PathStyle,
            out var pathStyleApplied);

        return new EditPlanMaterialAttachmentResult
        {
            UpdatedPlan = plan with
            {
                Transcript = new EditTranscriptPlan
                {
                    Path = nextPath
                }
            },
            Target = new EditPlanInspectionReplaceTarget
            {
                TargetType = EditPlanInspectionTargetTypes.Transcript,
                TargetKey = EditPlanInspectionTargetKeys.Transcript,
                Selector = request.Target
            },
            Added = true,
            PreviousPath = null,
            NextPath = nextPath,
            PathStyleApplied = pathStyleApplied
        };
    }

    private static EditPlanMaterialAttachmentResult AttachBeats(
        EditPlan plan,
        string outputBaseDirectory,
        EditPlanMaterialAttachmentRequest request)
    {
        if (plan.Beats is not null)
        {
            throw new InvalidOperationException("Plan already contains a beats target. Use replace-plan-material instead.");
        }

        var nextPath = EditPlanPathWriteStyleResolver.FormatPath(
            outputBaseDirectory,
            request.ResolvedPath,
            existingPath: null,
            request.PathStyle,
            out var pathStyleApplied);

        return new EditPlanMaterialAttachmentResult
        {
            UpdatedPlan = plan with
            {
                Beats = new EditBeatTrackPlan
                {
                    Path = nextPath
                }
            },
            Target = new EditPlanInspectionReplaceTarget
            {
                TargetType = EditPlanInspectionTargetTypes.Beats,
                TargetKey = EditPlanInspectionTargetKeys.Beats,
                Selector = request.Target
            },
            Added = true,
            PreviousPath = null,
            NextPath = nextPath,
            PathStyleApplied = pathStyleApplied
        };
    }

    private static EditPlanMaterialAttachmentResult AttachSubtitles(
        EditPlan plan,
        string outputBaseDirectory,
        EditPlanMaterialAttachmentRequest request)
    {
        if (plan.Subtitles is not null)
        {
            throw new InvalidOperationException("Plan already contains a subtitles target. Use replace-plan-material instead.");
        }

        var nextPath = EditPlanPathWriteStyleResolver.FormatPath(
            outputBaseDirectory,
            request.ResolvedPath,
            existingPath: null,
            request.PathStyle,
            out var pathStyleApplied);
        var subtitleMode = request.SubtitleMode ?? SubtitleMode.Sidecar;

        return new EditPlanMaterialAttachmentResult
        {
            UpdatedPlan = plan with
            {
                Subtitles = new EditSubtitlePlan
                {
                    Path = nextPath,
                    Mode = subtitleMode
                }
            },
            Target = new EditPlanInspectionReplaceTarget
            {
                TargetType = EditPlanInspectionTargetTypes.Subtitles,
                TargetKey = EditPlanInspectionTargetKeys.Subtitles,
                Selector = request.Target
            },
            Added = true,
            PreviousPath = null,
            NextPath = nextPath,
            PathStyleApplied = pathStyleApplied,
            PreviousSubtitleMode = null,
            NextSubtitleMode = subtitleMode
        };
    }

    private static EditPlanMaterialAttachmentResult UpsertArtifact(
        EditPlan plan,
        string outputBaseDirectory,
        EditPlanMaterialAttachmentRequest request,
        string artifactSlot,
        IReadOnlyList<EditPlanTemplateDefinition>? availableTemplates)
    {
        var template = EditPlanTemplateResolution.ResolveTemplate(plan, availableTemplates);
        if (template is null)
        {
            throw new InvalidOperationException("Artifact slot attachment requires a resolvable template definition.");
        }

        var declaredSlot = template.ArtifactSlots
            .FirstOrDefault(slot => string.Equals(slot.Id, artifactSlot, StringComparison.OrdinalIgnoreCase));
        if (declaredSlot is null)
        {
            throw new InvalidOperationException(
                $"Template '{template.Id}' does not declare artifact slot '{artifactSlot}'.");
        }

        var existingIndex = plan.Artifacts
            .Select((artifact, idx) => new { artifact, idx })
            .FirstOrDefault(item => string.Equals(item.artifact.SlotId, artifactSlot, StringComparison.OrdinalIgnoreCase))
            ?.idx ?? -1;
        var previousPath = existingIndex >= 0 ? plan.Artifacts[existingIndex].Path : null;
        var nextPath = EditPlanPathWriteStyleResolver.FormatPath(
            outputBaseDirectory,
            request.ResolvedPath,
            previousPath,
            request.PathStyle,
            out var pathStyleApplied);
        var updatedArtifacts = plan.Artifacts.ToList();

        if (existingIndex >= 0)
        {
            updatedArtifacts[existingIndex] = updatedArtifacts[existingIndex] with
            {
                Kind = declaredSlot.Kind,
                Path = nextPath
            };
        }
        else
        {
            updatedArtifacts.Add(new EditArtifactReference
            {
                SlotId = declaredSlot.Id,
                Kind = declaredSlot.Kind,
                Path = nextPath
            });
        }

        return new EditPlanMaterialAttachmentResult
        {
            UpdatedPlan = plan with
            {
                Artifacts = updatedArtifacts
            },
            Target = new EditPlanInspectionReplaceTarget
            {
                TargetType = EditPlanInspectionTargetTypes.Artifact,
                TargetKey = $"artifact:{declaredSlot.Id}",
                Selector = request.Target
            },
            Added = existingIndex < 0,
            PreviousPath = previousPath,
            NextPath = nextPath,
            PathStyleApplied = pathStyleApplied
        };
    }
}

public sealed record EditPlanMaterialReplacementRequest
{
    public required EditPlanInspectionTargetSelector Target { get; init; }

    public required string ResolvedPath { get; init; }

    public EditPlanPathWriteStyle PathStyle { get; init; } = EditPlanPathWriteStyle.Auto;

    public SubtitleMode? SubtitleMode { get; init; }
}

public sealed record EditPlanMaterialReplacementResult
{
    public required EditPlan UpdatedPlan { get; init; }

    public required EditPlanInspectionReplaceTarget Target { get; init; }

    public required string PreviousPath { get; init; }

    public required string NextPath { get; init; }

    public required EditPlanPathWriteStyle PathStyleApplied { get; init; }

    public SubtitleMode? PreviousSubtitleMode { get; init; }

    public SubtitleMode? NextSubtitleMode { get; init; }

    public bool Changed { get; init; }
}

public sealed record EditPlanMaterialAttachmentRequest
{
    public required EditPlanInspectionTargetSelector Target { get; init; }

    public required string ResolvedPath { get; init; }

    public EditPlanPathWriteStyle PathStyle { get; init; } = EditPlanPathWriteStyle.Auto;

    public SubtitleMode? SubtitleMode { get; init; }

    public AudioTrackRole? AudioTrackRole { get; init; }
}

public sealed record EditPlanMaterialAttachmentResult
{
    public required EditPlan UpdatedPlan { get; init; }

    public required EditPlanInspectionReplaceTarget Target { get; init; }

    public bool Added { get; init; }

    public string? PreviousPath { get; init; }

    public required string NextPath { get; init; }

    public required EditPlanPathWriteStyle PathStyleApplied { get; init; }

    public SubtitleMode? PreviousSubtitleMode { get; init; }

    public SubtitleMode? NextSubtitleMode { get; init; }

    public AudioTrackRole? PreviousAudioTrackRole { get; init; }

    public AudioTrackRole? NextAudioTrackRole { get; init; }
}

public enum EditPlanPathWriteStyle
{
    Auto,
    Relative,
    Absolute
}

internal static class EditPlanPathWriteStyleResolver
{
    public static string FormatPath(
        string outputBaseDirectory,
        string absolutePath,
        string? existingPath,
        EditPlanPathWriteStyle requestedStyle,
        out EditPlanPathWriteStyle appliedStyle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputBaseDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);

        var normalizedAbsolutePath = Path.GetFullPath(absolutePath);

        switch (requestedStyle)
        {
            case EditPlanPathWriteStyle.Absolute:
                appliedStyle = EditPlanPathWriteStyle.Absolute;
                return normalizedAbsolutePath;
            case EditPlanPathWriteStyle.Relative:
                appliedStyle = EditPlanPathWriteStyle.Relative;
                return ToRelativePath(outputBaseDirectory, normalizedAbsolutePath);
            case EditPlanPathWriteStyle.Auto:
            default:
                if (!string.IsNullOrWhiteSpace(existingPath) && Path.IsPathRooted(existingPath))
                {
                    appliedStyle = EditPlanPathWriteStyle.Absolute;
                    return normalizedAbsolutePath;
                }

                if (CanRelativize(outputBaseDirectory, normalizedAbsolutePath))
                {
                    appliedStyle = EditPlanPathWriteStyle.Relative;
                    return ToRelativePath(outputBaseDirectory, normalizedAbsolutePath);
                }

                appliedStyle = EditPlanPathWriteStyle.Absolute;
                return normalizedAbsolutePath;
        }
    }

    private static string ToRelativePath(string outputBaseDirectory, string absolutePath)
    {
        if (!CanRelativize(outputBaseDirectory, absolutePath))
        {
            throw new InvalidOperationException(
                $"Cannot write a relative path from '{outputBaseDirectory}' to '{absolutePath}'.");
        }

        return Path.GetRelativePath(outputBaseDirectory, absolutePath);
    }

    private static bool CanRelativize(string outputBaseDirectory, string absolutePath)
    {
        var baseRoot = Path.GetPathRoot(Path.GetFullPath(outputBaseDirectory));
        var targetRoot = Path.GetPathRoot(Path.GetFullPath(absolutePath));
        return string.Equals(baseRoot, targetRoot, StringComparison.OrdinalIgnoreCase);
    }
}
