using OpenVideoToolbox.Core.Audio;
using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class SilenceDetectionParserTests
{
    [Fact]
    public void Parse_MapsSilencedetectLogToSegments()
    {
        var parser = new SilenceDetectionParser();
        var result = new ExecutionResult
        {
            Status = ExecutionStatus.Succeeded,
            ExitCode = 0,
            StartedAtUtc = DateTimeOffset.UtcNow,
            FinishedAtUtc = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            CommandPlan = new CommandPlan
            {
                ToolName = "ffmpeg",
                ExecutablePath = "ffmpeg",
                CommandLine = "ffmpeg",
                Arguments = []
            },
            OutputLines =
            [
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = false,
                    Text = "[silencedetect @ 1] silence_start: 4.2"
                },
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = false,
                    Text = "[silencedetect @ 1] silence_end: 5.1 | silence_duration: 0.9"
                }
            ]
        };

        var document = parser.Parse(result, "input.mp4");

        var segment = Assert.Single(document.Segments);
        Assert.Equal(TimeSpan.FromSeconds(4.2), segment.Start);
        Assert.Equal(TimeSpan.FromSeconds(5.1), segment.End);
        Assert.Equal(TimeSpan.FromSeconds(0.9), segment.Duration);
    }
}
