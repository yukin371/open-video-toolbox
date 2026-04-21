using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task AudioAnalyze_RequiresOutputOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-analyze-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("audio-analyze", "input.mp4");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("audio-analyze <input> --output <audio.json>", result.StdOut, StringComparison.Ordinal);
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
