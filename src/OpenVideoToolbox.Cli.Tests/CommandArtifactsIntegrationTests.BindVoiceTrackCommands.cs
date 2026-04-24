using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task BindVoiceTrack_AddsDefaultVoiceMainTrack()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-bind-voice-{Guid.NewGuid():N}");
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
                "bind-voice-track",
                "--plan", planPath,
                "--path", audioPath,
                "--path-style", "relative",
                "--check-files");

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("bind-voice-track", envelope["command"]!.GetValue<string>());
            var payload = envelope["payload"]!.AsObject();
            Assert.Equal("voice-main", payload["voiceTrack"]!["trackId"]!.GetValue<string>());
            Assert.Equal("voice", payload["voiceTrack"]!["roleApplied"]!.GetValue<string>());
            Assert.True(payload["voiceTrack"]!["added"]!.GetValue<bool>());

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

    [Fact]
    public async Task BindVoiceTrack_PreservesExistingRoleWhenRoleIsOmitted()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-bind-voice-existing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var inputPath = Path.Combine(outputDirectory, "input.mp4");
        var audioPath = Path.Combine(outputDirectory, "audio", "vc.wav");
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
              "audioTracks": [
                {
                  "id": "voice-main",
                  "role": "effects",
                  "path": "old.wav",
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
                "bind-voice-track",
                "--plan", planPath,
                "--path", audioPath,
                "--path-style", "relative");

            Assert.Equal(0, result.ExitCode);

            var payload = JsonNode.Parse(result.StdOut)!["payload"]!.AsObject();
            Assert.Equal("effects", payload["voiceTrack"]!["roleApplied"]!.GetValue<string>());
            Assert.False(payload["voiceTrack"]!["added"]!.GetValue<bool>());
            Assert.Equal("effects", payload["voiceTrack"]!["previousAudioTrackRole"]!.GetValue<string>());
            Assert.Equal("effects", payload["voiceTrack"]!["nextAudioTrackRole"]!.GetValue<string>());
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
