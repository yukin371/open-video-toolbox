using System.Globalization;
using System.Text.Json;
using OpenVideoToolbox.Core.Execution;

namespace OpenVideoToolbox.Core.Audio;

public sealed class AudioAnalysisParser
{
    public AudioAnalysisDocument Parse(ExecutionResult executionResult, string inputPath)
    {
        ArgumentNullException.ThrowIfNull(executionResult);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

        var jsonBlock = ExtractLoudnormJson(executionResult.OutputLines);
        using var document = JsonDocument.Parse(jsonBlock);
        var root = document.RootElement;

        return new AudioAnalysisDocument
        {
            InputPath = inputPath,
            Analysis = new AudioAnalysisMetrics
            {
                IntegratedLoudness = ParseMetric(root, "input_i"),
                LoudnessRange = ParseMetric(root, "input_lra"),
                TruePeakDb = ParseMetric(root, "input_tp"),
                ThresholdDb = ParseMetric(root, "input_thresh"),
                TargetOffset = ParseMetric(root, "target_offset")
            }
        };
    }

    private static string ExtractLoudnormJson(IReadOnlyList<ProcessOutputLine> outputLines)
    {
        var lines = outputLines
            .Where(line => line.Channel == ProcessOutputChannel.StandardError)
            .Select(line => line.Text.Trim())
            .ToArray();

        var startIndex = Array.FindIndex(lines, line => line == "{");
        if (startIndex < 0)
        {
            throw new InvalidOperationException("FFmpeg loudnorm output did not contain a JSON payload.");
        }

        var endIndex = Array.FindIndex(lines, startIndex, line => line == "}");
        if (endIndex < 0)
        {
            throw new InvalidOperationException("FFmpeg loudnorm output ended before the JSON payload was complete.");
        }

        return string.Join(Environment.NewLine, lines[startIndex..(endIndex + 1)]);
    }

    private static double? ParseMetric(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDouble(out var numericValue) => numericValue,
            JsonValueKind.String => ParseMetricString(property.GetString()),
            _ => null
        };
    }

    private static double? ParseMetricString(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        return double.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}
