using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task DetectSilence_RequiresOutputOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-detect-silence-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("detect-silence", "input.mp4");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("detect-silence <input> --output <silence.json>", result.StdOut, StringComparison.Ordinal);
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
    public async Task DetectSilence_RejectsInvalidNoiseDb()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-detect-silence-noise-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "silence.json");

        try
        {
            var result = await RunCliAsync(
                "detect-silence",
                "input.mp4",
                "--output",
                outputPath,
                "--noise-db",
                "oops");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--noise-db' expects a numeric value.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("detect-silence <input> --output <silence.json>", result.StdOut, StringComparison.Ordinal);
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
