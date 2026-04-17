using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class DemucsSeparationRunnerTests
{
    [Fact]
    public async Task RunAsync_BuildsCommandAndPassesExpectedStemPaths()
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
        var runner = new DemucsSeparationRunner(new DemucsCommandBuilder(), fakeRunner);

        var result = await runner.RunAsync(
            new DemucsExecutionRequest
            {
                InputPath = "samples/input/source.mp4",
                OutputDirectory = "stems",
                Model = "htdemucs"
            },
            executablePath: "demucs-custom");

        Assert.Equal(ExecutionStatus.Succeeded, result.Status);
        Assert.Equal("demucs-custom", fakeRunner.LastRequest!.CommandPlan.ExecutablePath);
        Assert.Equal(2, fakeRunner.LastRequest.ProducedPaths.Count);
        Assert.Contains(fakeRunner.LastRequest.ProducedPaths, path => path.EndsWith(Path.Combine("htdemucs", "source", "vocals.wav"), StringComparison.Ordinal));
        Assert.Contains(fakeRunner.LastRequest.ProducedPaths, path => path.EndsWith(Path.Combine("htdemucs", "source", "no_vocals.wav"), StringComparison.Ordinal));
    }
}
