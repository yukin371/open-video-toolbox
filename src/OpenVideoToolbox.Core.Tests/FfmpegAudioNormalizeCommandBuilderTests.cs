using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfmpegAudioNormalizeCommandBuilderTests
{
    [Fact]
    public void Build_CreatesDeterministicAudioNormalizeCommandPlan()
    {
        var builder = new FfmpegAudioNormalizeCommandBuilder();
        var request = new AudioNormalizeRequest
        {
            InputPath = "samples/input/source.mp4",
            OutputPath = Path.Combine("output", "normalized.wav"),
            TargetLufs = -14,
            LoudnessRangeTarget = 9,
            TruePeakDb = -1,
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
                "loudnorm=I=-14:LRA=9:TP=-1",
                "-c:a",
                "pcm_s16le",
                Path.Combine("output", "normalized.wav")
            ],
            plan.Arguments);
    }

    [Fact]
    public void Build_ThrowsForUnsupportedOutputExtension()
    {
        var builder = new FfmpegAudioNormalizeCommandBuilder();
        var request = new AudioNormalizeRequest
        {
            InputPath = "samples/input/source.mp4",
            OutputPath = "output/normalized.ogg"
        };

        Assert.Throws<InvalidOperationException>(() => builder.Build(request));
    }
}
