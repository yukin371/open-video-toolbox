using System.Text;

namespace OpenVideoToolbox.Core.Editing;

public static class NarratedSlidesVariableResolver
{
    public static NarratedSlidesManifest Resolve(
        NarratedSlidesManifest manifest,
        IReadOnlyDictionary<string, string>? overlayVariables = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(manifest.Video);

        var variables = MergeVariables(manifest.Variables, overlayVariables);

        return manifest with
        {
            Variables = variables,
            Video = ResolveVideo(manifest.Video, variables),
            Template = manifest.Template is null ? null : ResolveTemplate(manifest.Template, variables),
            Subtitles = manifest.Subtitles is null ? null : ResolveSubtitles(manifest.Subtitles, variables),
            Bgm = manifest.Bgm is null ? null : ResolveBgm(manifest.Bgm, variables),
            Sections = manifest.Sections
                .Select((section, index) => ResolveSection(section, variables, index))
                .ToArray()
        };
    }

    private static IReadOnlyDictionary<string, string> MergeVariables(
        IReadOnlyDictionary<string, string>? manifestVariables,
        IReadOnlyDictionary<string, string>? overlayVariables)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        AddVariables(merged, manifestVariables, "manifest.variables");
        AddVariables(merged, overlayVariables, "overlay variables");

