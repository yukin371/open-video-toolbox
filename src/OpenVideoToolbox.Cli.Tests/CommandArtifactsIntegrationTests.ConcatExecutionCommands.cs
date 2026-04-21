using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task Concat_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-concat-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var inputListPath = Path.Combine(outputDirectory, "clips.txt");
        var outputPath = Path.Combine(outputDirectory, "merged.mp4");
        await File.WriteAllTextAsync(inputListPath, "file 'a.mp4'");

        try
        {
            var result = await RunCliAsync(
                "concat",
                "--input-list",
                inputListPath,
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("concat", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(inputListPath), envelope["concat"]!["inputListPath"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(outputPath), envelope["concat"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Null(envelope["execution"]);
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
