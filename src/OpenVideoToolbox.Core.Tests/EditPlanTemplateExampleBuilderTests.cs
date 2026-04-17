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
    public void BuildArtifactBindingsExample_ReturnsAllExpectedPlaceholdersForCommentaryCaptioned()
    {
        var template = BuiltInEditPlanTemplateCatalog.GetRequired("commentary-captioned");

        var example = EditPlanTemplateExampleBuilder.BuildArtifactBindingsExample(template);

        Assert.Equal("subtitles.srt", example["subtitles"]);
        Assert.Equal("audio/input.wav", example["bgm"]);
    }

    [Fact]
    public void BuildSupportingSignalExamples_ReturnsStableSignalCommandsAndConsumptionHints()
    {
        var template = BuiltInEditPlanTemplateCatalog.GetRequired("commentary-bgm");

        var signals = EditPlanTemplateExampleBuilder.BuildSupportingSignalExamples(template);

        Assert.Equal(2, signals.Count);
        Assert.Equal(EditPlanSupportingSignalKind.Transcript, signals[0].Kind);
        Assert.Equal("transcript.json", signals[0].OutputPath);
        Assert.Contains("ovt transcribe <input>", signals[0].Command, StringComparison.Ordinal);
        Assert.Contains("--transcript transcript.json", signals[0].Consumption, StringComparison.Ordinal);

        Assert.Equal(EditPlanSupportingSignalKind.Silence, signals[1].Kind);
        Assert.Equal("silence.json", signals[1].OutputPath);
        Assert.Contains("ovt detect-silence <input>", signals[1].Command, StringComparison.Ordinal);
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
        Assert.Equal(3, transcript.StrategyVariants.Count);
        Assert.Equal("grouped", transcript.StrategyVariants[0].Key);
        Assert.True(transcript.StrategyVariants[0].IsRecommended);
        Assert.Equal("max-gap", transcript.StrategyVariants[1].Key);
        Assert.True(transcript.StrategyVariants[1].IsRecommended);
        Assert.Equal("min-duration", transcript.StrategyVariants[2].Key);
        Assert.False(transcript.StrategyVariants[2].IsRecommended);

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
        Assert.Equal("grouped", transcript.StrategyVariants[0].Key);
        Assert.True(transcript.StrategyVariants[0].IsRecommended);
        Assert.Equal("max-gap", transcript.StrategyVariants[1].Key);
        Assert.True(transcript.StrategyVariants[1].IsRecommended);
        Assert.Equal(2, transcript.StrategyVariants.Single(variant => variant.Key == "grouped").EditPlan.Clips.Count);
        Assert.Equal(2, transcript.StrategyVariants.Single(variant => variant.Key == "max-gap").EditPlan.Clips.Count);
    }

    [Fact]
    public void BuildPreviewPlans_CommentaryCaptioned_CombinesCaptionAndAudioArtifacts()
    {
        var template = BuiltInEditPlanTemplateCatalog.GetRequired("commentary-captioned");

        var previews = EditPlanTemplateExampleBuilder.BuildPreviewPlans(template);

        Assert.Equal(2, previews.Count);
        Assert.DoesNotContain(previews, preview => preview.Mode == EditPlanSeedMode.Beats);
        Assert.All(previews, preview => Assert.Single(preview.EditPlan.AudioTracks));
        Assert.All(previews, preview => Assert.Single(preview.EditPlan.Artifacts.Where(artifact => artifact.Kind == "audio")));
        Assert.All(previews, preview => Assert.Single(preview.EditPlan.Artifacts.Where(artifact => artifact.Kind == "subtitle")));

        var transcript = Assert.Single(previews.Where(preview => preview.Mode == EditPlanSeedMode.Transcript));
        Assert.NotNull(transcript.EditPlan.Transcript);
        Assert.NotNull(transcript.EditPlan.Subtitles);
    }

    [Fact]
    public void BuildPreviewPlans_MusicCaptionedMontage_CombinesBeatAndCaptionShapes()
    {
        var template = BuiltInEditPlanTemplateCatalog.GetRequired("music-captioned-montage");

        var previews = EditPlanTemplateExampleBuilder.BuildPreviewPlans(template);

        Assert.Equal(2, previews.Count);
        Assert.DoesNotContain(previews, preview => preview.Mode == EditPlanSeedMode.Transcript);
        Assert.All(previews, preview => Assert.Single(preview.EditPlan.AudioTracks));
        Assert.All(previews, preview => Assert.NotNull(preview.EditPlan.Subtitles));

        var beats = Assert.Single(previews.Where(preview => preview.Mode == EditPlanSeedMode.Beats));
        Assert.NotNull(beats.EditPlan.Beats);
        Assert.Single(beats.EditPlan.Clips);
    }

    [Fact]
    public void BuildSupportingSignalExamples_ReturnsBeatAndStemGuidanceForMontageTemplates()
    {
        var template = BuiltInEditPlanTemplateCatalog.GetRequired("music-captioned-montage");

        var signals = EditPlanTemplateExampleBuilder.BuildSupportingSignalExamples(template);

        Assert.Equal(2, signals.Count);
        Assert.Equal(EditPlanSupportingSignalKind.Beats, signals[0].Kind);
        Assert.Equal(EditPlanSupportingSignalKind.Stems, signals[1].Kind);
        Assert.Equal("stems/", signals[1].OutputPath);
        Assert.Contains("ovt separate-audio <input>", signals[1].Command, StringComparison.Ordinal);
    }
}
