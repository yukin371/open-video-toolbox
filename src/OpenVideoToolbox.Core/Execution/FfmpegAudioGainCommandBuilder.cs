using System.Globalization;
using System.Text;

namespace OpenVideoToolbox.Core.Execution;

public sealed class FfmpegAudioGainCommandBuilder
{
    public CommandPlan Build(AudioGainRequest request, string executablePath = "ffmpeg")
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputPath);

        var arguments = new List<string>
        {
            request.OverwriteExisting ? "-y" : "-n",
            "-i",
            request.InputPath,
            "-vn",
            "-af",
            $"volume={request.GainDb.ToString("0.###", CultureInfo.InvariantCulture)}dB"
        };

        foreach (var option in FfmpegAudioOutputCodecArguments.Resolve(request.OutputPath, "audio-gain"))
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
