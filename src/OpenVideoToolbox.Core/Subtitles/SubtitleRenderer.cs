using System.Globalization;
using System.Text;

namespace OpenVideoToolbox.Core.Subtitles;

public sealed class SubtitleRenderer
{
    public SubtitleRenderResult Render(SubtitleRenderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Transcript);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputPath);

        ValidateTranscript(request.Transcript);

        var orderedSegments = request.Transcript.Segments
            .OrderBy(segment => segment.Start)
            .ThenBy(segment => segment.End)
            .ToArray();

        var content = request.Format switch
        {
            SubtitleFormat.Srt => RenderSrt(orderedSegments, request.MaxLineLength),
            SubtitleFormat.Ass => RenderAss(orderedSegments, request.MaxLineLength),
            _ => throw new InvalidOperationException($"Unsupported subtitle format '{request.Format}'.")
        };

        return new SubtitleRenderResult
        {
            OutputPath = request.OutputPath,
            Format = request.Format,
            Content = content,
            SegmentCount = orderedSegments.Length,
            MaxLineLength = request.MaxLineLength
        };
    }

    private static string RenderSrt(IReadOnlyList<TranscriptSegment> segments, int maxLineLength)
    {
        var builder = new StringBuilder();

        for (var index = 0; index < segments.Count; index++)
        {
            var segment = segments[index];
            builder.Append(index + 1);
            builder.AppendLine();
            builder.Append(FormatSrtTimestamp(segment.Start));
            builder.Append(" --> ");
            builder.AppendLine(FormatSrtTimestamp(segment.End));
            builder.AppendLine(FormatCueText(segment, maxLineLength, SubtitleFormat.Srt));

            if (index < segments.Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private static string RenderAss(IReadOnlyList<TranscriptSegment> segments, int maxLineLength)
    {
        var builder = new StringBuilder();
        builder.AppendLine("[Script Info]");
        builder.AppendLine("ScriptType: v4.00+");
        builder.AppendLine("WrapStyle: 0");
        builder.AppendLine("ScaledBorderAndShadow: yes");
        builder.AppendLine();
        builder.AppendLine("[V4+ Styles]");
        builder.AppendLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");
        builder.AppendLine("Style: Default,Arial,42,&H00FFFFFF,&H000000FF,&H00000000,&H64000000,-1,0,0,0,100,100,0,0,1,2,0,2,48,48,42,1");
        builder.AppendLine();
        builder.AppendLine("[Events]");
        builder.AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");

        foreach (var segment in segments)
        {
            builder.Append("Dialogue: 0,");
            builder.Append(FormatAssTimestamp(segment.Start));
            builder.Append(',');
            builder.Append(FormatAssTimestamp(segment.End));
            builder.Append(",Default,,0,0,0,,");
            builder.AppendLine(FormatCueText(segment, maxLineLength, SubtitleFormat.Ass));
        }

        return builder.ToString();
    }

    private static string FormatCueText(TranscriptSegment segment, int maxLineLength, SubtitleFormat format)
    {
        var normalized = NormalizeText(segment.Text);
        var wrapped = WrapText(normalized, maxLineLength);
        var output = format switch
        {
            SubtitleFormat.Srt => wrapped,
            SubtitleFormat.Ass => wrapped
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("{", "(", StringComparison.Ordinal)
                .Replace("}", ")", StringComparison.Ordinal)
                .Replace(Environment.NewLine, "\\N", StringComparison.Ordinal)
                .Replace("\n", "\\N", StringComparison.Ordinal),
            _ => wrapped
        };

        return string.IsNullOrWhiteSpace(segment.Speaker)
            ? output
            : $"{segment.Speaker}: {output}";
    }

    private static string NormalizeText(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var paragraphs = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(CollapseWhitespace)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        return string.Join("\n", paragraphs);
    }

    private static string WrapText(string text, int maxLineLength)
    {
        if (maxLineLength <= 0)
        {
            return text;
        }

        var paragraphs = text
            .Split('\n', StringSplitOptions.None)
            .Select(paragraph => WrapParagraph(paragraph, maxLineLength));

        return string.Join(Environment.NewLine, paragraphs);
    }

    private static string WrapParagraph(string paragraph, int maxLineLength)
    {
        if (paragraph.Length <= maxLineLength)
        {
            return paragraph;
        }

        var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var builder = new StringBuilder();
        var currentLength = 0;

        foreach (var word in words)
        {
            if (currentLength == 0)
            {
                builder.Append(word);
                currentLength = word.Length;
                continue;
            }

            if (currentLength + 1 + word.Length > maxLineLength)
            {
                builder.AppendLine();
                builder.Append(word);
                currentLength = word.Length;
                continue;
            }

            builder.Append(' ');
            builder.Append(word);
            currentLength += 1 + word.Length;
        }

        return builder.ToString();
    }

    private static string CollapseWhitespace(string input)
    {
        var builder = new StringBuilder(input.Length);
        var pendingWhitespace = false;

        foreach (var character in input)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingWhitespace = builder.Length > 0;
                continue;
            }

            if (pendingWhitespace)
            {
                builder.Append(' ');
                pendingWhitespace = false;
            }

            builder.Append(character);
        }

        return builder.ToString().Trim();
    }

    private static void ValidateTranscript(TranscriptDocument transcript)
    {
        foreach (var segment in transcript.Segments)
        {
            if (segment.End <= segment.Start)
            {
                throw new ArgumentException("Transcript segments must end after they start.", nameof(transcript));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(segment.Text);
        }
    }

    private static string FormatSrtTimestamp(TimeSpan value)
    {
        var totalHours = (int)Math.Floor(value.TotalHours);
        return $"{totalHours:00}:{value.Minutes:00}:{value.Seconds:00},{value.Milliseconds:000}";
    }

    private static string FormatAssTimestamp(TimeSpan value)
    {
        var totalHours = (int)Math.Floor(value.TotalHours);
        var centiseconds = value.Milliseconds / 10;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{totalHours}:{value.Minutes:00}:{value.Seconds:00}.{centiseconds:00}");
    }
}
