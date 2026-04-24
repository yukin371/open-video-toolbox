using OpenVideoToolbox.Core.Editing;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class EditPlanMaterialAttacherTests
{
    [Fact]
    public void Attach_TranscriptUsesRelativePathWhenTargetWasMissing()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-attach-core-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var transcriptPath = Path.Combine(outputDirectory, "signals", "transcript.json");
            Directory.CreateDirectory(Path.GetDirectoryName(transcriptPath)!);
            File.WriteAllText(transcriptPath, "{}");

            var plan = new EditPlan
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
            };

            var result = new EditPlanMaterialAttacher().Attach(
                plan,
                outputDirectory,
                new EditPlanMaterialAttachmentRequest
                {
                    Target = new EditPlanInspectionTargetSelector
                    {
                        Singleton = EditPlanInspectionTargetKeys.Transcript
                    },
                    ResolvedPath = transcriptPath,
                    PathStyle = EditPlanPathWriteStyle.Auto
                });

            Assert.True(result.Added);
            Assert.Null(result.PreviousPath);
            Assert.Equal(Path.Combine("signals", "transcript.json"), result.NextPath);
            Assert.Equal(EditPlanPathWriteStyle.Relative, result.PathStyleApplied);
            Assert.Equal(Path.Combine("signals", "transcript.json"), result.UpdatedPlan.Transcript!.Path);
        }
        finally
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Fact]
    public void Attach_ArtifactSlotCanUpsertDeclaredTemplateSlot()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-attach-artifact-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var bgmPath = Path.Combine(outputDirectory, "audio", "updated.wav");
            Directory.CreateDirectory(Path.GetDirectoryName(bgmPath)!);
            File.WriteAllText(bgmPath, "audio");

            var plan = new EditPlan
            {
                Source = new EditPlanSource
                {
                    InputPath = "input.mp4"
                },
                Template = new EditTemplateReference
                {
                    Id = "commentary-bgm"
                },
                Artifacts =
                [
                    new EditArtifactReference
                    {
                        SlotId = "bgm",
                        Kind = "audio",
                        Path = "audio/original.wav"
                    }
                ],
                Output = new EditOutputPlan
                {
                    Path = "final.mp4",
                    Container = "mp4"
                }
            };

            var result = new EditPlanMaterialAttacher().Attach(
                plan,
                outputDirectory,
                new EditPlanMaterialAttachmentRequest
                {
                    Target = new EditPlanInspectionTargetSelector
                    {
                        ArtifactSlot = "bgm"
                    },
                    ResolvedPath = bgmPath,
                    PathStyle = EditPlanPathWriteStyle.Relative
                },
                BuiltInEditPlanTemplateCatalog.GetAll());

            Assert.False(result.Added);
            Assert.Equal("audio/original.wav", result.PreviousPath);
            Assert.Equal(Path.Combine("audio", "updated.wav"), result.NextPath);
            Assert.Single(result.UpdatedPlan.Artifacts);
            Assert.Equal(Path.Combine("audio", "updated.wav"), result.UpdatedPlan.Artifacts[0].Path);
            Assert.Equal("audio", result.UpdatedPlan.Artifacts[0].Kind);
        }
        finally
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Fact]
    public void Attach_AudioTrackCanAddNewTrackWhenRoleIsProvided()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-attach-audio-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var audioPath = Path.Combine(outputDirectory, "audio", "dub.wav");
            Directory.CreateDirectory(Path.GetDirectoryName(audioPath)!);
            File.WriteAllText(audioPath, "audio");

            var plan = new EditPlan
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
            };

            var result = new EditPlanMaterialAttacher().Attach(
                plan,
                outputDirectory,
                new EditPlanMaterialAttachmentRequest
                {
                    Target = new EditPlanInspectionTargetSelector
                    {
                        AudioTrackId = "voice-main"
                    },
                    ResolvedPath = audioPath,
                    PathStyle = EditPlanPathWriteStyle.Relative,
                    AudioTrackRole = AudioTrackRole.Voice
                });

            Assert.True(result.Added);
            Assert.Null(result.PreviousPath);
            Assert.Null(result.PreviousAudioTrackRole);
            Assert.Equal(AudioTrackRole.Voice, result.NextAudioTrackRole);
            Assert.Single(result.UpdatedPlan.AudioTracks);
            Assert.Equal("voice-main", result.UpdatedPlan.AudioTracks[0].Id);
            Assert.Equal(AudioTrackRole.Voice, result.UpdatedPlan.AudioTracks[0].Role);
            Assert.Equal(Path.Combine("audio", "dub.wav"), result.UpdatedPlan.AudioTracks[0].Path);
        }
        finally
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }
}
