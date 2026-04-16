using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Beats;
using OpenVideoToolbox.Core.Subtitles;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class EditPlanTemplateFactoryTests
{
    [Fact]
    public void Create_SeedsPlanFromTemplateAndDuration()
    {
        var factory = new EditPlanTemplateFactory();

        var plan = factory.Create(
            "shorts-captioned",
            new EditPlanTemplateRequest
            {
                InputPath = "input.mp4",
                RenderOutputPath = "final.mp4",
                SourceDuration = TimeSpan.FromSeconds(42.5),
                ParameterOverrides = new Dictionary<string, string>
                {
                    ["captionStyle"] = "sidecar-clean"
                },
                TranscriptPath = "transcript.json",
                Transcript = new TranscriptDocument
                {
                    Language = "en",
                    Segments =
                    [
                        new TranscriptSegment
                        {
                            Id = "seg-001",
                            Start = TimeSpan.Zero,
                            End = TimeSpan.FromSeconds(1),
                            Text = "hello"
                        }
                    ]
                },
                BeatTrackPath = "beats.json",
                BeatTrack = new BeatTrackDocument
                {
                    SourcePath = "input.mp4",
                    SampleRateHz = 16000,
                    FrameDuration = TimeSpan.FromMilliseconds(50),
                    EstimatedBpm = 128
                },
                SubtitlePath = "subs/captions.srt",
                BgmPath = "audio/bgm.wav"
            });

        Assert.Equal("shorts-captioned", plan.Template!.Id);
        Assert.Equal("sidecar-clean", plan.Template.Parameters["captionStyle"]);
        Assert.Single(plan.Clips);
        Assert.Equal(TimeSpan.Zero, plan.Clips[0].InPoint);
        Assert.Equal(TimeSpan.FromSeconds(42.5), plan.Clips[0].OutPoint);
        Assert.Single(plan.AudioTracks);
        Assert.Equal("audio/bgm.wav", plan.AudioTracks[0].Path);
        Assert.Single(plan.Artifacts);
        Assert.Equal("subtitles", plan.Artifacts[0].SlotId);
        Assert.Equal("transcript.json", plan.Transcript!.Path);
        Assert.Equal("beats.json", plan.Beats!.Path);
        Assert.NotNull(plan.Subtitles);
        Assert.Equal("subs/captions.srt", plan.Subtitles!.Path);
        Assert.Equal(SubtitleMode.Sidecar, plan.Subtitles.Mode);
        Assert.True(plan.Extensions?.ContainsKey("x-template"));
    }

    [Fact]
    public void Create_WithoutProbeDuration_LeavesClipListEmpty()
    {
        var factory = new EditPlanTemplateFactory();

        var plan = factory.Create(
            "shorts-basic",
            new EditPlanTemplateRequest
            {
                InputPath = "input.mp4",
                RenderOutputPath = "final.mp4"
            });

        Assert.Empty(plan.Clips);
        Assert.Null(plan.Subtitles);
        Assert.Empty(plan.AudioTracks);
        Assert.Equal("fast", plan.Template!.Parameters["pace"]);
    }

    [Fact]
    public void Create_AllowsTemplateSubtitleToBeDisabled()
    {
        var factory = new EditPlanTemplateFactory();

        var plan = factory.Create(
            "shorts-captioned",
            new EditPlanTemplateRequest
            {
                InputPath = "input.mp4",
                RenderOutputPath = "final.mp4",
                DisableSubtitles = true
            });

        Assert.Null(plan.Subtitles);
    }

    [Fact]
    public void Create_CanSeedClipsFromBeatTrack()
    {
        var factory = new EditPlanTemplateFactory();

        var plan = factory.Create(
            "shorts-basic",
            new EditPlanTemplateRequest
            {
                InputPath = "input.mp4",
                RenderOutputPath = "final.mp4",
                BeatTrackPath = "beats.json",
                BeatTrack = new BeatTrackDocument
                {
                    SourcePath = "input.mp4",
                    SampleRateHz = 16000,
                    FrameDuration = TimeSpan.FromMilliseconds(50),
                    EstimatedBpm = 120,
                    Beats =
                    [
                        new BeatMarker { Index = 0, Time = TimeSpan.Zero, Strength = 0.9 },
                        new BeatMarker { Index = 1, Time = TimeSpan.FromSeconds(1), Strength = 0.9 },
                        new BeatMarker { Index = 2, Time = TimeSpan.FromSeconds(2), Strength = 0.9 },
                        new BeatMarker { Index = 3, Time = TimeSpan.FromSeconds(3), Strength = 0.9 },
                        new BeatMarker { Index = 4, Time = TimeSpan.FromSeconds(4), Strength = 0.9 }
                    ]
                },
                SeedClipsFromBeats = true,
                BeatGroupSize = 2
            });

        Assert.Equal(2, plan.Clips.Count);
        Assert.Equal(TimeSpan.Zero, plan.Clips[0].InPoint);
        Assert.Equal(TimeSpan.FromSeconds(2), plan.Clips[0].OutPoint);
        Assert.Equal(TimeSpan.FromSeconds(2), plan.Clips[1].InPoint);
        Assert.Equal(TimeSpan.FromSeconds(4), plan.Clips[1].OutPoint);
        Assert.Equal("beats.json", plan.Beats!.Path);
    }

    [Fact]
    public void Create_CanSeedClipsFromTranscriptSegments()
    {
        var factory = new EditPlanTemplateFactory();

        var plan = factory.Create(
            "shorts-basic",
            new EditPlanTemplateRequest
            {
                InputPath = "input.mp4",
                RenderOutputPath = "final.mp4",
                TranscriptPath = "transcript.json",
                Transcript = new TranscriptDocument
                {
                    Language = "en",
                    Segments =
                    [
                        new TranscriptSegment
                        {
                            Id = "seg-001",
                            Start = TimeSpan.Zero,
                            End = TimeSpan.FromSeconds(1.2),
                            Text = "Hello"
                        },
                        new TranscriptSegment
                        {
                            Id = "seg-002",
                            Start = TimeSpan.FromSeconds(1.2),
                            End = TimeSpan.FromSeconds(2.4),
                            Text = "World"
                        }
                    ]
                },
                SeedClipsFromTranscript = true
            });

        Assert.Equal(2, plan.Clips.Count);
        Assert.Equal(TimeSpan.Zero, plan.Clips[0].InPoint);
        Assert.Equal(TimeSpan.FromSeconds(1.2), plan.Clips[0].OutPoint);
        Assert.Equal("seg-001", plan.Clips[0].Label);
        Assert.Equal("transcript.json", plan.Transcript!.Path);
        Assert.Equal(2, plan.Transcript.SegmentCount);
    }

    [Fact]
    public void Create_BindsTemplateArtifactsFromGenericArtifactMap()
    {
        var factory = new EditPlanTemplateFactory();

        var plan = factory.Create(
            "commentary-bgm",
            new EditPlanTemplateRequest
            {
                InputPath = "input.mp4",
                RenderOutputPath = "final.mp4",
                ArtifactBindings = new Dictionary<string, string>
                {
                    ["bgm"] = "audio/theme.wav"
                }
            });

        Assert.Single(plan.Artifacts);
        Assert.Equal("bgm", plan.Artifacts[0].SlotId);
        Assert.Single(plan.AudioTracks);
        Assert.Equal("audio/theme.wav", plan.AudioTracks[0].Path);
    }

    [Fact]
    public void Create_ExplainerCaptioned_SeedsTranscriptClipsAndSubtitleArtifacts()
    {
        var factory = new EditPlanTemplateFactory();

        var plan = factory.Create(
            "explainer-captioned",
            new EditPlanTemplateRequest
            {
                InputPath = "input.mp4",
                RenderOutputPath = "final.mp4",
                TranscriptPath = "transcript.json",
                Transcript = new TranscriptDocument
                {
                    Language = "en",
                    Segments =
                    [
                        new TranscriptSegment
                        {
                            Id = "step-001",
                            Start = TimeSpan.Zero,
                            End = TimeSpan.FromSeconds(2),
                            Text = "Open the settings panel."
                        },
                        new TranscriptSegment
                        {
                            Id = "step-002",
                            Start = TimeSpan.FromSeconds(2),
                            End = TimeSpan.FromSeconds(4),
                            Text = "Enable the export toggle."
                        }
                    ]
                },
                ArtifactBindings = new Dictionary<string, string>
                {
                    ["subtitles"] = "subs/tutorial.srt"
                },
                SeedClipsFromTranscript = true
            });

        Assert.Equal("explainer-captioned", plan.Template!.Id);
        Assert.Equal("problem-solution", plan.Template.Parameters["structure"]);
        Assert.Equal(2, plan.Clips.Count);
        Assert.Equal("step-001", plan.Clips[0].Label);
        Assert.Single(plan.Artifacts);
        Assert.Equal("subtitles", plan.Artifacts[0].SlotId);
        Assert.NotNull(plan.Subtitles);
        Assert.Equal("subs/tutorial.srt", plan.Subtitles!.Path);
        Assert.Equal("transcript.json", plan.Transcript!.Path);
    }

    [Fact]
    public void Create_BeatMontage_BindsMusicArtifactAndBeatSeedClips()
    {
        var factory = new EditPlanTemplateFactory();

        var plan = factory.Create(
            "beat-montage",
            new EditPlanTemplateRequest
            {
                InputPath = "input.mp4",
                RenderOutputPath = "final.mp4",
                BeatTrackPath = "beats.json",
                BeatTrack = new BeatTrackDocument
                {
                    SourcePath = "input.mp4",
                    SampleRateHz = 16000,
                    FrameDuration = TimeSpan.FromMilliseconds(50),
                    EstimatedBpm = 128,
                    Beats =
                    [
                        new BeatMarker { Index = 0, Time = TimeSpan.Zero, Strength = 0.91 },
                        new BeatMarker { Index = 1, Time = TimeSpan.FromSeconds(1), Strength = 0.88 },
                        new BeatMarker { Index = 2, Time = TimeSpan.FromSeconds(2), Strength = 0.92 },
                        new BeatMarker { Index = 3, Time = TimeSpan.FromSeconds(3), Strength = 0.89 },
                        new BeatMarker { Index = 4, Time = TimeSpan.FromSeconds(4), Strength = 0.94 }
                    ]
                },
                ArtifactBindings = new Dictionary<string, string>
                {
                    ["bgm"] = "audio/montage.wav"
                },
                SeedClipsFromBeats = true,
                BeatGroupSize = 2
            });

        Assert.Equal("beat-montage", plan.Template!.Id);
        Assert.Equal("sync-cut", plan.Template.Parameters["pace"]);
        Assert.Equal(2, plan.Clips.Count);
        Assert.Single(plan.Artifacts);
        Assert.Equal("bgm", plan.Artifacts[0].SlotId);
        Assert.Single(plan.AudioTracks);
        Assert.Equal("audio/montage.wav", plan.AudioTracks[0].Path);
        Assert.Equal("beats.json", plan.Beats!.Path);
    }

    [Fact]
    public void Create_ExplicitSubtitlePathUsesSidecarWhenTemplateHasNoDefaultSubtitleMode()
    {
        var factory = new EditPlanTemplateFactory();

        var plan = factory.Create(
            "shorts-basic",
            new EditPlanTemplateRequest
            {
                InputPath = "input.mp4",
                RenderOutputPath = "final.mp4",
                SubtitlePath = "subs/captions.srt"
            });

        Assert.NotNull(plan.Subtitles);
        Assert.Equal("subs/captions.srt", plan.Subtitles!.Path);
        Assert.Equal(SubtitleMode.Sidecar, plan.Subtitles.Mode);
    }

    [Fact]
    public void Create_ThrowsWhenArtifactBindingIsNotDeclaredByTemplate()
    {
        var factory = new EditPlanTemplateFactory();

        var error = Assert.Throws<InvalidOperationException>(() => factory.Create(
            "shorts-basic",
            new EditPlanTemplateRequest
            {
                InputPath = "input.mp4",
                RenderOutputPath = "final.mp4",
                ArtifactBindings = new Dictionary<string, string>
                {
                    ["bgm"] = "audio/theme.wav"
                }
            }));

        Assert.Contains("does not declare artifact slot", error.Message);
    }
}
