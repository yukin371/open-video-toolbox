using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task Cut_RejectsInvalidTimeRange()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-cut-range-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "clip.mp4");

        try
        {
            var result = await RunCliAsync(
                "cut",
                "input.mp4",
                "--from",
                "00:00:05",
                "--to",
                "00:00:04",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--to' must be greater than '--from'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("cut <input>", result.StdOut, StringComparison.Ordinal);
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
