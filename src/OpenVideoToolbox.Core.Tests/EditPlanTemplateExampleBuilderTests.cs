using OpenVideoToolbox.Core;
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
    public void BuildArtifactBindingsExample_ReturnsStemArtifactPathForBeatMontage()
    {
        var template = BuiltInEditPlanTemplateCatalog.GetRequired("beat-montage");

        var example = EditPlanTemplateExampleBuilder.BuildArtifactBindingsExample(template);

        Assert.Equal("stems/htdemucs/input/no_vocals.wav", example["bgm"]);
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
        Assert.Contains("attach-plan-material --plan edit.json --transcript --path transcript.json --check-files", signals[0].Consumption, StringComparison.Ordinal);

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
    public void BuildPreviewPlans_TimelineEffectsStarter_ReturnsSchemaV2TimelineShape()
    {
        var template = BuiltInEditPlanTemplateCatalog.GetRequired("timeline-effects-starter");

        var previews = EditPlanTemplateExampleBuilder.BuildPreviewPlans(template);

        Assert.Equal(3, previews.Count);

        var manual = Assert.Single(previews.Where(preview => preview.Mode == EditPlanSeedMode.Manual));
        Assert.Equal(SchemaVersions.V2, manual.EditPlan.SchemaVersion);
        Assert.Empty(manual.EditPlan.Clips);
        Assert.Empty(manual.StrategyVariants);
        Assert.NotNull(manual.EditPlan.Timeline);
        Assert.Equal(2, manual.EditPlan.Timeline!.Tracks.Count);
        Assert.Equal("main", manual.EditPlan.Timeline.Tracks[0].Id);
        Assert.Equal(TrackKind.Video, manual.EditPlan.Timeline.Tracks[0].Kind);
        Assert.Equal("scale", manual.EditPlan.Timeline.Tracks[0].Effects[0].Type);
        Assert.Equal(2, manual.EditPlan.Timeline.Tracks[0].Clips.Count);
        Assert.Equal("brightness_contrast", manual.EditPlan.Timeline.Tracks[0].Clips[0].Effects[0].Type);
        Assert.Equal("fade", manual.EditPlan.Timeline.Tracks[0].Clips[0].Transitions!.Out!.Type);
        Assert.Equal("bgm", manual.EditPlan.Timeline.Tracks[1].Id);
        Assert.Equal(TrackKind.Audio, manual.EditPlan.Timeline.Tracks[1].Kind);
        Assert.Equal("volume", manual.EditPlan.Timeline.Tracks[1].Clips[0].Effects[0].Type);
        Assert.Equal("audio/input.wav", manual.EditPlan.Artifacts.Single(artifact => artifact.SlotId == "bgm").Path);

        var transcript = Assert.Single(previews.Where(preview => preview.Mode == EditPlanSeedMode.Transcript));
        Assert.Equal(SchemaVersions.V2, transcript.EditPlan.SchemaVersion);
        Assert.NotNull(transcript.EditPlan.Transcript);
        Assert.NotNull(transcript.EditPlan.Timeline);
        Assert.Equal(2, transcript.EditPlan.Timeline!.Tracks[0].Clips.Count);
        Assert.Equal("grouped", transcript.StrategyVariants[0].Key);
        Assert.True(transcript.StrategyVariants[0].IsRecommended);
        Assert.Equal("max-gap", transcript.StrategyVariants[1].Key);
        Assert.True(transcript.StrategyVariants[1].IsRecommended);
        Assert.Equal("min-duration", transcript.StrategyVariants[2].Key);
        Assert.False(transcript.StrategyVariants[2].IsRecommended);
        Assert.Equal("brightness_contrast", transcript.EditPlan.Timeline.Tracks[0].Clips[0].Effects[0].Type);
        Assert.Null(transcript.EditPlan.Timeline.Tracks[0].Clips[0].Transitions);

        var beats = Assert.Single(previews.Where(preview => preview.Mode == EditPlanSeedMode.Beats));
        Assert.Equal(SchemaVersions.V2, beats.EditPlan.SchemaVersion);
        Assert.NotNull(beats.EditPlan.Beats);
        Assert.NotNull(beats.EditPlan.Timeline);
        Assert.Single(beats.EditPlan.Timeline!.Tracks[0].Clips);
        Assert.Equal("brightness_contrast", beats.EditPlan.Timeline.Tracks[0].Clips[0].Effects[0].Type);
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
    public void BuildPreviewPlans_CanStampPluginTemplateSourceAcrossAllPreviewPlans()
    {
        var template = new EditPlanTemplateDefinition
        {
            Id = "plugin-captioned",
            DisplayName = "Plugin Captioned",
            Description = "Plugin template description",
            Category = "plugin",
            OutputContainer = "mp4",
            DefaultSubtitleMode = SubtitleMode.Sidecar,
            RecommendedSeedModes = [EditPlanSeedMode.Manual, EditPlanSeedMode.Transcript]
        };
        var source = new EditTemplateSourceReference
        {
            Kind = EditTemplateSourceKinds.Plugin,
            PluginId = "community-pack",
            PluginVersion = "1.0.0"
        };

        var previews = EditPlanTemplateExampleBuilder.BuildPreviewPlans(template, source);

        Assert.Equal(2, previews.Count);
        Assert.All(previews, preview =>
        {
            Assert.NotNull(preview.EditPlan.Template);
            Assert.Equal(EditTemplateSourceKinds.Plugin, preview.EditPlan.Template!.Source!.Kind);
            Assert.Equal("community-pack", preview.EditPlan.Template.Source.PluginId);
            Assert.Equal("1.0.0", preview.EditPlan.Template.Source.PluginVersion);
        });

        var transcript = Assert.Single(previews.Where(preview => preview.Mode == EditPlanSeedMode.Transcript));
        Assert.All(transcript.StrategyVariants, variant =>
        {
            Assert.NotNull(variant.EditPlan.Template);
            Assert.Equal(EditTemplateSourceKinds.Plugin, variant.EditPlan.Template!.Source!.Kind);
            Assert.Equal("community-pack", variant.EditPlan.Template.Source.PluginId);
        });
    }

    [Fact]
    public void BuildSupportingSignalExamples_ReturnsBeatAndStemGuidanceForMontageTemplates()
    {
        var template = BuiltInEditPlanTemplateCatalog.GetRequired("music-captioned-montage");

        var signals = EditPlanTemplateExampleBuilder.BuildSupportingSignalExamples(template);

        Assert.Equal(2, signals.Count);
        Assert.Equal(EditPlanSupportingSignalKind.Beats, signals[0].Kind);
        Assert.Contains("attach-plan-material --plan edit.json --beats --path beats.json --check-files", signals[0].Consumption, StringComparison.Ordinal);
        Assert.Equal(EditPlanSupportingSignalKind.Stems, signals[1].Kind);
        Assert.Equal("stems/", signals[1].OutputPath);
        Assert.Contains("ovt separate-audio <input>", signals[1].Command, StringComparison.Ordinal);
    }
}
