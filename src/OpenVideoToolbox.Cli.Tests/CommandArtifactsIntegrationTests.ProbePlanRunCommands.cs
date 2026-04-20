using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task Probe_RejectsMissingFfprobeExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-probe-missing-ffprobe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync(
                "probe",
                "input.mp4",
                "--ffprobe",
                "missing-ffprobe");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffprobe", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("probe <input>", result.StdOut, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Plan_RequiresInputPath()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-plan-input-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("plan");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Missing input file path.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("plan <input>", result.StdOut, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Run_RejectsMissingFfprobeExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-run-missing-ffprobe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync(
                "run",
                "input.mp4",
                "--ffprobe",
                "missing-ffprobe");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffprobe", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("run <input>", result.StdOut, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
}
