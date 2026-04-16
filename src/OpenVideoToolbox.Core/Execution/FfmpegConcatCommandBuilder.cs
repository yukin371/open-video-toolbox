using System.Text;

namespace OpenVideoToolbox.Core.Execution;

public sealed class FfmpegConcatCommandBuilder
{
    public CommandPlan Build(MediaConcatRequest request, string executablePath = "ffmpeg")
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InputListPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputPath);

        var arguments = new List<string>
        {
            request.OverwriteExisting ? "-y" : "-n",
            "-f",
            "concat",
            "-safe",
            "0",
            "-i",
            request.InputListPath
        };

        if (request.CopyStreams)
        {
            arguments.Add("-c");
            arguments.Add("copy");
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
