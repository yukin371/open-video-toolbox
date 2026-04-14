using OpenVideoToolbox.Core.Presets;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class BuiltInPresetCatalogTests
{
    [Fact]
    public void GetRequired_ReturnsKnownPreset()
    {
        var preset = BuiltInPresetCatalog.GetRequired("h264-aac-mp4");

        Assert.Equal("H.264 + AAC / MP4", preset.DisplayName);
        Assert.Equal(".mp4", preset.Output.ContainerExtension);
    }

    [Fact]
    public void GetAll_ReturnsMultipleBuiltInPresets()
    {
        var presets = BuiltInPresetCatalog.GetAll();

        Assert.True(presets.Count >= 3);
        Assert.Contains(presets, preset => preset.Id == "copy-aac-mkv");
    }
}

