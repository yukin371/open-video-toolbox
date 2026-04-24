using OpenVideoToolbox.Core.Editing;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class EditPlanValidatorTests
{
    [Fact]
    public void Validate_ReturnsValidForMinimalResolvedPlan()
    {
        var validator = new EditPlanValidator();
        var plan = CreatePlan();

        var result = validator.Validate(plan);

        Assert.Equal(EditPlanValidationMode.Basic, result.CheckMode);
        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
        Assert.Equal(0, result.Stats.TotalIssues);
        Assert.Equal(0, result.Stats.ErrorCount);
        Assert.Equal(0, result.Stats.WarningCount);
        Assert.Equal(0, result.Stats.BySeverity["error"]);
        Assert.Equal(0, result.Stats.BySeverity["warning"]);
        Assert.Empty(result.Stats.ByCode);
    }

    [Fact]
    public void Validate_ReturnsErrorsForInvalidClipAndOutput()
    {
        var validator = new EditPlanValidator();
        var plan = CreatePlan() with
        {
            Clips =
            [
                new EditClip
                {
                    Id = "clip-001",
                    InPoint = TimeSpan.FromSeconds(5),
                    OutPoint = TimeSpan.FromSeconds(1)
                },
                new EditClip
                {
                    Id = "clip-001",
                    InPoint = TimeSpan.FromSeconds(6),
                    OutPoint = TimeSpan.FromSeconds(8)
                }
            ],
            Output = new EditOutputPlan
            {
                Path = "final.mp4",
                Container = "mkv"
            }
        };

        var result = validator.Validate(plan);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "clips.range.invalid");
        Assert.Contains(result.Issues, issue => issue.Code == "clips.id.duplicate");
        Assert.Contains(result.Issues, issue => issue.Code == "output.container.mismatch");
        Assert.Equal(3, result.Stats.TotalIssues);
        Assert.Equal(3, result.Stats.ErrorCount);
        Assert.Equal(0, result.Stats.WarningCount);
        Assert.Equal(1, result.Stats.ByCode["clips.range.invalid"]);
        Assert.Equal(1, result.Stats.ByCode["clips.id.duplicate"]);
        Assert.Equal(1, result.Stats.ByCode["output.container.mismatch"]);

        var clipRangeIssue = Assert.Single(result.Issues.Where(issue => issue.Code == "clips.range.invalid"));
        Assert.Equal("clips", clipRangeIssue.Category);
        Assert.Equal("structure", clipRangeIssue.CheckStage);
        Assert.Equal("Adjust clip out so it is greater than clip in.", clipRangeIssue.Suggestion);
    }

    [Fact]
    public void Validate_ReturnsErrorsForTemplateArtifactMismatch()
    {
        var validator = new EditPlanValidator();
        var plan = CreatePlan() with
        {
            Template = new EditTemplateReference
            {
                Id = "shorts-captioned"
            },
            Artifacts =
            [
                new EditArtifactReference
                {
                    SlotId = "bgm",
                    Kind = "audio",
                    Path = "bgm.wav"
                },
                new EditArtifactReference
                {
                    SlotId = "subtitles",
                    Kind = "audio",
                    Path = "captions.srt"
                }
            ]
        };

        var result = validator.Validate(plan);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "artifacts.slotId.undeclared");
        Assert.Contains(result.Issues, issue => issue.Code == "artifacts.kind.mismatch");
    }

    [Fact]
    public void Validate_CheckFilesReportsMissingReferencedFiles()
    {
        var validator = new EditPlanValidator();
        var root = Path.Combine(Path.GetTempPath(), $"ovt-validate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var inputPath = Path.Combine(root, "input.mp4");
            File.WriteAllText(inputPath, "placeholder");

            var plan = CreatePlan(root) with
            {
                AudioTracks =
                [
                    new AudioTrackMix
                    {
                        Id = "bgm-01",
                        Role = AudioTrackRole.Bgm,
                        Path = Path.Combine(root, "missing-bgm.wav")
                    }
                ],
                Transcript = new EditTranscriptPlan
                {
                    Path = Path.Combine(root, "missing-transcript.json")
                }
            };

            var result = validator.Validate(plan, checkReferencedFiles: true);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.Code == "audioTracks.path.missing");
            Assert.Contains(result.Issues, issue => issue.Code == "transcript.path.missing");
            Assert.DoesNotContain(result.Issues, issue => issue.Code == "source.inputPath.missing");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Validate_ReturnsErrorForPluginTemplateWithoutCatalogContext()
    {
        var validator = new EditPlanValidator();
        var plan = CreatePlan() with
        {
            Template = new EditTemplateReference
            {
                Id = "plugin-captioned",
                Source = new EditTemplateSourceReference
                {
                    Kind = EditTemplateSourceKinds.Plugin,
                    PluginId = "community-pack",
                    PluginVersion = "1.0.0"
                }
            }
        };

        var result = validator.Validate(plan);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "template.source.catalog.required");
        var issue = Assert.Single(result.Issues.Where(candidate => candidate.Code == "template.source.catalog.required"));
        Assert.Equal("template", issue.Category);
        Assert.Equal("template", issue.CheckStage);
        Assert.Equal(
            "Re-run validate-plan with --plugin-dir pointing at the plugin catalog that provides this template.",
            issue.Suggestion);
    }

    [Fact]
    public void Validate_AllowsPluginTemplateWhenCatalogContextIsProvided()
    {
        var validator = new EditPlanValidator();
        var plan = CreatePlan() with
        {
            Template = new EditTemplateReference
            {
                Id = "plugin-captioned",
                Source = new EditTemplateSourceReference
                {
                    Kind = EditTemplateSourceKinds.Plugin,
                    PluginId = "community-pack",
                    PluginVersion = "1.0.0"
                }
            }
        };

        var result = validator.Validate(plan, availableTemplates: [CreatePluginTemplateDefinition("plugin-captioned")]);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_ReturnsErrorWhenPluginSourceIsMissingPluginId()
    {
        var validator = new EditPlanValidator();
        var plan = CreatePlan() with
        {
            Template = new EditTemplateReference
            {
                Id = "plugin-captioned",
                Source = new EditTemplateSourceReference
                {
                    Kind = EditTemplateSourceKinds.Plugin
                }
            }
        };

        var result = validator.Validate(plan, availableTemplates: [CreatePluginTemplateDefinition("plugin-captioned")]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "template.source.pluginId.required");
    }

    private static EditPlan CreatePlan(string? root = null)
    {
        var basePath = root ?? Path.GetTempPath();
        return new EditPlan
        {
            Source = new EditPlanSource
            {
                InputPath = Path.Combine(basePath, "input.mp4")
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
            Output = new EditOutputPlan
            {
                Path = Path.Combine(basePath, "final.mp4"),
                Container = "mp4"
            }
        };
    }

    private static EditPlanTemplateDefinition CreatePluginTemplateDefinition(string id)
    {
        return new EditPlanTemplateDefinition
        {
            Id = id,
            DisplayName = "Plugin Template",
            Description = "Plugin template description",
            Category = "plugin",
            OutputContainer = "mp4"
        };
    }
}