        return merged;
    }

    private static void AddVariables(
        Dictionary<string, string> target,
        IReadOnlyDictionary<string, string>? source,
        string logicalName)
    {
        if (source is null)
        {
            return;
        }

        foreach (var pair in source)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new InvalidOperationException($"Narrated-slides {logicalName} contains an empty key.");
            }

            target[pair.Key.Trim()] = pair.Value ?? string.Empty;
        }
    }

    private static NarratedSlidesVideoManifest ResolveVideo(
        NarratedSlidesVideoManifest video,
        IReadOnlyDictionary<string, string> variables)
    {
        return video with
        {
            Id = ResolveString(video.Id, variables, "video.id"),
            Title = ResolveString(video.Title, variables, "video.title"),
            AspectRatio = ResolveString(video.AspectRatio, variables, "video.aspectRatio"),
            Output = ResolveString(video.Output, variables, "video.output"),
            ProgressBar = video.ProgressBar is null ? null : ResolveProgressBar(video.ProgressBar, variables)
        };
    }

    private static NarratedSlidesProgressBarManifest ResolveProgressBar(
        NarratedSlidesProgressBarManifest progressBar,
        IReadOnlyDictionary<string, string> variables)
    {
        return progressBar with
        {
            Color = ResolveString(progressBar.Color, variables, "video.progressBar.color"),
            BackgroundColor = ResolveString(progressBar.BackgroundColor, variables, "video.progressBar.backgroundColor")
        };
    }

    private static NarratedSlidesTemplateManifest ResolveTemplate(
        NarratedSlidesTemplateManifest template,
        IReadOnlyDictionary<string, string> variables)
    {
        return template with
        {
            Id = ResolveRequiredString(template.Id, variables, "template.id")
        };
    }

    private static NarratedSlidesSubtitleManifest ResolveSubtitles(
        NarratedSlidesSubtitleManifest subtitles,
        IReadOnlyDictionary<string, string> variables)
    {
        return subtitles with
        {
            Path = ResolveRequiredString(subtitles.Path, variables, "subtitles.path")
        };
    }

    private static NarratedSlidesBgmManifest ResolveBgm(
        NarratedSlidesBgmManifest bgm,
        IReadOnlyDictionary<string, string> variables)
    {
        return bgm with
        {
            Path = ResolveString(bgm.Path, variables, "bgm.path"),
            Slot = bgm.Slot is null ? null : ResolveSlot(bgm.Slot, variables, "bgm.slot")
        };
    }

    private static NarratedSlidesSlotManifest ResolveSlot(
        NarratedSlidesSlotManifest slot,
        IReadOnlyDictionary<string, string> variables,
        string fieldName)
    {
        return slot with
        {
            Name = ResolveRequiredString(slot.Name, variables, $"{fieldName}.name")
        };
    }

    private static NarratedSlidesSectionManifest ResolveSection(
        NarratedSlidesSectionManifest section,
        IReadOnlyDictionary<string, string> variables,
        int index)
    {
        return section with
        {
            Id = ResolveRequiredString(section.Id, variables, $"sections[{index}].id"),
            Title = ResolveString(section.Title, variables, $"sections[{index}].title"),
            Visual = ResolveVisual(section.Visual, variables, index),
            Voice = ResolveVoice(section.Voice, variables, index)
        };
    }

    private static NarratedSlidesVisualManifest ResolveVisual(
        NarratedSlidesVisualManifest visual,
        IReadOnlyDictionary<string, string> variables,
        int index)
    {
        return visual with
        {
            Kind = ResolveRequiredString(visual.Kind, variables, $"sections[{index}].visual.kind"),
            Path = ResolveString(visual.Path, variables, $"sections[{index}].visual.path"),
            Slot = visual.Slot is null ? null : ResolveSlot(visual.Slot, variables, $"sections[{index}].visual.slot")
        };
    }

    private static NarratedSlidesVoiceManifest ResolveVoice(
        NarratedSlidesVoiceManifest voice,
        IReadOnlyDictionary<string, string> variables,
        int index)
    {
        return voice with
        {
            Path = ResolveRequiredString(voice.Path, variables, $"sections[{index}].voice.path")
        };
    }

    private static string ResolveRequiredString(
        string value,
        IReadOnlyDictionary<string, string> variables,
        string fieldName)
    {
        return ResolveString(value, variables, fieldName)
            ?? throw new InvalidOperationException(
                $"Narrated-slides field '{fieldName}' resolved to null unexpectedly.");
    }

    private static string? ResolveString(
        string? value,
        IReadOnlyDictionary<string, string> variables,
        string fieldName)
    {
        if (value is null || !value.Contains('$', StringComparison.Ordinal))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);

        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '$')
            {
                builder.Append(value[index]);
                continue;
            }

            if (index + 2 < value.Length && value[index + 1] == '$' && value[index + 2] == '{')
            {
                var closingBrace = value.IndexOf('}', index + 3);
                if (closingBrace < 0)
                {
                    throw new InvalidOperationException(
                        $"Narrated-slides field '{fieldName}' contains an unterminated escaped variable expression.");
                }

                builder.Append("${");
                builder.Append(value, index + 3, closingBrace - index - 3);
                builder.Append('}');
                index = closingBrace;
                continue;
            }

            if (index + 1 < value.Length && value[index + 1] == '{')
            {
                var closingBrace = value.IndexOf('}', index + 2);
                if (closingBrace < 0)
                {
                    throw new InvalidOperationException(
                        $"Narrated-slides field '{fieldName}' contains an unterminated variable expression.");
                }

                var expression = value.Substring(index + 2, closingBrace - index - 2);
                var separatorIndex = expression.IndexOf(":-", StringComparison.Ordinal);
                var name = separatorIndex >= 0 ? expression[..separatorIndex] : expression;
                var defaultValue = separatorIndex >= 0 ? expression[(separatorIndex + 2)..] : null;
                name = name.Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new InvalidOperationException(
                        $"Narrated-slides field '{fieldName}' contains an empty variable name.");
                }

                if (variables.TryGetValue(name, out var resolvedValue))
                {
                    builder.Append(resolvedValue);
                }
                else if (defaultValue is not null)
                {
                    builder.Append(defaultValue);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Narrated-slides field '{fieldName}' contains unresolved variable '{name}'.");
                }

                index = closingBrace;
                continue;
            }

            builder.Append(value[index]);
        }

        return builder.ToString();
    }
}
