using OpenVideoToolbox.Core.Speech;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class WhisperCppJsonParserTests
{
    [Fact]
    public void Parse_MapsWhisperCppJsonToTranscriptDocument()
    {
        var parser = new WhisperCppJsonParser();

        var transcript = parser.Parse(
            """
            {
              "result": {
                "language": "en"
              },
              "transcription": [
                {
                  "text": " Hello world",
                  "offsets": {
                    "from": 500,
                    "to": 2100
                  }
                },
                {
                  "text": "Second line",
                  "offsets": {
                    "from": 2100,
                    "to": 3800
                  }
                }
              ]
            }
            """);

        Assert.Equal("en", transcript.Language);
        Assert.Equal(2, transcript.Segments.Count);
        Assert.Equal("seg-001", transcript.Segments[0].Id);
        Assert.Equal(TimeSpan.FromMilliseconds(500), transcript.Segments[0].Start);
        Assert.Equal(TimeSpan.FromMilliseconds(2100), transcript.Segments[0].End);
        Assert.Equal("Hello world", transcript.Segments[0].Text);
    }
}
