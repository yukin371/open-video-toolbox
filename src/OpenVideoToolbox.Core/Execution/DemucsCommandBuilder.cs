using System.Text;

namespace OpenVideoToolbox.Core.Execution;

public sealed class DemucsCommandBuilder
{
    public CommandPlan Build(DemucsExecutionRequest request, string executablePath = "demucs")
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Model);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TwoStems);

        var arguments = new List<string>
        {
            "-o",
            request.OutputDirectory,
            "-n",
            request.Model,
            "--two-stems",
            request.TwoStems,
            request.InputPath
        };

        return new CommandPlan
        {
            ToolName = "demucs",
            ExecutablePath = executablePath,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(request.OutputDirectory)),
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
