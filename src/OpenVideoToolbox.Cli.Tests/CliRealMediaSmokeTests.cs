using System.Text.Json.Nodes;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Serialization;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed class CliRealMediaSmokeTests
{
    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ovt-cli-smoke-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void AssertEnvelopeValid(string stdout, string expectedCommand, out JsonObject envelope)
    {
        envelope = JsonNode.Parse(stdout)!.AsObject();
        Assert.Equal(expectedCommand, envelope["command"]!.GetValue<string>());
        Assert.False(envelope["preview"]!.GetValue<bool>());
        Assert.NotNull(envelope["payload"]);
    }

    private static void AssertJsonOutMatches(string jsonOutPath, string stdout)
    {
        Assert.True(File.Exists(jsonOutPath));
        var file = JsonNode.Parse(File.ReadAllText(jsonOutPath))!.AsObject();
        var console = JsonNode.Parse(stdout)!.AsObject();
        Assert.True(JsonNode.DeepEquals(console, file));
    }

    private static void WriteEditJson(string path, EditPlan plan)
    {
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(plan, OpenVideoToolboxJson.Default));
    }

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

        var dir = CreateTempDir();

        try
        {
            var inputPath = Path.Combine(dir, "speech-sample.wav");
            var outputPath = Path.Combine(dir, "transcript.json");
            var jsonOutPath = Path.Combine(dir, "transcribe.json");

            await CliTestProcessHelper.CreateSampleAudioAsync(inputPath, TimeSpan.FromSeconds(2));

            var result = await CliTestProcessHelper.RunCliAsync(
                "transcribe",
                inputPath,
                "--model", modelPath,
                "--output", outputPath,
                "--ffmpeg", "ffmpeg",
                "--whisper-cli", whisperCliPath,
                "--json-out", jsonOutPath,
                "--timeout-seconds", "120");

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
            Directory.Delete(dir, recursive: true);
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

        var dir = CreateTempDir();

        try
        {
            var inputPath = Path.Combine(dir, "input.wav");
            var stemsDirectory = Path.Combine(dir, "stems");
            var jsonOutPath = Path.Combine(dir, "separate-audio.json");

            await CliTestProcessHelper.CreateSampleAudioAsync(inputPath, TimeSpan.FromSeconds(2));

            var result = await CliTestProcessHelper.RunCliAsync(
                "separate-audio",
                inputPath,
                "--output-dir", stemsDirectory,
                "--model", "htdemucs",
                "--demucs", demucsPath,
                "--json-out", jsonOutPath,
                "--timeout-seconds", "300");

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
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ProbeCommand_ProducesStructuredEnvelope()
    {
        if (!CliTestProcessHelper.IsToolAvailable("ffmpeg") || !CliTestProcessHelper.IsToolAvailable("ffprobe"))
        {
            return;
        }

        var dir = CreateTempDir();

        try
        {
            var inputPath = Path.Combine(dir, "sample.mp4");
            var jsonOutPath = Path.Combine(dir, "probe.json");

            await CliTestProcessHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(2));

            var result = await CliTestProcessHelper.RunCliAsync(
                "probe", inputPath,
                "--ffprobe", "ffprobe",
                "--json-out", jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            AssertEnvelopeValid(result.StdOut, "probe", out _);
            AssertJsonOutMatches(jsonOutPath, result.StdOut);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task CutCommand_ProducesStructuredEnvelope()
    {
        if (!CliTestProcessHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var dir = CreateTempDir();

        try
        {
            var inputPath = Path.Combine(dir, "input.mp4");
            var outputPath = Path.Combine(dir, "clip.mp4");
            var jsonOutPath = Path.Combine(dir, "cut.json");

            await CliTestProcessHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(3));

            var result = await CliTestProcessHelper.RunCliAsync(
                "cut", inputPath,
                "--from", "00:00:00.500",
                "--to", "00:00:01.500",
                "--output", outputPath,
                "--ffmpeg", "ffmpeg",
                "--json-out", jsonOutPath,
                "--overwrite");

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            AssertEnvelopeValid(result.StdOut, "cut", out _);
            AssertJsonOutMatches(jsonOutPath, result.StdOut);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ConcatCommand_ProducesStructuredEnvelope()
    {
        if (!CliTestProcessHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var dir = CreateTempDir();

        try
        {
            var sourcePath = Path.Combine(dir, "source.mp4");
            var clip1Path = Path.Combine(dir, "clip-01.mp4");
            var clip2Path = Path.Combine(dir, "clip-02.mp4");
            var listPath = Path.Combine(dir, "clips.txt");
            var outputPath = Path.Combine(dir, "merged.mp4");
            var jsonOutPath = Path.Combine(dir, "concat.json");

            await CliTestProcessHelper.CreateSampleVideoAsync(sourcePath, TimeSpan.FromSeconds(4));

            await CliTestProcessHelper.RunCliAsync(
                "cut", sourcePath,
                "--from", "00:00:00", "--to", "00:00:01",
                "--output", clip1Path, "--ffmpeg", "ffmpeg", "--overwrite");

            await CliTestProcessHelper.RunCliAsync(
                "cut", sourcePath,
                "--from", "00:00:01", "--to", "00:00:02",
                "--output", clip2Path, "--ffmpeg", "ffmpeg", "--overwrite");

            await File.WriteAllLinesAsync(listPath,
            [
                $"file '{clip1Path.Replace("'", "'\\''", StringComparison.Ordinal)}'",
                $"file '{clip2Path.Replace("'", "'\\''", StringComparison.Ordinal)}'"
            ]);

            var result = await CliTestProcessHelper.RunCliAsync(
                "concat",
                "--input-list", listPath,
                "--output", outputPath,
                "--ffmpeg", "ffmpeg",
                "--json-out", jsonOutPath,
                "--overwrite");

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            AssertEnvelopeValid(result.StdOut, "concat", out _);
            AssertJsonOutMatches(jsonOutPath, result.StdOut);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ExtractAudioCommand_ProducesStructuredEnvelope()
    {
        if (!CliTestProcessHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var dir = CreateTempDir();

        try
        {
            var inputPath = Path.Combine(dir, "input.mp4");
            var outputPath = Path.Combine(dir, "audio.m4a");
            var jsonOutPath = Path.Combine(dir, "extract-audio.json");

            await CliTestProcessHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(2));

            var result = await CliTestProcessHelper.RunCliAsync(
                "extract-audio", inputPath,
                "--track", "0",
                "--output", outputPath,
                "--ffmpeg", "ffmpeg",
                "--json-out", jsonOutPath,
                "--overwrite");

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            AssertEnvelopeValid(result.StdOut, "extract-audio", out _);
            AssertJsonOutMatches(jsonOutPath, result.StdOut);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task AudioAnalyzeCommand_ProducesStructuredEnvelope()
    {
        if (!CliTestProcessHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var dir = CreateTempDir();

        try
        {
            var inputPath = Path.Combine(dir, "input.mp4");
            var outputPath = Path.Combine(dir, "audio.json");
            var jsonOutPath = Path.Combine(dir, "audio-analyze.json");

            await CliTestProcessHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(2));

            var result = await CliTestProcessHelper.RunCliAsync(
                "audio-analyze", inputPath,
                "--output", outputPath,
                "--ffmpeg", "ffmpeg",
                "--json-out", jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            AssertEnvelopeValid(result.StdOut, "audio-analyze", out _);
            AssertJsonOutMatches(jsonOutPath, result.StdOut);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task AudioGainCommand_ProducesStructuredEnvelope()
    {
        if (!CliTestProcessHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var dir = CreateTempDir();

        try
        {
            var inputPath = Path.Combine(dir, "input.wav");
            var outputPath = Path.Combine(dir, "gained.wav");
            var jsonOutPath = Path.Combine(dir, "audio-gain.json");

            await CliTestProcessHelper.CreateSampleAudioAsync(inputPath, TimeSpan.FromSeconds(2));

            var result = await CliTestProcessHelper.RunCliAsync(
                "audio-gain", inputPath,
                "--gain-db", "-6",
                "--output", outputPath,
                "--ffmpeg", "ffmpeg",
                "--json-out", jsonOutPath,
                "--overwrite");

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            AssertEnvelopeValid(result.StdOut, "audio-gain", out _);
            AssertJsonOutMatches(jsonOutPath, result.StdOut);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task DetectSilenceCommand_ProducesStructuredEnvelope()
    {
        if (!CliTestProcessHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var dir = CreateTempDir();

        try
        {
            var inputPath = Path.Combine(dir, "input.wav");
            var outputPath = Path.Combine(dir, "silence.json");
            var jsonOutPath = Path.Combine(dir, "detect-silence.json");

            await CliTestProcessHelper.CreateSampleAudioAsync(inputPath, TimeSpan.FromSeconds(2));

            var result = await CliTestProcessHelper.RunCliAsync(
                "detect-silence", inputPath,
                "--output", outputPath,
                "--ffmpeg", "ffmpeg",
                "--json-out", jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            AssertEnvelopeValid(result.StdOut, "detect-silence", out _);
            AssertJsonOutMatches(jsonOutPath, result.StdOut);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task BeatTrackCommand_ProducesStructuredEnvelope()
    {
        if (!CliTestProcessHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var dir = CreateTempDir();

        try
        {
            var inputPath = Path.Combine(dir, "input.wav");
            var outputPath = Path.Combine(dir, "beats.json");
            var jsonOutPath = Path.Combine(dir, "beat-track.json");

            await CliTestProcessHelper.CreateSampleAudioAsync(inputPath, TimeSpan.FromSeconds(3));

            var result = await CliTestProcessHelper.RunCliAsync(
                "beat-track", inputPath,
                "--output", outputPath,
                "--ffmpeg", "ffmpeg",
                "--json-out", jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            AssertEnvelopeValid(result.StdOut, "beat-track", out _);
            AssertJsonOutMatches(jsonOutPath, result.StdOut);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task SubtitleCommand_ProducesStructuredEnvelope()
    {
        if (!CliTestProcessHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var dir = CreateTempDir();

        try
        {
            var inputPath = Path.Combine(dir, "input.mp4");
            var transcriptPath = Path.Combine(dir, "transcript.json");
            var outputPath = Path.Combine(dir, "subtitles.srt");
            var jsonOutPath = Path.Combine(dir, "subtitle.json");

            await CliTestProcessHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(2));
            await File.WriteAllTextAsync(transcriptPath, """
                {
                  "schemaVersion": 1,
                  "language": "en",
                  "segments": [
                    {
                      "id": 0,
                      "start": "00:00:00",
                      "end": "00:00:01",
                      "text": "Hello world"
                    }
                  ]
                }
                """);

            var result = await CliTestProcessHelper.RunCliAsync(
                "subtitle", inputPath,
                "--transcript", transcriptPath,
                "--format", "srt",
                "--output", outputPath,
                "--json-out", jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            AssertEnvelopeValid(result.StdOut, "subtitle", out _);
            AssertJsonOutMatches(jsonOutPath, result.StdOut);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task RenderCommand_ProducesStructuredEnvelope()
    {
        if (!CliTestProcessHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var dir = CreateTempDir();

        try
        {
            var inputPath = Path.Combine(dir, "input.mp4");
            var outputPath = Path.Combine(dir, "output.mp4");
            var planPath = Path.Combine(dir, "edit.json");
            var jsonOutPath = Path.Combine(dir, "render.json");

            await CliTestProcessHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(2));

            WriteEditJson(planPath, new EditPlan
            {
                Source = new EditPlanSource { InputPath = inputPath },
                Clips =
                [
                    new EditClip
                    {
                        Id = "c1",
                        InPoint = TimeSpan.Zero,
                        OutPoint = TimeSpan.FromSeconds(1)
                    }
                ],
                Output = new EditOutputPlan
                {
                    Path = outputPath,
                    Container = "mp4"
                }
            });

            var result = await CliTestProcessHelper.RunCliAsync(
                "render",
                "--plan", planPath,
                "--output", outputPath,
                "--ffmpeg", "ffmpeg",
                "--json-out", jsonOutPath,
                "--overwrite");

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            AssertEnvelopeValid(result.StdOut, "render", out _);
            AssertJsonOutMatches(jsonOutPath, result.StdOut);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task MixAudioCommand_ProducesStructuredEnvelope()
    {
        if (!CliTestProcessHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var dir = CreateTempDir();

        try
        {
            var inputPath = Path.Combine(dir, "input.mp4");
            var bgmPath = Path.Combine(dir, "bgm.wav");
            var mixedPath = Path.Combine(dir, "mixed.wav");
            var planPath = Path.Combine(dir, "edit.json");
            var jsonOutPath = Path.Combine(dir, "mix-audio.json");

            await CliTestProcessHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(2));
            await CliTestProcessHelper.CreateSampleAudioAsync(bgmPath, TimeSpan.FromSeconds(2));

            WriteEditJson(planPath, new EditPlan
            {
                Source = new EditPlanSource { InputPath = inputPath },
                Clips =
                [
                    new EditClip
                    {
                        Id = "c1",
                        InPoint = TimeSpan.Zero,
                        OutPoint = TimeSpan.FromSeconds(1.5)
                    }
                ],
                AudioTracks =
                [
                    new AudioTrackMix
                    {
                        Id = "bgm-01",
                        Role = AudioTrackRole.Bgm,
                        Path = bgmPath,
                        Start = TimeSpan.Zero,
                        GainDb = -12
                    }
                ],
                Output = new EditOutputPlan
                {
                    Path = Path.Combine(dir, "final.mp4"),
                    Container = "mp4"
                }
            });

            var result = await CliTestProcessHelper.RunCliAsync(
                "mix-audio",
                "--plan", planPath,
                "--output", mixedPath,
                "--ffmpeg", "ffmpeg",
                "--json-out", jsonOutPath,
                "--overwrite");

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(mixedPath));
            AssertEnvelopeValid(result.StdOut, "mix-audio", out _);
            AssertJsonOutMatches(jsonOutPath, result.StdOut);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
