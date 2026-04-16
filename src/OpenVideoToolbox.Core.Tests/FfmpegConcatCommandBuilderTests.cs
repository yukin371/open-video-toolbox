using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfmpegConcatCommandBuilderTests
{
    [Fact]
    public void Build_CreatesDeterministicConcatCommandPlan()
    {
        var builder = new FfmpegConcatCommandBuilder();
        var request = new MediaConcatRequest
        {
            InputListPath = "clips.txt",
            OutputPath = Path.Combine("output", "merged.mp4"),
            OverwriteExisting = true
        };

        var plan = builder.Build(request);

        Assert.Equal(
            [
                "-y",
                "-f",
                "concat",
                "-safe",
                "0",
                "-i",
                "clips.txt",
                "-c",
                "copy",
                Path.Combine("output", "merged.mp4")
            ],
            plan.Arguments);
    }
}
