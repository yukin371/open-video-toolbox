using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task InspectPlan_ReturnsSummaryMaterialsAndValidation()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-inspect-plan-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var inputPath = Path.Combine(outputDirectory, "input.mp4");
        var audioPath = Path.Combine(outputDirectory, "dub.wav");
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var subtitlePath = Path.Combine(outputDirectory, "captions.srt");
        var planPath = Path.Combine(outputDirectory, "edit.json");

        await File.WriteAllTextAsync(inputPath, "fake-media");
        await File.WriteAllTextAsync(audioPath, "fake-audio");
        await File.WriteAllTextAsync(transcriptPath, "{}");
        await File.WriteAllTextAsync(subtitlePath, "1");
        await File.WriteAllTextAsync(
            planPath,
            """
            {
              "schemaVersion": 1,
              "source": {
                "inputPath": "input.mp4"
              },
              "template": {
                "id": "shorts-captioned"
              },
              "clips": [
                {
                  "id": "clip-001",
                  "in": "00:00:00",
                  "out": "00:00:01.500"
                }
              ],
              "audioTracks": [
                {
                  "id": "voice-main",
                  "role": "voice",
                  "path": "dub.wav",
                  "start": "00:00:00"
                }
              ],
              "transcript": {
                "path": "transcript.json",
                "language": "en",
                "segmentCount": 2
              },
              "subtitles": {
                "path": "captions.srt",
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
            var result = await RunCliAsync("inspect-plan", "--plan", planPath, "--check-files");

            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("inspect-plan", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(planPath), payload["planPath"]!.GetValue<string>());
            Assert.Equal(outputDirectory, payload["resolvedBaseDirectory"]!.GetValue<string>());
            Assert.True(payload["checkFiles"]!.GetValue<bool>());

            var summary = payload["summary"]!.AsObject();
            Assert.Equal(1, summary["clipCount"]!.GetValue<int>());
            Assert.Equal(1, summary["audioTrackCount"]!.GetValue<int>());
            Assert.Equal(0, summary["artifactCount"]!.GetValue<int>());
            Assert.True(summary["hasTranscript"]!.GetValue<bool>());
            Assert.False(summary["hasBeats"]!.GetValue<bool>());
            Assert.True(summary["hasSubtitles"]!.GetValue<bool>());

            var materials = payload["materials"]!.AsArray();
            Assert.Equal(4, materials.Count);
            Assert.Contains(materials, node => node!["targetKey"]!.GetValue<string>() == "source.inputPath");
            Assert.Contains(materials, node => node!["targetKey"]!.GetValue<string>() == "audioTrack:voice-main");
            Assert.Contains(materials, node => node!["targetKey"]!.GetValue<string>() == "transcript");
            Assert.Contains(materials, node => node!["targetKey"]!.GetValue<string>() == "subtitles");
            Assert.All(materials, node => Assert.True(node!["exists"]!.GetValue<bool>()));

            var replaceTargets = payload["replaceTargets"]!.AsArray();
            Assert.Equal(4, replaceTargets.Count);
            Assert.Contains(replaceTargets, node => node!["selector"]!["singleton"]?.GetValue<string>() == "source.inputPath");
            Assert.Contains(replaceTargets, node => node!["selector"]!["audioTrackId"]?.GetValue<string>() == "voice-main");
            Assert.Contains(replaceTargets, node => node!["selector"]!["singleton"]?.GetValue<string>() == "transcript");
            Assert.Contains(replaceTargets, node => node!["selector"]!["singleton"]?.GetValue<string>() == "subtitles");

            Assert.Empty(payload["missingBindings"]!.AsArray());

            var validation = payload["validation"]!.AsObject();
            Assert.True(validation["isValid"]!.GetValue<bool>());
            Assert.Empty(validation["issues"]!.AsArray());
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
