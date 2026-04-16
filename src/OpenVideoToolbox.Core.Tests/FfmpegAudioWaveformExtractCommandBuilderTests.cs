using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfmpegAudioWaveformExtractCommandBuilderTests
{
    [Fact]
    public void Build_CreatesDeterministicWaveformExtractCommandPlan()
    {
        var builder = new FfmpegAudioWaveformExtractCommandBuilder();
        var request = new AudioWaveformExtractRequest
        {
            InputPath = "samples/input/source.mp4",
            OutputPath = Path.Combine("output", "waveform.wav"),
            SampleRateHz = 22050,
            OverwriteExisting = true
        };

        var plan = builder.Build(request);

        Assert.Equal(
            [
                "-y",
                "-i",
                "samples/input/source.mp4",
                "-vn",
                "-ac",
                "1",
                "-ar",
                "22050",
                "-c:a",
                "pcm_s16le",
                Path.Combine("output", "waveform.wav")
            ],
            plan.Arguments);
    }
}
