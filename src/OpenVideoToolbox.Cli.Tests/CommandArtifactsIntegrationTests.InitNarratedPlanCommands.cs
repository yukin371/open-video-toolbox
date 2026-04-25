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

    [Fact]
    public async Task InitNarratedPlan_SupportsVariablesAndCliOverlay()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-narrated-vars-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var slidesDirectory = Path.Combine(outputDirectory, "slides");
        var audioDirectory = Path.Combine(outputDirectory, "audio");
        var subtitlesDirectory = Path.Combine(outputDirectory, "subtitles");
        Directory.CreateDirectory(slidesDirectory);
        Directory.CreateDirectory(audioDirectory);
        Directory.CreateDirectory(subtitlesDirectory);

        var imagePath = Path.Combine(slidesDirectory, "cover.png");
        var voicePath = Path.Combine(audioDirectory, "intro.wav");
        var bgmPath = Path.Combine(audioDirectory, "bgm.mp3");
        var subtitlePath = Path.Combine(subtitlesDirectory, "episode.srt");
        var manifestPath = Path.Combine(outputDirectory, "narrated.vars.json");
        var varsPath = Path.Combine(outputDirectory, "vars.json");
        var planPath = Path.Combine(outputDirectory, "edit.vars.v2.json");

        await File.WriteAllTextAsync(imagePath, "image");
        await File.WriteAllTextAsync(voicePath, "voice");
        await File.WriteAllTextAsync(bgmPath, "bgm");
        await File.WriteAllTextAsync(subtitlePath, "1\n00:00:00,000 --> 00:00:01,000\nHello\n");
        await File.WriteAllTextAsync(varsPath, """
{
  "episodeId": "episode-02",
  "slideName": "cover",
  "voiceName": "intro",
  "barColor": "yellow@0.9"
}
""");

        var manifest = new NarratedSlidesManifest
        {
            Variables = new Dictionary<string, string>
            {
                ["episodeId"] = "episode-01",
                ["slidesDir"] = "slides",
                ["audioDir"] = "audio",
                ["subtitlesDir"] = "subtitles"
            },
            Video = new NarratedSlidesVideoManifest
            {
                Id = "${episodeId}",
                Output = "exports/${episodeId}.mp4",
                ProgressBar = new NarratedSlidesProgressBarManifest
                {
                    Enabled = true,
                    Color = "${barColor:-white@0.95}",
                    BackgroundColor = "${barBackground:-black@0.25}"
                }
            },
            Template = new NarratedSlidesTemplateManifest
            {
                Id = "${templateId:-narrated-slides-starter}"
            },
            Subtitles = new NarratedSlidesSubtitleManifest
            {
                Path = "${subtitlesDir}/${subtitleName:-episode}.srt",
                Mode = SubtitleMode.Sidecar
            },
            Bgm = new NarratedSlidesBgmManifest
            {
                Path = "${audioDir}/bgm.mp3",
                Slot = new NarratedSlidesSlotManifest
                {
                    Name = "${bgmSlotName:-bgm}",
                    Required = false
                }
            },
            Sections =
            [
                new NarratedSlidesSectionManifest
                {
                    Id = "${sectionId:-cover}",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "${visualKind:-image}",
                        Path = "${slidesDir}/${slideName}.png"
                    },
                    Voice = new NarratedSlidesVoiceManifest
                    {
                        Path = "${audioDir}/${voiceName}.wav",
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
                planPath,
                "--vars",
                varsPath);

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            var payload = envelope["payload"]!.AsObject();
            Assert.Equal(
                Path.Combine(outputDirectory, "exports", "episode-02.mp4"),
                payload["renderOutputPath"]!.GetValue<string>());

            var writtenPlan = JsonNode.Parse(await File.ReadAllTextAsync(planPath))!.AsObject();
            Assert.Equal("narrated-slides-starter", writtenPlan["template"]!["id"]!.GetValue<string>());
            Assert.Equal(
                Path.Combine(outputDirectory, "exports", "episode-02.mp4"),
                writtenPlan["output"]!["path"]!.GetValue<string>());
            Assert.Equal(
                Path.GetFullPath(subtitlePath),
                writtenPlan["subtitles"]!["path"]!.GetValue<string>());

            var tracks = writtenPlan["timeline"]!["tracks"]!.AsArray();
            var mainTrack = tracks.Single(node => node!["id"]!.GetValue<string>() == "main")!.AsObject();
            var bgmTrack = tracks.Single(node => node!["id"]!.GetValue<string>() == "bgm")!.AsObject();

            Assert.Equal(
                Path.GetFullPath(imagePath),
                mainTrack["clips"]!.AsArray().Single()!["src"]!.GetValue<string>());
            Assert.Equal("cover-video", mainTrack["clips"]!.AsArray().Single()!["id"]!.GetValue<string>());
            Assert.Equal("yellow@0.9", mainTrack["effects"]![1]!["color"]!.GetValue<string>());
            Assert.Equal("black@0.25", mainTrack["effects"]![1]!["backgroundColor"]!.GetValue<string>());
            Assert.Equal(
                Path.GetFullPath(bgmPath),
                bgmTrack["clips"]!.AsArray().Single()!["src"]!.GetValue<string>());
            Assert.Equal("bgm", bgmTrack["slot"]!["name"]!.GetValue<string>());
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
    public async Task InitNarratedPlan_SkipsOptionalBgmSlotWhenUnbound()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-narrated-bgm-slot-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var slidesDirectory = Path.Combine(outputDirectory, "slides");
        var audioDirectory = Path.Combine(outputDirectory, "audio");
        Directory.CreateDirectory(slidesDirectory);
        Directory.CreateDirectory(audioDirectory);

        var imagePath = Path.Combine(slidesDirectory, "cover.png");
        var voicePath = Path.Combine(audioDirectory, "intro.wav");
        var manifestPath = Path.Combine(outputDirectory, "narrated.bgm-slot.json");
        var planPath = Path.Combine(outputDirectory, "edit.bgm-slot.v2.json");

        await File.WriteAllTextAsync(imagePath, "image");
        await File.WriteAllTextAsync(voicePath, "voice");

        var manifest = new NarratedSlidesManifest
        {
            Video = new NarratedSlidesVideoManifest
            {
                Output = "exports/final.mp4"
            },
            Bgm = new NarratedSlidesBgmManifest
            {
                Path = "${bgmPath:-}",
                Slot = new NarratedSlidesSlotManifest
                {
                    Name = "bgm",
                    Required = false
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

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.False(envelope["payload"]!["stats"]!["hasBgm"]!.GetValue<bool>());

            var writtenPlan = JsonNode.Parse(await File.ReadAllTextAsync(planPath))!.AsObject();
            var tracks = writtenPlan["timeline"]!["tracks"]!.AsArray();

            Assert.Equal(2, tracks.Count);
            Assert.DoesNotContain(tracks, node => node!["id"]!.GetValue<string>() == "bgm");
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
    public async Task InitNarratedPlan_ProjectsPlaceholderWhenOptionalVisualSlotIsUnbound()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-narrated-visual-slot-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var audioDirectory = Path.Combine(outputDirectory, "audio");
        Directory.CreateDirectory(audioDirectory);

        var voicePath = Path.Combine(audioDirectory, "intro.wav");
        var manifestPath = Path.Combine(outputDirectory, "narrated.visual-slot.json");
        var planPath = Path.Combine(outputDirectory, "edit.visual-slot.v2.json");

        await File.WriteAllTextAsync(voicePath, "voice");

        var manifest = new NarratedSlidesManifest
        {
            Video = new NarratedSlidesVideoManifest
            {
                Output = "exports/final.mp4"
            },
            Sections =
            [
                new NarratedSlidesSectionManifest
                {
                    Id = "cover",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "image",
                        Slot = new NarratedSlidesSlotManifest
                        {
                            Name = "cover-visual",
                            Required = false
                        }
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
            Assert.Equal(Path.GetFullPath(voicePath), writtenPlan["source"]!["inputPath"]!.GetValue<string>());

            var tracks = writtenPlan["timeline"]!["tracks"]!.AsArray();
            var mainTrack = tracks.Single(node => node!["id"]!.GetValue<string>() == "main")!.AsObject();
            var mainClip = mainTrack["clips"]!.AsArray().Single()!.AsObject();

            Assert.Null(mainClip["src"]);
            Assert.Equal("00:00:02.5000000", mainClip["duration"]!.GetValue<string>());
            Assert.Equal("color", mainClip["placeholder"]!["kind"]!.GetValue<string>());
            Assert.Equal("black", mainClip["placeholder"]!["color"]!.GetValue<string>());
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
    public async Task InitNarratedPlan_OptionalVisualSlotPlanCanRenderPreview()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-narrated-visual-slot-preview-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var audioDirectory = Path.Combine(outputDirectory, "audio");
        Directory.CreateDirectory(audioDirectory);

        var voicePath = Path.Combine(audioDirectory, "intro.wav");
        var manifestPath = Path.Combine(outputDirectory, "narrated.visual-slot.preview.json");
        var planPath = Path.Combine(outputDirectory, "edit.visual-slot.preview.v2.json");

        await File.WriteAllTextAsync(voicePath, "voice");

        var manifest = new NarratedSlidesManifest
        {
            Video = new NarratedSlidesVideoManifest
            {
                Output = "exports/final.mp4"
            },
            Sections =
            [
                new NarratedSlidesSectionManifest
                {
                    Id = "cover",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "image",
                        Slot = new NarratedSlidesSlotManifest
                        {
                            Name = "cover-visual",
                            Required = false
                        }
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
            var initResult = await RunCliAsync(
                "init-narrated-plan",
                "--manifest",
                manifestPath,
                "--output",
                planPath);

            Assert.Equal(0, initResult.ExitCode);

            var renderResult = await RunCliAsync(
                "render",
                "--plan",
                planPath,
                "--preview");

            Assert.Equal(0, renderResult.ExitCode);

            var payload = JsonNode.Parse(renderResult.StdOut)!.AsObject()["payload"]!.AsObject();
            var arguments = payload["executionPreview"]!["commandPlan"]!["arguments"]!.AsArray()
                .Select(node => node!.GetValue<string>())
                .ToArray();

            Assert.Equal(2, payload["render"]!["schemaVersion"]!.GetValue<int>());
            Assert.Equal(2, payload["executionPreview"]!["commandPlan"]!["schemaVersion"]!.GetValue<int>());
            Assert.Contains("lavfi", arguments);
            Assert.Contains("color=c=black:s=1920x1080:r=30", arguments);
            Assert.Contains("[v_out]", arguments);
            Assert.DoesNotContain("-an", arguments);
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
    public async Task InitNarratedPlan_RejectsUnresolvedVariables()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-narrated-unresolved-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var audioDirectory = Path.Combine(outputDirectory, "audio");
        Directory.CreateDirectory(audioDirectory);

        var voicePath = Path.Combine(audioDirectory, "intro.wav");
        var manifestPath = Path.Combine(outputDirectory, "narrated.unresolved.json");
        var planPath = Path.Combine(outputDirectory, "edit.unresolved.v2.json");

        await File.WriteAllTextAsync(voicePath, "voice");

        var manifest = new NarratedSlidesManifest
        {
            Video = new NarratedSlidesVideoManifest(),
            Sections =
            [
                new NarratedSlidesSectionManifest
                {
                    Id = "cover",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "image",
                        Path = "${missingAsset}"
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

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("unresolved variable 'missingAsset'", result.StdErr, StringComparison.Ordinal);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
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
}
