using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task SeparateAudio_RejectsMissingDemucsExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-separate-audio-demucs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync(
                "separate-audio",
                "input.mp4",
                "--output-dir",
                outputDirectory,
                "--demucs",
                "missing-demucs");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-demucs", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("separate-audio", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputDirectory), envelope["separateAudio"]!["outputDirectory"]!.GetValue<string>());
            Assert.Contains("missing-demucs", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
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
