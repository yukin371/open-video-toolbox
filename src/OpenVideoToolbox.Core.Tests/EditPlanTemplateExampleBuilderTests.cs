using OpenVideoToolbox.Core.Editing;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class EditPlanTemplateExampleBuilderTests
{
    [Fact]
    public void BuildArtifactBindingsExample_ReturnsExpectedSlotPlaceholders()
    {
        var template = BuiltInEditPlanTemplateCatalog.GetRequired("shorts-captioned");

        var example = EditPlanTemplateExampleBuilder.BuildArtifactBindingsExample(template);

        Assert.Equal("subtitles.srt", example["subtitles"]);
    }

    [Fact]
    public void BuildTemplateParamsExample_ReturnsParameterDefaults()
    {
        var template = BuiltInEditPlanTemplateCatalog.GetRequired("commentary-bgm");

        var example = EditPlanTemplateExampleBuilder.BuildTemplateParamsExample(template);

        Assert.Equal("high", example["narrationPriority"]);
        Assert.Equal("-12", example["bgmTargetGainDb"]);
    }

    [Fact]
    public void BuildArtifactBindingsExample_ReturnsEmptyMapForTemplateWithoutSlots()
    {
        var template = BuiltInEditPlanTemplateCatalog.GetRequired("shorts-basic");

        var example = EditPlanTemplateExampleBuilder.BuildArtifactBindingsExample(template);

        Assert.Empty(example);
    }

    [Fact]
    public void BuildTemplateParamsExample_PreservesSeedMetadataOnTemplate()
    {
        var template = BuiltInEditPlanTemplateCatalog.GetRequired("shorts-captioned");

        var example = EditPlanTemplateExampleBuilder.BuildTemplateParamsExample(template);

        Assert.Equal("hard-cut", example["hookStyle"]);
        Assert.Contains(EditPlanSeedMode.Beats, template.RecommendedSeedModes);
        Assert.Contains(EditPlanSeedMode.Transcript, template.RecommendedSeedModes);
    }

    [Fact]
    public void BuildArtifactBindingsExample_ReturnsAudioPlaceholderForBeatMontage()
    {
        var template = BuiltInEditPlanTemplateCatalog.GetRequired("beat-montage");

        var example = EditPlanTemplateExampleBuilder.BuildArtifactBindingsExample(template);

        Assert.Equal("audio/input.wav", example["bgm"]);
    }

    [Fact]
    public void BuildPreviewPlans_ReturnsSeedSpecificEditPlanShapes()
    {
        var template = BuiltInEditPlanTemplateCatalog.GetRequired("shorts-captioned");

        var previews = EditPlanTemplateExampleBuilder.BuildPreviewPlans(template);

        Assert.Equal(3, previews.Count);

        var manual = Assert.Single(previews.Where(preview => preview.Mode == EditPlanSeedMode.Manual));
        Assert.Empty(manual.EditPlan.Clips);
        Assert.Single(manual.EditPlan.Artifacts);
        Assert.Null(manual.EditPlan.Transcript);
        Assert.Null(manual.EditPlan.Beats);

        var transcript = Assert.Single(previews.Where(preview => preview.Mode == EditPlanSeedMode.Transcript));
        Assert.Equal(2, transcript.EditPlan.Clips.Count);
        Assert.NotNull(transcript.EditPlan.Transcript);
        Assert.Null(transcript.EditPlan.Beats);

        var beats = Assert.Single(previews.Where(preview => preview.Mode == EditPlanSeedMode.Beats));
        Assert.Single(beats.EditPlan.Clips);
        Assert.NotNull(beats.EditPlan.Beats);
        Assert.Null(beats.EditPlan.Transcript);
    }

    [Fact]
    public void BuildPreviewPlans_RespectsTemplateSeedModes()
    {
        var template = BuiltInEditPlanTemplateCatalog.GetRequired("commentary-bgm");

        var previews = EditPlanTemplateExampleBuilder.BuildPreviewPlans(template);

        Assert.Equal(2, previews.Count);
        Assert.DoesNotContain(previews, preview => preview.Mode == EditPlanSeedMode.Beats);
        Assert.All(previews, preview => Assert.Single(preview.EditPlan.AudioTracks));
    }

    [Fact]
    public void BuildPreviewPlans_ExposesTranscriptFocusedExplainerShapes()
    {
        var template = BuiltInEditPlanTemplateCatalog.GetRequired("explainer-captioned");

        var previews = EditPlanTemplateExampleBuilder.BuildPreviewPlans(template);

        Assert.Equal(2, previews.Count);
        Assert.DoesNotContain(previews, preview => preview.Mode == EditPlanSeedMode.Beats);
        Assert.All(previews, preview => Assert.Single(preview.EditPlan.Artifacts));

        var transcript = Assert.Single(previews.Where(preview => preview.Mode == EditPlanSeedMode.Transcript));
        Assert.Equal(2, transcript.EditPlan.Clips.Count);
        Assert.NotNull(transcript.EditPlan.Subtitles);
        Assert.NotNull(transcript.EditPlan.Transcript);
    }
}
