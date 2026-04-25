using System.Text.Json;
using System.Text.Json.Nodes;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Serialization;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task InitNarratedPlan_WritesPlanAndReturnsStructuredEnvelope()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-narrated-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var slidesDirectory = Path.Combine(outputDirectory, "slides");
        var audioDirectory = Path.Combine(outputDirectory, "audio");
        var subtitlesDirectory = Path.Combine(outputDirectory, "subtitles");
        Directory.CreateDirectory(slidesDirectory);
        Directory.CreateDirectory(audioDirectory);
        Directory.CreateDirectory(subtitlesDirectory);

        var introVideoPath = Path.Combine(slidesDirectory, "intro.mp4");
        var deepDiveVideoPath = Path.Combine(slidesDirectory, "deep-dive.mp4");
        var introVoicePath = Path.Combine(audioDirectory, "intro.wav");
        var deepDiveVoicePath = Path.Combine(audioDirectory, "deep-dive.wav");
        var bgmPath = Path.Combine(audioDirectory, "bgm.mp3");
        var subtitlePath = Path.Combine(subtitlesDirectory, "podcast.srt");
        var manifestPath = Path.Combine(outputDirectory, "narrated.json");
        var planPath = Path.Combine(outputDirectory, "edit.v2.json");
        var jsonOutPath = Path.Combine(outputDirectory, "init-narrated-plan.json");

        await File.WriteAllTextAsync(introVideoPath, "video-intro");
        await File.WriteAllTextAsync(deepDiveVideoPath, "video-deep-dive");
        await File.WriteAllTextAsync(introVoicePath, "voice-intro");
        await File.WriteAllTextAsync(deepDiveVoicePath, "voice-deep-dive");
        await File.WriteAllTextAsync(bgmPath, "bgm");
        await File.WriteAllTextAsync(subtitlePath, "1\n00:00:00,000 --> 00:00:01,000\nHello\n");

        var manifest = new NarratedSlidesManifest
        {
            Video = new NarratedSlidesVideoManifest
            {
                Id = "episode-01",
                Output = "exports/final.mp4"
            },
            Subtitles = new NarratedSlidesSubtitleManifest
            {
                Path = Path.Combine("subtitles", "podcast.srt").Replace('\\', '/'),
                Mode = SubtitleMode.Sidecar
            },
            Bgm = new NarratedSlidesBgmManifest
            {
                Path = Path.Combine("audio", "bgm.mp3").Replace('\\', '/'),
                GainDb = -16
            },
            Sections =
            [
                new NarratedSlidesSectionManifest
                {
                    Id = "intro",
                    Title = "Intro",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "video",
                        Path = Path.Combine("slides", "intro.mp4").Replace('\\', '/'),
                        DurationMs = 5000
                    },
                    Voice = new NarratedSlidesVoiceManifest
                    {
                        Path = Path.Combine("audio", "intro.wav").Replace('\\', '/'),
                        DurationMs = 3000
                    }
                },
                new NarratedSlidesSectionManifest
                {
                    Id = "deep-dive",
                    Title = "Deep Dive",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "video",
                        Path = Path.Combine("slides", "deep-dive.mp4").Replace('\\', '/'),
                        DurationMs = 7000
                    },
                    Voice = new NarratedSlidesVoiceManifest
                    {
                        Path = Path.Combine("audio", "deep-dive.wav").Replace('\\', '/'),
                        DurationMs = 4000
                    }
                }
            ]
        };

        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, OpenVideoToolboxJson.Default));

        try
        {
            var result = await RunCliAsync(
                "init-narrated-plan",
                "--manifest",
                manifestPath,
                "--output",
                planPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("init-narrated-plan", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(manifestPath), payload["manifestPath"]!.GetValue<string>());
            Assert.Equal("narrated-slides-starter", payload["template"]!["id"]!.GetValue<string>());
            Assert.Equal("builtIn", payload["template"]!["source"]!["kind"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(planPath), payload["planPath"]!.GetValue<string>());
            Assert.Equal(0, payload["probedSectionCount"]!.GetValue<int>());
            Assert.Equal(2, payload["stats"]!["sectionCount"]!.GetValue<int>());
            Assert.True(payload["stats"]!["hasSubtitles"]!.GetValue<bool>());
            Assert.True(payload["stats"]!["hasBgm"]!.GetValue<bool>());

            Assert.True(File.Exists(planPath));
            Assert.True(File.Exists(jsonOutPath));

            var writtenPlan = JsonNode.Parse(await File.ReadAllTextAsync(planPath))!.AsObject();
            Assert.Equal(2, writtenPlan["schemaVersion"]!.GetValue<int>());
            Assert.Equal("narrated-slides-starter", writtenPlan["template"]!["id"]!.GetValue<string>());
            Assert.Equal("builtIn", writtenPlan["template"]!["source"]!["kind"]!.GetValue<string>());
            Assert.Equal(3, writtenPlan["timeline"]!["tracks"]!.AsArray().Count);

            var writtenJsonOut = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            Assert.Equal("init-narrated-plan", writtenJsonOut["command"]!.GetValue<string>());
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
    public async Task InitNarratedPlan_RejectsMissingSectionFiles()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-narrated-missing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var manifestPath = Path.Combine(outputDirectory, "narrated.json");
        var planPath = Path.Combine(outputDirectory, "edit.v2.json");

        var manifest = new NarratedSlidesManifest
        {
            Video = new NarratedSlidesVideoManifest(),
            Sections =
            [
                new NarratedSlidesSectionManifest
                {
                    Id = "intro",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "video",
                        Path = "slides/missing.mp4",
                        DurationMs = 3000
                    },
                    Voice = new NarratedSlidesVoiceManifest
                    {
                        Path = "audio/missing.wav",
                        DurationMs = 3000
                    }
                }
            ]
        };

        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, OpenVideoToolboxJson.Default));

        try
        {
            var result = await RunCliAsync(
                "init-narrated-plan",
                "--manifest",
                manifestPath,
                "--output",
                planPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("points to a missing file", result.StdErr, StringComparison.Ordinal);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("init-narrated-plan", envelope["command"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(manifestPath), envelope["payload"]!["manifest"]!["path"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(planPath), envelope["payload"]!["manifest"]!["outputPath"]!.GetValue<string>());
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
    public async Task InitNarratedPlan_SupportsImageSections()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-narrated-image-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var slidesDirectory = Path.Combine(outputDirectory, "slides");
        var audioDirectory = Path.Combine(outputDirectory, "audio");
        Directory.CreateDirectory(slidesDirectory);
        Directory.CreateDirectory(audioDirectory);

        var imagePath = Path.Combine(slidesDirectory, "cover.png");
        var voicePath = Path.Combine(audioDirectory, "intro.wav");
        var manifestPath = Path.Combine(outputDirectory, "narrated.image.json");
        var planPath = Path.Combine(outputDirectory, "edit.image.v2.json");

        await File.WriteAllTextAsync(imagePath, "image");
        await File.WriteAllTextAsync(voicePath, "voice-intro");

        var manifest = new NarratedSlidesManifest
        {
            Video = new NarratedSlidesVideoManifest
            {
                Id = "episode-image"
            },
            Sections =
            [
                new NarratedSlidesSectionManifest
                {
                    Id = "cover",
                    Title = "Cover",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "image",
                        Path = Path.Combine("slides", "cover.png").Replace('\\', '/')
                    },
                    Voice = new NarratedSlidesVoiceManifest
                    {
                        Path = Path.Combine("audio", "intro.wav").Replace('\\', '/'),
                        DurationMs = 2500
                    }
                }
            ]
        };

        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, OpenVideoToolboxJson.Default));

        try
        {
            var result = await RunCliAsync(
                "init-narrated-plan",
                "--manifest",
                manifestPath,
                "--output",
                planPath);

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("init-narrated-plan", envelope["command"]!.GetValue<string>());
            Assert.Equal(0, envelope["payload"]!["probedSectionCount"]!.GetValue<int>());

            var writtenPlan = JsonNode.Parse(await File.ReadAllTextAsync(planPath))!.AsObject();
            var tracks = writtenPlan["timeline"]!["tracks"]!.AsArray();
            var mainTrack = tracks.Single(node => node!["id"]!.GetValue<string>() == "main")!.AsObject();
            var mainClip = mainTrack["clips"]!.AsArray().Single()!.AsObject();

            Assert.Equal(Path.GetFullPath(imagePath), mainClip["src"]!.GetValue<string>());
            Assert.Equal("00:00:02.5000000", mainClip["duration"]!.GetValue<string>());
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
    public async Task InitNarratedPlan_AddsProgressBarEffectWhenConfigured()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-narrated-progress-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var slidesDirectory = Path.Combine(outputDirectory, "slides");
        var audioDirectory = Path.Combine(outputDirectory, "audio");
        Directory.CreateDirectory(slidesDirectory);
        Directory.CreateDirectory(audioDirectory);

        var imagePath = Path.Combine(slidesDirectory, "cover.png");
        var voicePath = Path.Combine(audioDirectory, "intro.wav");
        var manifestPath = Path.Combine(outputDirectory, "narrated.progress.json");
        var planPath = Path.Combine(outputDirectory, "edit.progress.v2.json");

        await File.WriteAllTextAsync(imagePath, "image");
        await File.WriteAllTextAsync(voicePath, "voice-intro");

        var manifest = new NarratedSlidesManifest
        {
            Video = new NarratedSlidesVideoManifest
            {
                ProgressBar = new NarratedSlidesProgressBarManifest
                {
                    Enabled = true,
                    Height = 10,
                    Margin = 24,
                    Color = "yellow@0.9",
                    BackgroundColor = "black@0.2"
                }
            },
            Sections =
            [
                new NarratedSlidesSectionManifest
                {
                    Id = "cover",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "image",
                        Path = Path.Combine("slides", "cover.png").Replace('\\', '/')
                    },
                    Voice = new NarratedSlidesVoiceManifest
                    {
                        Path = Path.Combine("audio", "intro.wav").Replace('\\', '/'),
                        DurationMs = 2500
                    }
                }
            ]
        };

        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, OpenVideoToolboxJson.Default));

        try
        {
            var result = await RunCliAsync(
                "init-narrated-plan",
                "--manifest",
                manifestPath,
                "--output",
                planPath);

            Assert.Equal(0, result.ExitCode);

            var writtenPlan = JsonNode.Parse(await File.ReadAllTextAsync(planPath))!.AsObject();
            var mainTrack = writtenPlan["timeline"]!["tracks"]!
                .AsArray()
                .Single(node => node!["id"]!.GetValue<string>() == "main")!
                .AsObject();

            var effects = mainTrack["effects"]!.AsArray();
            Assert.Equal("scale", effects[0]!["type"]!.GetValue<string>());
            Assert.Equal("progress_bar", effects[1]!["type"]!.GetValue<string>());
            Assert.Equal(2.5, effects[1]!["durationSeconds"]!.GetValue<double>());
            Assert.Equal(10, effects[1]!["height"]!.GetValue<int>());
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
