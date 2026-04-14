using System.Text;
using OpenVideoToolbox.Core.Jobs;
using OpenVideoToolbox.Core.Presets;

namespace OpenVideoToolbox.Core.Execution;

public sealed class FfmpegCommandBuilder
{
    public CommandPlan Build(JobDefinition job, string executablePath = "ffmpeg")
    {
        ArgumentNullException.ThrowIfNull(job);

        var arguments = new List<string>();
        var outputPath = BuildOutputPath(job.Output);

        arguments.Add(job.Output.OverwriteExisting ? "-y" : "-n");
        arguments.Add("-i");
        arguments.Add(job.Source.InputPath);

        ApplyPresetArguments(arguments, job.Preset);
        arguments.Add(outputPath);

        return new CommandPlan
        {
            ToolName = "ffmpeg",
            ExecutablePath = executablePath,
            WorkingDirectory = job.Output.OutputDirectory,
            Arguments = arguments,
            CommandLine = BuildCommandLine(executablePath, arguments)
        };
    }

    public static string BuildOutputPath(JobOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);

        var extension = NormalizeExtension(output.ContainerExtension);
        return Path.Combine(output.OutputDirectory, output.FileNameStem + extension);
    }

    private static void ApplyPresetArguments(List<string> arguments, PresetDefinition preset)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(preset);

        if (preset.Kind == PresetKind.AudioOnly)
        {
            arguments.Add("-vn");
        }
        else if (preset.Video is not null)
        {
            arguments.Add("-c:v");
            arguments.Add(preset.Video.Encoder);

            if (!string.IsNullOrWhiteSpace(preset.Video.Preset))
            {
                arguments.Add("-preset");
                arguments.Add(preset.Video.Preset);
            }

            if (preset.Video.Crf is not null)
            {
                arguments.Add("-crf");
                arguments.Add(preset.Video.Crf.Value.ToString());
            }

            if (!string.IsNullOrWhiteSpace(preset.Video.PixelFormat))
            {
                arguments.Add("-pix_fmt");
                arguments.Add(preset.Video.PixelFormat);
            }

            AppendExtraArguments(arguments, preset.Video.ExtraArguments);
        }
        else
        {
            arguments.Add("-vn");
        }

        if (preset.Audio is not null)
        {
            arguments.Add("-c:a");
            arguments.Add(preset.Audio.Encoder);

            if (preset.Audio.BitrateKbps is not null)
            {
                arguments.Add("-b:a");
                arguments.Add($"{preset.Audio.BitrateKbps.Value}k");
            }

            if (preset.Audio.Channels is not null)
            {
                arguments.Add("-ac");
                arguments.Add(preset.Audio.Channels.Value.ToString());
            }

            if (preset.Audio.SampleRate is not null)
            {
                arguments.Add("-ar");
                arguments.Add(preset.Audio.SampleRate.Value.ToString());
            }

            AppendExtraArguments(arguments, preset.Audio.ExtraArguments);
        }
        else
        {
            arguments.Add("-an");
        }

        if (preset.Output.FastStart && !HasFastStartArguments(preset.ExtraArguments))
        {
            arguments.Add("-movflags");
            arguments.Add("+faststart");
        }

        AppendExtraArguments(arguments, preset.ExtraArguments);
    }

    private static void AppendExtraArguments(List<string> arguments, IReadOnlyList<string> extraArguments)
    {
        foreach (var argument in extraArguments)
        {
            if (!string.IsNullOrWhiteSpace(argument))
            {
                arguments.Add(argument);
            }
        }
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("Container extension is required.", nameof(extension));
        }

        return extension.StartsWith('.') ? extension : $".{extension}";
    }

    private static bool HasFastStartArguments(IReadOnlyList<string> extraArguments)
    {
        for (var index = 0; index < extraArguments.Count - 1; index++)
        {
            if (string.Equals(extraArguments[index], "-movflags", StringComparison.OrdinalIgnoreCase)
                && extraArguments[index + 1].Contains("+faststart", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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
