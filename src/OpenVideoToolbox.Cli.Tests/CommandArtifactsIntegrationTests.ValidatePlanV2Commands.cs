using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task ValidatePlan_AcceptsSchemaV2Timeline()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-validate-v2-valid-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "edit.v2.json");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 2,
              "source": {
                "inputPath": "input.mp4"
              },
              "timeline": {
                "duration": "00:00:05",
                "resolution": { "w": 1920, "h": 1080 },
                "frameRate": 30,
                "tracks": [
                  {
                    "id": "main",
                    "kind": "video",
                    "clips": [
                      {
                        "id": "clip-001",
                        "start": "00:00:00",
                        "in": "00:00:01",
                        "out": "00:00:03"
                      }
                    ]
                  }
                ]
              },
              "output": {
                "path": "final.mp4",
                "container": "mp4"
              }
            }
            """);

        try
        {
            var result = await RunCliAsync("validate-plan", "--plan", planPath);

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            var payload = envelope["payload"]!.AsObject();

            Assert.Equal("validate-plan", envelope["command"]!.GetValue<string>());
            Assert.True(payload["isValid"]!.GetValue<bool>());
            Assert.Equal("basic", payload["checkMode"]!.GetValue<string>());
            Assert.Equal(0, payload["stats"]!["totalIssues"]!.GetValue<int>());
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
    public async Task ValidatePlan_ReportsSchemaV2TimelineErrors()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-validate-v2-invalid-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "invalid.v2.json");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 2,
              "source": {
                "inputPath": "input.mp4"
              },
              "timeline": {
                "resolution": { "w": 0, "h": 1080 },
                "frameRate": 0,
                "tracks": [
                  {
                    "id": "main",
                    "kind": "video",
                    "clips": [
                      {
                        "id": "clip-001",
                        "start": "-00:00:01"
                      },
                      {
                        "id": "clip-001",
                        "start": "00:00:01"
                      }
                    ]
                  },
                  {
                    "id": "main",
                    "kind": "audio",
                    "clips": []
                  }
                ]
              },
              "output": {
                "path": "final.mp4",
                "container": "mp4"
              }
            }
            """);

        try
        {
            var result = await RunCliAsync("validate-plan", "--plan", planPath);

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            var payload = envelope["payload"]!.AsObject();
            var issues = payload["issues"]!.AsArray();

            Assert.False(payload["isValid"]!.GetValue<bool>());
            Assert.Equal("basic", payload["checkMode"]!.GetValue<string>());
            Assert.True(payload["stats"]!["totalIssues"]!.GetValue<int>() >= 6);
            Assert.Contains(issues, issue => issue!["code"]!.GetValue<string>() == "timeline.resolution.invalid");
            Assert.Contains(issues, issue => issue!["code"]!.GetValue<string>() == "timeline.frameRate.invalid");
            Assert.Contains(issues, issue => issue!["code"]!.GetValue<string>() == "timeline.track.id.duplicate");
            Assert.Contains(issues, issue => issue!["code"]!.GetValue<string>() == "timeline.clip.id.duplicate");
            Assert.Contains(issues, issue => issue!["code"]!.GetValue<string>() == "timeline.clip.start.invalid");
            Assert.Contains(issues, issue => issue!["code"]!.GetValue<string>() == "timeline.clip.video.range.required");
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
    public async Task ValidatePlan_RecognizesBuiltInTimelineEffects()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-validate-v2-effects-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "effects.v2.json");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 2,
              "source": {
                "inputPath": "input.mp4"
              },
              "timeline": {
                "duration": "00:00:05",
                "resolution": { "w": 1920, "h": 1080 },
                "frameRate": 30,
                "tracks": [
                  {
                    "id": "main",
                    "kind": "video",
                    "effects": [
                      {
                        "type": "fade"
                      }
                    ],
                    "clips": [
                      {
                        "id": "clip-001",
                        "start": "00:00:00",
                        "in": "00:00:00",
                        "out": "00:00:03"
                      }
                    ]
                  }
                ]
              },
              "output": {
                "path": "final.mp4",
                "container": "mp4"
              }
            }
            """);

        try
        {
            var result = await RunCliAsync("validate-plan", "--plan", planPath);

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            var payload = envelope["payload"]!.AsObject();
            var issues = payload["issues"]!.AsArray();

            Assert.True(payload["isValid"]!.GetValue<bool>());
            Assert.DoesNotContain(issues, issue => issue!["code"]!.GetValue<string>() == "timeline.effect.type.unknown");
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
    public async Task ValidatePlan_WarnsForUnknownTimelineEffects()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-validate-v2-unknown-effect-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var planPath = Path.Combine(outputDirectory, "unknown-effect.v2.json");

        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 2,
              "source": {
                "inputPath": "input.mp4"
              },
              "timeline": {
                "duration": "00:00:05",
                "resolution": { "w": 1920, "h": 1080 },
                "frameRate": 30,
                "tracks": [
                  {
                    "id": "main",
                    "kind": "video",
                    "effects": [
                      {
                        "type": "unknown_effect"
                      }
                    ],
                    "clips": [
                      {
                        "id": "clip-001",
                        "start": "00:00:00",
                        "in": "00:00:00",
                        "out": "00:00:03"
                      }
                    ]
                  }
                ]
              },
              "output": {
                "path": "final.mp4",
                "container": "mp4"
              }
            }
            """);

        try
        {
            var result = await RunCliAsync("validate-plan", "--plan", planPath);

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            var payload = envelope["payload"]!.AsObject();
            var issues = payload["issues"]!.AsArray();
            var issue = Assert.Single(issues.Where(issue => issue!["code"]!.GetValue<string>() == "timeline.effect.type.unknown"));

            Assert.True(payload["isValid"]!.GetValue<bool>());
            Assert.Equal("warning", issue!["severity"]!.GetValue<string>());
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
