using System.Globalization;
using System.Text.Json;
using OpenVideoToolbox.Core.Subtitles;

namespace OpenVideoToolbox.Core.Speech;

public sealed class WhisperCppJsonParser
{
    public TranscriptDocument Parse(string jsonContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonContent);

        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;
        var language = TryReadLanguage(root);
        var segments = new List<TranscriptSegment>();

        if (root.TryGetProperty("transcription", out var transcription) &&
            transcription.ValueKind == JsonValueKind.Array)
        {
            var index = 1;
            foreach (var item in transcription.EnumerateArray())
            {
                var text = ReadText(item);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var (start, end) = ReadOffsets(item);
                segments.Add(new TranscriptSegment
                {
                    Id = $"seg-{index:000}",
                    Start = start,
                    End = end,
                    Text = text.Trim()
                });
                index++;
            }
        }

        return new TranscriptDocument
        {
            Language = language,
            Segments = segments
        };
    }

    private static string? TryReadLanguage(JsonElement root)
    {
        if (root.TryGetProperty("result", out var result) &&
            result.ValueKind == JsonValueKind.Object &&
            result.TryGetProperty("language", out var language) &&
            language.ValueKind == JsonValueKind.String)
        {
            return language.GetString();
        }

        return null;
    }

    private static string? ReadText(JsonElement item)
    {
        if (!item.TryGetProperty("text", out var text) || text.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return text.GetString();
    }

    private static (TimeSpan Start, TimeSpan End) ReadOffsets(JsonElement item)
    {
        if (item.TryGetProperty("offsets", out var offsets) &&
            offsets.ValueKind == JsonValueKind.Object &&
            TryReadMilliseconds(offsets, "from", out var fromMs) &&
            TryReadMilliseconds(offsets, "to", out var toMs))
        {
            return (TimeSpan.FromMilliseconds(fromMs), TimeSpan.FromMilliseconds(toMs));
        }

        if (item.TryGetProperty("timestamps", out var timestamps) &&
            timestamps.ValueKind == JsonValueKind.Object &&
            TryReadTimestamp(timestamps, "from", out var start) &&
            TryReadTimestamp(timestamps, "to", out var end))
        {
            return (start, end);
        }

        return (TimeSpan.Zero, TimeSpan.Zero);
    }

    private static bool TryReadMilliseconds(JsonElement parent, string propertyName, out double milliseconds)
    {
        milliseconds = 0;
        if (!parent.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out milliseconds))
        {
            return true;
        }

        if (property.ValueKind == JsonValueKind.String &&
            double.TryParse(property.GetString(), NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out milliseconds))
        {
            return true;
        }

        return false;
    }

    private static bool TryReadTimestamp(JsonElement parent, string propertyName, out TimeSpan value)
    {
        value = default;
        if (!parent.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var rawValue = property.GetString();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        rawValue = rawValue.Replace(',', '.');
        return TimeSpan.TryParse(rawValue, CultureInfo.InvariantCulture, out value);
    }
}
