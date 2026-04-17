using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfmpegAudioGainCommandBuilderTests
{
    [Fact]
    public void Build_CreatesDeterministicAudioGainCommandPlan()
    {
        var builder = new FfmpegAudioGainCommandBuilder();
        var request = new AudioGainRequest
        {
            InputPath = "samples/input/source.mp4",
            OutputPath = Path.Combine("output", "gain.wav"),
            GainDb = -4.5,
            OverwriteExisting = true
        };

        var plan = builder.Build(request);

        Assert.Equal(
            [
                "-y",
                "-i",
                "samples/input/source.mp4",
                "-vn",
                "-af",
                "volume=-4.5dB",
                "-c:a",
                "pcm_s16le",
                Path.Combine("output", "gain.wav")
            ],
            plan.Arguments);
    }

    [Fact]
    public void Build_ThrowsForUnsupportedOutputExtension()
    {
        var builder = new FfmpegAudioGainCommandBuilder();
        var request = new AudioGainRequest
        {
            InputPath = "samples/input/source.mp4",
            OutputPath = "output/gain.ogg",
            GainDb = 3
        };

        Assert.Throws<InvalidOperationException>(() => builder.Build(request));
    }
}
