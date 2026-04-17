using System.Text;

namespace OpenVideoToolbox.Core.Execution;

public sealed class WhisperCppCommandBuilder
{
    public CommandPlan Build(WhisperCppExecutionRequest request, string executablePath = "whisper-cli")
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InputWavePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ModelPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputFilePrefix);

        var arguments = new List<string>
        {
            "-m",
            request.ModelPath,
            "-f",
            request.InputWavePath,
            "-ojf",
            "-of",
            request.OutputFilePrefix,
            "-np"
        };

        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            arguments.Add("-l");
            arguments.Add(request.Language);
        }

        if (request.TranslateToEnglish)
        {
            arguments.Add("-tr");
        }

        return new CommandPlan
        {
            ToolName = "whisper-cli",
            ExecutablePath = executablePath,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(request.OutputFilePrefix)),
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
