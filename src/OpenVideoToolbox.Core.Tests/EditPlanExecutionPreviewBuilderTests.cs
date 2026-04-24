using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class EditPlanExecutionPreviewBuilderTests
{
    [Fact]
    public void BuildRenderPreview_IncludesProducedPathsAndSideEffects()
    {
        var builder = new EditPlanExecutionPreviewBuilder();
        var request = new EditPlanRenderRequest
        {
            Plan = new EditPlan
            {
                Source = new EditPlanSource
                {
                    InputPath = "input.mp4"
                },
                Clips =
                [
                    new EditClip
                    {
                        Id = "clip-001",
                        InPoint = TimeSpan.Zero,
                        OutPoint = TimeSpan.FromSeconds(3)
                    }
                ],
                Subtitles = new EditSubtitlePlan
                {
                    Path = "subs/captions.srt",
                    Mode = SubtitleMode.Sidecar
                },
                Output = new EditOutputPlan
                {
                    Path = Path.GetFullPath("final.mp4"),
                    Container = "mp4"
                }
            }
        };

        var preview = builder.BuildRenderPreview(request);

        Assert.Equal(1, preview.SchemaVersion);
        Assert.Equal("render", preview.Operation);
        Assert.True(preview.IsPreview);
        Assert.True(preview.PathsResolved);
        Assert.Equal("ffmpeg", preview.CommandPlan.ToolName);
        Assert.Equal(2, preview.ProducedPaths.Count);
        Assert.Contains("final.mp4", preview.ProducedPaths[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("final.srt", preview.ProducedPaths[1], StringComparison.OrdinalIgnoreCase);
        var sideEffect = Assert.Single(preview.SideEffects);
        Assert.Equal("copy-subtitle-sidecar", sideEffect.Type);
        Assert.Contains("captions.srt", sideEffect.SourcePath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("final.srt", sideEffect.DestinationPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildAudioMixPreview_UsesMixOutputAsProducedPath()
    {
        var builder = new EditPlanExecutionPreviewBuilder();
        var request = new EditPlanAudioMixRequest
        {
            Plan = new EditPlan
            {
                Source = new EditPlanSource
                {
                    InputPath = "input.mp4"
                },
                Clips =
                [
                    new EditClip
                    {
                        Id = "clip-001",
                        InPoint = TimeSpan.Zero,
                        OutPoint = TimeSpan.FromSeconds(3)
                    }
                ],
                AudioTracks =
                [
                    new AudioTrackMix
                    {
                        Id = "bgm-01",
                        Role = AudioTrackRole.Bgm,
                        Path = "audio/bgm.wav"
                    }
                ],
                Output = new EditOutputPlan
                {
                    Path = "final.mp4",
                    Container = "mp4"
                }
            },
            OutputPath = Path.GetFullPath("mixed.wav")
        };

        var preview = builder.BuildAudioMixPreview(request);

        Assert.Equal(1, preview.SchemaVersion);
        Assert.Equal("mix-audio", preview.Operation);
        Assert.True(preview.IsPreview);
        Assert.True(preview.PathsResolved);
        Assert.Equal("ffmpeg", preview.CommandPlan.ToolName);
        Assert.Single(preview.ProducedPaths);
        Assert.Equal(Path.GetFullPath("mixed.wav"), preview.ProducedPaths[0]);
        Assert.Empty(preview.SideEffects);
    }

    [Fact]
    public void BuildRenderPreview_UsesTimelineBuilderForSchemaV2()
    {
        var builder = new EditPlanExecutionPreviewBuilder();
        var request = new EditPlanRenderRequest
        {
            Plan = new EditPlan
            {
                SchemaVersion = SchemaVersions.V2,
                Source = new EditPlanSource
                {
                    InputPath = Path.GetFullPath("input.mp4")
                },
                Timeline = new EditPlanTimeline
                {
                    Duration = TimeSpan.FromSeconds(3),
                    Resolution = new TimelineResolution
                    {
                        W = 1920,
                        H = 1080
                    },
                    FrameRate = 30,
                    Tracks =
                    [
                        new TimelineTrack
                        {
                            Id = "main",
                            Kind = TrackKind.Video,
                            Clips =
                            [
                                new TimelineClip
                                {
                                    Id = "clip-001",
                                    Start = TimeSpan.Zero,
                                    InPoint = TimeSpan.Zero,
                                    OutPoint = TimeSpan.FromSeconds(3)
                                }
                            ]
                        }
                    ]
                },
                Output = new EditOutputPlan
                {
                    Path = Path.GetFullPath("timeline-final.mp4"),
                    Container = "mp4"
                }
            }
        };

        var preview = builder.BuildRenderPreview(request);

        Assert.Equal(SchemaVersions.V2, preview.CommandPlan.SchemaVersion);
        Assert.Contains("[v_out]", preview.CommandPlan.Arguments);
        Assert.Contains(Path.GetFullPath("timeline-final.mp4"), preview.ProducedPaths);
    }
}
