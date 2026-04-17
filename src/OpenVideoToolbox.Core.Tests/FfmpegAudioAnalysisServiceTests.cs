using OpenVideoToolbox.Core.Audio;
using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfmpegAudioAnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_RunsFfmpegAndParsesLoudnormOutput()
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
                    Text = "{"
                },
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = false,
                    Text = "\"input_i\" : \"-18.00\","
                },
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = false,
                    Text = "\"input_lra\" : \"6.00\","
                },
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = false,
                    Text = "\"input_tp\" : \"-1.20\","
                },
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = false,
                    Text = "\"input_thresh\" : \"-28.00\","
                },
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = false,
                    Text = "\"target_offset\" : \"0.00\""
                },
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = false,
                    Text = "}"
                }
            ]
        }));
        var service = new FfmpegAudioAnalysisService(
            new AudioAnalysisRunner(new FfmpegAudioAnalysisCommandBuilder(), runner),
            new AudioAnalysisParser());

        var analysis = await service.AnalyzeAsync("samples/input/demo.mp4", executablePath: "ffmpeg-custom");

        Assert.Equal("demo.mp4", Path.GetFileName(analysis.InputPath));
        Assert.Equal(-18.0, analysis.Analysis.IntegratedLoudness);
        Assert.Equal("ffmpeg-custom", runner.LastRequest!.CommandPlan.ExecutablePath);
        Assert.Contains("loudnorm=print_format=json", runner.LastRequest.CommandPlan.Arguments);
    }

    [Fact]
    public async Task AnalyzeAsync_ThrowsWhenProcessFails()
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
        var service = new FfmpegAudioAnalysisService(
            new AudioAnalysisRunner(new FfmpegAudioAnalysisCommandBuilder(), runner),
            new AudioAnalysisParser());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AnalyzeAsync("samples/input/demo.mp4"));

        Assert.Contains("audio analysis failed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
