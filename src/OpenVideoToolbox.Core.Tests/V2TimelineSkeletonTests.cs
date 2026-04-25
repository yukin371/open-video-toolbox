using System.Text.Json;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Serialization;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class V2TimelineSkeletonTests
{
    [Fact]
    public void EditPlan_RoundTrips_WithTimelineAndOptionalClipSource()
    {
        var plan = new EditPlan
        {
            SchemaVersion = SchemaVersions.V2,
            Source = new EditPlanSource
            {
                InputPath = "input.mp4"
            },
            Timeline = new EditPlanTimeline
            {
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
                                InPoint = TimeSpan.FromSeconds(1),
                                OutPoint = TimeSpan.FromSeconds(3)
                            },
                            new TimelineClip
                            {
                                Id = "logo-001",
                                Src = "logo.png",
                                Start = TimeSpan.FromSeconds(1),
                                Duration = TimeSpan.FromSeconds(2)
                            }
                        ]
                    }
                ]
            },
            Output = new EditOutputPlan
            {
                Path = "final.mp4",
                Container = "mp4"
            }
        };

        var json = JsonSerializer.Serialize(plan, OpenVideoToolboxJson.Shared);
        var restored = JsonSerializer.Deserialize<EditPlan>(json, OpenVideoToolboxJson.Shared);

        Assert.Contains("\"schemaVersion\": 2", json);
        Assert.Contains("\"timeline\":", json);
        Assert.NotNull(restored);
        Assert.NotNull(restored!.Timeline);
        Assert.Equal(TimeSpan.FromSeconds(1), restored.Timeline!.Tracks[0].Clips[0].InPoint);
        Assert.Null(restored.Timeline.Tracks[0].Clips[0].Src);
        Assert.Equal(TimeSpan.FromSeconds(2), restored.Timeline.Tracks[0].Clips[1].Duration);
    }

    [Fact]
    public void Validate_ReturnsValidForMinimalSchemaV2Plan()
    {
        var validator = new EditPlanValidator();
        var plan = CreateSchemaV2Plan();

        var result = validator.Validate(plan);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_ReturnsErrorsForInvalidSchemaV2Timeline()
    {
        var validator = new EditPlanValidator();
        var plan = new EditPlan
        {
            SchemaVersion = SchemaVersions.V2,
            Source = new EditPlanSource
            {
                InputPath = "input.mp4"
            },
            Timeline = new EditPlanTimeline
            {
                Resolution = new TimelineResolution
                {
                    W = 0,
                    H = 1080
                },
                FrameRate = 0,
                Tracks =
                [
                    new TimelineTrack
                    {
                        Id = "main",
                        Kind = TrackKind.Video,
                        Effects =
                        [
                            new TimelineEffect
                            {
                                Type = "auto_ducking",
                                Extensions = new Dictionary<string, JsonElement>
                                {
                                    ["reference"] = JsonDocument.Parse("\"missing-track\"").RootElement.Clone()
                                }
                            }
                        ],
                        Clips =
                        [
                            new TimelineClip
                            {
                                Id = "clip-001",
                                Start = TimeSpan.FromSeconds(-1),
                                Duration = TimeSpan.FromSeconds(-2),
                                Transitions = new ClipTransitions
                                {
                                    In = new Transition
                                    {
                                        Type = "fade",
                                        Duration = 3
                                    }
                                }
                            },
                            new TimelineClip
                            {
                                Id = "clip-001",
                                Start = TimeSpan.FromSeconds(1)
                            }
                        ]
                    },
                    new TimelineTrack
                    {
                        Id = "main",
                        Kind = TrackKind.Audio,
                        Clips = []
                    }
                ]
            },
            Output = new EditOutputPlan
            {
                Path = "final.mp4",
                Container = "mp4"
            }
        };

        var result = validator.Validate(plan);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "timeline.resolution.invalid");
        Assert.Contains(result.Issues, issue => issue.Code == "timeline.frameRate.invalid");
        Assert.Contains(result.Issues, issue => issue.Code == "timeline.track.id.duplicate");
        Assert.Contains(result.Issues, issue => issue.Code == "timeline.clip.id.duplicate");
        Assert.Contains(result.Issues, issue => issue.Code == "timeline.clip.start.invalid");
        Assert.Contains(result.Issues, issue => issue.Code == "timeline.clip.duration.invalid");
        Assert.Contains(result.Issues, issue => issue.Code == "timeline.clip.video.range.required");
        Assert.Contains(result.Issues, issue => issue.Code == "timeline.transition.duration.exceedsClip");
        Assert.Contains(result.Issues, issue => issue.Code == "timeline.effect.reference.track.missing");
    }

    [Fact]
    public void EditPlan_RoundTrips_WithPlaceholderClip()
    {
        var plan = CreateSchemaV2Plan() with
        {
            Timeline = CreateSchemaV2Plan().Timeline! with
            {
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
                                Id = "placeholder-001",
                                Start = TimeSpan.Zero,
                                Duration = TimeSpan.FromSeconds(2),
                                Placeholder = new TimelineClipPlaceholder
                                {
                                    Kind = "color",
                                    Color = "black"
                                }
                            }
                        ]
                    }
                ]
            }
        };

        var json = JsonSerializer.Serialize(plan, OpenVideoToolboxJson.Shared);
        var restored = JsonSerializer.Deserialize<EditPlan>(json, OpenVideoToolboxJson.Shared);

        Assert.Contains("\"placeholder\":", json);
        Assert.NotNull(restored);
        Assert.Equal("color", restored!.Timeline!.Tracks[0].Clips[0].Placeholder!.Kind);
        Assert.Equal("black", restored.Timeline.Tracks[0].Clips[0].Placeholder!.Color);
    }

    [Fact]
    public void Validate_ReturnsValidForColorPlaceholderTimelineClip()
    {
        var validator = new EditPlanValidator();
        var plan = CreateSchemaV2Plan() with
        {
            Timeline = new EditPlanTimeline
            {
                Duration = TimeSpan.FromSeconds(4),
                Resolution = new TimelineResolution
                {
                    W = 1280,
                    H = 720
                },
                FrameRate = 24,
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
                                Id = "placeholder-001",
                                Start = TimeSpan.Zero,
                                Duration = TimeSpan.FromSeconds(4),
                                Placeholder = new TimelineClipPlaceholder
                                {
                                    Kind = "color",
                                    Color = "black"
                                }
                            }
                        ]
                    }
                ]
            }
        };

        var result = validator.Validate(plan);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_ReturnsErrorsForInvalidPlaceholderTimelineClip()
    {
        var validator = new EditPlanValidator();
        var plan = CreateSchemaV2Plan() with
        {
            Timeline = new EditPlanTimeline
            {
                Duration = TimeSpan.FromSeconds(5),
                FrameRate = 24,
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
                                Id = "placeholder-unknown",
                                Start = TimeSpan.Zero,
                                Duration = TimeSpan.FromSeconds(2),
                                Placeholder = new TimelineClipPlaceholder
                                {
                                    Kind = "gradient",
                                    Color = "black"
                                }
                            },
                            new TimelineClip
                            {
                                Id = "placeholder-conflict",
                                Src = "conflict.mp4",
                                Start = TimeSpan.FromSeconds(2),
                                InPoint = TimeSpan.Zero,
                                OutPoint = TimeSpan.FromSeconds(1),
                                Placeholder = new TimelineClipPlaceholder
                                {
                                    Kind = "color"
                                }
                            }
                        ]
                    }
                ]
            }
        };

        var result = validator.Validate(plan);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "timeline.placeholder.resolution.required");
        Assert.Contains(result.Issues, issue => issue.Code == "timeline.clip.placeholder.kind.unsupported");
        Assert.Contains(result.Issues, issue => issue.Code == "timeline.clip.placeholder.src.conflict");
        Assert.Contains(result.Issues, issue => issue.Code == "timeline.clip.placeholder.range.conflict");
        Assert.Contains(result.Issues, issue => issue.Code == "timeline.clip.placeholder.color.required");
        Assert.Contains(result.Issues, issue => issue.Code == "timeline.clip.placeholder.duration.required");
    }

    [Fact]
    public void Validate_ReturnsWarningForUnknownTimelineEffect()
    {
        var validator = new EditPlanValidator();
        var registry = new EffectRegistry();
        registry.Register(new FakeEffectDefinition("fade", EffectCategory.Transition));
        var basePlan = CreateSchemaV2Plan();
        var baseTimeline = Assert.IsType<EditPlanTimeline>(basePlan.Timeline);
        var baseTrack = baseTimeline.Tracks[0];
        var plan = basePlan with
        {
            Timeline = baseTimeline with
            {
                Tracks =
                [
                    baseTrack with
                    {
                        Effects =
                        [
                            new TimelineEffect
                            {
                                Type = "unknown_effect"
                            }
                        ]
                    }
                ]
            }
        };

        var result = validator.Validate(plan, effectRegistry: registry);

        Assert.True(result.IsValid);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(EditPlanValidationSeverity.Warning, issue.Severity);
        Assert.Equal("timeline.effect.type.unknown", issue.Code);
        Assert.Equal("timeline", issue.Category);
    }

    [Fact]
    public void Validate_ReturnsErrorsForSchemaVersionAndTimelineMismatch()
    {
        var validator = new EditPlanValidator();
        var schemaV2WithoutTimeline = CreateSchemaV2Plan() with
        {
            Timeline = null
        };
        var schemaV1WithTimeline = CreateSchemaV2Plan() with
        {
            SchemaVersion = SchemaVersions.V1
        };

        var missingTimeline = validator.Validate(schemaV2WithoutTimeline);
        var mismatchedTimeline = validator.Validate(schemaV1WithTimeline);

        Assert.Contains(missingTimeline.Issues, issue => issue.Code == "timeline.required");
        Assert.Contains(mismatchedTimeline.Issues, issue => issue.Code == "timeline.schemaVersion.mismatch");
    }

    private sealed record FakeEffectDefinition(string Type, string Category) : IEffectDefinition
    {
        public string DisplayName => Type;

        public string? Description => null;

        public EffectParameterSchema Parameters { get; } = new()
        {
            Items = new Dictionary<string, EffectParameterDescriptor>()
        };

        public FfmpegFilterTemplateSet? FfmpegTemplates => null;
    }

    private static EditPlan CreateSchemaV2Plan()
    {
        return new EditPlan
        {
            SchemaVersion = SchemaVersions.V2,
            Source = new EditPlanSource
            {
                InputPath = "input.mp4"
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
                                InPoint = TimeSpan.FromSeconds(1),
                                OutPoint = TimeSpan.FromSeconds(3)
                            }
                        ]
                    }
                ]
            },
            Output = new EditOutputPlan
            {
                Path = "final.mp4",
                Container = "mp4"
            }
        };
    }
}
