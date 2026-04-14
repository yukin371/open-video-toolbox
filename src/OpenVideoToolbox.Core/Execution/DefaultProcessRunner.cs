using System.Diagnostics;

namespace OpenVideoToolbox.Core.Execution;

public sealed class DefaultProcessRunner : IProcessRunner
{
    public async Task<ExecutionResult> ExecuteAsync(ProcessExecutionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var outputLines = new List<ProcessOutputLine>();
        var outputLock = new object();
        var startedAtUtc = DateTimeOffset.UtcNow;
        var timeoutTriggered = false;
        var cancelled = false;

        var startInfo = new ProcessStartInfo
        {
            FileName = request.CommandPlan.ExecutablePath,
            WorkingDirectory = string.IsNullOrWhiteSpace(request.CommandPlan.WorkingDirectory)
                ? Environment.CurrentDirectory
                : request.CommandPlan.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in request.CommandPlan.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        var stdoutCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stderrCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is null)
            {
                stdoutCompletion.TrySetResult();
                return;
            }

            lock (outputLock)
            {
                outputLines.Add(new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    IsError = false,
                    Text = args.Data
                });
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is null)
            {
                stderrCompletion.TrySetResult();
                return;
            }

            lock (outputLock)
            {
                outputLines.Add(new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    IsError = true,
                    Text = args.Data
                });
            }
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"Failed to start process '{request.CommandPlan.ExecutablePath}'.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var waitForExitTask = process.WaitForExitAsync();

            if (request.Timeout is { } timeout && timeout > TimeSpan.Zero)
            {
                var completedTask = await Task.WhenAny(waitForExitTask, Task.Delay(timeout, cancellationToken)).ConfigureAwait(false);
                if (completedTask != waitForExitTask)
                {
                    timeoutTriggered = !cancellationToken.IsCancellationRequested;
                    cancelled = cancellationToken.IsCancellationRequested;
                    TryKill(process);
                }
                else
                {
                    await waitForExitTask.ConfigureAwait(false);
                }
            }
            else
            {
                try
                {
                    await waitForExitTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                    TryKill(process);
                }
            }
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
            TryKill(process);
        }

        await SafeWaitForExitAsync(process).ConfigureAwait(false);
        await Task.WhenAll(stdoutCompletion.Task, stderrCompletion.Task).ConfigureAwait(false);

        var finishedAtUtc = DateTimeOffset.UtcNow;
        var status = ResolveStatus(process, timeoutTriggered, cancelled || cancellationToken.IsCancellationRequested);

        return new ExecutionResult
        {
            Status = status,
            ExitCode = process.HasExited ? process.ExitCode : null,
            StartedAtUtc = startedAtUtc,
            FinishedAtUtc = finishedAtUtc,
            Duration = finishedAtUtc - startedAtUtc,
            CommandPlan = request.CommandPlan,
            OutputLines = outputLines.OrderBy(line => line.TimestampUtc).ToArray(),
            ErrorMessage = status switch
            {
                ExecutionStatus.TimedOut => "Process execution timed out.",
                ExecutionStatus.Cancelled => "Process execution was cancelled.",
                ExecutionStatus.Failed when process.ExitCode is { } exitCode => $"Process exited with code {exitCode}.",
                _ => null
            },
            ProducedPaths = request.ProducedPaths
        };
    }

    private static ExecutionStatus ResolveStatus(Process process, bool timeoutTriggered, bool cancelled)
    {
        if (timeoutTriggered)
        {
            return ExecutionStatus.TimedOut;
        }

        if (cancelled)
        {
            return ExecutionStatus.Cancelled;
        }

        return process.ExitCode == 0
            ? ExecutionStatus.Succeeded
            : ExecutionStatus.Failed;
    }

    private static async Task SafeWaitForExitAsync(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                await process.WaitForExitAsync().ConfigureAwait(false);
            }
        }
        catch
        {
            // Ignore failures during cleanup.
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best-effort termination only.
        }
    }
}
