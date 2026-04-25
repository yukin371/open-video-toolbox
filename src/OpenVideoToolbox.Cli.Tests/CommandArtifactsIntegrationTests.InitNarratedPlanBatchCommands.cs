using System.Text.Json;
using System.Text.Json.Nodes;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Serialization;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task InitNarratedPlanBatch_WritesPlansAndSummary()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-narrated-batch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        var episodeOneDirectory = Path.Combine(outputDirectory, "episodes", "episode-01");
        var episodeTwoDirectory = Path.Combine(outputDirectory, "episodes", "episode-02");
        var varsDirectory = Path.Combine(outputDirectory, "vars");
        Directory.CreateDirectory(Path.Combine(episodeOneDirectory, "slides"));
        Directory.CreateDirectory(Path.Combine(episodeOneDirectory, "audio"));
        Directory.CreateDirectory(Path.Combine(episodeTwoDirectory, "slides"));
        Directory.CreateDirectory(Path.Combine(episodeTwoDirectory, "audio"));
        Directory.CreateDirectory(varsDirectory);

        var episodeOneManifestPath = Path.Combine(episodeOneDirectory, "narrated.json");
        var episodeTwoManifestPath = Path.Combine(episodeTwoDirectory, "narrated.json");
        var batchManifestPath = Path.Combine(outputDirectory, "batch.json");
        var varsPath = Path.Combine(varsDirectory, "episode-01.json");

        await File.WriteAllTextAsync(Path.Combine(episodeOneDirectory, "slides", "intro.png"), "image-01");
        await File.WriteAllTextAsync(Path.Combine(episodeOneDirectory, "audio", "voice.wav"), "voice-01");
        await File.WriteAllTextAsync(Path.Combine(episodeTwoDirectory, "slides", "chapter.png"), "image-02");
        await File.WriteAllTextAsync(Path.Combine(episodeTwoDirectory, "audio", "voice.wav"), "voice-02");

        await File.WriteAllTextAsync(
            varsPath,
            """
            {
              "slideFile": "intro.png",
              "voiceFile": "voice.wav"
            }
            """);

        var episodeOneManifest = new NarratedSlidesManifest
        {
            Variables = new Dictionary<string, string>
            {
                ["slideFile"] = "missing.png",
                ["voiceFile"] = "missing.wav"
            },
            Video = new NarratedSlidesVideoManifest
            {
                Id = "episode-01",
                Output = "exports/final.mp4"
            },
            Sections =
            [
                new NarratedSlidesSectionManifest
                {
                    Id = "intro",
                    Title = "Intro",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "image",
                        Path = "slides/${slideFile}",
                        DurationMs = 3000
                    },
                    Voice = new NarratedSlidesVoiceManifest
                    {
                        Path = "audio/${voiceFile}",
                        DurationMs = 3000
                    }
                }
            ]
        };

        var episodeTwoManifest = new NarratedSlidesManifest
        {
            Video = new NarratedSlidesVideoManifest
            {
                Id = "episode-02",
                Output = "exports/final.mp4"
            },
            Sections =
            [
                new NarratedSlidesSectionManifest
                {
                    Id = "chapter-01",
                    Title = "Chapter 01",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "image",
                        Path = "slides/chapter.png",
                        DurationMs = 4000
                    },
                    Voice = new NarratedSlidesVoiceManifest
                    {
                        Path = "audio/voice.wav",
                        DurationMs = 4000
                    }
                }
            ]
        };

        await File.WriteAllTextAsync(episodeOneManifestPath, JsonSerializer.Serialize(episodeOneManifest, OpenVideoToolboxJson.Default));
        await File.WriteAllTextAsync(episodeTwoManifestPath, JsonSerializer.Serialize(episodeTwoManifest, OpenVideoToolboxJson.Default));
        await File.WriteAllTextAsync(
            batchManifestPath,
            """
            {
              "schemaVersion": 1,
              "items": [
                {
                  "id": "episode-01",
                  "manifest": "episodes/episode-01/narrated.json",
                  "vars": "vars/episode-01.json"
                },
                {
                  "id": "episode-02",
                  "manifest": "episodes/episode-02/narrated.json",
                  "output": "custom/episode-02/edit.json",
                  "renderOutput": "exports/custom-episode-02.mp4"
                }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync("init-narrated-plan-batch", "--manifest", batchManifestPath);

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("init-narrated-plan-batch", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            Assert.Equal(2, payload["itemCount"]!.GetValue<int>());
            Assert.Equal(2, payload["succeededCount"]!.GetValue<int>());
            Assert.Equal(0, payload["failedCount"]!.GetValue<int>());
            Assert.True(File.Exists(payload["summaryPath"]!.GetValue<string>()));

            var results = payload["results"]!.AsArray();
            Assert.Equal("succeeded", results[0]!["status"]!.GetValue<string>());
            Assert.Equal("succeeded", results[1]!["status"]!.GetValue<string>());

            var defaultPlanPath = Path.Combine(outputDirectory, "tasks", "episode-01", "edit.json");
            var customPlanPath = Path.Combine(outputDirectory, "custom", "episode-02", "edit.json");
            Assert.Equal(defaultPlanPath, results[0]!["planPath"]!.GetValue<string>());
            Assert.Equal(customPlanPath, results[1]!["planPath"]!.GetValue<string>());
            Assert.True(File.Exists(defaultPlanPath));
            Assert.True(File.Exists(customPlanPath));
            Assert.True(File.Exists(results[0]!["resultPath"]!.GetValue<string>()));
            Assert.True(File.Exists(results[1]!["resultPath"]!.GetValue<string>()));

            Assert.Equal(
                Path.Combine(outputDirectory, "tasks", "episode-01", "exports", "final.mp4"),
                results[0]!["result"]!["renderOutputPath"]!.GetValue<string>());
            Assert.Equal(
                Path.Combine(outputDirectory, "exports", "custom-episode-02.mp4"),
                results[1]!["result"]!["renderOutputPath"]!.GetValue<string>());

            var writtenPlan = JsonNode.Parse(await File.ReadAllTextAsync(defaultPlanPath))!.AsObject();
            Assert.Equal(2, writtenPlan["schemaVersion"]!.GetValue<int>());
            Assert.Equal(
                Path.GetFullPath(Path.Combine(episodeOneDirectory, "slides", "intro.png")),
                writtenPlan["timeline"]!["tracks"]![0]!["clips"]![0]!["src"]!.GetValue<string>());
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
    public async Task InitNarratedPlanBatch_ReturnsPartialFailureSummary()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-init-narrated-batch-partial-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(outputDirectory, "episodes", "episode-01", "slides"));
        Directory.CreateDirectory(Path.Combine(outputDirectory, "episodes", "episode-01", "audio"));

        var manifestPath = Path.Combine(outputDirectory, "episodes", "episode-01", "narrated.json");
        var batchManifestPath = Path.Combine(outputDirectory, "batch.json");

        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "episodes", "episode-01", "slides", "intro.png"), "image");
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "episodes", "episode-01", "audio", "voice.wav"), "voice");

        var manifest = new NarratedSlidesManifest
        {
            Video = new NarratedSlidesVideoManifest
            {
                Id = "episode-01",
                Output = "exports/final.mp4"
            },
            Sections =
            [
                new NarratedSlidesSectionManifest
                {
                    Id = "intro",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "image",
                        Path = "slides/intro.png",
                        DurationMs = 2500
                    },
                    Voice = new NarratedSlidesVoiceManifest
                    {
                        Path = "audio/voice.wav",
                        DurationMs = 2500
                    }
                }
            ]
        };

        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, OpenVideoToolboxJson.Default));
        await File.WriteAllTextAsync(
            batchManifestPath,
            """
            {
              "schemaVersion": 1,
              "items": [
                {
                  "id": "episode-01",
                  "manifest": "episodes/episode-01/narrated.json"
                },
                {
                  "id": "missing",
                  "manifest": "episodes/missing/narrated.json"
                }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync("init-narrated-plan-batch", "--manifest", batchManifestPath);

            Assert.Equal(2, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!["payload"]!.AsObject();
            Assert.Equal(2, payload["itemCount"]!.GetValue<int>());
            Assert.Equal(1, payload["succeededCount"]!.GetValue<int>());
            Assert.Equal(1, payload["failedCount"]!.GetValue<int>());
            Assert.Equal("failed", payload["results"]![1]!["status"]!.GetValue<string>());
            Assert.NotNull(payload["results"]![1]!["error"]);
            Assert.True(File.Exists(payload["results"]![1]!["resultPath"]!.GetValue<string>()));
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
