using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class ExternalDependencyInspectorTests
{
    [Fact]
    public async Task InspectAsync_ReturnsAvailableExecutableWhenProbeSucceeds()
    {
        var definition = new DependencyProbeDefinition
        {
            Id = "ffmpeg",
            Kind = DependencyProbeKind.Executable,
            Required = true,
            Source = DependencyValueSource.Default,
            ResolvedValue = "ffmpeg",
            ProbeArguments = ["-version"],
            Timeout = TimeSpan.FromSeconds(5)
        };

        var runner = new FakeProcessRunner(request =>
            Task.FromResult(CreateExecutionResult(
                request.CommandPlan,
                ExecutionStatus.Succeeded,
                exitCode: 0,
                output: "ffmpeg version 7.0")));

        var inspector = new ExternalDependencyInspector(runner);

        var report = await inspector.InspectAsync([definition]);
        var dependency = Assert.Single(report.Dependencies);

        Assert.True(report.IsHealthy);
        Assert.Equal(0, report.MissingRequiredCount);
        Assert.Equal(0, report.MissingOptionalCount);
        Assert.True(dependency.IsAvailable);
        Assert.True(dependency.ProbeSucceeded);
        Assert.Equal(0, dependency.ExitCode);
        Assert.Contains("ffmpeg version 7.0", dependency.Detail, StringComparison.Ordinal);
        Assert.NotNull(runner.LastRequest);
        Assert.Equal("ffmpeg", runner.LastRequest!.CommandPlan.ExecutablePath);
        Assert.Equal(["-version"], runner.LastRequest.CommandPlan.Arguments);
    }

    [Fact]
    public async Task InspectAsync_KeepsExecutableAvailableWhenProbeReturnsNonZeroExitCode()
    {
        var definition = new DependencyProbeDefinition
        {
            Id = "demucs",
            Kind = DependencyProbeKind.Executable,
            Required = false,
            Source = DependencyValueSource.Option,
            ResolvedValue = "demucs",
            ProbeArguments = ["--help"]
        };

        var runner = new FakeProcessRunner(request =>
            Task.FromResult(CreateExecutionResult(
                request.CommandPlan,
                ExecutionStatus.Failed,
                exitCode: 2,
                output: "usage: demucs [options]")));

        var inspector = new ExternalDependencyInspector(runner);

        var report = await inspector.InspectAsync([definition]);
        var dependency = Assert.Single(report.Dependencies);

        Assert.True(report.IsHealthy);
        Assert.True(dependency.IsAvailable);
        Assert.False(dependency.ProbeSucceeded);
        Assert.Equal(2, dependency.ExitCode);
        Assert.Contains("Process exited with code 2.", dependency.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InspectAsync_ReturnsUnavailableWhenExecutableCannotStart()
    {
        var definition = new DependencyProbeDefinition
        {
            Id = "ffprobe",
            Kind = DependencyProbeKind.Executable,
            Required = true,
            Source = DependencyValueSource.Option,
            ResolvedValue = "missing-ffprobe",
            ProbeArguments = ["-version"]
        };

        var runner = new FakeProcessRunner(_ => throw new InvalidOperationException("Failed to start process 'missing-ffprobe'."));
        var inspector = new ExternalDependencyInspector(runner);

        var report = await inspector.InspectAsync(
        [
            definition,
            new DependencyProbeDefinition
            {
                Id = "whisper-model",
                Kind = DependencyProbeKind.File,
                Required = false,
                Source = DependencyValueSource.Unset
            }
        ]);

        Assert.False(report.IsHealthy);
        Assert.Equal(1, report.MissingRequiredCount);
        Assert.Equal(1, report.MissingOptionalCount);

        var dependency = Assert.Single(report.Dependencies.Where(static item => item.Id == "ffprobe"));
        Assert.False(dependency.IsAvailable);
        Assert.False(dependency.ProbeSucceeded);
        Assert.Null(dependency.ExitCode);
        Assert.Equal("missing-ffprobe", dependency.ResolvedValue);
        Assert.Contains("missing-ffprobe", dependency.Detail, StringComparison.Ordinal);

        var modelDependency = Assert.Single(report.Dependencies.Where(static item => item.Id == "whisper-model"));
        Assert.False(modelDependency.IsAvailable);
        Assert.False(modelDependency.ProbeSucceeded);
        Assert.Null(modelDependency.ExitCode);
        Assert.Null(modelDependency.ResolvedValue);
        Assert.Equal("Dependency path is not configured.", modelDependency.Detail);
    }

    [Fact]
    public async Task InspectAsync_ReturnsAvailableFileDependencyWhenPathExists()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-dependency-file-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        var modelPath = Path.Combine(tempDirectory, "model.gguf");
        await File.WriteAllTextAsync(modelPath, "model");

        try
        {
            var inspector = new ExternalDependencyInspector(new FakeProcessRunner(_ => throw new NotSupportedException()));
            var report = await inspector.InspectAsync(
            [
                new DependencyProbeDefinition
                {
                    Id = "whisper-model",
                    Kind = DependencyProbeKind.File,
                    Required = false,
                    Source = DependencyValueSource.Option,
                    ResolvedValue = modelPath
                }
            ]);

            var dependency = Assert.Single(report.Dependencies);
            Assert.True(report.IsHealthy);
            Assert.True(dependency.IsAvailable);
            Assert.True(dependency.ProbeSucceeded);
            Assert.Equal(Path.GetFullPath(modelPath), dependency.ResolvedValue);
            Assert.Equal("File exists.", dependency.Detail);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task InspectAsync_ReturnsUnavailableFileDependencyWhenPathIsMissing()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-dependency-missing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        var modelPath = Path.Combine(tempDirectory, "missing-model.gguf");

        try
        {
            var inspector = new ExternalDependencyInspector(new FakeProcessRunner(_ => throw new NotSupportedException()));
            var report = await inspector.InspectAsync(
            [
                new DependencyProbeDefinition
                {
                    Id = "whisper-model",
                    Kind = DependencyProbeKind.File,
                    Required = false,
                    Source = DependencyValueSource.Option,
                    ResolvedValue = modelPath
                }
            ]);

            var dependency = Assert.Single(report.Dependencies);
            Assert.True(report.IsHealthy);
            Assert.False(dependency.IsAvailable);
            Assert.False(dependency.ProbeSucceeded);
            Assert.Null(dependency.ExitCode);
            Assert.Equal(Path.GetFullPath(modelPath), dependency.ResolvedValue);
            Assert.Equal($"File not found: {Path.GetFullPath(modelPath)}", dependency.Detail);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static ExecutionResult CreateExecutionResult(
        CommandPlan commandPlan,
        ExecutionStatus status,
        int exitCode,
        string output)
    {
        var startedAt = DateTimeOffset.UtcNow;

        return new ExecutionResult
        {
            Status = status,
            ExitCode = exitCode,
            StartedAtUtc = startedAt,
            FinishedAtUtc = startedAt.AddMilliseconds(10),
            Duration = TimeSpan.FromMilliseconds(10),
            CommandPlan = commandPlan,
            ErrorMessage = status == ExecutionStatus.Failed
                ? $"Process exited with code {exitCode}."
                : null,
            OutputLines =
            [
                new ProcessOutputLine
                {
                    TimestampUtc = startedAt,
                    Channel = ProcessOutputChannel.StandardOutput,
                    IsError = false,
                    Text = output
                }
            ]
        };
    }
}
