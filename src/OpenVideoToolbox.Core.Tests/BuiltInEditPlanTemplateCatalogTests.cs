using OpenVideoToolbox.Core.Editing;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class BuiltInEditPlanTemplateCatalogTests
{
    [Fact]
    public void GetAll_ReturnsStableBuiltInTemplates()
    {
        var templates = BuiltInEditPlanTemplateCatalog.GetAll();

        Assert.Contains(templates, template => template.Id == "shorts-basic");
        Assert.Contains(templates, template => template.Id == "shorts-captioned");
        Assert.Contains(templates, template => template.Id == "commentary-bgm");
        Assert.Contains(templates, template => template.Id == "explainer-captioned");
        Assert.Contains(templates, template => template.Id == "beat-montage");
        Assert.Contains(
            templates.Single(template => template.Id == "shorts-basic").RecommendedSeedModes,
            mode => mode == EditPlanSeedMode.Transcript);
        Assert.DoesNotContain(
            templates.Single(template => template.Id == "commentary-bgm").RecommendedSeedModes,
            mode => mode == EditPlanSeedMode.Beats);
        Assert.DoesNotContain(
            templates.Single(template => template.Id == "beat-montage").RecommendedSeedModes,
            mode => mode == EditPlanSeedMode.Transcript);
    }

    [Fact]
    public void GetRequired_ThrowsForUnknownTemplate()
    {
        Assert.Throws<InvalidOperationException>(() => BuiltInEditPlanTemplateCatalog.GetRequired("missing-template"));
    }

    [Fact]
    public void GetAll_CanFilterByCategory()
    {
        var templates = BuiltInEditPlanTemplateCatalog.GetAll(category: "commentary", seedMode: null);

        var template = Assert.Single(templates);
        Assert.Equal("commentary-bgm", template.Id);
    }

    [Fact]
    public void GetAll_CanFilterBySeedMode()
    {
        var templates = BuiltInEditPlanTemplateCatalog.GetAll(category: null, seedMode: EditPlanSeedMode.Beats);

        Assert.Equal(3, templates.Count);
        Assert.DoesNotContain(templates, template => template.Id == "commentary-bgm");
    }

    [Fact]
    public void GetAll_CanCombineCategoryAndSeedMode()
    {
        var templates = BuiltInEditPlanTemplateCatalog.GetAll(category: "short-form", seedMode: EditPlanSeedMode.Transcript);

        Assert.Equal(2, templates.Count);
        Assert.All(templates, template => Assert.Equal("short-form", template.Category));
    }

    [Fact]
    public void GetAll_CanFilterByArtifactKind()
    {
        var templates = BuiltInEditPlanTemplateCatalog.GetAll(new EditPlanTemplateCatalogQuery
        {
            ArtifactKind = "subtitle"
        });

        Assert.Equal(2, templates.Count);
        Assert.Contains(templates, template => template.Id == "shorts-captioned");
        Assert.Contains(templates, template => template.Id == "explainer-captioned");
    }

    [Fact]
    public void GetAll_CanFilterByHasArtifacts()
    {
        var templates = BuiltInEditPlanTemplateCatalog.GetAll(new EditPlanTemplateCatalogQuery
        {
            HasArtifacts = false
        });

        var template = Assert.Single(templates);
        Assert.Equal("shorts-basic", template.Id);
    }

    [Fact]
    public void GetAll_CanFilterByHasSubtitles()
    {
        var templates = BuiltInEditPlanTemplateCatalog.GetAll(new EditPlanTemplateCatalogQuery
        {
            HasSubtitles = true
        });

        Assert.Equal(2, templates.Count);
        Assert.Contains(templates, template => template.Id == "shorts-captioned");
        Assert.Contains(templates, template => template.Id == "explainer-captioned");
    }

    [Fact]
    public void GetSummaries_ReturnStableMachineFriendlyView()
    {
        var summary = Assert.Single(BuiltInEditPlanTemplateCatalog.GetSummaries(new EditPlanTemplateCatalogQuery
        {
            Category = "commentary",
            HasArtifacts = true
        }));

        Assert.Equal("commentary-bgm", summary.Id);
        Assert.Equal("mp4", summary.OutputContainer);
        Assert.True(summary.HasArtifacts);
        Assert.False(summary.HasSubtitles);
        Assert.Contains("audio", summary.ArtifactKinds);
        Assert.Contains(EditPlanSeedMode.Transcript, summary.RecommendedSeedModes);
    }
}
