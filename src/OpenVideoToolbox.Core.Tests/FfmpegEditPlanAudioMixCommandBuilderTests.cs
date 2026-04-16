using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfmpegEditPlanAudioMixCommandBuilderTests
{
    [Fact]
    public void Build_CreatesDeterministicMixAudioCommandPlan()
    {
        var builder = new FfmpegEditPlanAudioMixCommandBuilder();
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
                        InPoint = TimeSpan.FromSeconds(1),
                        OutPoint = TimeSpan.FromSeconds(3)
                    },
                    new EditClip
                    {
                        Id = "clip-002",
                        InPoint = TimeSpan.FromSeconds(5),
                        OutPoint = TimeSpan.FromSeconds(7.5)
                    }
                ],
                AudioTracks =
                [
                    new AudioTrackMix
                    {
                        Id = "bgm-01",
                        Role = AudioTrackRole.Bgm,
                        Path = "bgm.wav",
                        Start = TimeSpan.FromSeconds(0.5),
                        GainDb = -10
                    }
                ],
                Output = new EditOutputPlan
                {
                    Path = "final.mp4",
                    Container = "mp4"
                }
            },
            OutputPath = Path.Combine("output", "mixed.wav"),
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
        Assert.Equal("[a_mix]", plan.Arguments[8]);
        Assert.Equal("-vn", plan.Arguments[9]);
        Assert.Equal("-c:a", plan.Arguments[10]);
        Assert.Equal("pcm_s16le", plan.Arguments[11]);
        Assert.Equal(Path.Combine("output", "mixed.wav"), plan.Arguments[^1]);
        Assert.Contains("atrim=start=1.000:end=3.000", plan.Arguments[6]);
        Assert.Contains("atrim=start=5.000:end=7.500", plan.Arguments[6]);
        Assert.Contains("concat=n=2:v=0:a=1[a_base]", plan.Arguments[6]);
        Assert.Contains("adelay=500:all=1", plan.Arguments[6]);
        Assert.Contains("volume=-10dB", plan.Arguments[6]);
        Assert.Contains("amix=inputs=2:normalize=0:duration=longest[a_mix]", plan.Arguments[6]);
    }

    [Fact]
    public void Build_ThrowsForUnsupportedOutputExtension()
    {
        var builder = new FfmpegEditPlanAudioMixCommandBuilder();
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
                        OutPoint = TimeSpan.FromSeconds(2)
                    }
                ],
                Output = new EditOutputPlan
                {
                    Path = "final.mp4",
                    Container = "mp4"
                }
            },
            OutputPath = "mixed.ogg"
        };

        Assert.Throws<InvalidOperationException>(() => builder.Build(request));
    }
}
