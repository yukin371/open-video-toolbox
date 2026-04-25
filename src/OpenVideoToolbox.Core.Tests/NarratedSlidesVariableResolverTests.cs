using OpenVideoToolbox.Core.Editing;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class NarratedSlidesVariableResolverTests
{
    [Fact]
    public void Resolve_ReplacesSupportedFieldsAndOverlayWins()
    {
        var manifest = new NarratedSlidesManifest
        {
            Variables = new Dictionary<string, string>
            {
                ["episodeId"] = "episode-01",
                ["slidesDir"] = "slides",
                ["voicePath"] = "audio/intro.wav",
                ["barColor"] = "white@0.95"
            },
            Video = new NarratedSlidesVideoManifest
            {
                Id = "${episodeId}",
                Title = "Episode $${episodeId}",
                Output = "exports/${episodeId}.mp4",
                ProgressBar = new NarratedSlidesProgressBarManifest
                {
                    Color = "${barColor}",
                    BackgroundColor = "${barBackground:-black@0.28}"
                }
            },
            Template = new NarratedSlidesTemplateManifest
            {
                Id = "${templateId:-narrated-slides-starter}"
            },
            Subtitles = new NarratedSlidesSubtitleManifest
            {
                Path = "${subtitleDir:-subtitles}/${episodeId}.srt",
                Mode = SubtitleMode.Sidecar
            },
            Bgm = new NarratedSlidesBgmManifest
            {
                Path = "${bgmPath:-audio/bgm.mp3}"
            },
            Sections =
            [
                new NarratedSlidesSectionManifest
                {
                    Id = "${sectionId:-intro}",
                    Title = "Slide $${1}",
                    Visual = new NarratedSlidesVisualManifest
                    {
                        Kind = "${visualKind:-image}",
                        Path = "${slidesDir}/${slideName:-cover}.png"
                    },
                    Voice = new NarratedSlidesVoiceManifest
                    {
                        Path = "${voicePath}"
                    }
                }
            ]
        };

        var resolved = NarratedSlidesVariableResolver.Resolve(
            manifest,
            new Dictionary<string, string>
            {
                ["episodeId"] = "episode-02",
                ["barColor"] = "yellow@0.9"
            });

        Assert.Equal("episode-02", resolved.Video.Id);
        Assert.Equal("Episode ${episodeId}", resolved.Video.Title);
        Assert.Equal("exports/episode-02.mp4", resolved.Video.Output);
        Assert.Equal("yellow@0.9", resolved.Video.ProgressBar!.Color);
        Assert.Equal("black@0.28", resolved.Video.ProgressBar.BackgroundColor);
        Assert.Equal("narrated-slides-starter", resolved.Template!.Id);
        Assert.Equal("subtitles/episode-02.srt", resolved.Subtitles!.Path);
        Assert.Equal("audio/bgm.mp3", resolved.Bgm!.Path);
        Assert.Equal("intro", resolved.Sections[0].Id);
        Assert.Equal("Slide ${1}", resolved.Sections[0].Title);
        Assert.Equal("image", resolved.Sections[0].Visual.Kind);
        Assert.Equal("slides/cover.png", resolved.Sections[0].Visual.Path);
        Assert.Equal("audio/intro.wav", resolved.Sections[0].Voice.Path);
        Assert.Equal("episode-02", resolved.Variables["episodeId"]);
    }

    [Fact]
    public void Resolve_RejectsUnresolvedVariable()
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
                        Kind = "image",
                        Path = "${missingAsset}"
                    },
                    Voice = new NarratedSlidesVoiceManifest
                    {
                        Path = "audio/intro.wav"
                    }
                }
            ]
        };

        var ex = Assert.Throws<InvalidOperationException>(() => NarratedSlidesVariableResolver.Resolve(manifest));

        Assert.Contains("sections[0].visual.path", ex.Message, StringComparison.Ordinal);
        Assert.Contains("missingAsset", ex.Message, StringComparison.Ordinal);
    }
}
