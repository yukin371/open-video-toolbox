using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class DemucsCommandBuilderTests
{
    [Fact]
    public void Build_CreatesDeterministicDemucsCommandPlan()
    {
        var builder = new DemucsCommandBuilder();
        var request = new DemucsExecutionRequest
        {
            InputPath = "samples/input/source.mp4",
            OutputDirectory = "stems",
            Model = "htdemucs"
        };

        var plan = builder.Build(request);

        Assert.Equal(
            [
                "-o",
                "stems",
                "-n",
                "htdemucs",
                "--two-stems",
                "vocals",
                "samples/input/source.mp4"
            ],
            plan.Arguments);
    }
}
