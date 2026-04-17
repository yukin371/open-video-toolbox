using System.Globalization;
using System.Text.RegularExpressions;
using OpenVideoToolbox.Core.Execution;

namespace OpenVideoToolbox.Core.Audio;

public sealed class SilenceDetectionParser
{
    private static readonly Regex StartPattern = new(@"silence_start:\s*(?<value>-?\d+(\.\d+)?)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex EndPattern = new(@"silence_end:\s*(?<end>-?\d+(\.\d+)?)\s*\|\s*silence_duration:\s*(?<duration>-?\d+(\.\d+)?)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public SilenceDetectionDocument Parse(ExecutionResult executionResult, string inputPath)
    {
        ArgumentNullException.ThrowIfNull(executionResult);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

        var segments = new List<SilenceSegment>();
        TimeSpan? pendingStart = null;

        foreach (var line in executionResult.OutputLines.Where(line => line.Channel == ProcessOutputChannel.StandardError))
        {
            var text = line.Text.Trim();

            var startMatch = StartPattern.Match(text);
            if (startMatch.Success && TryParseSeconds(startMatch.Groups["value"].Value, out var start))
            {
                pendingStart = start;
                continue;
            }

            var endMatch = EndPattern.Match(text);
            if (endMatch.Success &&
                pendingStart is { } segmentStart &&
                TryParseSeconds(endMatch.Groups["end"].Value, out var end) &&
                TryParseSeconds(endMatch.Groups["duration"].Value, out var duration))
            {
                segments.Add(new SilenceSegment
                {
                    Start = segmentStart,
                    End = end,
                    Duration = duration
                });
                pendingStart = null;
            }
        }

        return new SilenceDetectionDocument
        {
            InputPath = inputPath,
            Segments = segments
        };
    }

    private static bool TryParseSeconds(string rawValue, out TimeSpan value)
    {
        value = default;
        if (!double.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var seconds))
        {
            return false;
        }

        value = TimeSpan.FromSeconds(seconds);
        return true;
    }
}
