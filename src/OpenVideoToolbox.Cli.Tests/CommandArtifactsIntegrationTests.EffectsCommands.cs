using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task Effects_List_ReturnsBuiltInCatalog()
    {
        var result = await RunCliAsync("effects");

        Assert.Equal(0, result.ExitCode);

        var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
        var payload = envelope["payload"]!.AsObject();
        var effects = payload["effects"]!.AsArray();

        Assert.Equal("effects", envelope["command"]!.GetValue<string>());
        Assert.Equal("list", payload["mode"]!.GetValue<string>());
        Assert.True(payload["count"]!.GetValue<int>() >= 10);
        Assert.Contains(effects, effect => effect!["type"]!.GetValue<string>() == "fade");
        Assert.Contains(effects, effect => effect!["type"]!.GetValue<string>() == "auto_ducking");
    }

    [Fact]
    public async Task Effects_List_CanFilterByCategory()
    {
        var result = await RunCliAsync("effects", "list", "--category", "audio");

        Assert.Equal(0, result.ExitCode);

        var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
        var payload = envelope["payload"]!.AsObject();
        var effects = payload["effects"]!.AsArray();

        Assert.Equal("audio", payload["filters"]!["category"]!.GetValue<string>());
        Assert.NotEmpty(effects);
        Assert.All(effects, effect => Assert.Equal("audio", effect!["category"]!.GetValue<string>()));
        Assert.Contains(effects, effect => effect!["type"]!.GetValue<string>() == "auto_ducking");
    }

    [Fact]
    public async Task Effects_Describe_ReturnsFullDefinition()
    {
        var result = await RunCliAsync("effects", "describe", "fade");

        Assert.Equal(0, result.ExitCode);

        var envelope = JsonNode.Parse(result.StdOut)!.AsObject();
        var payload = envelope["payload"]!.AsObject();
        var effect = payload["effect"]!.AsObject();

        Assert.Equal("describe", payload["mode"]!.GetValue<string>());
        Assert.Equal("fade", effect["type"]!.GetValue<string>());
        Assert.Equal("transitionTemplate", effect["templateMode"]!.GetValue<string>());
        Assert.Equal("transition", effect["category"]!.GetValue<string>());
        Assert.Equal("built-in", effect["source"]!.GetValue<string>());
        Assert.Equal("float", effect["parameters"]!["duration"]!["type"]!.GetValue<string>());
        Assert.Equal("fade=t=in:d={duration}:alpha=1", effect["ffmpegTemplates"]!["transitions"]!["in"]!.GetValue<string>());
    }
}
