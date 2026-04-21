using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task Presets_JsonOut_MatchesStdout()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ovt-presets-jsonout-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var jsonOutPath = Path.Combine(tempDir, "presets.json");

            var result = await RunCliAsync("presets", "--json-out", jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();

            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("presets", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            Assert.NotNull(stdout["payload"]);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Plan_JsonOut_MatchesStdout()
    {
        if (!CliTestProcessHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var tempDir = Path.Combine(Path.GetTempPath(), $"ovt-plan-jsonout-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var inputPath = Path.Combine(tempDir, "input.mp4");
            var jsonOutPath = Path.Combine(tempDir, "plan.json");

            await CliTestProcessHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(2));

            var result = await RunCliAsync(
                "plan", inputPath,
                "--preset", "h264-aac-mp4",
                "--json-out", jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();

            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("plan", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            Assert.NotNull(stdout["payload"]);
            Assert.NotNull(stdout["payload"]!["job"]);
            Assert.NotNull(stdout["payload"]!["commandPlan"]);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
