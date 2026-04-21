using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task SeparateAudio_RequiresOutputDirectoryOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-separate-audio-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("separate-audio", "input.mp4");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output-dir' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("separate-audio <input> --output-dir <path>", result.StdOut, StringComparison.Ordinal);
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
