using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Subtitles;

namespace OpenVideoToolbox.Cli;

internal static class CliOptionParsing
{
    public static bool TryParseFileCommand(
        string[] args,
        out string? inputPath,
        out IReadOnlyDictionary<string, string> options,
        out string? error)
    {
        inputPath = null;
        options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        error = null;

        if (args.Length == 0)
        {
            error = "Missing input file path.";
            return false;
        }

        inputPath = args[0];
        if (!TryParseOptions(args.Skip(1).ToArray(), out var parsedOptions, out error))
        {
            return false;
        }

        options = parsedOptions;
        return true;
    }

    public static bool TryParseOptions(string[] args, out IReadOnlyDictionary<string, string> options, out string? error)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        error = null;

        for (var index = 0; index < args.Length; index++)
        {
            var token = args[index];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                error = $"Unexpected token '{token}'.";
                options = result;
                return false;
            }

            if (index == args.Length - 1 || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                result[token] = "true";
                continue;
            }

            result[token] = args[index + 1];
            index++;
        }

        options = result;
        return true;
    }

    public static string? GetOption(IReadOnlyDictionary<string, string> options, string name)
    {
        return options.TryGetValue(name, out var value) ? value : null;
    }

    public static bool TryGetRequiredOption(
        IReadOnlyDictionary<string, string> options,
        string name,
        out string? value,
        out string? error)
    {
        value = GetOption(options, name);
        if (!string.IsNullOrWhiteSpace(value))
        {
            error = null;
            return true;
        }

        error = $"Option '{name}' is required.";
        return false;
    }

    public static bool TryGetIntOption(
        IReadOnlyDictionary<string, string> options,
        string name,
        out int? value,
        out string? error)
    {
        value = null;
        error = null;

        if (!options.TryGetValue(name, out var rawValue))
        {
            return true;
        }

        if (int.TryParse(rawValue, out var parsed))
        {
            value = parsed;
            return true;
        }

        error = $"Option '{name}' expects an integer value.";
        return false;
    }

    public static bool TryGetBoolOption(
        IReadOnlyDictionary<string, string> options,
        string name,
        out bool? value,
        out string? error)
    {
        value = null;
        error = null;

        if (!options.TryGetValue(name, out var rawValue))
        {
            return true;
        }

        if (bool.TryParse(rawValue, out var parsed))
        {
            value = parsed;
            return true;
        }

        error = $"Option '{name}' expects a boolean value.";
        return false;
    }

    public static bool TryGetRequiredIntOption(
        IReadOnlyDictionary<string, string> options,
        string name,
        out int value,
        out string? error)
    {
        value = default;
        if (!options.TryGetValue(name, out var rawValue))
        {
            error = $"Option '{name}' is required.";
            return false;
        }

        if (int.TryParse(rawValue, out var parsed))
        {
            value = parsed;
            error = null;
            return true;
        }

        error = $"Option '{name}' expects an integer value.";
        return false;
    }

    public static bool TryGetRequiredDoubleOption(
        IReadOnlyDictionary<string, string> options,
        string name,
        out double value,
        out string? error)
    {
        value = default;
        if (!options.TryGetValue(name, out var rawValue))
        {
            error = $"Option '{name}' is required.";
            return false;
        }

        if (double.TryParse(rawValue, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            error = null;
            return true;
        }

        error = $"Option '{name}' expects a numeric value.";
        return false;
    }

    public static bool TryGetDoubleOption(
        IReadOnlyDictionary<string, string> options,
        string name,
        out double? value,
        out string? error)
    {
        value = null;
        error = null;

        if (!options.TryGetValue(name, out var rawValue))
        {
            return true;
        }

        if (double.TryParse(rawValue, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            return true;
        }

        error = $"Option '{name}' expects a numeric value.";
        return false;
    }

    public static bool TryGetRequiredTimeSpanOption(
        IReadOnlyDictionary<string, string> options,
        string name,
        out TimeSpan value,
        out string? error)
    {
        value = default;
        if (!options.TryGetValue(name, out var rawValue))
        {
            error = $"Option '{name}' is required.";
            return false;
        }

        if (TimeSpan.TryParse(rawValue, out var parsed))
        {
            value = parsed;
            error = null;
            return true;
        }

        error = $"Option '{name}' expects a time span like 00:00:12.500.";
        return false;
    }

    public static bool TryParseSubtitleFormat(string rawValue, out SubtitleFormat format, out string? error)
    {
        if (Enum.TryParse<SubtitleFormat>(rawValue, ignoreCase: true, out format))
        {
            error = null;
            return true;
        }

        error = "Option '--format' expects one of: srt, ass.";
        return false;
    }

    public static bool TryParseSubtitleModeOption(string? rawValue, out SubtitleMode? mode, out bool disableSubtitles, out string? error)
    {
        mode = null;
        disableSubtitles = false;
        error = null;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return true;
        }

        if (string.Equals(rawValue, "none", StringComparison.OrdinalIgnoreCase))
        {
            disableSubtitles = true;
            return true;
        }

        if (Enum.TryParse<SubtitleMode>(rawValue, ignoreCase: true, out var parsed))
        {
            mode = parsed;
            return true;
        }

        error = "Option '--subtitle-mode' expects one of: sidecar, burnIn, none.";
        return false;
    }

    public static bool TryParseSeedModeOption(string? rawValue, out EditPlanSeedMode? mode, out string? error)
    {
        mode = null;
        error = null;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return true;
        }

        if (Enum.TryParse<EditPlanSeedMode>(rawValue, ignoreCase: true, out var parsed))
        {
            mode = parsed;
            return true;
        }

        error = "Option '--seed-mode' expects one of: manual, transcript, beats.";
        return false;
    }

    public static bool TryParseEditPlanPathWriteStyleOption(string? rawValue, out EditPlanPathWriteStyle style, out string? error)
    {
        style = EditPlanPathWriteStyle.Auto;
        error = null;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return true;
        }

        if (Enum.TryParse<EditPlanPathWriteStyle>(rawValue, ignoreCase: true, out var parsed))
        {
            style = parsed;
            return true;
        }

        error = "Option '--path-style' expects one of: auto, relative, absolute.";
        return false;
    }

    public static bool TryParseRequiredSubtitleMode(string? rawValue, out SubtitleMode mode, out string? error)
    {
        mode = default;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            error = "Option '--subtitle-mode' is required.";
            return false;
        }

        if (Enum.TryParse<SubtitleMode>(rawValue, ignoreCase: true, out var parsed))
        {
            mode = parsed;
            error = null;
            return true;
        }

        error = "Option '--subtitle-mode' expects one of: sidecar, burnIn.";
        return false;
    }

    public static bool TryParseAudioTrackRole(string? rawValue, out AudioTrackRole? role, out string? error)
    {
        role = null;
        error = null;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return true;
        }

        if (Enum.TryParse<AudioTrackRole>(rawValue, ignoreCase: true, out var parsed))
        {
            role = parsed;
            return true;
        }

        error = "Option '--audio-track-role' expects one of: original, voice, bgm, effects.";
        return false;
    }
}
