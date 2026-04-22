using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task AudioNormalize_RequiresOutputOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-normalize-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("audio-normalize", "input.mp4");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("audio-normalize <input> --output <path>", result.StdOut, StringComparison.Ordinal);
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
    public async Task AudioNormalize_RejectsInvalidTargetLufs()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-normalize-invalid-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "normalized.wav");

        try
        {
            var result = await RunCliAsync(
                "audio-normalize",
                "input.mp4",
                "--target-lufs",
                "oops",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--target-lufs' expects a numeric value.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("audio-normalize <input> --output <path>", result.StdOut, StringComparison.Ordinal);
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
