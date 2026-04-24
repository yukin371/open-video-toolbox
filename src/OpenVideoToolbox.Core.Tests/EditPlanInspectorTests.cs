using OpenVideoToolbox.Core.Editing;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class EditPlanInspectorTests
{
    [Fact]
    public void Inspect_ReturnsSummaryMaterialsAndReplaceTargets()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ovt-inspect-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(Path.Combine(root, "input.mp4"), "video");
            File.WriteAllText(Path.Combine(root, "dub.wav"), "audio");
            File.WriteAllText(Path.Combine(root, "transcript.json"), "{}");
            File.WriteAllText(Path.Combine(root, "subs.srt"), "1");

            var plan = new EditPlan
            {
                Source = new EditPlanSource
                {
                    InputPath = "input.mp4"
                },
                Template = new EditTemplateReference
                {
                    Id = "inspect-template"
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
                        Id = "voice-main",
                        Role = AudioTrackRole.Voice,
                        Path = "dub.wav"
                    }
                ],
                Transcript = new EditTranscriptPlan
                {
                    Path = "transcript.json",
                    SegmentCount = 2
                },
                Subtitles = new EditSubtitlePlan
                {
                    Path = "subs.srt",
                    Mode = SubtitleMode.Sidecar
                },
                Output = new EditOutputPlan
                {
                    Path = "final.mp4",
                    Container = "mp4"
                }
            };

            var template = new EditPlanTemplateDefinition
            {
                Id = "inspect-template",
                DisplayName = "Inspect Template",
                Description = "Inspect template description",
                Category = "test",
                OutputContainer = "mp4",
                DefaultSubtitleMode = SubtitleMode.Sidecar,
                SupportingSignals =
                [
                    new EditPlanSupportingSignalHint
                    {
                        Kind = EditPlanSupportingSignalKind.Transcript,
                        Reason = "Transcript should be available."
                    },
                    new EditPlanSupportingSignalHint
                    {
                        Kind = EditPlanSupportingSignalKind.Beats,
                        Reason = "Beats can be attached later."
                    }
                ]
            };

            var result = new EditPlanInspector().Inspect(plan, root, checkReferencedFiles: true, availableTemplates: [template]);

            Assert.Equal("inspect-template", result.Template!.Id);
            Assert.Equal(1, result.Summary.ClipCount);
            Assert.Equal(1, result.Summary.AudioTrackCount);
            Assert.Equal(0, result.Summary.ArtifactCount);
            Assert.True(result.Summary.HasTranscript);
            Assert.False(result.Summary.HasBeats);
            Assert.True(result.Summary.HasSubtitles);
            Assert.True(result.Validation.IsValid);
            Assert.Empty(result.Validation.Issues);

            Assert.Collection(
                result.Materials,
                material =>
                {
                    Assert.Equal(EditPlanInspectionTargetTypes.Source, material.TargetType);
                    Assert.Equal(EditPlanInspectionTargetKeys.SourceInput, material.TargetKey);
                    Assert.True(material.Exists);
                },
                material =>
                {
                    Assert.Equal(EditPlanInspectionTargetTypes.AudioTrack, material.TargetType);
                    Assert.Equal("audioTrack:voice-main", material.TargetKey);
                    Assert.Equal(AudioTrackRole.Voice, material.Role);
                    Assert.True(material.Exists);
                },
                material =>
                {
                    Assert.Equal(EditPlanInspectionTargetTypes.Transcript, material.TargetType);
                    Assert.Equal(EditPlanInspectionTargetKeys.Transcript, material.TargetKey);
                    Assert.True(material.Exists);
                },
                material =>
                {
                    Assert.Equal(EditPlanInspectionTargetTypes.Subtitles, material.TargetType);
                    Assert.Equal(EditPlanInspectionTargetKeys.Subtitles, material.TargetKey);
                    Assert.Equal(SubtitleMode.Sidecar, material.Mode);
                    Assert.True(material.Exists);
                });

            Assert.Collection(
                result.ReplaceTargets,
                target => Assert.Equal(EditPlanInspectionTargetKeys.SourceInput, target.Selector.Singleton),
                target => Assert.Equal("voice-main", target.Selector.AudioTrackId),
                target => Assert.Equal(EditPlanInspectionTargetKeys.Transcript, target.Selector.Singleton),
                target => Assert.Equal(EditPlanInspectionTargetKeys.Subtitles, target.Selector.Singleton));

            Assert.Collection(
                result.Signals,
                signal =>
                {
                    Assert.Equal(EditPlanInspectionSignalKinds.Transcript, signal.Kind);
                    Assert.True(signal.ExpectedByTemplate);
                    Assert.True(signal.Attached);
                    Assert.Equal(EditPlanInspectionSignalStatuses.AttachedPresent, signal.Status);
                    Assert.Equal(EditPlanInspectionSignalBindingStatuses.Attached, signal.BindingStatus);
                    Assert.Equal(EditPlanInspectionSignalFileStatuses.Present, signal.FileStatus);
                    Assert.True(signal.Exists);
                },
                signal =>
                {
                    Assert.Equal(EditPlanInspectionSignalKinds.Beats, signal.Kind);
                    Assert.True(signal.ExpectedByTemplate);
                    Assert.False(signal.Attached);
                    Assert.Equal(EditPlanInspectionSignalStatuses.ExpectedUnbound, signal.Status);
                    Assert.Equal(EditPlanInspectionSignalBindingStatuses.Unbound, signal.BindingStatus);
                    Assert.Equal(EditPlanInspectionSignalFileStatuses.Unbound, signal.FileStatus);
                    Assert.Null(signal.Exists);
                },
                signal =>
                {
                    Assert.Equal(EditPlanInspectionSignalKinds.Subtitles, signal.Kind);
                    Assert.True(signal.ExpectedByTemplate);
                    Assert.True(signal.Attached);
                    Assert.Equal(EditPlanInspectionSignalStatuses.AttachedPresent, signal.Status);
                    Assert.Equal(EditPlanInspectionSignalBindingStatuses.Attached, signal.BindingStatus);
                    Assert.Equal(EditPlanInspectionSignalFileStatuses.Present, signal.FileStatus);
                    Assert.Equal(SubtitleMode.Sidecar, signal.Mode);
                    Assert.True(signal.Exists);
                });

            Assert.Empty(result.MissingBindings);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Inspect_ReportsMissingRequiredArtifactSlotsAndMissingPaths()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ovt-inspect-missing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var plan = new EditPlan
            {
                Source = new EditPlanSource
                {
                    InputPath = "missing-input.mp4"
                },
                Template = new EditTemplateReference
                {
                    Id = "bgm-template"
                },
                AudioTracks =
                [
                    new AudioTrackMix
                    {
                        Id = "voice-main",
                        Role = AudioTrackRole.Voice,
                        Path = "missing-dub.wav"
                    }
                ],
                Output = new EditOutputPlan
                {
                    Path = "final.mp4",
                    Container = "mp4"
                }
            };

            var template = new EditPlanTemplateDefinition
            {
                Id = "bgm-template",
                DisplayName = "Bgm Template",
                Description = "Requires a bgm slot",
                Category = "test",
                OutputContainer = "mp4",
                ArtifactSlots =
                [
                    new EditPlanArtifactSlot
                    {
                        Id = "bgm",
                        Kind = "audio",
                        Description = "Background music",
                        Required = true
                    }
                ]
            };

            var result = new EditPlanInspector().Inspect(plan, root, checkReferencedFiles: true, availableTemplates: [template]);

            Assert.False(result.Validation.IsValid);
            Assert.Contains(result.Validation.Issues, issue => issue.Code == "source.inputPath.missing");
            Assert.Contains(result.Validation.Issues, issue => issue.Code == "audioTracks.path.missing");
            Assert.Contains(result.Validation.Issues, issue => issue.Code == "artifacts.slot.required");

            Assert.Contains(result.MissingBindings, binding =>
                binding.TargetType == EditPlanInspectionTargetTypes.Source
                && binding.Reason == EditPlanInspectionMissingBindingReasons.PathMissing);
            Assert.Contains(result.MissingBindings, binding =>
                binding.TargetType == EditPlanInspectionTargetTypes.AudioTrack
                && binding.Reason == EditPlanInspectionMissingBindingReasons.PathMissing);
            Assert.Contains(result.MissingBindings, binding =>
                binding.TargetType == EditPlanInspectionTargetTypes.Artifact
                && binding.TargetKey == "artifact:bgm"
                && binding.Reason == EditPlanInspectionMissingBindingReasons.RequiredSlotUnbound);

            Assert.Collection(
                result.Signals,
                signal =>
                {
                    Assert.Equal(EditPlanInspectionSignalKinds.Transcript, signal.Kind);
                    Assert.False(signal.ExpectedByTemplate);
                    Assert.False(signal.Attached);
                    Assert.Equal(EditPlanInspectionSignalStatuses.OptionalUnbound, signal.Status);
                    Assert.Equal(EditPlanInspectionSignalFileStatuses.Unbound, signal.FileStatus);
                },
                signal =>
                {
                    Assert.Equal(EditPlanInspectionSignalKinds.Beats, signal.Kind);
                    Assert.False(signal.ExpectedByTemplate);
                    Assert.False(signal.Attached);
                    Assert.Equal(EditPlanInspectionSignalStatuses.OptionalUnbound, signal.Status);
                    Assert.Equal(EditPlanInspectionSignalFileStatuses.Unbound, signal.FileStatus);
                },
                signal =>
                {
                    Assert.Equal(EditPlanInspectionSignalKinds.Subtitles, signal.Kind);
                    Assert.False(signal.Attached);
                    Assert.False(signal.ExpectedByTemplate);
                    Assert.Equal(EditPlanInspectionSignalStatuses.OptionalUnbound, signal.Status);
                    Assert.Equal(EditPlanInspectionSignalFileStatuses.Unbound, signal.FileStatus);
                });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Inspect_ReportsAttachedSignalStatusForMissingAndUncheckedFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ovt-inspect-signal-status-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(Path.Combine(root, "input.mp4"), "video");

            var plan = new EditPlan
            {
                Source = new EditPlanSource
                {
                    InputPath = "input.mp4"
                },
                Template = new EditTemplateReference
                {
                    Id = "signal-status-template"
                },
                Transcript = new EditTranscriptPlan
                {
                    Path = "missing-transcript.json",
                    SegmentCount = 1
                },
                Output = new EditOutputPlan
                {
                    Path = "final.mp4",
                    Container = "mp4"
                }
            };

            var template = new EditPlanTemplateDefinition
            {
                Id = "signal-status-template",
                DisplayName = "Signal Status Template",
                Description = "Used to inspect signal status variants.",
                Category = "test",
                OutputContainer = "mp4",
                SupportingSignals =
                [
                    new EditPlanSupportingSignalHint
                    {
                        Kind = EditPlanSupportingSignalKind.Transcript,
                        Reason = "Transcript should be available."
                    }
                ]
            };

            var checkedResult = new EditPlanInspector().Inspect(plan, root, checkReferencedFiles: true, availableTemplates: [template]);
            var uncheckedResult = new EditPlanInspector().Inspect(plan, root, checkReferencedFiles: false, availableTemplates: [template]);

            var checkedTranscript = Assert.Single(checkedResult.Signals, signal => signal.Kind == EditPlanInspectionSignalKinds.Transcript);
            Assert.Equal(EditPlanInspectionSignalStatuses.AttachedMissing, checkedTranscript.Status);
            Assert.Equal(EditPlanInspectionSignalFileStatuses.Missing, checkedTranscript.FileStatus);
            Assert.False(checkedTranscript.Exists);

            var uncheckedTranscript = Assert.Single(uncheckedResult.Signals, signal => signal.Kind == EditPlanInspectionSignalKinds.Transcript);
            Assert.Equal(EditPlanInspectionSignalStatuses.AttachedNotChecked, uncheckedTranscript.Status);
            Assert.Equal(EditPlanInspectionSignalFileStatuses.NotChecked, uncheckedTranscript.FileStatus);
            Assert.Null(uncheckedTranscript.Exists);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
