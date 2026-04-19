using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed class CliRealMediaSmokeTests
{
    [Fact]
    public async Task TranscribeCommand_ProducesStructuredEnvelope_WhenConfigured()
    {
        if (!CliTestProcessHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var modelPath = CliTestProcessHelper.GetOptionalFilePathFromEnvironment("OVT_WHISPER_MODEL_PATH");
        if (modelPath is null)
        {
            return;
        }

        var whisperCliPath = CliTestProcessHelper.GetToolFromEnvironmentOrDefault("OVT_WHISPER_CLI_PATH", "whisper-cli");
        if (!CliTestProcessHelper.IsToolAvailable(whisperCliPath))
        {
            return;
        }

        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-cli-real-transcribe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var inputPath = Path.Combine(outputDirectory, "speech-sample.wav");
            var outputPath = Path.Combine(outputDirectory, "transcript.json");
            var jsonOutPath = Path.Combine(outputDirectory, "transcribe.json");

            await CliTestProcessHelper.CreateSampleAudioAsync(inputPath, TimeSpan.FromSeconds(2));

            var result = await CliTestProcessHelper.RunCliAsync(
                "transcribe",
                inputPath,
                "--model",
                modelPath,
                "--output",
                outputPath,
                "--ffmpeg",
                "ffmpeg",
                "--whisper-cli",
                whisperCliPath,
                "--json-out",
                jsonOutPath,
                "--timeout-seconds",
                "120");

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            var transcriptFile = JsonNode.Parse(await File.ReadAllTextAsync(outputPath))!.AsObject();

            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal(Path.GetFullPath(outputPath), stdout["transcribe"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(modelPath), stdout["transcribe"]!["modelPath"]!.GetValue<string>());
            Assert.True(stdout["transcribe"]!["segmentCount"]!.GetValue<int>() >= 0);
            Assert.True(JsonNode.DeepEquals(stdout["transcript"], transcriptFile));
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
    public async Task SeparateAudioCommand_ProducesStructuredEnvelope_WhenConfigured()
    {
        if (!CliTestProcessHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var demucsPath = CliTestProcessHelper.GetToolFromEnvironmentOrDefault("OVT_DEMUCS_PATH", "demucs");
        if (!CliTestProcessHelper.IsToolAvailable(demucsPath))
        {
            return;
        }

        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-cli-real-demucs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var inputPath = Path.Combine(outputDirectory, "input.wav");
            var stemsDirectory = Path.Combine(outputDirectory, "stems");
            var jsonOutPath = Path.Combine(outputDirectory, "separate-audio.json");

            await CliTestProcessHelper.CreateSampleAudioAsync(inputPath, TimeSpan.FromSeconds(2));

            var result = await CliTestProcessHelper.RunCliAsync(
                "separate-audio",
                inputPath,
                "--output-dir",
                stemsDirectory,
                "--model",
                "htdemucs",
                "--demucs",
                demucsPath,
                "--json-out",
                jsonOutPath,
                "--timeout-seconds",
                "300");

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();

            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal(Path.GetFullPath(stemsDirectory), stdout["separateAudio"]!["outputDirectory"]!.GetValue<string>());
            Assert.Equal("htdemucs", stdout["separateAudio"]!["model"]!.GetValue<string>());

            var vocalsPath = stdout["stems"]!["vocals"]!.GetValue<string>();
            var accompanimentPath = stdout["stems"]!["accompaniment"]!.GetValue<string>();
            Assert.True(File.Exists(vocalsPath));
            Assert.True(File.Exists(accompanimentPath));
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
