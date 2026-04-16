using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfmpegEditPlanRenderCommandBuilderTests
{
    [Fact]
    public void Build_CreatesDeterministicRenderCommandPlan()
    {
        var builder = new FfmpegEditPlanRenderCommandBuilder();
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
                        InPoint = TimeSpan.FromSeconds(2),
                        OutPoint = TimeSpan.FromSeconds(5)
                    },
                    new EditClip
                    {
                        Id = "clip-002",
                        InPoint = TimeSpan.FromSeconds(8),
                        OutPoint = TimeSpan.FromSeconds(11.5)
                    }
                ],
                AudioTracks =
                [
                    new AudioTrackMix
                    {
                        Id = "bgm-01",
                        Role = AudioTrackRole.Bgm,
                        Path = "bgm.wav",
                        Start = TimeSpan.FromSeconds(1.25),
                        GainDb = -8
                    }
                ],
                Subtitles = new EditSubtitlePlan
                {
                    Path = Path.Combine(Path.GetTempPath(), "captions.srt"),
                    Mode = SubtitleMode.BurnIn
                },
                Output = new EditOutputPlan
                {
                    Path = Path.Combine("output", "final.mp4"),
                    Container = "mp4"
                }
            },
            OverwriteExisting = true
        };

        var plan = builder.Build(request);

        Assert.Equal("ffmpeg", plan.ToolName);
        Assert.Equal("-y", plan.Arguments[0]);
        Assert.Equal("-i", plan.Arguments[1]);
        Assert.Equal("input.mp4", plan.Arguments[2]);
        Assert.Equal("-i", plan.Arguments[3]);
        Assert.Equal("bgm.wav", plan.Arguments[4]);
        Assert.Equal("-filter_complex", plan.Arguments[5]);
        Assert.Equal("-map", plan.Arguments[7]);
        Assert.Equal("[v_burn]", plan.Arguments[8]);
        Assert.Equal("-map", plan.Arguments[9]);
        Assert.Equal("[a_mix]", plan.Arguments[10]);
        Assert.Equal(Path.Combine("output", "final.mp4"), plan.Arguments[^1]);
        Assert.Contains("trim=start=2.000:end=5.000", plan.Arguments[6]);
        Assert.Contains("trim=start=8.000:end=11.500", plan.Arguments[6]);
        Assert.Contains("concat=n=2:v=1:a=1[v_base][a_base]", plan.Arguments[6]);
        Assert.Contains("adelay=1250:all=1", plan.Arguments[6]);
        Assert.Contains("volume=-8dB", plan.Arguments[6]);
        Assert.Contains("amix=inputs=2:normalize=0:duration=longest[a_mix]", plan.Arguments[6]);
        Assert.Contains("subtitles='", plan.Arguments[6]);
    }

    [Fact]
    public void Build_Throws_WhenPlanHasNoClips()
    {
        var builder = new FfmpegEditPlanRenderCommandBuilder();
        var request = new EditPlanRenderRequest
        {
            Plan = new EditPlan
            {
                Source = new EditPlanSource
                {
                    InputPath = "input.mp4"
                },
                Output = new EditOutputPlan
                {
                    Path = "final.mp4",
                    Container = "mp4"
                }
            }
        };

        Assert.Throws<ArgumentException>(() => builder.Build(request));
    }
}
