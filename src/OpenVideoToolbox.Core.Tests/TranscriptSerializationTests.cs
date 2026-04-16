using System.Text.Json;
using OpenVideoToolbox.Core.Serialization;
using OpenVideoToolbox.Core.Subtitles;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class TranscriptSerializationTests
{
    [Fact]
    public void TranscriptDocument_RoundTrips_WithSegments()
    {
        var transcript = new TranscriptDocument
        {
            Language = "en",
            Segments =
            [
                new TranscriptSegment
                {
                    Id = "seg-001",
                    Start = TimeSpan.Zero,
                    End = TimeSpan.FromSeconds(1.5),
                    Text = "Hello"
                }
            ]
        };

        var json = JsonSerializer.Serialize(transcript, OpenVideoToolboxJson.Shared);
        var restored = JsonSerializer.Deserialize<TranscriptDocument>(json, OpenVideoToolboxJson.Shared);

        Assert.Contains("\"segments\":", json);
        Assert.Contains("\"language\": \"en\"", json);
        Assert.NotNull(restored);
        Assert.Equal("seg-001", restored!.Segments[0].Id);
        Assert.Equal(TimeSpan.FromSeconds(1.5), restored.Segments[0].End);
        Assert.Equal("Hello", restored.Segments[0].Text);
    }
}
