using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfmpegAudioExtractCommandBuilderTests
{
    [Fact]
    public void Build_CreatesDeterministicAudioExtractCommandPlan()
    {
        var builder = new FfmpegAudioExtractCommandBuilder();
        var request = new AudioExtractRequest
        {
            InputPath = "samples/input/source.mp4",
            OutputPath = Path.Combine("output", "voice.m4a"),
            TrackIndex = 1,
            OverwriteExisting = true
        };

        var plan = builder.Build(request);

        Assert.Equal(
            [
                "-y",
                "-i",
                "samples/input/source.mp4",
                "-map",
                "0:a:1",
                "-vn",
                "-c",
                "copy",
                Path.Combine("output", "voice.m4a")
            ],
            plan.Arguments);
    }

    [Fact]
    public void Build_Throws_WhenTrackIndexIsNegative()
    {
        var builder = new FfmpegAudioExtractCommandBuilder();
        var request = new AudioExtractRequest
        {
            InputPath = "samples/input/source.mp4",
            OutputPath = "voice.m4a",
            TrackIndex = -1,
            OverwriteExisting = false
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Build(request));
    }
}
