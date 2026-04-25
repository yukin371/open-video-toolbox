using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenVideoToolbox.Core;
using OpenVideoToolbox.Core.Editing;

namespace OpenVideoToolbox.Core.Execution;

/// <summary>
/// Structured error produced during filter generation, carrying enough context
/// for higher layers to identify the failing clip/effect segment.
/// </summary>
public sealed record FilterBuildError
{
    public required string Code { get; init; }

    public string? ClipId { get; init; }

    public string? EffectType { get; init; }

    public required int FilterIndex { get; init; }

    public string? FfmpegError { get; init; }

    public required string Message { get; init; }
}

/// <summary>
/// Builds an FFmpeg command plan from a v2 timeline-based edit plan.
/// </summary>
public sealed class FfmpegTimelineRenderCommandBuilder
{
    private static readonly Regex PlaceholderRegex = new(@"\{(?<name>[A-Za-z0-9_]+)\}", RegexOptions.Compiled);
    private static readonly HashSet<string> StillImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp",
        ".bmp",
        ".tif",
        ".tiff"
    };

    private readonly EffectRegistry _effectRegistry;

    public FfmpegTimelineRenderCommandBuilder(EffectRegistry? effectRegistry = null)
    {
        _effectRegistry = effectRegistry ?? BuiltInEffectCatalog.CreateRegistry();
    }

    public CommandPlan Build(EditPlanRenderRequest request, string executablePath = "ffmpeg")
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Plan);

        var timeline = request.Plan.Timeline
            ?? throw new ArgumentException(
                "Render request must contain a v2 timeline. For v1 plans, use FfmpegEditPlanRenderCommandBuilder instead.",
                nameof(request));

        ValidateTimeline(request.Plan, timeline);

        var inputs = CollectInputs(request.Plan, timeline);
        var inputIndices = inputs
            .Select((input, index) => new KeyValuePair<string, int>(input.Key, index))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        var graph = BuildFilterComplex(request.Plan, timeline, inputIndices);

        var arguments = new List<string>
        {
            request.OverwriteExisting ? "-y" : "-n"
        };

        foreach (var input in inputs)
        {
            if (input.Placeholder is { } placeholder)
            {
                AppendPlaceholderInputArguments(arguments, placeholder, timeline);
                continue;
            }

            if (input.LoopStillImage)
            {
                arguments.Add("-loop");
                arguments.Add("1");

                if (input.FrameRate is { } frameRate && frameRate > 0)
                {
                    arguments.Add("-framerate");
                    arguments.Add(frameRate.ToString(CultureInfo.InvariantCulture));
                }
            }

            arguments.Add("-i");
            arguments.Add(input.Path!);
        }

        arguments.Add("-filter_complex");
        arguments.Add(graph.FilterGraph);

        if (graph.VideoLabel is null && graph.AudioLabel is null)
        {
            throw new ArgumentException("Timeline render plan did not produce any output streams.", nameof(request));
        }

        if (graph.VideoLabel is null)
        {
            arguments.Add("-vn");
        }
        else
        {
            arguments.Add("-map");
            arguments.Add(FormatLabel(graph.VideoLabel));
            arguments.Add("-c:v");
            arguments.Add("libx264");
            arguments.Add("-pix_fmt");
            arguments.Add("yuv420p");
            arguments.Add("-movflags");
            arguments.Add("+faststart");
        }

        if (graph.AudioLabel is null)
        {
            arguments.Add("-an");
        }
        else
        {
            arguments.Add("-map");
            arguments.Add(FormatLabel(graph.AudioLabel));
            arguments.Add("-c:a");
            arguments.Add("aac");
            arguments.Add("-b:a");
            arguments.Add("192k");
            arguments.Add("-shortest");
        }

        arguments.Add(request.Plan.Output.Path);

        return new CommandPlan
        {
            SchemaVersion = SchemaVersions.V2,
            ToolName = "ffmpeg",
            ExecutablePath = executablePath,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(request.Plan.Output.Path)),
            Arguments = arguments,
            CommandLine = BuildCommandLine(executablePath, arguments)
        };
    }

    private TimelineRenderGraph BuildFilterComplex(
        EditPlan plan,
        EditPlanTimeline timeline,
        IReadOnlyDictionary<string, int> inputIndices)
    {
        var filters = new List<string>();
        var videoTrackLabels = new List<string>();
        var audioTrackLabels = new List<string>();

        foreach (var track in timeline.Tracks.Where(track => !track.Muted && track.Clips.Count > 0))
        {
            var (trackVideoLabel, trackAudioLabel) = ProcessTrack(plan, timeline, track, inputIndices, filters);
            if (!string.IsNullOrWhiteSpace(trackVideoLabel))
            {
                videoTrackLabels.Add(trackVideoLabel);
            }

            if (!string.IsNullOrWhiteSpace(trackAudioLabel))
            {
                audioTrackLabels.Add(trackAudioLabel);
            }
        }

        var videoLabel = ComposeOutputLabel(videoTrackLabels, filters, isVideo: true, "v_out");
        if (videoLabel is not null && plan.Subtitles?.Mode == SubtitleMode.BurnIn)
        {
            var burnLabel = "v_burn";
            filters.Add($"{FormatLabel(videoLabel)}subtitles='{EscapeFilterPath(plan.Subtitles.Path)}'{FormatLabel(burnLabel)}");
            videoLabel = burnLabel;
        }

        var audioLabel = ComposeOutputLabel(audioTrackLabels, filters, isVideo: false, "a_out");

        return new TimelineRenderGraph(string.Join(";", filters), videoLabel, audioLabel);
    }

    private (string? videoLabel, string? audioLabel) ProcessTrack(
        EditPlan plan,
        EditPlanTimeline timeline,
        TimelineTrack track,
        IReadOnlyDictionary<string, int> inputIndices,
        ICollection<string> filters)
    {
        var isVideo = track.Kind == TrackKind.Video;
        var clipLabels = new List<string>();

        foreach (var clip in track.Clips)
        {
            clipLabels.Add(ProcessClip(plan, timeline, track, clip, inputIndices, filters));
        }

        if (clipLabels.Count == 0)
        {
            return (null, null);
        }

        var currentLabel = clipLabels[0];
        for (var index = 1; index < clipLabels.Count; index++)
        {
            var transition = ResolveTransition(track.Clips[index - 1], track.Clips[index]);
            currentLabel = transition is null
                ? ConcatLabels(track, currentLabel, clipLabels[index], filters, isVideo, index)
                : ApplyTransition(track, currentLabel, clipLabels[index], transition, track.Clips[index].Start.TotalSeconds, filters, isVideo, index);
        }

        currentLabel = ApplyTrackEffects(track, timeline, currentLabel, filters);

        return isVideo ? (currentLabel, null) : (null, currentLabel);
    }

    private string ProcessClip(
        EditPlan plan,
        EditPlanTimeline timeline,
        TimelineTrack track,
        TimelineClip clip,
        IReadOnlyDictionary<string, int> inputIndices,
        ICollection<string> filters)
    {
        var inputKey = ResolveClipInputKey(plan, clip);
        var inputIndex = inputIndices[inputKey];
        var isVideo = track.Kind == TrackKind.Video;
        var inputLabel = isVideo ? $"{inputIndex}:v" : $"{inputIndex}:a";
        var outputLabel = BuildClipLabel(track, clip, isVideo);
        var filterParts = new List<string>();

        if (clip.Placeholder is not null)
        {
            filterParts.Add($"trim=duration={FormatFilterSeconds(clip.Duration!.Value)}");
        }
        else if (clip.InPoint is { } inPoint && clip.OutPoint is { } outPoint)
        {
            filterParts.Add(isVideo
                ? $"trim=start={FormatFilterSeconds(inPoint)}:end={FormatFilterSeconds(outPoint)}"
                : $"atrim=start={FormatFilterSeconds(inPoint)}:end={FormatFilterSeconds(outPoint)}");
        }
        else if (clip.Duration is { } duration)
        {
            filterParts.Add(isVideo
                ? $"trim=duration={FormatFilterSeconds(duration)}"
                : $"atrim=duration={FormatFilterSeconds(duration)}");
        }

        filterParts.Add(isVideo ? "setpts=PTS-STARTPTS" : "asetpts=PTS-STARTPTS");
        filterParts.AddRange(GenerateEffectFilters(track, timeline, clip, clip.Effects));

        filters.Add($"{FormatLabel(inputLabel)}{string.Join(",", filterParts)}{FormatLabel(outputLabel)}");
        return outputLabel;
    }

    private string ApplyTrackEffects(
        TimelineTrack track,
        EditPlanTimeline timeline,
        string currentLabel,
        ICollection<string> filters)
    {
        if (track.Effects.Count == 0)
        {
            return currentLabel;
        }

        var syntheticClip = track.Clips[0];
        var effectFilters = GenerateEffectFilters(track, timeline, syntheticClip, track.Effects);
        if (effectFilters.Count == 0)
        {
            return currentLabel;
        }

        var outputLabel = $"{BuildTrackLabel(track, track.Kind == TrackKind.Video)}_fx";
        filters.Add($"{FormatLabel(currentLabel)}{string.Join(",", effectFilters)}{FormatLabel(outputLabel)}");
        return outputLabel;
    }

    private string ConcatLabels(
        TimelineTrack track,
        string previousLabel,
        string nextLabel,
        ICollection<string> filters,
        bool isVideo,
        int index)
    {
        var outputLabel = $"{BuildTrackLabel(track, isVideo)}_concat_{index}";
        filters.Add(
            $"{FormatLabel(previousLabel)}{FormatLabel(nextLabel)}concat=n=2:v={(isVideo ? 1 : 0)}:a={(isVideo ? 0 : 1)}{FormatLabel(outputLabel)}");
        return outputLabel;
    }

    private string ApplyTransition(
        TimelineTrack track,
        string previousLabel,
        string nextLabel,
        Transition transition,
        double offset,
        ICollection<string> filters,
        bool isVideo,
        int index)
    {
        var outputLabel = $"{BuildTrackLabel(track, isVideo)}_transition_{index}";
        if (isVideo)
        {
            var transitionName = MapVideoTransitionType(transition.Type);
            filters.Add(
                $"{FormatLabel(previousLabel)}{FormatLabel(nextLabel)}xfade=transition={transitionName}:duration={transition.Duration.ToString("0.###", CultureInfo.InvariantCulture)}:offset={offset.ToString("0.###", CultureInfo.InvariantCulture)}{FormatLabel(outputLabel)}");
        }
        else
        {
            filters.Add(
                $"{FormatLabel(previousLabel)}{FormatLabel(nextLabel)}acrossfade=d={transition.Duration.ToString("0.###", CultureInfo.InvariantCulture)}{FormatLabel(outputLabel)}");
        }

        return outputLabel;
    }

    private static string? ComposeOutputLabel(
        IReadOnlyList<string> labels,
        ICollection<string> filters,
        bool isVideo,
        string outputLabel)
    {
        if (labels.Count == 0)
        {
            return null;
        }

        if (labels.Count == 1)
        {
            filters.Add($"{FormatLabel(labels[0])}{(isVideo ? "null" : "anull")}{FormatLabel(outputLabel)}");
            return outputLabel;
        }

        if (isVideo)
        {
            var currentLabel = labels[0];
            for (var index = 1; index < labels.Count; index++)
            {
                var intermediateLabel = index == labels.Count - 1 ? outputLabel : $"v_overlay_{index}";
                filters.Add($"{FormatLabel(currentLabel)}{FormatLabel(labels[index])}overlay=eof_action=pass{FormatLabel(intermediateLabel)}");
                currentLabel = intermediateLabel;
            }

            return outputLabel;
        }

        var joinedInputs = string.Concat(labels.Select(FormatLabel));
        filters.Add($"{joinedInputs}amix=inputs={labels.Count}:duration=longest{FormatLabel(outputLabel)}");
        return outputLabel;
    }

    private List<string> GenerateEffectFilters(
        TimelineTrack track,
        EditPlanTimeline timeline,
        TimelineClip clip,
        IReadOnlyList<TimelineEffect> effects)
    {
        var filters = new List<string>();

        foreach (var effect in effects)
        {
            var definition = _effectRegistry.Get(effect.Type);
            if (definition?.FfmpegTemplates?.Filters is not { Count: > 0 } templateFilters)
            {
                continue;
            }

            foreach (var template in templateFilters)
            {
                filters.Add(ApplyTemplate(effect, definition, template, new EffectRenderContext
                {
                    Clip = clip,
                    Track = track,
                    Timeline = timeline
                }));
            }
        }

        return filters;
    }

    private static string ApplyTemplate(
        TimelineEffect effect,
        IEffectDefinition definition,
        string template,
        EffectRenderContext context)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in definition.Parameters.Items)
        {
            if (TryGetParameterValue(effect.Extensions, parameter.Key, out var runtimeValue))
            {
                values[parameter.Key] = SerializeParameterValue(runtimeValue);
                continue;
            }

            if (parameter.Value.DefaultValue is { } defaultValue)
            {
                values[parameter.Key] = SerializeParameterValue(defaultValue);
                continue;
            }

            if (parameter.Value.Required)
            {
                throw new ArgumentException(
                    $"Timeline effect '{effect.Type}' is missing required parameter '{parameter.Key}' on clip '{context.Clip.Id}'.");
            }
        }

        var rendered = PlaceholderRegex.Replace(template, match =>
        {
            var name = match.Groups["name"].Value;
            if (values.TryGetValue(name, out var value))
            {
                return value;
            }

            throw new ArgumentException(
                $"Timeline effect '{effect.Type}' could not resolve template parameter '{name}' on clip '{context.Clip.Id}'.");
        });

        return rendered;
    }

    private static bool TryGetParameterValue(
        IDictionary<string, JsonElement>? extensions,
        string parameterName,
        out JsonElement value)
    {
        if (extensions is not null && extensions.TryGetValue(parameterName, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private static string SerializeParameterValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Array => value.GetRawText(),
            JsonValueKind.Object => value.GetRawText(),
            JsonValueKind.Null => string.Empty,
            _ => value.GetRawText()
        };
    }

    private static Transition? ResolveTransition(TimelineClip previousClip, TimelineClip nextClip)
    {
        if (previousClip.Transitions?.Out is { } previousOut && nextClip.Transitions?.In is { } nextIn)
        {
            return previousOut.Duration <= nextIn.Duration ? previousOut : nextIn;
        }

        return previousClip.Transitions?.Out ?? nextClip.Transitions?.In;
    }

    private static string MapVideoTransitionType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "dissolve" => "fade",
            _ => type.ToLowerInvariant()
        };
    }

    private static List<TimelineRenderInput> CollectInputs(EditPlan plan, EditPlanTimeline timeline)
    {
        var inputs = new List<TimelineRenderInput>();
        var byPath = new Dictionary<string, TimelineRenderInput>(StringComparer.OrdinalIgnoreCase);

        foreach (var track in timeline.Tracks)
        {
            foreach (var clip in track.Clips)
            {
                var key = ResolveClipInputKey(plan, clip);
                if (!byPath.TryGetValue(key, out var existing))
                {
                    existing = CreateTimelineRenderInput(plan, clip);
                    byPath[key] = existing;
                    inputs.Add(existing);
                }

                if (track.Kind == TrackKind.Video && existing.Path is { } path && IsStillImagePath(path))
                {
                    existing.LoopStillImage = true;
                    existing.FrameRate ??= timeline.FrameRate;
                }
            }
        }

        return inputs;
    }

    private static void ValidateTimeline(EditPlan plan, EditPlanTimeline timeline)
    {
        if (timeline.Tracks.Count == 0)
        {
            throw new ArgumentException("Timeline must contain at least one track.", nameof(timeline));
        }

        if (string.IsNullOrWhiteSpace(plan.Output.Path))
        {
            throw new ArgumentException("Timeline render plan must specify an output path.", nameof(plan));
        }

        if (plan.Subtitles is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(plan.Subtitles.Path);
        }

        foreach (var track in timeline.Tracks)
        {
            if (string.IsNullOrWhiteSpace(track.Id))
            {
                throw new ArgumentException("Timeline tracks must specify a non-empty id.", nameof(timeline));
            }

            foreach (var clip in track.Clips)
            {
                if (string.IsNullOrWhiteSpace(clip.Id))
                {
                    throw new ArgumentException($"Track '{track.Id}' contains a clip with an empty id.", nameof(timeline));
                }

                if (clip.Placeholder is not null)
                {
                    ValidatePlaceholderClip(timeline, track, clip);
                }
                else
                {
                    _ = ResolveClipSourcePath(plan, clip);
                }

                if (clip.Start < TimeSpan.Zero)
                {
                    throw new ArgumentException($"Clip '{clip.Id}' start must be non-negative.", nameof(timeline));
                }

                if (clip.InPoint.HasValue != clip.OutPoint.HasValue)
                {
                    throw new ArgumentException($"Clip '{clip.Id}' must specify both in/out or neither.", nameof(timeline));
                }

                if (clip.Placeholder is not null && (clip.InPoint.HasValue || clip.OutPoint.HasValue))
                {
                    throw new ArgumentException($"Placeholder clip '{clip.Id}' cannot specify in/out.", nameof(timeline));
                }

                if (clip.InPoint is { } inPoint && clip.OutPoint is { } outPoint && outPoint <= inPoint)
                {
                    throw new ArgumentException($"Clip '{clip.Id}' out must be greater than in.", nameof(timeline));
                }

                if (clip.Duration is null && (!clip.InPoint.HasValue || !clip.OutPoint.HasValue))
                {
                    throw new ArgumentException(
                        $"Clip '{clip.Id}' must provide either duration or both in/out points.",
                        nameof(timeline));
                }

                var clipDuration = GetClipDuration(clip);
                if (clip.Transitions?.In is { } transitionIn
                    && clipDuration is { } resolvedDuration
                    && transitionIn.Duration > resolvedDuration.TotalSeconds)
                {
                    throw new ArgumentException(
                        $"Clip '{clip.Id}' transition-in duration ({transitionIn.Duration}s) exceeds clip duration ({resolvedDuration.TotalSeconds}s). Code: ERR_TRANSITION_EXCEEDS_CLIP",
                        nameof(timeline));
                }

                if (clip.Transitions?.Out is { } transitionOut
                    && clipDuration is { } resolvedOutDuration
                    && transitionOut.Duration > resolvedOutDuration.TotalSeconds)
                {
                    throw new ArgumentException(
                        $"Clip '{clip.Id}' transition-out duration ({transitionOut.Duration}s) exceeds clip duration ({resolvedOutDuration.TotalSeconds}s). Code: ERR_TRANSITION_EXCEEDS_CLIP",
                        nameof(timeline));
                }
            }
        }
    }

    private static void ValidatePlaceholderClip(EditPlanTimeline timeline, TimelineTrack track, TimelineClip clip)
    {
        if (track.Kind != TrackKind.Video)
        {
            throw new ArgumentException($"Placeholder clip '{clip.Id}' is only supported on video tracks.", nameof(timeline));
        }

        if (!string.IsNullOrWhiteSpace(clip.Src))
        {
            throw new ArgumentException($"Placeholder clip '{clip.Id}' cannot specify src.", nameof(timeline));
        }

        if (clip.Duration is null || clip.Duration <= TimeSpan.Zero)
        {
            throw new ArgumentException($"Placeholder clip '{clip.Id}' must specify a positive duration.", nameof(timeline));
        }

        var placeholder = clip.Placeholder!;
        if (!string.Equals(placeholder.Kind, "color", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Placeholder clip '{clip.Id}' uses unsupported kind '{placeholder.Kind}'.",
                nameof(timeline));
        }

        if (string.IsNullOrWhiteSpace(placeholder.Color))
        {
            throw new ArgumentException($"Placeholder clip '{clip.Id}' must specify a non-empty color.", nameof(timeline));
        }

        if (timeline.Resolution is null || timeline.Resolution.W <= 0 || timeline.Resolution.H <= 0)
        {
            throw new ArgumentException(
                $"Placeholder clip '{clip.Id}' requires timeline resolution to build a video source.",
                nameof(timeline));
        }
    }

    private static string ResolveClipInputKey(EditPlan plan, TimelineClip clip)
    {
        return clip.Placeholder is null
            ? $"path:{ResolveClipSourcePath(plan, clip)}"
            : $"placeholder:{clip.Id}";
    }

    private static TimelineRenderInput CreateTimelineRenderInput(EditPlan plan, TimelineClip clip)
    {
        if (clip.Placeholder is { } placeholder)
        {
            return new TimelineRenderInput
            {
                Key = ResolveClipInputKey(plan, clip),
                Placeholder = placeholder
            };
        }

        return new TimelineRenderInput
        {
            Key = ResolveClipInputKey(plan, clip),
            Path = ResolveClipSourcePath(plan, clip)
        };
    }

    private static string ResolveClipSourcePath(EditPlan plan, TimelineClip clip)
    {
        var sourcePath = string.IsNullOrWhiteSpace(clip.Src) ? plan.Source.InputPath : clip.Src;
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException($"Clip '{clip.Id}' does not specify src and plan.source.inputPath is empty.");
        }

        return sourcePath;
    }

    private static TimeSpan? GetClipDuration(TimelineClip clip)
    {
        if (clip.Duration is { } duration)
        {
            return duration;
        }

        if (clip.InPoint is { } inPoint && clip.OutPoint is { } outPoint)
        {
            return outPoint - inPoint;
        }

        return null;
    }

    private static string BuildClipLabel(TimelineTrack track, TimelineClip clip, bool isVideo)
    {
        return $"{(isVideo ? "v" : "a")}_t{SanitizeLabel(track.Id)}_c{SanitizeLabel(clip.Id)}";
    }

    private static string BuildTrackLabel(TimelineTrack track, bool isVideo)
    {
        return $"{(isVideo ? "v" : "a")}_t{SanitizeLabel(track.Id)}";
    }

    private static string SanitizeLabel(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        }

        return sb.Length == 0 ? "_" : sb.ToString();
    }

    private static string FormatFilterSeconds(TimeSpan value)
    {
        return value.TotalSeconds.ToString("0.000", CultureInfo.InvariantCulture);
    }

    private static string EscapeFilterPath(string path)
    {
        var normalized = Path.GetFullPath(path).Replace("\\", "/", StringComparison.Ordinal);
        return normalized
            .Replace(":", "\\:", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal);
    }

    private static bool IsStillImagePath(string path)
    {
        var extension = Path.GetExtension(path);
        return !string.IsNullOrWhiteSpace(extension) && StillImageExtensions.Contains(extension);
    }

    private static void AppendPlaceholderInputArguments(
        ICollection<string> arguments,
        TimelineClipPlaceholder placeholder,
        EditPlanTimeline timeline)
    {
        if (!string.Equals(placeholder.Kind, "color", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Unsupported placeholder kind '{placeholder.Kind}'.", nameof(placeholder));
        }

        var resolution = timeline.Resolution
            ?? throw new ArgumentException("Timeline resolution is required for placeholder inputs.", nameof(timeline));

        var colorInput = $"color=c={placeholder.Color}:s={resolution.W}x{resolution.H}";
        if (timeline.FrameRate is { } frameRate && frameRate > 0)
        {
            colorInput += $":r={frameRate.ToString(CultureInfo.InvariantCulture)}";
        }

        arguments.Add("-f");
        arguments.Add("lavfi");
        arguments.Add("-i");
        arguments.Add(colorInput);
    }

    private static string BuildCommandLine(string executablePath, IReadOnlyList<string> arguments)
    {
        var builder = new StringBuilder(executablePath);
        foreach (var argument in arguments)
        {
            builder.Append(' ');
            builder.Append(Quote(argument));
        }

        return builder.ToString();
    }

    private static string Quote(string argument)
    {
        if (argument.Length == 0)
        {
            return "\"\"";
        }

        return argument.IndexOfAny([' ', '\t', '"']) >= 0
            ? $"\"{argument.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : argument;
    }

    private static string FormatLabel(string label) => $"[{label}]";

    private sealed class TimelineRenderInput
    {
        public required string Key { get; init; }

        public string? Path { get; init; }

        public TimelineClipPlaceholder? Placeholder { get; init; }

        public bool LoopStillImage { get; set; }

        public int? FrameRate { get; set; }
    }

    private sealed record TimelineRenderGraph(string FilterGraph, string? VideoLabel, string? AudioLabel);
}
