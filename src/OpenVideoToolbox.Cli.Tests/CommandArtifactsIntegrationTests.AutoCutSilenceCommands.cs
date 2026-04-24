using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task AutoCutSilence_ClipsOnlyMode_UsesExplicitSourceDuration()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-auto-cut-clips-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var silencePath = Path.Combine(outputDirectory, "silence.json");
        var outputPath = Path.Combine(outputDirectory, "clips.json");

        await File.WriteAllTextAsync(
            silencePath,
            """
            {
              "schemaVersion": 1,
              "inputPath": "input.mp4",
              "segments": [
                { "start": "00:00:02", "end": "00:00:03", "duration": "00:00:01" }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync(
                "auto-cut-silence",
                "--silence", silencePath,
                "--clips-only",
                "--source-duration-ms", "5000",
                "--padding-ms", "0",
                "--merge-gap-ms", "0",
                "--min-clip-duration-ms", "0",
                "--output", outputPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("auto-cut-silence", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            Assert.Equal("clipsOnly", payload["autoCutSilence"]!["mode"]!.GetValue<string>());
            Assert.True(payload["autoCutSilence"]!["usedExplicitSourceDuration"]!.GetValue<bool>());
            Assert.Equal(2, payload["result"]!["stats"]!["generatedClipCount"]!.GetValue<int>());

            var clips = payload["result"]!["clips"]!.AsArray();
            Assert.Equal(2, clips.Count);
            Assert.Equal("clip-001", clips[0]!["id"]!.GetValue<string>());
            Assert.Equal("00:00:00", clips[0]!["in"]!.GetValue<string>());
            Assert.Equal("00:00:02", clips[0]!["out"]!.GetValue<string>());
            Assert.Equal("00:00:03", clips[1]!["in"]!.GetValue<string>());
            Assert.Equal("00:00:05", clips[1]!["out"]!.GetValue<string>());

            var writtenClips = JsonNode.Parse(await File.ReadAllTextAsync(outputPath))!.AsArray();
            Assert.True(JsonNode.DeepEquals(clips, writtenClips));
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
    public async Task AutoCutSilence_PlanMode_ProbesDurationAndWritesPlan()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-auto-cut-plan-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(Path.Combine(outputDirectory, "media"));
        var silencePath = Path.Combine(outputDirectory, "silence.json");
        var planPath = Path.Combine(outputDirectory, "edit.json");
        var ffprobePath = WriteExecutableScript(
            outputDirectory,
            "fake-ffprobe",
            """
            @echo off
            echo {"format":{"duration":"12.0"},"streams":[]}
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf '{"format":{"duration":"12.0"},"streams":[]}\n'
            """);

        await File.WriteAllTextAsync(
            silencePath,
            """
            {
              "schemaVersion": 1,
              "inputPath": "media/input.mp4",
              "segments": [
                { "start": "00:00:04", "end": "00:00:05", "duration": "00:00:01" }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync(
                "auto-cut-silence",
                "--silence", silencePath,
                "--ffprobe", ffprobePath,
                "--template", "shorts-basic",
                "--render-output", "final.mp4",
                "--output", planPath,
                "--padding-ms", "0",
                "--merge-gap-ms", "0",
                "--min-clip-duration-ms", "0");

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(planPath));

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            var payload = envelope["payload"]!.AsObject();
            Assert.Equal("plan", payload["autoCutSilence"]!["mode"]!.GetValue<string>());
            Assert.False(payload["autoCutSilence"]!["usedExplicitSourceDuration"]!.GetValue<bool>());
            Assert.Equal(Path.GetFullPath(Path.Combine(outputDirectory, "media", "input.mp4")), payload["autoCutSilence"]!["sourcePath"]!.GetValue<string>());

            var plan = JsonNode.Parse(await File.ReadAllTextAsync(planPath))!.AsObject();
            Assert.Equal(1, plan["schemaVersion"]!.GetValue<int>());
            Assert.Equal(Path.GetFullPath(Path.Combine(outputDirectory, "media", "input.mp4")), plan["source"]!["inputPath"]!.GetValue<string>());
            Assert.Equal("shorts-basic", plan["template"]!["id"]!.GetValue<string>());
            Assert.Equal("final.mp4", plan["output"]!["path"]!.GetValue<string>());
            Assert.Equal("mp4", plan["output"]!["container"]!.GetValue<string>());
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
    public async Task AutoCutSilence_PlanMode_UsesV2TemplateAndRenderPreviewCanConsumePlan()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-auto-cut-plan-v2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var silencePath = Path.Combine(outputDirectory, "silence.json");
        var planPath = Path.Combine(outputDirectory, "edit.v2.json");

        await File.WriteAllTextAsync(
            silencePath,
            """
            {
              "schemaVersion": 1,
              "inputPath": "input.mp4",
              "segments": [
                { "start": "00:00:02", "end": "00:00:03", "duration": "00:00:01" }
              ]
            }
            """);

        try
        {
            var result = await RunCliAsync(
                "auto-cut-silence",
                "--silence", silencePath,
                "--source-duration-ms", "5000",
                "--template", "timeline-effects-starter",
                "--render-output", "final.mp4",
                "--output", planPath,
                "--padding-ms", "0",
                "--merge-gap-ms", "0",
                "--min-clip-duration-ms", "0");

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(planPath));

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            var payload = envelope["payload"]!.AsObject();
            Assert.Equal("plan", payload["autoCutSilence"]!["mode"]!.GetValue<string>());
            Assert.True(payload["autoCutSilence"]!["usedExplicitSourceDuration"]!.GetValue<bool>());

            var plan = JsonNode.Parse(await File.ReadAllTextAsync(planPath))!.AsObject();
            Assert.Equal(2, plan["schemaVersion"]!.GetValue<int>());
            Assert.Equal("timeline-effects-starter", plan["template"]!["id"]!.GetValue<string>());
            Assert.True(plan["clips"]!.AsArray().Count == 0);
            Assert.NotNull(plan["timeline"]);
            var tracks = plan["timeline"]!["tracks"]!.AsArray();
            var track = Assert.Single(tracks)!.AsObject();
            Assert.Equal("scale", track["effects"]![0]!["type"]!.GetValue<string>());
            Assert.Equal(2, track["clips"]!.AsArray().Count);
            Assert.Equal("brightness_contrast", track["clips"]![0]!["effects"]![0]!["type"]!.GetValue<string>());
            Assert.Equal("00:00:02", track["clips"]![1]!["start"]!.GetValue<string>());

            var renderResult = await RunCliAsync("render", "--plan", planPath, "--preview");

            Assert.Equal(0, renderResult.ExitCode);

            var renderPayload = JsonNode.Parse(renderResult.StdOut)!.AsObject();
            Assert.Equal(2, renderPayload["payload"]!["executionPreview"]!["commandPlan"]!["schemaVersion"]!.GetValue<int>());
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
    public async Task AutoCutSilence_PlanMode_RequiresRenderOutput()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-auto-cut-missing-render-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var silencePath = Path.Combine(outputDirectory, "silence.json");

        await File.WriteAllTextAsync(
            silencePath,
            """
            {
              "schemaVersion": 1,
              "inputPath": "input.mp4",
              "segments": []
            }
            """);

        try
        {
            var result = await RunCliAsync(
                "auto-cut-silence",
                "--silence", silencePath,
                "--source-duration-ms", "5000");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--render-output' is required", result.StdErr, StringComparison.Ordinal);
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
