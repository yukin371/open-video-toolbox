using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class AudioGainRunnerTests
{
    [Fact]
    public async Task RunAsync_BuildsCommandAndPassesProducedPath()
    {
        var fakeRunner = new FakeProcessRunner(request => Task.FromResult(new ExecutionResult
        {
            Status = ExecutionStatus.Succeeded,
            ExitCode = 0,
            StartedAtUtc = DateTimeOffset.UtcNow,
            FinishedAtUtc = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            CommandPlan = request.CommandPlan,
            ProducedPaths = request.ProducedPaths
        }));
        var runner = new AudioGainRunner(new FfmpegAudioGainCommandBuilder(), fakeRunner);
        var request = new AudioGainRequest
        {
            InputPath = "samples/input/source.mp4",
            OutputPath = Path.Combine("output", "gain.wav"),
            GainDb = 2,
            OverwriteExisting = true
        };

        var result = await runner.RunAsync(request, executablePath: "ffmpeg-custom");

        Assert.Equal(ExecutionStatus.Succeeded, result.Status);
        Assert.Equal("ffmpeg-custom", fakeRunner.LastRequest!.CommandPlan.ExecutablePath);
        Assert.Single(fakeRunner.LastRequest.ProducedPaths);
        Assert.Equal(Path.Combine("output", "gain.wav"), fakeRunner.LastRequest.ProducedPaths[0]);
    }
}
