using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task Transcribe_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-transcribe-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "transcript.json");

        try
        {
            var result = await RunCliAsync(
                "transcribe",
                "input.mp4",
                "--model",
                "ggml-base.bin",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("transcribe", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["transcribe"]!["outputPath"]!.GetValue<string>());
            Assert.EndsWith("ggml-base.bin", envelope["transcribe"]!["modelPath"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
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
