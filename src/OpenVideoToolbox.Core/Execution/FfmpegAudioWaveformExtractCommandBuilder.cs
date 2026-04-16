using System.Text;

namespace OpenVideoToolbox.Core.Execution;

public sealed class FfmpegAudioWaveformExtractCommandBuilder
{
    public CommandPlan Build(AudioWaveformExtractRequest request, string executablePath = "ffmpeg")
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputPath);

        if (request.SampleRateHz <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Sample rate must be greater than zero.");
        }

        var arguments = new List<string>
        {
            request.OverwriteExisting ? "-y" : "-n",
            "-i",
            request.InputPath,
            "-vn",
            "-ac",
            "1",
            "-ar",
            request.SampleRateHz.ToString(),
            "-c:a",
            "pcm_s16le",
            request.OutputPath
        };

        return new CommandPlan
        {
            ToolName = "ffmpeg",
            ExecutablePath = executablePath,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(request.OutputPath)),
            Arguments = arguments,
            CommandLine = BuildCommandLine(executablePath, arguments)
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
