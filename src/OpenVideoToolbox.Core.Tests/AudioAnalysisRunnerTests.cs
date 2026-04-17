using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class AudioAnalysisRunnerTests
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
        var runner = new AudioAnalysisRunner(new FfmpegAudioAnalysisCommandBuilder(), fakeRunner);

        var result = await runner.RunAsync(new AudioAnalysisRequest
        {
            InputPath = "samples/input/source.mp4"
        }, executablePath: "ffmpeg-custom");

        Assert.Equal(ExecutionStatus.Succeeded, result.Status);
        Assert.Equal("ffmpeg-custom", fakeRunner.LastRequest!.CommandPlan.ExecutablePath);
        Assert.Empty(fakeRunner.LastRequest.ProducedPaths);
    }
}
