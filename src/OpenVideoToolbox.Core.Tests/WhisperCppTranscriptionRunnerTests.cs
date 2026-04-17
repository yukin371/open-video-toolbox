using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class WhisperCppTranscriptionRunnerTests
{
    [Fact]
    public async Task RunAsync_BuildsCommandAndPassesProducedJsonPath()
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
        var runner = new WhisperCppTranscriptionRunner(new WhisperCppCommandBuilder(), fakeRunner);
        var request = new WhisperCppExecutionRequest
        {
            InputWavePath = "temp/input.wav",
            ModelPath = "models/ggml-base.bin",
            OutputFilePrefix = "temp/transcript"
        };

        var result = await runner.RunAsync(request, executablePath: "whisper-cli-custom");

        Assert.Equal(ExecutionStatus.Succeeded, result.Status);
        Assert.Equal("whisper-cli-custom", fakeRunner.LastRequest!.CommandPlan.ExecutablePath);
        Assert.Single(fakeRunner.LastRequest.ProducedPaths);
        Assert.Equal("temp/transcript.json", fakeRunner.LastRequest.ProducedPaths[0].Replace('\\', '/'));
    }
}
