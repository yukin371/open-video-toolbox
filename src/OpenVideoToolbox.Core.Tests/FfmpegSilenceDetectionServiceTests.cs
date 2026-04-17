using OpenVideoToolbox.Core.Audio;
using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfmpegSilenceDetectionServiceTests
{
    [Fact]
    public async Task DetectAsync_RunsFfmpegAndParsesSilenceSegments()
    {
        var runner = new FakeProcessRunner(request => Task.FromResult(new ExecutionResult
        {
            Status = ExecutionStatus.Succeeded,
            ExitCode = 0,
            StartedAtUtc = DateTimeOffset.UtcNow,
            FinishedAtUtc = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            CommandPlan = request.CommandPlan,
            OutputLines =
            [
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = false,
                    Text = "[silencedetect @ 1] silence_start: 1.5"
                },
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = false,
                    Text = "[silencedetect @ 1] silence_end: 2.1 | silence_duration: 0.6"
                }
            ]
        }));
        var service = new FfmpegSilenceDetectionService(
            new SilenceDetectionRunner(new FfmpegSilenceDetectionCommandBuilder(), runner),
            new SilenceDetectionParser());

        var document = await service.DetectAsync("samples/input/demo.mp4", executablePath: "ffmpeg-custom");

        var segment = Assert.Single(document.Segments);
        Assert.Equal(TimeSpan.FromSeconds(1.5), segment.Start);
        Assert.Equal("ffmpeg-custom", runner.LastRequest!.CommandPlan.ExecutablePath);
        Assert.Contains(runner.LastRequest.CommandPlan.Arguments, argument => argument.Contains("silencedetect=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DetectAsync_ThrowsWhenProcessFails()
    {
        var runner = new FakeProcessRunner(request => Task.FromResult(new ExecutionResult
        {
            Status = ExecutionStatus.Failed,
            ExitCode = 1,
            StartedAtUtc = DateTimeOffset.UtcNow,
            FinishedAtUtc = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            CommandPlan = request.CommandPlan,
            OutputLines =
            [
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = true,
                    Text = "ffmpeg not found"
                }
            ],
            ErrorMessage = "Process exited with code 1."
        }));
        var service = new FfmpegSilenceDetectionService(
            new SilenceDetectionRunner(new FfmpegSilenceDetectionCommandBuilder(), runner),
            new SilenceDetectionParser());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DetectAsync("samples/input/demo.mp4"));

        Assert.Contains("silence detection failed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
