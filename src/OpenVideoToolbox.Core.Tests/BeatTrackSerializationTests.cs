using System.Text.Json;
using OpenVideoToolbox.Core.Beats;
using OpenVideoToolbox.Core.Serialization;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class BeatTrackSerializationTests
{
    [Fact]
    public void BeatTrackDocument_RoundTrips_WithBeatMarkers()
    {
        var beatTrack = new BeatTrackDocument
        {
            SourcePath = "input.mp4",
            SampleRateHz = 16000,
            FrameDuration = TimeSpan.FromMilliseconds(50),
            EstimatedBpm = 120,
            Beats =
            [
                new BeatMarker
                {
                    Index = 0,
                    Time = TimeSpan.FromSeconds(0.5),
                    Strength = 0.82
                }
            ]
        };

        var json = JsonSerializer.Serialize(beatTrack, OpenVideoToolboxJson.Shared);
        var restored = JsonSerializer.Deserialize<BeatTrackDocument>(json, OpenVideoToolboxJson.Shared);

        Assert.Contains("\"estimatedBpm\": 120", json);
        Assert.NotNull(restored);
        Assert.Equal(16000, restored!.SampleRateHz);
        Assert.Single(restored.Beats);
        Assert.Equal(TimeSpan.FromSeconds(0.5), restored.Beats[0].Time);
    }
}
