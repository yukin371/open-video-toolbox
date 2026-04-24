using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed class ContractSnapshotTests
{
    private static readonly string SnapshotsDir = Path.GetFullPath(Path.Combine(
        Path.GetDirectoryName(typeof(ContractSnapshotTests).Assembly.Location)!,
        "..", "..", "..", "..", "..",
        "src", "OpenVideoToolbox.Cli.Tests", "snapshots"));

    [Fact]
    public async Task Presets_ContractSnapshot()
    {
        var result = await CliTestProcessHelper.RunCliAsync("presets");
        Assert.Equal(0, result.ExitCode);

        var actual = JsonNode.Parse(result.StdOut)!;
        var expected = LoadSnapshot("presets.json");

        Assert.True(JsonNode.DeepEquals(expected, actual),
            $"Contract snapshot mismatch for 'presets'.\nExpected:\n{expected}\n\nActual:\n{actual}");
    }

    [Fact]
    public async Task TemplatesCatalog_ContractSnapshot()
    {
        var result = await CliTestProcessHelper.RunCliAsync("templates");
        Assert.Equal(0, result.ExitCode);

        var actual = JsonNode.Parse(result.StdOut)!;
        var expected = LoadSnapshot("templates-catalog.json");

        Assert.True(JsonNode.DeepEquals(expected, actual),
            $"Contract snapshot mismatch for 'templates catalog'.");
    }

    [Fact]
    public async Task TemplatesShortsCaptioned_ContractSnapshot()
    {
        var result = await CliTestProcessHelper.RunCliAsync("templates", "shorts-captioned");
        Assert.Equal(0, result.ExitCode);

        var actual = JsonNode.Parse(result.StdOut)!;
        var expected = LoadSnapshot("templates-shorts-captioned.json");

        Assert.True(JsonNode.DeepEquals(expected, actual),
            $"Contract snapshot mismatch for 'templates shorts-captioned'.");
    }

    [Fact]
    public async Task TemplatesBeatMontage_ContractSnapshot()
    {
        var result = await CliTestProcessHelper.RunCliAsync("templates", "beat-montage");
        Assert.Equal(0, result.ExitCode);

        var actual = JsonNode.Parse(result.StdOut)!;
        var expected = LoadSnapshot("templates-beat-montage.json");

        Assert.True(JsonNode.DeepEquals(expected, actual),
            $"Contract snapshot mismatch for 'templates beat-montage'.");
    }

    [Fact]
    public async Task ValidatePlan_ContractStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ovt-snapshot-validate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var planPath = Path.Combine(tempDir, "edit.json");
            await File.WriteAllTextAsync(planPath, """
                {
                  "schemaVersion": 1,
                  "source": { "inputPath": "input.mp4" },
                  "clips": [{ "id": "c1", "in": "00:00:00", "out": "00:00:01" }],
                  "output": { "path": "output.mp4", "container": "mp4" }
                }
                """);

            var result = await CliTestProcessHelper.RunCliAsync("validate-plan", "--plan", planPath);
            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();

            // Envelope structure
            Assert.Equal("validate-plan", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            // Payload structure (machine-independent)
            var payload = envelope["payload"]!.AsObject();
            Assert.True(payload["isValid"]!.GetValue<bool>());
            Assert.NotNull(payload["issues"]);
            Assert.IsType<JsonArray>(payload["issues"]);

            // Normalize paths for structural comparison
            payload.Remove("planPath");
            payload.Remove("resolvedBaseDirectory");

            var expectedPayload = LoadSnapshot("validate-plan-valid.json")!["payload"]!.AsObject();
            expectedPayload.Remove("planPath");
            expectedPayload.Remove("resolvedBaseDirectory");

            Assert.True(JsonNode.DeepEquals(expectedPayload, payload),
                $"Contract structure mismatch for 'validate-plan'.\nExpected:\n{expectedPayload}\n\nActual:\n{payload}");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task InspectPlan_ContractStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ovt-snapshot-inspect-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "input.mp4"), "video");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "dub.wav"), "audio");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "transcript.json"), "{}");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "subs.srt"), "1");

            var planPath = Path.Combine(tempDir, "edit.json");
            await File.WriteAllTextAsync(planPath, """
                {
                  "schemaVersion": 1,
                  "source": { "inputPath": "input.mp4" },
                  "template": { "id": "shorts-captioned" },
                  "clips": [{ "id": "c1", "in": "00:00:00", "out": "00:00:01" }],
                  "audioTracks": [{ "id": "voice-main", "role": "voice", "path": "dub.wav" }],
                  "transcript": { "path": "transcript.json", "segmentCount": 2 },
                  "subtitles": { "path": "subs.srt", "mode": "sidecar" },
                  "output": { "path": "output.mp4", "container": "mp4" }
                }
                """);

            var result = await CliTestProcessHelper.RunCliAsync("inspect-plan", "--plan", planPath, "--check-files");
            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("inspect-plan", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            NormalizeInspectPayload(payload);

            var expectedPayload = LoadSnapshot("inspect-plan-valid.json")!["payload"]!.AsObject();
            NormalizeInspectPayload(expectedPayload);

            Assert.True(JsonNode.DeepEquals(expectedPayload, payload),
                $"Contract structure mismatch for 'inspect-plan'.{Environment.NewLine}Expected:{Environment.NewLine}{expectedPayload}{Environment.NewLine}{Environment.NewLine}Actual:{Environment.NewLine}{payload}");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ReplacePlanMaterial_ContractStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ovt-snapshot-replace-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "input.mp4"), "video");
            Directory.CreateDirectory(Path.Combine(tempDir, "assets"));
            await File.WriteAllTextAsync(Path.Combine(tempDir, "assets", "updated.wav"), "audio");

            var planPath = Path.Combine(tempDir, "edit.json");
            await File.WriteAllTextAsync(planPath, """
                {
                  "schemaVersion": 1,
                  "source": { "inputPath": "input.mp4" },
                  "audioTracks": [{ "id": "voice-main", "role": "voice", "path": "dub.wav" }],
                  "output": { "path": "output.mp4", "container": "mp4" }
                }
                """);

            var result = await CliTestProcessHelper.RunCliAsync(
                "replace-plan-material",
                "--plan", planPath,
                "--audio-track-id", "voice-main",
                "--path", Path.Combine(tempDir, "assets", "updated.wav"),
                "--path-style", "relative",
                "--check-files");
            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("replace-plan-material", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            payload.Remove("planPath");
            payload.Remove("outputPlanPath");

            var expectedPayload = LoadSnapshot("replace-plan-material-valid.json")!["payload"]!.AsObject();

            Assert.True(JsonNode.DeepEquals(expectedPayload, payload),
                $"Contract structure mismatch for 'replace-plan-material'.{Environment.NewLine}Expected:{Environment.NewLine}{expectedPayload}{Environment.NewLine}{Environment.NewLine}Actual:{Environment.NewLine}{payload}");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AttachPlanMaterial_ContractStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ovt-snapshot-attach-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "input.mp4"), "video");
            Directory.CreateDirectory(Path.Combine(tempDir, "signals"));
            await File.WriteAllTextAsync(Path.Combine(tempDir, "signals", "transcript.json"), "{}");

            var planPath = Path.Combine(tempDir, "edit.json");
            await File.WriteAllTextAsync(planPath, """
                {
                  "schemaVersion": 1,
                  "source": { "inputPath": "input.mp4" },
                  "output": { "path": "output.mp4", "container": "mp4" }
                }
                """);

            var result = await CliTestProcessHelper.RunCliAsync(
                "attach-plan-material",
                "--plan", planPath,
                "--transcript",
                "--path", Path.Combine(tempDir, "signals", "transcript.json"),
                "--check-files");
            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("attach-plan-material", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            payload.Remove("planPath");
            payload.Remove("outputPlanPath");

            var expectedPayload = LoadSnapshot("attach-plan-material-valid.json")!["payload"]!.AsObject();

            Assert.True(JsonNode.DeepEquals(expectedPayload, payload),
                $"Contract structure mismatch for 'attach-plan-material'.{Environment.NewLine}Expected:{Environment.NewLine}{expectedPayload}{Environment.NewLine}{Environment.NewLine}Actual:{Environment.NewLine}{payload}");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task BindVoiceTrack_ContractStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ovt-snapshot-bind-voice-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "input.mp4"), "video");
            Directory.CreateDirectory(Path.Combine(tempDir, "audio"));
            await File.WriteAllTextAsync(Path.Combine(tempDir, "audio", "dub.wav"), "audio");

            var planPath = Path.Combine(tempDir, "edit.json");
            await File.WriteAllTextAsync(planPath, """
                {
                  "schemaVersion": 1,
                  "source": { "inputPath": "input.mp4" },
                  "output": { "path": "output.mp4", "container": "mp4" }
                }
                """);

            var result = await CliTestProcessHelper.RunCliAsync(
                "bind-voice-track",
                "--plan", planPath,
                "--path", Path.Combine(tempDir, "audio", "dub.wav"),
                "--path-style", "relative",
                "--check-files");
            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("bind-voice-track", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            payload.Remove("planPath");
            payload.Remove("outputPlanPath");

            var expectedPayload = LoadSnapshot("bind-voice-track-valid.json")!["payload"]!.AsObject();

            Assert.True(JsonNode.DeepEquals(expectedPayload, payload),
                $"Contract structure mismatch for 'bind-voice-track'.{Environment.NewLine}Expected:{Environment.NewLine}{expectedPayload}{Environment.NewLine}{Environment.NewLine}Actual:{Environment.NewLine}{payload}");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task BindVoiceTrackBatch_ContractStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ovt-snapshot-bind-voice-batch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var planPath = Path.Combine(tempDir, "edit.json");
            Directory.CreateDirectory(Path.Combine(tempDir, "audio"));
            await File.WriteAllTextAsync(Path.Combine(tempDir, "input.mp4"), "video");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "audio", "dub.wav"), "audio");
            await File.WriteAllTextAsync(planPath, """
                {
                  "schemaVersion": 1,
                  "source": { "inputPath": "input.mp4" },
                  "output": { "path": "output.mp4", "container": "mp4" }
                }
                """);
            var manifestPath = Path.Combine(tempDir, "batch.json");
            await File.WriteAllTextAsync(manifestPath, """
                {
                  "schemaVersion": 1,
                  "items": [
                    {
                      "plan": "edit.json",
                      "path": "audio/dub.wav",
                      "checkFiles": true,
                      "pathStyle": "relative"
                    }
                  ]
                }
                """);

            var result = await CliTestProcessHelper.RunCliAsync("bind-voice-track-batch", "--manifest", manifestPath);
            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("bind-voice-track-batch", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            payload.Remove("manifestPath");
            payload.Remove("manifestBaseDirectory");
            payload["results"]![0]!["result"]!.AsObject().Remove("planPath");
            payload["results"]![0]!["result"]!.AsObject().Remove("outputPlanPath");

            var expectedPayload = LoadSnapshot("bind-voice-track-batch-valid.json")!["payload"]!.AsObject();

            Assert.True(JsonNode.DeepEquals(expectedPayload, payload),
                $"Contract structure mismatch for 'bind-voice-track-batch'.{Environment.NewLine}Expected:{Environment.NewLine}{expectedPayload}{Environment.NewLine}{Environment.NewLine}Actual:{Environment.NewLine}{payload}");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void NormalizeInspectPayload(JsonObject payload)
    {
        payload.Remove("planPath");
        payload.Remove("resolvedBaseDirectory");

        foreach (var materialNode in payload["materials"]!.AsArray())
        {
            materialNode!.AsObject().Remove("resolvedPath");
        }

        foreach (var bindingNode in payload["missingBindings"]!.AsArray())
        {
            bindingNode!.AsObject().Remove("resolvedPath");
        }

        foreach (var signalNode in payload["signals"]!.AsArray())
        {
            signalNode!.AsObject().Remove("resolvedPath");
        }
    }

    private static JsonNode LoadSnapshot(string fileName)
    {
        var path = Path.Combine(SnapshotsDir, fileName);
        Assert.True(File.Exists(path), $"Snapshot file not found: {path}");
        return JsonNode.Parse(File.ReadAllText(path))!;
    }
}
