using OpenVideoToolbox.Core.Editing;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class NarratedSlidesPlanBuilderTests
{
    [Fact]
    public void Build_CreatesStableV2TimelinePlan()
    {
        var manifest = new NarratedSlidesManifest
        {
            Video = new NarratedSlidesVideoManifest
            {
                Id = "episode-01",
                Resolution = new NarratedSlidesResolutionManifest
                {
                    W = 1280,
                    H = 720
                },
                FrameRate = 24
            },
            Subtitles = new NarratedSlidesSubtitleManifest
            {
                Path = "subs.srt",
                Mode = SubtitleMode.Sidecar
            },
            Bgm = new NarratedSlidesBgmManifest
            {
                Path = "bgm.mp3",
                GainDb = -20
            },
            Sections =
            [
                new NarratedSlidesSectionManifest
                {
                    Id = "intro",
                    Title = "Intro",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "video",
                        Path = "intro.mp4"
                    },
                    Voice = new NarratedSlidesVoiceManifest
                    {
                        Path = "intro.wav"
                    }
                },
                new NarratedSlidesSectionManifest
                {
                    Id = "deep-dive",
                    Title = "Deep Dive",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "video",
                        Path = "deep-dive.mp4"
                    },
                    Voice = new NarratedSlidesVoiceManifest
                    {
                        Path = "deep-dive.wav"
                    }
                }
            ]
        };

        var result = new NarratedSlidesPlanBuilder().Build(new NarratedSlidesPlanBuildRequest
        {
            Manifest = manifest,
            TemplateId = NarratedSlidesPlanBuilder.DefaultTemplateId,
            RenderOutputPath = "output/final.mp4",
            SubtitlePath = "subs.srt",
            SubtitleMode = SubtitleMode.Sidecar,
            BgmPath = "bgm.mp3",
            BgmGainDb = -20,
            Sections =
            [
                new NarratedSlidesResolvedSection
                {
                    Id = "intro",
                    Title = "Intro",
                    VisualPath = "intro.mp4",
                    VisualDuration = TimeSpan.FromSeconds(5),
                    VoicePath = "intro.wav",
                    VoiceDuration = TimeSpan.FromSeconds(3)
                },
                new NarratedSlidesResolvedSection
                {
                    Id = "deep-dive",
                    Title = "Deep Dive",
                    VisualPath = "deep-dive.mp4",
                    VisualDuration = TimeSpan.FromSeconds(7),
                    VoicePath = "deep-dive.wav",
                    VoiceDuration = TimeSpan.FromSeconds(4)
                }
            ]
        });

        Assert.Equal(2, result.Plan.SchemaVersion);
        Assert.Equal("narrated-slides-starter", result.Plan.Template!.Id);
        Assert.Equal("builtIn", result.Plan.Template.Source!.Kind);
        Assert.Equal("output/final.mp4", result.Plan.Output.Path);
        Assert.Equal("mp4", result.Plan.Output.Container);
        Assert.Equal("intro.mp4", result.Plan.Source.InputPath);
        Assert.NotNull(result.Plan.Subtitles);
        Assert.Equal("subs.srt", result.Plan.Subtitles!.Path);
        Assert.Equal(TimeSpan.FromSeconds(7), result.Plan.Timeline!.Duration);
        Assert.Equal(1280, result.Plan.Timeline.Resolution!.W);
        Assert.Equal(720, result.Plan.Timeline.Resolution!.H);
        Assert.Equal(24, result.Plan.Timeline.FrameRate);

        Assert.Collection(
            result.Plan.Timeline.Tracks,
            track =>
            {
                Assert.Equal("main", track.Id);
                Assert.Equal(TrackKind.Video, track.Kind);
                Assert.Equal("scale", track.Effects[0].Type);
                Assert.Equal(2, track.Clips.Count);
                Assert.Equal("intro.mp4", track.Clips[0].Src);
                Assert.Equal(TimeSpan.Zero, track.Clips[0].Start);
                Assert.Equal(TimeSpan.FromSeconds(3), track.Clips[0].Duration);
                Assert.Equal(TimeSpan.FromSeconds(3), track.Clips[1].Start);
                Assert.Equal(TimeSpan.FromSeconds(4), track.Clips[1].Duration);
            },
            track =>
            {
                Assert.Equal("voice", track.Id);
                Assert.Equal(TrackKind.Audio, track.Kind);
                Assert.Equal(2, track.Clips.Count);
                Assert.Equal("intro.wav", track.Clips[0].Src);
                Assert.Equal(TimeSpan.FromSeconds(3), track.Clips[1].Start);
            },
            track =>
            {
                Assert.Equal("bgm", track.Id);
                Assert.Equal(TrackKind.Audio, track.Kind);
                var clip = Assert.Single(track.Clips);
                Assert.Equal("bgm.mp3", clip.Src);
                Assert.Equal(TimeSpan.FromSeconds(7), clip.Duration);
                Assert.Equal("volume", clip.Effects[0].Type);
            });

        Assert.Equal(2, result.Stats.SectionCount);
        Assert.Equal(TimeSpan.FromSeconds(7), result.Stats.TotalDuration);
        Assert.True(result.Stats.HasSubtitles);
        Assert.True(result.Stats.HasBgm);
    }

    [Fact]
    public void Build_RejectsVisualShorterThanVoice()
    {
        var manifest = new NarratedSlidesManifest
        {
            Video = new NarratedSlidesVideoManifest(),
            Sections =
            [
                new NarratedSlidesSectionManifest
                {
                    Id = "intro",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "video",
                        Path = "intro.mp4"
                    },
                    Voice = new NarratedSlidesVoiceManifest
                    {
                        Path = "intro.wav"
                    }
                }
            ]
        };

        var ex = Assert.Throws<ArgumentException>(() => new NarratedSlidesPlanBuilder().Build(new NarratedSlidesPlanBuildRequest
        {
            Manifest = manifest,
            TemplateId = NarratedSlidesPlanBuilder.DefaultTemplateId,
            RenderOutputPath = "output/final.mp4",
            Sections =
            [
                new NarratedSlidesResolvedSection
                {
                    Id = "intro",
                    VisualPath = "intro.mp4",
                    VisualDuration = TimeSpan.FromSeconds(2),
                    VoicePath = "intro.wav",
                    VoiceDuration = TimeSpan.FromSeconds(3)
                }
            ]
        }));

        Assert.Contains("visual duration cannot be shorter than voice duration", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_AddsProgressBarTrackEffectWhenEnabled()
    {
        var manifest = new NarratedSlidesManifest
        {
            Video = new NarratedSlidesVideoManifest
            {
                ProgressBar = new NarratedSlidesProgressBarManifest
                {
                    Enabled = true,
                    Height = 10,
                    Margin = 24,
                    Color = "yellow@0.9",
                    BackgroundColor = "black@0.2"
                }
            },
            Sections =
            [
                new NarratedSlidesSectionManifest
                {
                    Id = "intro",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "image",
                        Path = "cover.png"
                    },
                    Voice = new NarratedSlidesVoiceManifest
                    {
                        Path = "intro.wav"
                    }
                }
            ]
        };

        var result = new NarratedSlidesPlanBuilder().Build(new NarratedSlidesPlanBuildRequest
        {
            Manifest = manifest,
            TemplateId = NarratedSlidesPlanBuilder.DefaultTemplateId,
            RenderOutputPath = "output/final.mp4",
            Sections =
            [
                new NarratedSlidesResolvedSection
                {
                    Id = "intro",
                    VisualPath = "cover.png",
                    VisualDuration = TimeSpan.FromSeconds(3),
                    VoicePath = "intro.wav",
                    VoiceDuration = TimeSpan.FromSeconds(3)
                }
            ]
        });

        var mainTrack = Assert.Single(result.Plan.Timeline!.Tracks.Where(track => track.Id == "main"));
        Assert.Equal(2, mainTrack.Effects.Count);
        Assert.Equal("progress_bar", mainTrack.Effects[1].Type);
        var extensions = Assert.IsAssignableFrom<IDictionary<string, System.Text.Json.JsonElement>>(mainTrack.Effects[1].Extensions);
        Assert.Equal("3", extensions["durationSeconds"].GetRawText());
        Assert.Equal("10", extensions["height"].GetRawText());
    }
}
