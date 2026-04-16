using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfmpegCutCommandBuilderTests
{
    [Fact]
    public void Build_CreatesDeterministicCutCommandPlan()
    {
        var builder = new FfmpegCutCommandBuilder();
        var request = new MediaCutRequest
        {
            InputPath = "samples/input/source.mp4",
            OutputPath = Path.Combine("output", "clip-01.mp4"),
            Start = TimeSpan.FromSeconds(12.5),
            End = TimeSpan.FromSeconds(27.75),
            OverwriteExisting = true
        };

        var plan = builder.Build(request);

        Assert.Equal("ffmpeg", plan.ToolName);
        Assert.Equal(Path.GetDirectoryName(Path.GetFullPath(request.OutputPath)), plan.WorkingDirectory);
        Assert.Equal(
            [
                "-y",
                "-i",
                "samples/input/source.mp4",
                "-ss",
                "00:00:12.500",
                "-to",
                "00:00:27.750",
                "-map",
                "0",
                "-c",
                "copy",
                Path.Combine("output", "clip-01.mp4")
            ],
            plan.Arguments);
    }

    [Fact]
    public void Build_Throws_WhenEndIsNotAfterStart()
    {
        var builder = new FfmpegCutCommandBuilder();
        var request = new MediaCutRequest
        {
            InputPath = "samples/input/source.mp4",
            OutputPath = "clip.mp4",
            Start = TimeSpan.FromSeconds(30),
            End = TimeSpan.FromSeconds(30),
            OverwriteExisting = false
        };

        Assert.Throws<ArgumentException>(() => builder.Build(request));
    }
}
