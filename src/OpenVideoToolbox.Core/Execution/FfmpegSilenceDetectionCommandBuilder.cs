using System.Globalization;
using System.Text;

namespace OpenVideoToolbox.Core.Execution;

public sealed class FfmpegSilenceDetectionCommandBuilder
{
    public CommandPlan Build(SilenceDetectionRequest request, string executablePath = "ffmpeg")
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InputPath);

        if (request.MinimumDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Minimum duration must be zero or greater.");
        }

        var durationSeconds = request.MinimumDuration.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
        var noiseDb = request.NoiseDb.ToString("0.###", CultureInfo.InvariantCulture);
        var arguments = new List<string>
        {
            "-i",
            request.InputPath,
            "-vn",
            "-af",
            $"silencedetect=noise={noiseDb}dB:d={durationSeconds}",
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
