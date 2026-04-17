using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class SilenceDetectionRunnerTests
{
    [Fact]
    public async Task RunAsync_BuildsCommandWithoutProducedPaths()
    {
        var fakeRunner = new FakeProcessRunner(request => Task.FromResult(new ExecutionResult
        {
            Status = ExecutionStatus.Succeeded,
            ExitCode = 0,
            StartedAtUtc = DateTimeOffset.UtcNow,
            FinishedAtUtc = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            CommandPlan = request.CommandPlan
        }));
        var runner = new SilenceDetectionRunner(new FfmpegSilenceDetectionCommandBuilder(), fakeRunner);

        var result = await runner.RunAsync(new SilenceDetectionRequest
        {
            InputPath = "samples/input/source.mp4"
        }, executablePath: "ffmpeg-custom");

        Assert.Equal(ExecutionStatus.Succeeded, result.Status);
        Assert.Equal("ffmpeg-custom", fakeRunner.LastRequest!.CommandPlan.ExecutablePath);
        Assert.Empty(fakeRunner.LastRequest.ProducedPaths);
    }
}
