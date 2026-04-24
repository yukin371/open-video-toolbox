using OpenVideoToolbox.Core.Editing;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class EditPlanMaterialReplacerTests
{
    [Fact]
    public void Replace_AudioTrackWithRelativePathStyle_RewritesPathRelativeToOutputPlan()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-replace-core-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var replacementPath = Path.Combine(outputDirectory, "audio", "updated.wav");
            Directory.CreateDirectory(Path.GetDirectoryName(replacementPath)!);
            File.WriteAllText(replacementPath, "audio");

            var plan = new EditPlan
            {
                Source = new EditPlanSource
                {
                    InputPath = "input.mp4"
                },
                AudioTracks =
                [
                    new AudioTrackMix
                    {
                        Id = "voice-main",
                        Role = AudioTrackRole.Voice,
                        Path = "audio/original.wav"
                    }
                ],
                Output = new EditOutputPlan
                {
                    Path = "final.mp4",
                    Container = "mp4"
                }
            };

            var result = new EditPlanMaterialReplacer().Replace(
                plan,
                outputDirectory,
                new EditPlanMaterialReplacementRequest
                {
                    Target = new EditPlanInspectionTargetSelector
                    {
                        AudioTrackId = "voice-main"
                    },
                    ResolvedPath = replacementPath,
                    PathStyle = EditPlanPathWriteStyle.Relative
                });

            Assert.Equal("audio/original.wav", result.PreviousPath);
            Assert.Equal(Path.Combine("audio", "updated.wav"), result.NextPath);
            Assert.Equal(EditPlanPathWriteStyle.Relative, result.PathStyleApplied);
            Assert.True(result.Changed);
            Assert.Equal(Path.Combine("audio", "updated.wav"), result.UpdatedPlan.AudioTracks[0].Path);
        }
        finally
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Fact]
    public void Replace_SubtitlesCanUpdateModeAndAutoKeepsAbsolutePathWhenOriginalWasAbsolute()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-replace-subtitles-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var originalPath = Path.Combine(outputDirectory, "subs", "original.srt");
            var replacementPath = Path.Combine(outputDirectory, "subs", "updated.srt");
            Directory.CreateDirectory(Path.GetDirectoryName(originalPath)!);
            File.WriteAllText(originalPath, "1");
            File.WriteAllText(replacementPath, "2");

            var plan = new EditPlan
            {
                Source = new EditPlanSource
                {
                    InputPath = "input.mp4"
                },
                Subtitles = new EditSubtitlePlan
                {
                    Path = originalPath,
                    Mode = SubtitleMode.Sidecar
                },
                Output = new EditOutputPlan
                {
                    Path = "final.mp4",
                    Container = "mp4"
                }
            };

            var result = new EditPlanMaterialReplacer().Replace(
                plan,
                outputDirectory,
                new EditPlanMaterialReplacementRequest
                {
                    Target = new EditPlanInspectionTargetSelector
                    {
                        Singleton = EditPlanInspectionTargetKeys.Subtitles
                    },
                    ResolvedPath = replacementPath,
                    SubtitleMode = SubtitleMode.BurnIn
                });

            Assert.Equal(EditPlanPathWriteStyle.Absolute, result.PathStyleApplied);
            Assert.Equal(originalPath, result.PreviousPath);
            Assert.Equal(replacementPath, result.NextPath);
            Assert.Equal(SubtitleMode.Sidecar, result.PreviousSubtitleMode);
            Assert.Equal(SubtitleMode.BurnIn, result.NextSubtitleMode);
            Assert.True(result.Changed);
            Assert.Equal(replacementPath, result.UpdatedPlan.Subtitles!.Path);
            Assert.Equal(SubtitleMode.BurnIn, result.UpdatedPlan.Subtitles.Mode);
        }
        finally
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }
}
