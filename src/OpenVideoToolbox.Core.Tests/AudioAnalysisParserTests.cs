using OpenVideoToolbox.Core.Audio;
using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class AudioAnalysisParserTests
{
    [Fact]
    public void Parse_ReadsLoudnormJsonPayloadFromStderr()
    {
        var parser = new AudioAnalysisParser();
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
                WorkingDirectory = ".",
                Arguments = [],
                CommandLine = "ffmpeg"
            },
            OutputLines =
            [
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = false,
                    Text = "[Parsed_loudnorm_0 @ 000001] "
                },
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
                    Text = "\"input_i\" : \"-16.40\","
                },
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = false,
                    Text = "\"input_lra\" : \"5.80\","
                },
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = false,
                    Text = "\"input_tp\" : \"-0.90\","
                },
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = false,
                    Text = "\"input_thresh\" : \"-27.50\","
                },
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = false,
                    Text = "\"target_offset\" : \"-0.10\""
                },
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = false,
                    Text = "}"
                }
            ]
        };

        var document = parser.Parse(result, "input.mp4");

        Assert.Equal("input.mp4", document.InputPath);
        Assert.Equal(-16.4, document.Analysis.IntegratedLoudness);
        Assert.Equal(5.8, document.Analysis.LoudnessRange);
        Assert.Equal(-0.9, document.Analysis.TruePeakDb);
        Assert.Equal(-27.5, document.Analysis.ThresholdDb);
        Assert.Equal(-0.1, document.Analysis.TargetOffset);
    }

    [Fact]
    public void Parse_ThrowsWhenJsonPayloadIsMissing()
    {
        var parser = new AudioAnalysisParser();
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
                WorkingDirectory = ".",
                Arguments = [],
                CommandLine = "ffmpeg"
            },
            OutputLines = []
        };

        var exception = Assert.Throws<InvalidOperationException>(() => parser.Parse(result, "input.mp4"));

        Assert.Contains("JSON payload", exception.Message, StringComparison.Ordinal);
    }
}
