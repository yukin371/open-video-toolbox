using System.Globalization;
using System.Text;
using OpenVideoToolbox.Core.Editing;

namespace OpenVideoToolbox.Core.Execution;

public sealed class FfmpegEditPlanRenderCommandBuilder
{
    private readonly EditPlanAudioFilterGraphBuilder _audioGraphBuilder;

    public FfmpegEditPlanRenderCommandBuilder()
        : this(new EditPlanAudioFilterGraphBuilder())
    {
    }

    public FfmpegEditPlanRenderCommandBuilder(EditPlanAudioFilterGraphBuilder audioGraphBuilder)
    {
        _audioGraphBuilder = audioGraphBuilder;
    }

    public CommandPlan Build(EditPlanRenderRequest request, string executablePath = "ffmpeg")
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Plan);

        Validate(request.Plan);

        var arguments = new List<string>
        {
            request.OverwriteExisting ? "-y" : "-n",
            "-i",
            request.Plan.Source.InputPath
        };

        foreach (var audioTrack in request.Plan.AudioTracks)
        {
            arguments.Add("-i");
            arguments.Add(audioTrack.Path);
        }

        var graph = BuildFilterGraph(request.Plan);

        arguments.Add("-filter_complex");
        arguments.Add(graph.FilterGraph);
        arguments.Add("-map");
        arguments.Add(graph.VideoLabel);

        if (graph.AudioLabel is null)
        {
            arguments.Add("-an");
        }
        else
        {
            arguments.Add("-map");
            arguments.Add(graph.AudioLabel);
            arguments.Add("-c:a");
            arguments.Add("aac");
            arguments.Add("-b:a");
            arguments.Add("192k");
            arguments.Add("-shortest");
        }

        arguments.Add("-c:v");
        arguments.Add("libx264");
        arguments.Add("-pix_fmt");
        arguments.Add("yuv420p");
        arguments.Add("-movflags");
        arguments.Add("+faststart");
        arguments.Add(request.Plan.Output.Path);

        return new CommandPlan
        {
            ToolName = "ffmpeg",
            ExecutablePath = executablePath,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(request.Plan.Output.Path)),
            Arguments = arguments,
            CommandLine = BuildCommandLine(executablePath, arguments)
        };
    }

    private RenderFilterGraph BuildFilterGraph(EditPlan plan)
    {
        var filters = new List<string>();

        for (var index = 0; index < plan.Clips.Count; index++)
        {
            var clip = plan.Clips[index];
            filters.Add(
                $"[0:v]trim=start={FormatFilterSeconds(clip.InPoint)}:end={FormatFilterSeconds(clip.OutPoint)},setpts=PTS-STARTPTS[v{index}]");
            filters.Add(
                $"[0:a]atrim=start={FormatFilterSeconds(clip.InPoint)}:end={FormatFilterSeconds(clip.OutPoint)},asetpts=PTS-STARTPTS[a{index}]");
        }

        var concatInputs = new StringBuilder();
        for (var index = 0; index < plan.Clips.Count; index++)
        {
            concatInputs.Append($"[v{index}][a{index}]");
        }

        const string videoBaseLabel = "[v_base]";
        const string audioBaseLabel = "[a_base]";
        filters.Add($"{concatInputs}concat=n={plan.Clips.Count}:v=1:a=1{videoBaseLabel}{audioBaseLabel}");

        var videoLabel = videoBaseLabel;
        if (plan.Subtitles?.Mode == SubtitleMode.BurnIn)
        {
            const string burnInVideoLabel = "[v_burn]";
            filters.Add($"{videoBaseLabel}subtitles='{EscapeFilterPath(plan.Subtitles.Path)}'{burnInVideoLabel}");
            videoLabel = burnInVideoLabel;
        }

        var audioGraph = _audioGraphBuilder.Build(plan);
        filters.AddRange(audioGraph.Filters.Skip(plan.Clips.Count + 1));

        return new RenderFilterGraph(string.Join(";", filters), videoLabel, audioGraph.OutputLabel);
    }

    private static void Validate(EditPlan plan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.Source.InputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.Output.Path);

        if (plan.Clips.Count == 0)
        {
            throw new ArgumentException("Render plan must contain at least one clip.", nameof(plan));
        }

        EditPlanAudioFilterGraphBuilder.Validate(plan);

        if (plan.Subtitles is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(plan.Subtitles.Path);
        }
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

    private sealed record RenderFilterGraph(string FilterGraph, string VideoLabel, string? AudioLabel);
}
