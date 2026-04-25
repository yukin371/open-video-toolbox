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
            NormalizeMaterialMutationPayload(payload);

            var expectedPayload = LoadSnapshot("replace-plan-material-valid.json")!["payload"]!.AsObject();
            NormalizeMaterialMutationPayload(expectedPayload);

            Assert.True(JsonNode.DeepEquals(expectedPayload, payload),
                $"Contract structure mismatch for 'replace-plan-material'.{Environment.NewLine}Expected:{Environment.NewLine}{expectedPayload}{Environment.NewLine}{Environment.NewLine}Actual:{Environment.NewLine}{payload}");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ReplacePlanMaterialBatch_ContractStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ovt-snapshot-replace-batch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(tempDir, "tasks", "job-a", "audio"));

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "tasks", "job-a", "input.mp4"), "video");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "tasks", "job-a", "audio", "updated.wav"), "audio");

            var planPath = Path.Combine(tempDir, "tasks", "job-a", "edit.json");
            await File.WriteAllTextAsync(
                planPath,
                """
                {
                  "schemaVersion": 1,
                  "source": { "inputPath": "input.mp4" },
                  "audioTracks": [
                    { "id": "voice-main", "role": "voice", "path": "dub.wav" }
                  ],
                  "output": { "path": "output.mp4", "container": "mp4" }
                }
                """);

            var manifestPath = Path.Combine(tempDir, "batch.json");
            await File.WriteAllTextAsync(manifestPath, """
                {
                  "schemaVersion": 1,
                  "items": [
                    {
                      "id": "job-a",
                      "plan": "tasks/job-a/edit.json",
                      "path": "tasks/job-a/audio/updated.wav",
                      "audioTrackId": "voice-main",
                      "checkFiles": true,
                      "pathStyle": "relative"
                    }
                  ]
                }
                """);

            var result = await CliTestProcessHelper.RunCliAsync("replace-plan-material-batch", "--manifest", manifestPath);
            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("replace-plan-material-batch", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            NormalizeReplacePlanMaterialBatchPayload(payload);

            var expectedPayload = LoadSnapshot("replace-plan-material-batch-valid.json")!["payload"]!.AsObject();
            NormalizeReplacePlanMaterialBatchPayload(expectedPayload);

            Assert.True(JsonNode.DeepEquals(expectedPayload, payload),
                $"Contract structure mismatch for 'replace-plan-material-batch'.{Environment.NewLine}Expected:{Environment.NewLine}{expectedPayload}{Environment.NewLine}{Environment.NewLine}Actual:{Environment.NewLine}{payload}");
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
            NormalizeMaterialMutationPayload(payload);

            var expectedPayload = LoadSnapshot("attach-plan-material-valid.json")!["payload"]!.AsObject();
            NormalizeMaterialMutationPayload(expectedPayload);

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
            NormalizeMaterialMutationPayload(payload);

            var expectedPayload = LoadSnapshot("bind-voice-track-valid.json")!["payload"]!.AsObject();
            NormalizeMaterialMutationPayload(expectedPayload);

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
                      "id": "job-a",
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
            payload.Remove("summaryPath");
            payload["results"]![0]!.AsObject().Remove("planPath");
            payload["results"]![0]!.AsObject().Remove("outputPlanPath");
            payload["results"]![0]!.AsObject().Remove("resultPath");
            payload["results"]![0]!["result"]!.AsObject().Remove("planPath");
            payload["results"]![0]!["result"]!.AsObject().Remove("outputPlanPath");
            NormalizeMaterialMutationPayload(payload["results"]![0]!["result"]!.AsObject());

            var expectedPayload = LoadSnapshot("bind-voice-track-batch-valid.json")!["payload"]!.AsObject();
            NormalizeMaterialMutationPayload(expectedPayload["results"]![0]!["result"]!.AsObject());

            Assert.True(JsonNode.DeepEquals(expectedPayload, payload),
                $"Contract structure mismatch for 'bind-voice-track-batch'.{Environment.NewLine}Expected:{Environment.NewLine}{expectedPayload}{Environment.NewLine}{Environment.NewLine}Actual:{Environment.NewLine}{payload}");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task AttachPlanMaterialBatch_ContractStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ovt-snapshot-attach-batch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(tempDir, "tasks", "job-a", "signals"));

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "tasks", "job-a", "input.mp4"), "video");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "tasks", "job-a", "signals", "transcript.json"), "{}");
            var planPath = Path.Combine(tempDir, "tasks", "job-a", "edit.json");
            await File.WriteAllTextAsync(
                planPath,
                """
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
                      "id": "job-a",
                      "plan": "tasks/job-a/edit.json",
                      "path": "tasks/job-a/signals/transcript.json",
                      "transcript": true,
                      "checkFiles": true,
                      "pathStyle": "relative"
                    }
                  ]
                }
                """);

            var result = await CliTestProcessHelper.RunCliAsync("attach-plan-material-batch", "--manifest", manifestPath);
            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("attach-plan-material-batch", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            NormalizeAttachPlanMaterialBatchPayload(payload);

            var expectedPayload = LoadSnapshot("attach-plan-material-batch-valid.json")!["payload"]!.AsObject();
            NormalizeAttachPlanMaterialBatchPayload(expectedPayload);

            Assert.True(JsonNode.DeepEquals(expectedPayload, payload),
                $"Contract structure mismatch for 'attach-plan-material-batch'.{Environment.NewLine}Expected:{Environment.NewLine}{expectedPayload}{Environment.NewLine}{Environment.NewLine}Actual:{Environment.NewLine}{payload}");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ScaffoldTemplateBatch_ContractStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ovt-snapshot-scaffold-batch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(tempDir, "inputs"));

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "inputs", "input.mp4"), "video");
            var manifestPath = Path.Combine(tempDir, "batch.json");
            await File.WriteAllTextAsync(manifestPath, """
                {
                  "schemaVersion": 1,
                  "items": [
                    {
                      "id": "job-a",
                      "input": "inputs/input.mp4",
                      "template": "shorts-captioned"
                    }
                  ]
                }
                """);

            var result = await CliTestProcessHelper.RunCliAsync("scaffold-template-batch", "--manifest", manifestPath);
            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("scaffold-template-batch", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            NormalizeScaffoldTemplateBatchPayload(payload);

            var expectedPayload = LoadSnapshot("scaffold-template-batch-valid.json")!["payload"]!.AsObject();

            Assert.True(JsonNode.DeepEquals(expectedPayload, payload),
                $"Contract structure mismatch for 'scaffold-template-batch'.{Environment.NewLine}Expected:{Environment.NewLine}{expectedPayload}{Environment.NewLine}{Environment.NewLine}Actual:{Environment.NewLine}{payload}");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task InitNarratedPlanBatch_ContractStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ovt-snapshot-init-narrated-batch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(tempDir, "episodes", "episode-01", "slides"));
        Directory.CreateDirectory(Path.Combine(tempDir, "episodes", "episode-01", "audio"));

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "episodes", "episode-01", "slides", "intro.png"), "image");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "episodes", "episode-01", "audio", "voice.wav"), "voice");

            var narratedManifestPath = Path.Combine(tempDir, "episodes", "episode-01", "narrated.json");
            await File.WriteAllTextAsync(narratedManifestPath, """
                {
                  "schemaVersion": 1,
                  "video": {
                    "id": "episode-01",
                    "output": "exports/final.mp4"
                  },
                  "sections": [
                    {
                      "id": "intro",
                      "title": "Intro",
                      "visual": {
                        "kind": "image",
                        "path": "slides/intro.png",
                        "durationMs": 3000
                      },
                      "voice": {
                        "path": "audio/voice.wav",
                        "durationMs": 3000
                      }
                    }
                  ]
                }
                """);

            var manifestPath = Path.Combine(tempDir, "batch.json");
            await File.WriteAllTextAsync(manifestPath, """
                {
                  "schemaVersion": 1,
                  "items": [
                    {
                      "id": "episode-01",
                      "manifest": "episodes/episode-01/narrated.json"
                    }
                  ]
                }
                """);

            var result = await CliTestProcessHelper.RunCliAsync("init-narrated-plan-batch", "--manifest", manifestPath);
            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("init-narrated-plan-batch", envelope["command"]!.GetValue<string>());
            Assert.False(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            NormalizeInitNarratedPlanBatchPayload(payload);

            var expectedPayload = LoadSnapshot("init-narrated-plan-batch-valid.json")!["payload"]!.AsObject();

            Assert.True(JsonNode.DeepEquals(expectedPayload, payload),
                $"Contract structure mismatch for 'init-narrated-plan-batch'.{Environment.NewLine}Expected:{Environment.NewLine}{expectedPayload}{Environment.NewLine}{Environment.NewLine}Actual:{Environment.NewLine}{payload}");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task RenderBatchPreview_ContractStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ovt-snapshot-render-batch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(tempDir, "tasks", "job-a"));

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "tasks", "job-a", "input.mp4"), "video");
            var planPath = Path.Combine(tempDir, "tasks", "job-a", "edit.json");
            await File.WriteAllTextAsync(
                planPath,
                """
                {
                  "schemaVersion": 1,
                  "source": { "inputPath": "input.mp4" },
                  "clips": [
                    { "id": "clip-001", "in": "00:00:00", "out": "00:00:02", "label": "intro" }
                  ],
                  "audioTracks": [],
                  "artifacts": [],
                  "output": { "path": "final.mp4", "container": "mp4" }
                }
                """);
            var manifestPath = Path.Combine(tempDir, "batch.json");
            await File.WriteAllTextAsync(manifestPath, """
                {
                  "schemaVersion": 1,
                  "items": [
                    {
                      "id": "job-a",
                      "plan": "tasks/job-a/edit.json"
                    }
                  ]
                }
                """);

            var result = await CliTestProcessHelper.RunCliAsync("render-batch", "--manifest", manifestPath, "--preview");
            Assert.Equal(0, result.ExitCode);

            var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("render-batch", envelope["command"]!.GetValue<string>());
            Assert.True(envelope["preview"]!.GetValue<bool>());

            var payload = envelope["payload"]!.AsObject();
            NormalizeRenderBatchPayload(payload);

            var expectedPayload = LoadSnapshot("render-batch-preview-valid.json")!["payload"]!.AsObject();

            Assert.True(JsonNode.DeepEquals(expectedPayload, payload),
                $"Contract structure mismatch for 'render-batch'.{Environment.NewLine}Expected:{Environment.NewLine}{expectedPayload}{Environment.NewLine}{Environment.NewLine}Actual:{Environment.NewLine}{payload}");
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

    private static void NormalizeScaffoldTemplateBatchPayload(JsonObject payload)
    {
        payload.Remove("manifestPath");
        payload.Remove("manifestBaseDirectory");
        payload.Remove("summaryPath");

        foreach (var resultNode in payload["results"]!.AsArray())
        {
            var result = resultNode!.AsObject();
            result.Remove("inputPath");
            result.Remove("workdir");
            result.Remove("resultPath");

            if (result["result"] is JsonObject scaffoldResult)
            {
                NormalizeScaffoldTemplatePayload(scaffoldResult);
            }
        }
    }

    private static void NormalizeInitNarratedPlanBatchPayload(JsonObject payload)
    {
        payload.Remove("manifestPath");
        payload.Remove("manifestBaseDirectory");
        payload.Remove("summaryPath");

        foreach (var resultNode in payload["results"]!.AsArray())
        {
            var result = resultNode!.AsObject();
            result.Remove("manifestPath");
            result.Remove("planPath");
            result.Remove("resultPath");

            if (result["result"] is JsonObject narratedResult)
            {
                NormalizeInitNarratedPlanPayload(narratedResult);
            }
        }
    }

    private static void NormalizeAttachPlanMaterialBatchPayload(JsonObject payload)
    {
        payload.Remove("manifestPath");
        payload.Remove("manifestBaseDirectory");
        payload.Remove("summaryPath");

        foreach (var resultNode in payload["results"]!.AsArray())
        {
            var result = resultNode!.AsObject();
            result.Remove("planPath");
            result.Remove("outputPlanPath");
            result.Remove("resultPath");

            if (result["result"] is JsonObject attachResult)
            {
                attachResult.Remove("planPath");
                attachResult.Remove("outputPlanPath");
                NormalizeMaterialMutationPayload(attachResult);
            }
        }
    }

    private static void NormalizeReplacePlanMaterialBatchPayload(JsonObject payload)
    {
        payload.Remove("manifestPath");
        payload.Remove("manifestBaseDirectory");
        payload.Remove("summaryPath");

        foreach (var resultNode in payload["results"]!.AsArray())
        {
            var result = resultNode!.AsObject();
            result.Remove("planPath");
            result.Remove("outputPlanPath");
            result.Remove("resultPath");

            if (result["result"] is JsonObject replaceResult)
            {
                replaceResult.Remove("planPath");
                replaceResult.Remove("outputPlanPath");
                NormalizeMaterialMutationPayload(replaceResult);
            }
        }
    }

    private static void NormalizeMaterialMutationPayload(JsonObject payload)
    {
        NormalizePathValue(payload, "previousPath");
        NormalizePathValue(payload, "nextPath");

        if (payload["target"] is JsonObject target)
        {
            NormalizePathValue(target, "previousPath");
            NormalizePathValue(target, "nextPath");
        }

        if (payload["voiceTrack"] is JsonObject voiceTrack)
        {
            NormalizePathValue(voiceTrack, "previousPath");
            NormalizePathValue(voiceTrack, "nextPath");
        }
    }

    private static void NormalizeScaffoldTemplatePayload(JsonObject payload)
    {
        var scaffold = payload["scaffold"]!.AsObject();
        scaffold.Remove("outputDirectory");
        scaffold.Remove("planPath");
        scaffold["writtenFiles"] = new JsonArray(
            scaffold["writtenFiles"]!
                .AsArray()
                .Select(node => JsonValue.Create(Path.GetFileName(node!.GetValue<string>())))
                .ToArray());

        var editPlan = payload["editPlan"]!.AsObject();
        editPlan["source"]!.AsObject().Remove("inputPath");
        editPlan["output"]!.AsObject().Remove("path");
    }

    private static void NormalizeRenderBatchPayload(JsonObject payload)
    {
        payload.Remove("manifestPath");
        payload.Remove("manifestBaseDirectory");
        payload.Remove("summaryPath");

        foreach (var resultNode in payload["results"]!.AsArray())
        {
            var result = resultNode!.AsObject();
            result.Remove("planPath");
            result.Remove("outputPath");
            result.Remove("resultPath");

            if (result["result"] is JsonObject renderResult)
            {
                NormalizeRenderPayload(renderResult);
            }
        }
    }

    private static void NormalizeInitNarratedPlanPayload(JsonObject payload)
    {
        payload.Remove("manifestPath");
        payload.Remove("planPath");
        payload.Remove("renderOutputPath");

        var editPlan = payload["editPlan"]!.AsObject();
        editPlan["source"]!.AsObject().Remove("inputPath");
        var template = editPlan["template"]!.AsObject();
        var templateSource = template["source"]!.AsObject();
        if (templateSource["pluginId"] is null)
        {
            templateSource.Remove("pluginId");
        }

        if (templateSource["pluginVersion"] is null)
        {
            templateSource.Remove("pluginVersion");
        }

        editPlan["output"]!.AsObject().Remove("path");

        var tracks = editPlan["timeline"]!["tracks"]!.AsArray();
        foreach (var trackNode in tracks)
        {
            foreach (var clipNode in trackNode!["clips"]!.AsArray())
            {
                clipNode!.AsObject().Remove("src");
            }
        }

        foreach (var audioTrackNode in editPlan["audioTracks"]!.AsArray())
        {
            audioTrackNode!.AsObject().Remove("path");
        }
    }

    private static void NormalizeRenderPayload(JsonObject payload)
    {
        var render = payload["render"]!.AsObject();
        render["source"]!.AsObject().Remove("inputPath");
        render["output"]!.AsObject().Remove("path");

        var executionPreview = payload["executionPreview"]!.AsObject();
        var commandPlan = executionPreview["commandPlan"]!.AsObject();
        commandPlan.Remove("workingDirectory");
        commandPlan.Remove("commandLine");
        commandPlan["arguments"] = new JsonArray(
            commandPlan["arguments"]!
                .AsArray()
                .Select(node =>
                {
                    var value = node!.GetValue<string>();
                    return JsonValue.Create(Path.IsPathRooted(value) ? Path.GetFileName(value) : value);
                })
                .ToArray());

        var producedPaths = executionPreview["producedPaths"]!.AsArray();
        executionPreview["producedPaths"] = new JsonArray(
            producedPaths.Select(node => JsonValue.Create(Path.GetFileName(node!.GetValue<string>()))).ToArray());
    }

    private static JsonNode LoadSnapshot(string fileName)
    {
        var path = Path.Combine(SnapshotsDir, fileName);
        Assert.True(File.Exists(path), $"Snapshot file not found: {path}");
        return JsonNode.Parse(File.ReadAllText(path))!;
    }

    private static void NormalizePathValue(JsonObject node, string propertyName)
    {
        if (node[propertyName] is not JsonValue value)
        {
            return;
        }

        var stringValue = value.TryGetValue<string>(out var pathValue) ? pathValue : null;
        if (string.IsNullOrEmpty(stringValue))
        {
            return;
        }

        node[propertyName] = stringValue.Replace('\\', '/');
    }
}
