using System.Globalization;
using System.Text.Json;

namespace OpenVideoToolbox.Core.Media;

public sealed class FfprobeJsonParser
{
    public MediaProbeResult Parse(string json, string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var formatElement = root.TryGetProperty("format", out var foundFormat) ? foundFormat : default;
        var streams = root.TryGetProperty("streams", out var foundStreams) && foundStreams.ValueKind == JsonValueKind.Array
            ? foundStreams
            : default;

        return new MediaProbeResult
        {
            SourcePath = sourcePath,
            FileName = GetFileName(sourcePath),
            Format = ParseFormat(formatElement),
            Streams = ParseStreams(streams)
        };
    }

    private static string GetFileName(string sourcePath)
    {
        var normalizedPath = sourcePath.Replace('\\', '/');
        return Path.GetFileName(normalizedPath);
    }

    private static MediaFormatInfo ParseFormat(JsonElement format)
    {
        if (format.ValueKind == JsonValueKind.Undefined || format.ValueKind == JsonValueKind.Null)
        {
            return new MediaFormatInfo();
        }

        return new MediaFormatInfo
        {
            ContainerName = GetString(format, "format_name"),
            ContainerLongName = GetString(format, "format_long_name"),
            Duration = ParseTimeSpan(GetString(format, "duration")),
            SizeBytes = ParseLong(GetString(format, "size")),
            Bitrate = ParseLong(GetString(format, "bit_rate"))
        };
    }

    private static IReadOnlyList<MediaStreamInfo> ParseStreams(JsonElement streams)
    {
        if (streams.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<MediaStreamInfo>();

        foreach (var stream in streams.EnumerateArray())
        {
            results.Add(new MediaStreamInfo
            {
                Index = GetInt(stream, "index") ?? 0,
                Kind = ParseKind(GetString(stream, "codec_type")),
                CodecName = GetString(stream, "codec_name"),
                CodecLongName = GetString(stream, "codec_long_name"),
                Language = GetNestedString(stream, "tags", "language"),
                Width = GetInt(stream, "width"),
                Height = GetInt(stream, "height"),
                FrameRate = ParseFrameRate(GetString(stream, "avg_frame_rate") ?? GetString(stream, "r_frame_rate")),
                Channels = GetInt(stream, "channels"),
                SampleRate = GetInt(stream, "sample_rate"),
                ChannelLayout = GetString(stream, "channel_layout"),
                Bitrate = ParseLong(GetString(stream, "bit_rate")),
                Duration = ParseTimeSpan(GetString(stream, "duration"))
            });
        }

        return results;
    }

    private static MediaStreamKind ParseKind(string? value)
    {
        return value switch
        {
            "video" => MediaStreamKind.Video,
            "audio" => MediaStreamKind.Audio,
            "subtitle" => MediaStreamKind.Subtitle,
            "data" => MediaStreamKind.Data,
            "attachment" => MediaStreamKind.Attachment,
            _ => MediaStreamKind.Unknown
        };
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            _ => null
        };
    }

    private static string? GetNestedString(JsonElement element, string objectName, string propertyName)
    {
        if (!element.TryGetProperty(objectName, out var nested) || nested.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return GetString(nested, propertyName);
    }

    private static int? GetInt(JsonElement element, string propertyName)
    {
        var value = GetString(element, propertyName);

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static long? ParseLong(string? value)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static TimeSpan? ParseTimeSpan(string? value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            ? TimeSpan.FromSeconds(seconds)
            : null;
    }

    private static double? ParseFrameRate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "0/0")
        {
            return null;
        }

        var parts = value.Split('/', StringSplitOptions.TrimEntries);
        if (parts.Length == 2
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var numerator)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var denominator)
            && denominator != 0)
        {
            return numerator / denominator;
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}
