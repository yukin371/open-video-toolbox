using OpenVideoToolbox.Core.Editing;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class BuiltInEffectCatalogTests
{
    [Fact]
    public void GetAll_ReturnsStableBuiltInCatalog()
    {
        var effects = BuiltInEffectCatalog.GetAll();

        Assert.True(effects.Count >= 10);
        Assert.Equal(effects.OrderBy(effect => effect.Type, StringComparer.OrdinalIgnoreCase).Select(effect => effect.Type), effects.Select(effect => effect.Type));
        Assert.Contains(effects, effect => effect.Type == "fade" && effect.Category == EffectCategory.Transition);
        Assert.Contains(effects, effect => effect.Type == "auto_ducking" && effect.Category == EffectCategory.Audio);
    }

    [Fact]
    public void GetAll_CanFilterByCategory()
    {
        var effects = BuiltInEffectCatalog.GetAll(EffectCategory.Audio);

        Assert.NotEmpty(effects);
        Assert.All(effects, effect => Assert.Equal(EffectCategory.Audio, effect.Category));
        Assert.Contains(effects, effect => effect.Type == "volume");
        Assert.Contains(effects, effect => effect.Type == "auto_ducking");
    }

    [Fact]
    public void CreateRegistry_ResolvesKnownDefinitions()
    {
        var registry = BuiltInEffectCatalog.CreateRegistry();

        var fade = registry.Get("fade");
        var autoDucking = registry.Get("auto_ducking");

        Assert.NotNull(fade);
        Assert.NotNull(autoDucking);
        Assert.NotNull(fade!.FfmpegTemplates);
        Assert.NotNull(fade.FfmpegTemplates!.Transitions);
        Assert.Null(autoDucking!.FfmpegTemplates);
        Assert.True(autoDucking.Parameters.Items.ContainsKey("reference"));
    }
}
