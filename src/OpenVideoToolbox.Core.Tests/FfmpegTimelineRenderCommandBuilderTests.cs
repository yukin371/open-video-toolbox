using OpenVideoToolbox.Core;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfmpegTimelineRenderCommandBuilderTests
{
    [Fact]
    public void Build_ReturnsCommandPlanForMinimalVideoTimeline()
    {
        var builder = new FfmpegTimelineRenderCommandBuilder();
        var request = new EditPlanRenderRequest
        {
            Plan = CreateTimelinePlan()
        };

        var commandPlan = builder.Build(request, "ffmpeg-custom");

        Assert.Equal(SchemaVersions.V2, commandPlan.SchemaVersion);
        Assert.Equal("ffmpeg", commandPlan.ToolName);
        Assert.Equal("ffmpeg-custom", commandPlan.ExecutablePath);
        Assert.Contains("-filter_complex", commandPlan.Arguments);
        Assert.Contains("-map", commandPlan.Arguments);
        Assert.Contains("[v_out]", commandPlan.Arguments);
        Assert.Contains("-an", commandPlan.Arguments);
        Assert.Contains("libx264", commandPlan.Arguments);
        Assert.Contains("+faststart", commandPlan.Arguments);
        Assert.Contains("trim=start=0.000:end=3.000,setpts=PTS-STARTPTS", commandPlan.Arguments.Single(argument => argument.Contains("trim=start=0.000:end=3.000", StringComparison.Ordinal)));
    }

    [Fact]
    public void Build_AppliesTemplatesAndCompositesTracks()
    {
        var builder = new FfmpegTimelineRenderCommandBuilder();
        var request = new EditPlanRenderRequest
        {
            Plan = CreateCompositeTimelinePlan()
        };

        var commandPlan = builder.Build(request);
        var filterGraph = commandPlan.Arguments.Single(argument => argument.Contains("overlay=eof_action=pass", StringComparison.Ordinal));

        Assert.Contains("eq=brightness=0.1:contrast=1", filterGraph, StringComparison.Ordinal);
        Assert.Contains("volume=-6dB", filterGraph, StringComparison.Ordinal);
        Assert.Contains("overlay=eof_action=pass", filterGraph, StringComparison.Ordinal);
        Assert.Contains("amix=inputs=2:duration=longest", filterGraph, StringComparison.Ordinal);
        Assert.Contains("xfade=transition=fade:duration=0.5:offset=2.5", filterGraph, StringComparison.Ordinal);

        var logoInputIndex = commandPlan.Arguments
            .Select((argument, index) => new { argument, index })
            .Single(item => item.argument.EndsWith("logo.png", StringComparison.OrdinalIgnoreCase))
            .index;
        Assert.True(logoInputIndex >= 0);
        Assert.Equal("-i", commandPlan.Arguments[logoInputIndex - 1]);
        Assert.Equal("30", commandPlan.Arguments[logoInputIndex - 2]);
        Assert.Equal("-framerate", commandPlan.Arguments[logoInputIndex - 3]);
        Assert.Equal("1", commandPlan.Arguments[logoInputIndex - 4]);
        Assert.Equal("-loop", commandPlan.Arguments[logoInputIndex - 5]);
    }

    private static EditPlan CreateTimelinePlan()
    {
        return new EditPlan
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
        };
    }

    private static EditPlan CreateCompositeTimelinePlan()
    {
        return new EditPlan
        {
            SchemaVersion = SchemaVersions.V2,
            Source = new EditPlanSource
            {
                InputPath = Path.GetFullPath("input.mp4")
            },
            Timeline = new EditPlanTimeline
            {
                Duration = TimeSpan.FromSeconds(5),
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
                                OutPoint = TimeSpan.FromSeconds(3),
                                Effects =
                                [
                                    new TimelineEffect
                                    {
                                        Type = "brightness_contrast",
                                        Extensions = new Dictionary<string, System.Text.Json.JsonElement>
                                        {
                                            ["brightness"] = System.Text.Json.JsonSerializer.SerializeToElement(0.1)
                                        }
                                    }
                                ],
                                Transitions = new ClipTransitions
                                {
                                    Out = new Transition
                                    {
                                        Type = "fade",
                                        Duration = 0.5
                                    }
                                }
                            },
                            new TimelineClip
                            {
                                Id = "clip-002",
                                Start = TimeSpan.FromSeconds(2.5),
                                InPoint = TimeSpan.Zero,
                                OutPoint = TimeSpan.FromSeconds(2),
                                Transitions = new ClipTransitions
                                {
                                    In = new Transition
                                    {
                                        Type = "fade",
                                        Duration = 0.5
                                    }
                                }
                            }
                        ]
                    },
                    new TimelineTrack
                    {
                        Id = "logo",
                        Kind = TrackKind.Video,
                        Clips =
                        [
                            new TimelineClip
                            {
                                Id = "logo-001",
                                Src = Path.GetFullPath("logo.png"),
                                Start = TimeSpan.Zero,
                                Duration = TimeSpan.FromSeconds(5)
                            }
                        ]
                    },
                    new TimelineTrack
                    {
                        Id = "voice",
                        Kind = TrackKind.Audio,
                        Clips =
                        [
                            new TimelineClip
                            {
                                Id = "voice-001",
                                Src = Path.GetFullPath("voice.wav"),
                                Start = TimeSpan.Zero,
                                InPoint = TimeSpan.Zero,
                                OutPoint = TimeSpan.FromSeconds(5)
                            }
                        ]
                    },
                    new TimelineTrack
                    {
                        Id = "bgm",
                        Kind = TrackKind.Audio,
                        Clips =
                        [
                            new TimelineClip
                            {
                                Id = "bgm-001",
                                Src = Path.GetFullPath("bgm.wav"),
                                Start = TimeSpan.Zero,
                                InPoint = TimeSpan.Zero,
                                OutPoint = TimeSpan.FromSeconds(5),
                                Effects =
                                [
                                    new TimelineEffect
                                    {
                                        Type = "volume",
                                        Extensions = new Dictionary<string, System.Text.Json.JsonElement>
                                        {
                                            ["gainDb"] = System.Text.Json.JsonSerializer.SerializeToElement(-6)
                                        }
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            Output = new EditOutputPlan
            {
                Path = Path.GetFullPath("timeline-composite.mp4"),
                Container = "mp4"
            }
        };
    }
}
