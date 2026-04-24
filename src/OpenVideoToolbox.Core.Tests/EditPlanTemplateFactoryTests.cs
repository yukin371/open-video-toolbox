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
    public void Create_CanGroupTranscriptSegmentsIntoDeterministicSeedClips()
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
                            End = TimeSpan.FromSeconds(1),
                            Text = "Intro"
                        },
                        new TranscriptSegment
                        {
                            Id = "seg-002",
                            Start = TimeSpan.FromSeconds(1),
                            End = TimeSpan.FromSeconds(2.5),
                            Text = "Detail"
                        },
                        new TranscriptSegment
                        {
                            Id = "seg-003",
                            Start = TimeSpan.FromSeconds(2.5),
                            End = TimeSpan.FromSeconds(4),
                            Text = "Wrap"
                        }
                    ]
                },
                SeedClipsFromTranscript = true,
                TranscriptSegmentGroupSize = 2
            });

        Assert.Equal(2, plan.Clips.Count);
        Assert.Equal(TimeSpan.Zero, plan.Clips[0].InPoint);
        Assert.Equal(TimeSpan.FromSeconds(2.5), plan.Clips[0].OutPoint);
        Assert.Equal("seg-001..seg-002", plan.Clips[0].Label);
        Assert.Equal(TimeSpan.FromSeconds(2.5), plan.Clips[1].InPoint);
        Assert.Equal(TimeSpan.FromSeconds(4), plan.Clips[1].OutPoint);
        Assert.Equal("seg-003", plan.Clips[1].Label);
    }

    [Fact]
    public void Create_CanFilterTranscriptSegmentsByMinimumDurationBeforeGrouping()
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
                            End = TimeSpan.FromMilliseconds(250),
                            Text = "Too short"
                        },
                        new TranscriptSegment
                        {
                            Id = "seg-002",
                            Start = TimeSpan.FromMilliseconds(250),
                            End = TimeSpan.FromSeconds(1),
                            Text = "Keep"
                        },
                        new TranscriptSegment
                        {
                            Id = "seg-003",
                            Start = TimeSpan.FromSeconds(1),
                            End = TimeSpan.FromSeconds(2),
                            Text = "Keep too"
                        }
                    ]
                },
                SeedClipsFromTranscript = true,
                MinTranscriptSegmentDuration = TimeSpan.FromMilliseconds(500),
                TranscriptSegmentGroupSize = 2
            });

        var clip = Assert.Single(plan.Clips);
        Assert.Equal(TimeSpan.FromMilliseconds(250), clip.InPoint);
        Assert.Equal(TimeSpan.FromSeconds(2), clip.OutPoint);
        Assert.Equal("seg-002..seg-003", clip.Label);
    }

    [Fact]
    public void Create_CanSplitTranscriptSeedGroupsWhenGapExceedsThreshold()
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
                            End = TimeSpan.FromMilliseconds(600),
                            Text = "First"
                        },
                        new TranscriptSegment
                        {
                            Id = "seg-002",
                            Start = TimeSpan.FromMilliseconds(750),
                            End = TimeSpan.FromSeconds(1.3),
                            Text = "Near"
                        },
                        new TranscriptSegment
                        {
                            Id = "seg-003",
                            Start = TimeSpan.FromSeconds(2),
                            End = TimeSpan.FromSeconds(2.8),
                            Text = "Far"
                        }
                    ]
                },
                SeedClipsFromTranscript = true,
                TranscriptSegmentGroupSize = 3,
                MaxTranscriptGap = TimeSpan.FromMilliseconds(200)
            });

        Assert.Equal(2, plan.Clips.Count);
        Assert.Equal("seg-001..seg-002", plan.Clips[0].Label);
        Assert.Equal(TimeSpan.Zero, plan.Clips[0].InPoint);
        Assert.Equal(TimeSpan.FromSeconds(1.3), plan.Clips[0].OutPoint);
        Assert.Equal("seg-003", plan.Clips[1].Label);
        Assert.Equal(TimeSpan.FromSeconds(2), plan.Clips[1].InPoint);
        Assert.Equal(TimeSpan.FromSeconds(2.8), plan.Clips[1].OutPoint);
    }

    [Fact]
    public void Create_RejectsNonPositiveTranscriptSegmentGroupSize()
    {
        var factory = new EditPlanTemplateFactory();

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Create(
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
                            End = TimeSpan.FromSeconds(1),
                            Text = "Hello"
                        }
                    ]
                },
                SeedClipsFromTranscript = true,
                TranscriptSegmentGroupSize = 0
            }));
    }

    [Fact]
    public void Create_RejectsNegativeMinimumTranscriptSegmentDuration()
    {
        var factory = new EditPlanTemplateFactory();

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Create(
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
                            End = TimeSpan.FromSeconds(1),
                            Text = "Hello"
                        }
                    ]
                },
                SeedClipsFromTranscript = true,
                MinTranscriptSegmentDuration = TimeSpan.FromMilliseconds(-1)
            }));
    }

    [Fact]
    public void Create_RejectsNegativeMaximumTranscriptGap()
    {
        var factory = new EditPlanTemplateFactory();

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Create(
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
                            End = TimeSpan.FromSeconds(1),
                            Text = "Hello"
                        }
                    ]
                },
                SeedClipsFromTranscript = true,
                MaxTranscriptGap = TimeSpan.FromMilliseconds(-1)
            }));
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
    public void Create_CommentaryCaptioned_BindsSubtitleAndBgmArtifacts()
    {
        var factory = new EditPlanTemplateFactory();

        var plan = factory.Create(
            "commentary-captioned",
            new EditPlanTemplateRequest
            {
                InputPath = "input.mp4",
                RenderOutputPath = "final.mp4",
                ArtifactBindings = new Dictionary<string, string>
                {
                    ["subtitles"] = "subs/commentary.srt",
                    ["bgm"] = "audio/commentary-bed.wav"
                },
                TranscriptPath = "transcript.json",
                Transcript = new TranscriptDocument
                {
                    Language = "en",
                    Segments =
                    [
                        new TranscriptSegment
                        {
                            Id = "line-001",
                            Start = TimeSpan.Zero,
                            End = TimeSpan.FromSeconds(2.5),
                            Text = "Welcome back."
                        }
                    ]
                },
                SeedClipsFromTranscript = true
            });

        Assert.Equal("commentary-captioned", plan.Template!.Id);
        Assert.Equal("clean-sidecar", plan.Template.Parameters["captionStyle"]);
        Assert.Single(plan.Clips);
        Assert.Equal("line-001", plan.Clips[0].Label);
        Assert.Equal(2, plan.Artifacts.Count);
        Assert.Single(plan.AudioTracks);
        Assert.Equal("audio/commentary-bed.wav", plan.AudioTracks[0].Path);
        Assert.NotNull(plan.Subtitles);
        Assert.Equal("subs/commentary.srt", plan.Subtitles!.Path);
        Assert.Equal("transcript.json", plan.Transcript!.Path);
    }

    [Fact]
    public void Create_TimelineEffectsStarter_CanSeedTimelineFromTranscriptSegments()
    {
        var factory = new EditPlanTemplateFactory();

        var plan = factory.Create(
            "timeline-effects-starter",
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

        Assert.Equal(SchemaVersions.V2, plan.SchemaVersion);
        Assert.Empty(plan.Clips);
        Assert.NotNull(plan.Timeline);

        var videoTrack = Assert.Single(plan.Timeline!.Tracks.Where(track => track.Kind == TrackKind.Video));
        Assert.Equal(2, videoTrack.Clips.Count);
        Assert.Equal(TimeSpan.Zero, videoTrack.Clips[0].Start);
        Assert.Equal(TimeSpan.Zero, videoTrack.Clips[0].InPoint);
        Assert.Equal(TimeSpan.FromSeconds(1.2), videoTrack.Clips[0].OutPoint);
        Assert.Equal(TimeSpan.FromSeconds(1.2), videoTrack.Clips[1].Start);
        Assert.Equal(TimeSpan.FromSeconds(1.2), videoTrack.Clips[1].InPoint);
        Assert.Equal(TimeSpan.FromSeconds(2.4), videoTrack.Clips[1].OutPoint);
        Assert.Equal("brightness_contrast", videoTrack.Clips[0].Effects[0].Type);
        Assert.Null(videoTrack.Clips[0].Transitions);
        Assert.Equal(TimeSpan.FromSeconds(2.4), plan.Timeline.Duration);
        Assert.Equal("transcript.json", plan.Transcript!.Path);
        Assert.Equal(2, plan.Transcript.SegmentCount);
    }

    [Fact]
    public void Create_TimelineEffectsStarter_CanSeedTimelineFromBeatGroups()
    {
        var factory = new EditPlanTemplateFactory();

        var plan = factory.Create(
            "timeline-effects-starter",
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

        Assert.Equal(SchemaVersions.V2, plan.SchemaVersion);
        Assert.Empty(plan.Clips);
        Assert.NotNull(plan.Timeline);

        var videoTrack = Assert.Single(plan.Timeline!.Tracks.Where(track => track.Kind == TrackKind.Video));
        Assert.Equal(2, videoTrack.Clips.Count);
        Assert.Equal(TimeSpan.Zero, videoTrack.Clips[0].Start);
        Assert.Equal(TimeSpan.FromSeconds(2), videoTrack.Clips[0].OutPoint);
        Assert.Equal(TimeSpan.FromSeconds(2), videoTrack.Clips[1].Start);
        Assert.Equal(TimeSpan.FromSeconds(2), videoTrack.Clips[1].InPoint);
        Assert.Equal(TimeSpan.FromSeconds(4), videoTrack.Clips[1].OutPoint);
        Assert.Equal("brightness_contrast", videoTrack.Clips[1].Effects[0].Type);
        Assert.Null(videoTrack.Clips[1].Transitions);
        Assert.Equal(TimeSpan.FromSeconds(4), plan.Timeline.Duration);
        Assert.Equal("beats.json", plan.Beats!.Path);
        Assert.Equal(120, plan.Beats.EstimatedBpm);
    }

    [Fact]
    public void Create_MusicCaptionedMontage_BindsCaptionAndMusicForBeatSeed()
    {
        var factory = new EditPlanTemplateFactory();

        var plan = factory.Create(
            "music-captioned-montage",
            new EditPlanTemplateRequest
            {
                InputPath = "input.mp4",
                RenderOutputPath = "final.mp4",
                ArtifactBindings = new Dictionary<string, string>
                {
                    ["subtitles"] = "subs/lyrics.srt",
                    ["bgm"] = "audio/anthem.wav"
                },
                BeatTrackPath = "beats.json",
                BeatTrack = new BeatTrackDocument
                {
                    SourcePath = "input.mp4",
                    SampleRateHz = 16000,
                    FrameDuration = TimeSpan.FromMilliseconds(50),
                    EstimatedBpm = 132,
                    Beats =
                    [
                        new BeatMarker { Index = 0, Time = TimeSpan.Zero, Strength = 0.91 },
                        new BeatMarker { Index = 1, Time = TimeSpan.FromSeconds(1), Strength = 0.88 },
                        new BeatMarker { Index = 2, Time = TimeSpan.FromSeconds(2), Strength = 0.93 },
                        new BeatMarker { Index = 3, Time = TimeSpan.FromSeconds(3), Strength = 0.87 },
                        new BeatMarker { Index = 4, Time = TimeSpan.FromSeconds(4), Strength = 0.95 }
                    ]
                },
                SeedClipsFromBeats = true,
                BeatGroupSize = 2
            });

        Assert.Equal("music-captioned-montage", plan.Template!.Id);
        Assert.Equal("punchy-hook", plan.Template.Parameters["captionStyle"]);
        Assert.Equal(2, plan.Clips.Count);
        Assert.Equal(2, plan.Artifacts.Count);
        Assert.Single(plan.AudioTracks);
        Assert.Equal("audio/anthem.wav", plan.AudioTracks[0].Path);
        Assert.NotNull(plan.Subtitles);
        Assert.Equal("subs/lyrics.srt", plan.Subtitles!.Path);
        Assert.Equal("beats.json", plan.Beats!.Path);
        Assert.Equal(132, plan.Beats.EstimatedBpm);
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
