using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Media;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfprobeMediaProbeServiceTests
{
    [Fact]
    public async Task ProbeAsync_RunsFfprobeAndParsesStdoutJson()
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
                    IsError = false,
                    Text =
                        """
                        {"streams":[{"index":0,"codec_type":"video","codec_name":"h264","width":1280,"height":720,"avg_frame_rate":"30000/1001"}],"format":{"format_name":"mov,mp4","duration":"10.0","size":"1024","bit_rate":"8192"}}
                        """
                }
            ]
        }));
        var service = new FfprobeMediaProbeService(runner, new FfprobeJsonParser());

        var result = await service.ProbeAsync("samples/input/demo.mp4", executablePath: "ffprobe-custom");

        Assert.Equal("demo.mp4", result.FileName);
        Assert.Equal("mov,mp4", result.Format.ContainerName);
        Assert.Equal("ffprobe-custom", runner.LastRequest!.CommandPlan.ExecutablePath);
        Assert.Contains("-show_streams", runner.LastRequest.CommandPlan.Arguments);
    }

    [Fact]
    public async Task ProbeAsync_Throws_WhenProcessFails()
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
                    IsError = true,
                    Text = "ffprobe not found"
                }
            ],
            ErrorMessage = "Process exited with code 1."
        }));
        var service = new FfprobeMediaProbeService(runner, new FfprobeJsonParser());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProbeAsync("samples/input/demo.mp4"));

        Assert.Contains("ffprobe failed", exception.Message);
    }
}

