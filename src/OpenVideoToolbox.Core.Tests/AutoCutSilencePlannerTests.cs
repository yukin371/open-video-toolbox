using OpenVideoToolbox.Core.Audio;
using OpenVideoToolbox.Core.Editing;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class AutoCutSilencePlannerTests
{
    [Fact]
    public void BuildClips_InvertsSilenceIntoStableClipRanges()
    {
        var planner = new AutoCutSilencePlanner();

        var result = planner.BuildClips(new AutoCutSilenceRequest
        {
            SourcePath = "input.mp4",
            SourceDuration = TimeSpan.FromSeconds(15),
            Silence = CreateSilenceDocument((3.5, 5.2), (8.0, 10.5)),
            Padding = TimeSpan.Zero,
            MergeGap = TimeSpan.Zero,
            MinClipDuration = TimeSpan.Zero
        });

        Assert.Collection(
            result.Clips,
            clip =>
            {
                Assert.Equal("clip-001", clip.Id);
                Assert.Equal(TimeSpan.Zero, clip.InPoint);
                Assert.Equal(TimeSpan.FromSeconds(3.5), clip.OutPoint);
            },
            clip =>
            {
                Assert.Equal("clip-002", clip.Id);
                Assert.Equal(TimeSpan.FromSeconds(5.2), clip.InPoint);
                Assert.Equal(TimeSpan.FromSeconds(8), clip.OutPoint);
            },
            clip =>
            {
                Assert.Equal("clip-003", clip.Id);
                Assert.Equal(TimeSpan.FromSeconds(10.5), clip.InPoint);
                Assert.Equal(TimeSpan.FromSeconds(15), clip.OutPoint);
            });
    }

    [Fact]
    public void BuildClips_AppliesPaddingClampAndMergeGap()
    {
        var planner = new AutoCutSilencePlanner();

        var result = planner.BuildClips(new AutoCutSilenceRequest
        {
            SourcePath = "input.mp4",
            SourceDuration = TimeSpan.FromSeconds(10),
            Silence = CreateSilenceDocument((2.0, 2.2), (4.0, 4.2)),
            Padding = TimeSpan.FromMilliseconds(250),
            MergeGap = TimeSpan.FromMilliseconds(750),
            MinClipDuration = TimeSpan.Zero
        });

        var clip = Assert.Single(result.Clips);
        Assert.Equal("clip-001", clip.Id);
        Assert.Equal(TimeSpan.Zero, clip.InPoint);
        Assert.Equal(TimeSpan.FromSeconds(10), clip.OutPoint);
    }

    [Fact]
    public void BuildClips_FiltersSegmentsShorterThanMinimumDuration()
    {
        var planner = new AutoCutSilencePlanner();

        var result = planner.BuildClips(new AutoCutSilenceRequest
        {
            SourcePath = "input.mp4",
            SourceDuration = TimeSpan.FromSeconds(8),
            Silence = CreateSilenceDocument((1.0, 1.2), (3.0, 6.8)),
            Padding = TimeSpan.Zero,
            MergeGap = TimeSpan.Zero,
            MinClipDuration = TimeSpan.FromSeconds(1.5)
        });

        var clip = Assert.Single(result.Clips);
        Assert.Equal("clip-001", clip.Id);
        Assert.Equal(TimeSpan.FromSeconds(1.2), clip.InPoint);
        Assert.Equal(TimeSpan.FromSeconds(3.0), clip.OutPoint);
        Assert.Equal(1, result.Stats.GeneratedClipCount);
    }

    [Fact]
    public void BuildPlan_CreatesSchemaV1EditPlan()
    {
        var planner = new AutoCutSilencePlanner();

        var result = planner.BuildPlan(new AutoCutSilenceRequest
        {
            SourcePath = "C:\\work\\input.mp4",
            SourceDuration = TimeSpan.FromSeconds(12),
            Silence = CreateSilenceDocument((4.0, 5.0)),
            Padding = TimeSpan.Zero,
            MergeGap = TimeSpan.Zero,
            MinClipDuration = TimeSpan.Zero,
            TemplateId = "shorts-basic",
            RenderOutputPath = "output/final.mp4"
        });

        Assert.NotNull(result.Plan);
        Assert.Equal(1, result.Plan!.SchemaVersion);
        Assert.Equal("C:\\work\\input.mp4", result.Plan.Source.InputPath);
        Assert.Equal("shorts-basic", result.Plan.Template!.Id);
        Assert.Equal("output/final.mp4", result.Plan.Output.Path);
        Assert.Equal("mp4", result.Plan.Output.Container);
        Assert.Equal(result.Clips, result.Plan.Clips);
    }

    [Fact]
    public void BuildPlan_UsesV2TemplateToCreateTimelinePlan()
    {
        var planner = new AutoCutSilencePlanner();

        var result = planner.BuildPlan(new AutoCutSilenceRequest
        {
            SourcePath = "C:\\work\\input.mp4",
            SourceDuration = TimeSpan.FromSeconds(5),
            Silence = CreateSilenceDocument((2.0, 3.0)),
            Padding = TimeSpan.Zero,
            MergeGap = TimeSpan.Zero,
            MinClipDuration = TimeSpan.Zero,
            TemplateId = "timeline-effects-starter",
            RenderOutputPath = "output/final.mp4"
        });

        Assert.NotNull(result.Plan);
        Assert.Equal(2, result.Plan!.SchemaVersion);
        Assert.Empty(result.Plan.Clips);
        Assert.NotNull(result.Plan.Timeline);
        Assert.Equal("timeline-effects-starter", result.Plan.Template!.Id);

        var videoTrack = Assert.Single(result.Plan.Timeline!.Tracks.Where(track => track.Kind == TrackKind.Video));
        Assert.Equal("main", videoTrack.Id);
        Assert.Equal("scale", videoTrack.Effects[0].Type);
        Assert.Equal(2, videoTrack.Clips.Count);
        Assert.Equal(TimeSpan.Zero, videoTrack.Clips[0].Start);
        Assert.Equal(TimeSpan.Zero, videoTrack.Clips[0].InPoint);
        Assert.Equal(TimeSpan.FromSeconds(2), videoTrack.Clips[0].OutPoint);
        Assert.Equal("brightness_contrast", videoTrack.Clips[0].Effects[0].Type);
        Assert.Equal(TimeSpan.FromSeconds(2), videoTrack.Clips[1].Start);
        Assert.Equal(TimeSpan.FromSeconds(3), videoTrack.Clips[1].InPoint);
        Assert.Equal(TimeSpan.FromSeconds(5), videoTrack.Clips[1].OutPoint);
    }

    private static SilenceDetectionDocument CreateSilenceDocument(params (double Start, double End)[] ranges)
    {
        return new SilenceDetectionDocument
        {
            InputPath = "input.mp4",
            Segments = ranges
                .Select(range => new SilenceSegment
                {
                    Start = TimeSpan.FromSeconds(range.Start),
                    End = TimeSpan.FromSeconds(range.End),
                    Duration = TimeSpan.FromSeconds(range.End - range.Start)
                })
                .ToArray()
        };
    }
}
