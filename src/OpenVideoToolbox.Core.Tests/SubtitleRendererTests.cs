using OpenVideoToolbox.Core.Subtitles;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class SubtitleRendererTests
{
    [Fact]
    public void Render_Srt_ProducesSequentialCueBlocks()
    {
        var renderer = new SubtitleRenderer();
        var result = renderer.Render(
            new SubtitleRenderRequest
            {
                Transcript = new TranscriptDocument
                {
                    Language = "zh-CN",
                    Segments =
                    [
                        new TranscriptSegment
                        {
                            Id = "seg-001",
                            Start = TimeSpan.FromSeconds(0.5),
                            End = TimeSpan.FromSeconds(2.25),
                            Text = "Hello world from toolbox"
                        },
                        new TranscriptSegment
                        {
                            Id = "seg-002",
                            Start = TimeSpan.FromSeconds(3),
                            End = TimeSpan.FromSeconds(4.5),
                            Text = "Second line"
                        }
                    ]
                },
                Format = SubtitleFormat.Srt,
                OutputPath = "subtitles.srt",
                MaxLineLength = 12
            });

        Assert.Equal(2, result.SegmentCount);
        Assert.Contains("1", result.Content);
        Assert.Contains("00:00:00,500 --> 00:00:02,250", result.Content);
        Assert.Contains("Hello world", result.Content);
        Assert.Contains("from toolbox", result.Content);
        Assert.Contains("2", result.Content);
        Assert.Contains("00:00:03,000 --> 00:00:04,500", result.Content);
    }

    [Fact]
    public void Render_Ass_ProducesDialogueLinesAndEscapesLineBreaks()
    {
        var renderer = new SubtitleRenderer();
        var result = renderer.Render(
            new SubtitleRenderRequest
            {
                Transcript = new TranscriptDocument
                {
                    Segments =
                    [
                        new TranscriptSegment
                        {
                            Start = TimeSpan.FromSeconds(1),
                            End = TimeSpan.FromSeconds(2.34),
                            Text = "Wrapped subtitle line for ass rendering",
                            Speaker = "Host"
                        }
                    ]
                },
                Format = SubtitleFormat.Ass,
                OutputPath = "subtitles.ass",
                MaxLineLength = 14
            });

        Assert.Contains("[Script Info]", result.Content);
        Assert.Contains("Style: Default,Arial,42", result.Content);
        Assert.Contains("Dialogue: 0,0:00:01.00,0:00:02.34,Default,,0,0,0,,Host:", result.Content);
        Assert.Contains("\\N", result.Content);
    }

    [Fact]
    public void Render_ThrowsWhenSegmentEndIsNotAfterStart()
    {
        var renderer = new SubtitleRenderer();

        Assert.Throws<ArgumentException>(() => renderer.Render(
            new SubtitleRenderRequest
            {
                Transcript = new TranscriptDocument
                {
                    Segments =
                    [
                        new TranscriptSegment
                        {
                            Start = TimeSpan.FromSeconds(5),
                            End = TimeSpan.FromSeconds(5),
                            Text = "invalid"
                        }
                    ]
                },
                Format = SubtitleFormat.Srt,
                OutputPath = "subtitles.srt"
            }));
    }
}
