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

    private static JsonNode LoadSnapshot(string fileName)
    {
        var path = Path.Combine(SnapshotsDir, fileName);
        Assert.True(File.Exists(path), $"Snapshot file not found: {path}");
        return JsonNode.Parse(File.ReadAllText(path))!;
    }
}
