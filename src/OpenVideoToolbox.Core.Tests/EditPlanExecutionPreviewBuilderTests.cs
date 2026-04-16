using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
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
                    Path = "final.mp4",
                    Container = "mp4"
                }
            }
        };

        var preview = builder.BuildRenderPreview(request);

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
            OutputPath = "mixed.wav"
        };

        var preview = builder.BuildAudioMixPreview(request);

        Assert.Equal("ffmpeg", preview.CommandPlan.ToolName);
        Assert.Single(preview.ProducedPaths);
        Assert.Equal("mixed.wav", preview.ProducedPaths[0]);
        Assert.Empty(preview.SideEffects);
    }
}
