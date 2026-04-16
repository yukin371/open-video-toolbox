using OpenVideoToolbox.Core.Editing;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class EditPlanPathResolverTests
{
    [Fact]
    public void ResolvePaths_ResolvesRelativePathsAgainstPlanDirectory()
    {
        var baseDirectory = Path.Combine(Path.GetTempPath(), "ovt-edit-plan");
        var plan = new EditPlan
        {
            Source = new EditPlanSource
            {
                InputPath = "input/source.mp4"
            },
            Template = new EditTemplateReference
            {
                Id = "shorts-basic"
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
                    Path = "audio/bgm.wav",
                    Start = TimeSpan.FromSeconds(1)
                }
            ],
            Artifacts =
            [
                new EditArtifactReference
                {
                    SlotId = "subtitles",
                    Kind = "subtitle",
                    Path = "artifacts/subs/captions.srt"
                }
            ],
            Transcript = new EditTranscriptPlan
            {
                Path = "text/transcript.json",
                Language = "en",
                SegmentCount = 2
            },
            Beats = new EditBeatTrackPlan
            {
                Path = "beats/beats.json",
                EstimatedBpm = 98
            },
            Subtitles = new EditSubtitlePlan
            {
                Path = "subs/captions.srt",
                Mode = SubtitleMode.Sidecar
            },
            Output = new EditOutputPlan
            {
                Path = "output/final.mp4",
                Container = "mp4"
            }
        };

        var resolved = EditPlanPathResolver.ResolvePaths(plan, baseDirectory);

        Assert.Equal(Path.GetFullPath(Path.Combine(baseDirectory, "input/source.mp4")), resolved.Source.InputPath);
        Assert.Equal(Path.GetFullPath(Path.Combine(baseDirectory, "audio/bgm.wav")), resolved.AudioTracks[0].Path);
        Assert.Equal(Path.GetFullPath(Path.Combine(baseDirectory, "artifacts/subs/captions.srt")), resolved.Artifacts[0].Path);
        Assert.Equal(Path.GetFullPath(Path.Combine(baseDirectory, "text/transcript.json")), resolved.Transcript!.Path);
        Assert.Equal(Path.GetFullPath(Path.Combine(baseDirectory, "beats/beats.json")), resolved.Beats!.Path);
        Assert.Equal(Path.GetFullPath(Path.Combine(baseDirectory, "subs/captions.srt")), resolved.Subtitles!.Path);
        Assert.Equal(Path.GetFullPath(Path.Combine(baseDirectory, "output/final.mp4")), resolved.Output.Path);
        Assert.Equal("shorts-basic", resolved.Template!.Id);
    }
}
