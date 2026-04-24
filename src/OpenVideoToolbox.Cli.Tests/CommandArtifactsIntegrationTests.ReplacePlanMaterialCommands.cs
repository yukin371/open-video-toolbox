using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task ReplacePlanMaterial_ReplacesAudioTrackAndWritesUpdatedPlan()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-replace-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var inputPath = Path.Combine(outputDirectory, "input.mp4");
        var replacementAudioPath = Path.Combine(outputDirectory, "assets", "updated.wav");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(inputPath, "fake-media");
        Directory.CreateDirectory(Path.GetDirectoryName(replacementAudioPath)!);
        await File.WriteAllTextAsync(replacementAudioPath, "fake-audio");
        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": {
                "inputPath": "input.mp4"
              },
              "audioTracks": [
                {
                  "id": "voice-main",
                  "role": "voice",
                  "path": "dub.wav",
                  "start": "00:00:00"
                }
              ],
              "output": {
                "path": "final.mp4",
                "container": "mp4"
              }
            }
            """);

        try
        {
            var result = await RunCliAsync(
                "replace-plan-material",
                "--plan", planPath,
                "--audio-track-id", "voice-main",
                "--path", replacementAudioPath,
                "--path-style", "relative",
                "--check-files");

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("replace-plan-material", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(planPath), payload["planPath"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(planPath), payload["outputPlanPath"]!.GetValue<string>());
            Assert.True(payload["changed"]!.GetValue<bool>());
            Assert.True(payload["validation"]!["isValid"]!.GetValue<bool>());

            var target = payload["target"]!.AsObject();
            Assert.Equal("audioTrack", target["targetType"]!.GetValue<string>());
            Assert.Equal("audioTrack:voice-main", target["targetKey"]!.GetValue<string>());
            Assert.Equal("voice-main", target["selector"]!["audioTrackId"]!.GetValue<string>());
            Assert.Equal("dub.wav", target["previousPath"]!.GetValue<string>());
            Assert.Equal(Path.Combine("assets", "updated.wav"), target["nextPath"]!.GetValue<string>());
            Assert.Equal("relative", target["pathStyleApplied"]!.GetValue<string>());

            var updatedPlan = JsonNode.Parse(await File.ReadAllTextAsync(planPath))!.AsObject();
            Assert.Equal(Path.Combine("assets", "updated.wav"), updatedPlan["audioTracks"]![0]!["path"]!.GetValue<string>());
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
    public async Task ReplacePlanMaterial_CanWriteToNewPlanAndUpdateSubtitleMode()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-replace-cli-subtitles-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var inputPath = Path.Combine(outputDirectory, "input.mp4");
        var originalSubtitlePath = Path.Combine(outputDirectory, "subs", "captions.srt");
        var replacementSubtitlePath = Path.Combine(outputDirectory, "subs", "captions-new.srt");
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var outputPlanPath = Path.Combine(outputDirectory, "generated", "edit.updated.json");

        Directory.CreateDirectory(Path.GetDirectoryName(originalSubtitlePath)!);
        await File.WriteAllTextAsync(inputPath, "fake-media");
        await File.WriteAllTextAsync(originalSubtitlePath, "1");
        await File.WriteAllTextAsync(replacementSubtitlePath, "2");
        await File.WriteAllTextAsync(
            planPath,
            $$"""
            {
              "schemaVersion": 1,
              "source": {
                "inputPath": "input.mp4"
              },
              "subtitles": {
                "path": "{{originalSubtitlePath.Replace("\\", "\\\\")}}",
                "mode": "sidecar"
              },
              "output": {
                "path": "final.mp4",
                "container": "mp4"
              }
            }
            """);

        try
        {
            var result = await RunCliAsync(
                "replace-plan-material",
                "--plan", planPath,
                "--subtitles",
                "--path", replacementSubtitlePath,
                "--subtitle-mode", "burnIn",
                "--write-to", outputPlanPath);

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            var payload = envelope["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPlanPath), payload["outputPlanPath"]!.GetValue<string>());
            Assert.True(File.Exists(outputPlanPath));

            var target = payload["target"]!.AsObject();
            Assert.Equal("absolute", target["pathStyleApplied"]!.GetValue<string>());
            Assert.Equal("sidecar", target["previousSubtitleMode"]!.GetValue<string>());
            Assert.Equal("burnIn", target["nextSubtitleMode"]!.GetValue<string>());

            var originalPlan = JsonNode.Parse(await File.ReadAllTextAsync(planPath))!.AsObject();
            var updatedPlan = JsonNode.Parse(await File.ReadAllTextAsync(outputPlanPath))!.AsObject();
            Assert.Equal(originalSubtitlePath, originalPlan["subtitles"]!["path"]!.GetValue<string>());
            Assert.Equal("sidecar", originalPlan["subtitles"]!["mode"]!.GetValue<string>());
            Assert.Equal(replacementSubtitlePath, updatedPlan["subtitles"]!["path"]!.GetValue<string>());
            Assert.Equal("burnIn", updatedPlan["subtitles"]!["mode"]!.GetValue<string>());
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
    public async Task ReplacePlanMaterial_RequireValidBlocksWriteWhenUpdatedPlanFailsValidation()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-replace-cli-invalid-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var inputPath = Path.Combine(outputDirectory, "input.mp4");
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var outputPlanPath = Path.Combine(outputDirectory, "generated", "edit.updated.json");
        var missingReplacementPath = Path.Combine(outputDirectory, "missing", "updated.wav");

        await File.WriteAllTextAsync(inputPath, "fake-media");
        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": {
                "inputPath": "input.mp4"
              },
              "audioTracks": [
                {
                  "id": "voice-main",
                  "role": "voice",
                  "path": "dub.wav",
                  "start": "00:00:00"
                }
              ],
              "output": {
                "path": "final.mp4",
                "container": "mp4"
              }
            }
            """);

        try
        {
            var result = await RunCliAsync(
                "replace-plan-material",
                "--plan", planPath,
                "--audio-track-id", "voice-main",
                "--path", missingReplacementPath,
                "--write-to", outputPlanPath,
                "--path-style", "relative",
                "--check-files",
                "--require-valid");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("require-valid", result.StdErr, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(outputPlanPath));

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("replace-plan-material", envelope["command"]!.GetValue<string>());
            var payload = envelope["payload"]!.AsObject();
            Assert.NotNull(payload["error"]);
            Assert.False(payload["replacePlanMaterial"]!["validation"]!["isValid"]!.GetValue<bool>());
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
