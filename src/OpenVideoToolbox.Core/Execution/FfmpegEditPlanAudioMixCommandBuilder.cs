using System.Text;

namespace OpenVideoToolbox.Core.Execution;

public sealed class FfmpegEditPlanAudioMixCommandBuilder
{
    private readonly EditPlanAudioFilterGraphBuilder _audioGraphBuilder;

    public FfmpegEditPlanAudioMixCommandBuilder()
        : this(new EditPlanAudioFilterGraphBuilder())
    {
    }

    public FfmpegEditPlanAudioMixCommandBuilder(EditPlanAudioFilterGraphBuilder audioGraphBuilder)
    {
        _audioGraphBuilder = audioGraphBuilder;
    }

    public CommandPlan Build(EditPlanAudioMixRequest request, string executablePath = "ffmpeg")
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputPath);

        var graph = _audioGraphBuilder.Build(request.Plan);

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

        arguments.Add("-filter_complex");
        arguments.Add(string.Join(";", graph.Filters));
        arguments.Add("-map");
        arguments.Add(graph.OutputLabel);
        arguments.Add("-vn");

        foreach (var option in ResolveAudioCodecArguments(request.OutputPath))
        {
            arguments.Add(option);
        }

        arguments.Add(request.OutputPath);

        return new CommandPlan
        {
            ToolName = "ffmpeg",
            ExecutablePath = executablePath,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(request.OutputPath)),
            Arguments = arguments,
            CommandLine = BuildCommandLine(executablePath, arguments)
        };
    }

    private static IReadOnlyList<string> ResolveAudioCodecArguments(string outputPath)
    {
        var extension = Path.GetExtension(outputPath).ToLowerInvariant();
        return extension switch
        {
            ".wav" => ["-c:a", "pcm_s16le"],
            ".flac" => ["-c:a", "flac"],
            ".mp3" => ["-c:a", "libmp3lame", "-b:a", "192k"],
            ".aac" or ".m4a" => ["-c:a", "aac", "-b:a", "192k"],
            _ => throw new InvalidOperationException($"Unsupported mix-audio output extension '{extension}'. Use .wav, .flac, .mp3, .aac, or .m4a.")
        };
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
}
