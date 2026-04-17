using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfmpegAudioAnalysisCommandBuilderTests
{
    [Fact]
    public void Build_CreatesDeterministicAudioAnalyzeCommandPlan()
    {
        var builder = new FfmpegAudioAnalysisCommandBuilder();
        var request = new AudioAnalysisRequest
        {
            InputPath = "samples/input/source.mp4"
        };

        var plan = builder.Build(request);

        Assert.Equal(
            [
                "-i",
                "samples/input/source.mp4",
                "-vn",
                "-af",
                "loudnorm=print_format=json",
                "-f",
                "null",
                "-"
            ],
            plan.Arguments);
    }
}
