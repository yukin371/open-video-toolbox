using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfmpegSilenceDetectionCommandBuilderTests
{
    [Fact]
    public void Build_CreatesDeterministicDetectSilenceCommandPlan()
    {
        var builder = new FfmpegSilenceDetectionCommandBuilder();
        var request = new SilenceDetectionRequest
        {
            InputPath = "samples/input/source.mp4",
            NoiseDb = -28.5,
            MinimumDuration = TimeSpan.FromMilliseconds(750)
        };

        var plan = builder.Build(request);

        Assert.Equal(
            [
                "-i",
                "samples/input/source.mp4",
                "-vn",
                "-af",
                "silencedetect=noise=-28.5dB:d=0.75",
                "-f",
                "null",
                "-"
            ],
            plan.Arguments);
    }
}
