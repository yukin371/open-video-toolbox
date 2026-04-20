using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task Subtitle_RejectsUnsupportedFormat()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-subtitle-format-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var outputPath = Path.Combine(outputDirectory, "subs.srt");
        await File.WriteAllTextAsync(transcriptPath, """{"schemaVersion":1,"language":"en","segments":[]}""");

        try
        {
            var result = await RunCliAsync(
                "subtitle",
                "input.mp4",
                "--transcript",
                transcriptPath,
                "--format",
                "vtt",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--format' expects one of: srt, ass.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("subtitle <input> --transcript <transcript.json> --format <srt|ass>", result.StdOut, StringComparison.Ordinal);
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
    public async Task Subtitle_RequiresTranscriptOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-subtitle-transcript-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "subs.srt");

        try
        {
            var result = await RunCliAsync(
                "subtitle",
                "input.mp4",
                "--format",
                "srt",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--transcript' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("subtitle <input> --transcript <transcript.json> --format <srt|ass>", result.StdOut, StringComparison.Ordinal);
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
    public async Task Subtitle_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-subtitle-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var outputPath = Path.Combine(outputDirectory, "subs.srt");
        var jsonOutPath = Path.Combine(outputDirectory, "subtitle.json");
        await File.WriteAllTextAsync(
            transcriptPath,
            """{"schemaVersion":1,"language":"en","segments":[{"id":"seg-001","start":"00:00:00","end":"00:00:01","text":"hello world"}]}""");

        try
        {
            var result = await RunCliAsync(
                "subtitle",
                "input.mp4",
                "--transcript",
                transcriptPath,
                "--format",
                "srt",
                "--output",
                outputPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal(Path.GetFullPath(outputPath), stdout["subtitle"]!["outputPath"]!.GetValue<string>());
            Assert.Equal("srt", stdout["subtitle"]!["format"]!.GetValue<string>());
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
