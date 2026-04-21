using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task AudioGain_RequiresGainDbOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-gain-db-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "gain.wav");

        try
        {
            var result = await RunCliAsync(
                "audio-gain",
                "input.mp4",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--gain-db' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("audio-gain <input> --gain-db <n> --output <path>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
    public async Task AudioGain_RejectsInvalidGainDb()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-gain-invalid-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "gain.wav");

        try
        {
            var result = await RunCliAsync(
                "audio-gain",
                "input.mp4",
                "--gain-db",
                "oops",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--gain-db' expects a numeric value.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("audio-gain <input> --gain-db <n> --output <path>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
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
