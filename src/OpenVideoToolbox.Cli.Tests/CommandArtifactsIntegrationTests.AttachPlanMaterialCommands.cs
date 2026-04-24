using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task AttachPlanMaterial_AttachesTranscriptToExistingPlan()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-attach-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var inputPath = Path.Combine(outputDirectory, "input.mp4");
        var transcriptPath = Path.Combine(outputDirectory, "signals", "transcript.json");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(inputPath, "fake-media");
        Directory.CreateDirectory(Path.GetDirectoryName(transcriptPath)!);
        await File.WriteAllTextAsync(transcriptPath, "{}");
        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": {
                "inputPath": "input.mp4"
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
                "attach-plan-material",
                "--plan", planPath,
                "--transcript",
                "--path", transcriptPath,
                "--check-files");

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("attach-plan-material", envelope["command"]!.GetValue<string>());
            var payload = envelope["payload"]!.AsObject();
            Assert.True(payload["added"]!.GetValue<bool>());
            Assert.True(payload["validation"]!["isValid"]!.GetValue<bool>());
            Assert.Equal("transcript", payload["target"]!["targetType"]!.GetValue<string>());
            Assert.Equal(Path.Combine("signals", "transcript.json"), payload["target"]!["nextPath"]!.GetValue<string>());

            var updatedPlan = JsonNode.Parse(await File.ReadAllTextAsync(planPath))!.AsObject();
            Assert.Equal(Path.Combine("signals", "transcript.json"), updatedPlan["transcript"]!["path"]!.GetValue<string>());
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
    public async Task AttachPlanMaterial_CanUpsertDeclaredArtifactSlot()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-attach-artifact-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var inputPath = Path.Combine(outputDirectory, "input.mp4");
        var bgmPath = Path.Combine(outputDirectory, "audio", "theme.wav");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(inputPath, "fake-media");
        Directory.CreateDirectory(Path.GetDirectoryName(bgmPath)!);
        await File.WriteAllTextAsync(bgmPath, "fake-audio");
        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": {
                "inputPath": "input.mp4"
              },
              "template": {
                "id": "commentary-bgm"
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
                "attach-plan-material",
                "--plan", planPath,
                "--artifact-slot", "bgm",
                "--path", bgmPath,
                "--path-style", "relative",
                "--check-files");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!["payload"]!.AsObject();
            Assert.True(payload["added"]!.GetValue<bool>());
            Assert.Equal("artifact", payload["target"]!["targetType"]!.GetValue<string>());
            Assert.Equal("artifact:bgm", payload["target"]!["targetKey"]!.GetValue<string>());
            Assert.Equal(Path.Combine("audio", "theme.wav"), payload["target"]!["nextPath"]!.GetValue<string>());

            var updatedPlan = JsonNode.Parse(await File.ReadAllTextAsync(planPath))!.AsObject();
            Assert.Equal("bgm", updatedPlan["artifacts"]![0]!["slotId"]!.GetValue<string>());
            Assert.Equal("audio", updatedPlan["artifacts"]![0]!["kind"]!.GetValue<string>());
            Assert.Equal(Path.Combine("audio", "theme.wav"), updatedPlan["artifacts"]![0]!["path"]!.GetValue<string>());
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
    public async Task AttachPlanMaterial_RequireValidBlocksWriteWhenAttachedPlanFailsValidation()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-attach-invalid-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var inputPath = Path.Combine(outputDirectory, "input.mp4");
        var missingTranscriptPath = Path.Combine(outputDirectory, "missing", "transcript.json");
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var outputPlanPath = Path.Combine(outputDirectory, "generated", "edit.updated.json");

        await File.WriteAllTextAsync(inputPath, "fake-media");
        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": {
                "inputPath": "input.mp4"
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
                "attach-plan-material",
                "--plan", planPath,
                "--transcript",
                "--path", missingTranscriptPath,
                "--write-to", outputPlanPath,
                "--check-files",
                "--require-valid");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("require-valid", result.StdErr, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(outputPlanPath));

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("attach-plan-material", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["payload"]!["attachPlanMaterial"]!["validation"]!["isValid"]!.GetValue<bool>());
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
    public async Task AttachPlanMaterial_CanAddAudioTrackWithExplicitRole()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-attach-audio-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var inputPath = Path.Combine(outputDirectory, "input.mp4");
        var audioPath = Path.Combine(outputDirectory, "audio", "dub.wav");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(inputPath, "fake-media");
        Directory.CreateDirectory(Path.GetDirectoryName(audioPath)!);
        await File.WriteAllTextAsync(audioPath, "fake-audio");
        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": {
                "inputPath": "input.mp4"
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
                "attach-plan-material",
                "--plan", planPath,
                "--audio-track-id", "voice-main",
                "--audio-track-role", "voice",
                "--path", audioPath,
                "--path-style", "relative",
                "--check-files");

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("attach-plan-material", envelope["command"]!.GetValue<string>());
            var payload = envelope["payload"]!.AsObject();
            Assert.True(payload["added"]!.GetValue<bool>());
            Assert.Equal("audioTrack", payload["target"]!["targetType"]!.GetValue<string>());
            Assert.Equal("audioTrack:voice-main", payload["target"]!["targetKey"]!.GetValue<string>());
            Assert.Equal("voice", payload["target"]!["nextAudioTrackRole"]!.GetValue<string>());

            var updatedPlan = JsonNode.Parse(await File.ReadAllTextAsync(planPath))!.AsObject();
            Assert.Equal("voice-main", updatedPlan["audioTracks"]![0]!["id"]!.GetValue<string>());
            Assert.Equal("voice", updatedPlan["audioTracks"]![0]!["role"]!.GetValue<string>());
            Assert.Equal(Path.Combine("audio", "dub.wav"), updatedPlan["audioTracks"]![0]!["path"]!.GetValue<string>());
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
