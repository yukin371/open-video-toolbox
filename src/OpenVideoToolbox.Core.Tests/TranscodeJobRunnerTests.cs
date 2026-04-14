using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Jobs;
using OpenVideoToolbox.Core.Presets;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class TranscodeJobRunnerTests
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
        var runner = new TranscodeJobRunner(new FfmpegCommandBuilder(), fakeRunner);

        var result = await runner.RunAsync(BuildJob(), executablePath: "ffmpeg-custom");

        Assert.Equal(ExecutionStatus.Succeeded, result.Status);
        Assert.Equal("ffmpeg-custom", fakeRunner.LastRequest!.CommandPlan.ExecutablePath);
        Assert.Single(fakeRunner.LastRequest.ProducedPaths);
        Assert.Equal(Path.Combine("output", "clip.mp4"), fakeRunner.LastRequest.ProducedPaths[0]);
    }

    private static JobDefinition BuildJob()
    {
        return new JobDefinition
        {
            Id = "job-runner-01",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Source = new JobSource
            {
                InputPath = "samples/input/clip.mkv"
            },
            Output = new JobOutput
            {
                OutputDirectory = "output",
                FileNameStem = "clip",
                ContainerExtension = ".mp4",
                OverwriteExisting = true
            },
            Preset = BuiltInPresetCatalog.GetRequired("h264-aac-mp4")
        };
    }
}
