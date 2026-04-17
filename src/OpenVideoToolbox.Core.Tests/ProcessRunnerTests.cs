using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class ProcessRunnerTests
{
    [Fact]
    public async Task ExecuteAsync_CapturesOutput_AndReturnsSuccess()
    {
        var runner = new DefaultProcessRunner();
        var request = new ProcessExecutionRequest
        {
            CommandPlan = new CommandPlan
            {
                ToolName = "dotnet",
                ExecutablePath = "dotnet",
                Arguments = ["--version"],
                CommandLine = "dotnet --version"
            }
        };

        var result = await runner.ExecuteAsync(request);

        Assert.Equal(ExecutionStatus.Succeeded, result.Status);
        Assert.True(result.ExitCode == 0);
        Assert.NotEmpty(result.OutputLines);
        Assert.Contains(result.OutputLines, line => line.Channel == ProcessOutputChannel.StandardOutput);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsTimedOut_WhenTimeoutIsExceeded()
    {
        var runner = new DefaultProcessRunner();
        var request = new ProcessExecutionRequest
        {
            CommandPlan = CreateSleepCommandPlan(),
            Timeout = TimeSpan.FromMilliseconds(200)
        };

        var result = await runner.ExecuteAsync(request);

        Assert.Equal(ExecutionStatus.TimedOut, result.Status);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotMarkNormalStderrLinesAsErrors()
    {
        var runner = new DefaultProcessRunner();
        var request = new ProcessExecutionRequest
        {
            CommandPlan = CreateStandardErrorCommandPlan()
        };

        var result = await runner.ExecuteAsync(request);

        Assert.Equal(ExecutionStatus.Succeeded, result.Status);
        Assert.Contains(result.OutputLines, line => line.Channel == ProcessOutputChannel.StandardError);
        Assert.DoesNotContain(result.OutputLines, line => line.Channel == ProcessOutputChannel.StandardError && line.IsError);
    }

    private static CommandPlan CreateSleepCommandPlan()
    {
        return OperatingSystem.IsWindows()
            ? new CommandPlan
            {
                ToolName = "powershell",
                ExecutablePath = "powershell",
                Arguments = ["-NoProfile", "-Command", "Start-Sleep -Seconds 2; Write-Output done"],
                CommandLine = "powershell -NoProfile -Command \"Start-Sleep -Seconds 2; Write-Output done\""
            }
            : new CommandPlan
            {
                ToolName = "bash",
                ExecutablePath = "bash",
                Arguments = ["-lc", "sleep 2; echo done"],
                CommandLine = "bash -lc \"sleep 2; echo done\""
            };
    }

    private static CommandPlan CreateStandardErrorCommandPlan()
    {
        return OperatingSystem.IsWindows()
            ? new CommandPlan
            {
                ToolName = "powershell",
                ExecutablePath = "powershell",
                Arguments = ["-NoProfile", "-Command", "[Console]::Error.WriteLine('progress=42')"],
                CommandLine = "powershell -NoProfile -Command \"[Console]::Error.WriteLine('progress=42')\""
            }
            : new CommandPlan
            {
                ToolName = "bash",
                ExecutablePath = "bash",
                Arguments = ["-lc", "printf 'progress=42\\n' 1>&2"],
                CommandLine = "bash -lc \"printf 'progress=42\\\\n' 1>&2\""
            };
    }
}
