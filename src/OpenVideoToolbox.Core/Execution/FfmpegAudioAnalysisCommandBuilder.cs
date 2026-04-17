using System.Text;

namespace OpenVideoToolbox.Core.Execution;

public sealed class FfmpegAudioAnalysisCommandBuilder
{
    public CommandPlan Build(AudioAnalysisRequest request, string executablePath = "ffmpeg")
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InputPath);

        var arguments = new List<string>
        {
            "-i",
            request.InputPath,
            "-vn",
            "-af",
            "loudnorm=print_format=json",
            "-f",
            "null",
            "-"
        };

        return new CommandPlan
        {
            ToolName = "ffmpeg",
            ExecutablePath = executablePath,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(request.InputPath)),
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
