using OpenVideoToolbox.Core;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class EditPlanExportServiceTests
{
    [Fact]
    public async Task ExportAsync_WrapsV1PlanIntoEdl()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-export-v1-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var sourcePath = Path.Combine(tempDirectory, "input.mp4");
            var outputPath = Path.Combine(tempDirectory, "exports", "v1.edl");
            await File.WriteAllTextAsync(sourcePath, "video");

            var service = new EditPlanExportService();
            var result = await service.ExportAsync(new ProjectExportRequest
            {
                Plan = new EditPlan
                {
                    Source = new EditPlanSource
                    {
                        InputPath = sourcePath
                    },
                    Clips =
                    [
                        new EditClip
                        {
                            Id = "clip-001",
                            InPoint = TimeSpan.Zero,
                            OutPoint = TimeSpan.FromSeconds(2),
                            Label = "intro"
                        },
                        new EditClip
                        {
                            Id = "clip-002",
                            InPoint = TimeSpan.FromSeconds(5),
                            OutPoint = TimeSpan.FromSeconds(6)
                        }
                    ],
                    AudioTracks =
                    [
                        new AudioTrackMix
                        {
                            Id = "bgm-01",
                            Role = AudioTrackRole.Bgm,
                            Path = Path.Combine(tempDirectory, "bgm.wav")
                        }
                    ],
                    Output = new EditOutputPlan
                    {
                        Path = Path.Combine(tempDirectory, "final.mp4"),
                        Container = "mp4"
                    }
                },
                Format = ProjectExportFormat.Edl,
                OutputPath = outputPath,
                Title = "V1_TEST"
            });

            Assert.Equal(ProjectExportFormats.Edl, result.Format);
            Assert.Equal(ProjectExportFidelityLevels.L1, result.FidelityLevel);
            Assert.Equal(30, result.FrameRate);
            Assert.Equal(2, result.EventCount);
            Assert.Contains(result.Warnings, item => item.Code == ProjectExportWarningCodes.V1Wrapped);
            Assert.Contains(result.Warnings, item => item.Code == ProjectExportWarningCodes.FrameRateDefaulted);
            Assert.Contains(result.Warnings, item => item.Code == ProjectExportWarningCodes.AudioIgnored);

            var edl = await File.ReadAllTextAsync(outputPath);
            var expected = $"""
TITLE: V1_TEST
FCM: NON-DROP FRAME

001  INPUT    V     C        00:00:00:00 00:00:02:00 00:00:00:00 00:00:02:00
* FROM CLIP NAME: intro
* SOURCE FILE: {sourcePath}

002  INPUT    V     C        00:00:05:00 00:00:06:00 00:00:02:00 00:00:03:00
* FROM CLIP NAME: clip-002
* SOURCE FILE: {sourcePath}

""";

            Assert.Equal(
                NormalizeLineEndings(expected).TrimEnd(),
                NormalizeLineEndings(edl).TrimEnd());
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExportAsync_UsesPrimaryTimelineTrackAndAggregatesWarnings()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-export-v2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var sourcePath = Path.Combine(tempDirectory, "input.mp4");
            var alternatePath = Path.Combine(tempDirectory, "alt.mp4");
            var outputPath = Path.Combine(tempDirectory, "timeline.edl");
            await File.WriteAllTextAsync(sourcePath, "video");
            await File.WriteAllTextAsync(alternatePath, "video");

            var service = new EditPlanExportService();
            var result = await service.ExportAsync(new ProjectExportRequest
            {
                Plan = new EditPlan
                {
                    SchemaVersion = SchemaVersions.V2,
                    Source = new EditPlanSource
                    {
                        InputPath = sourcePath
                    },
                    Timeline = new EditPlanTimeline
                    {
                        Duration = TimeSpan.FromSeconds(4),
                        FrameRate = 24,
                        Tracks =
                        [
                            new TimelineTrack
                            {
                                Id = "secondary",
                                Kind = TrackKind.Video,
                                Clips =
                                [
                                    new TimelineClip
                                    {
                                        Id = "secondary-clip",
                                        Src = alternatePath,
                                        Start = TimeSpan.Zero,
                                        InPoint = TimeSpan.Zero,
                                        OutPoint = TimeSpan.FromSeconds(1)
                                    }
                                ]
                            },
                            new TimelineTrack
                            {
                                Id = "main",
                                Kind = TrackKind.Video,
                                Effects =
                                [
                                    new TimelineEffect
                                    {
                                        Type = "scale"
                                    }
                                ],
                                Clips =
                                [
                                    new TimelineClip
                                    {
                                        Id = "main-clip",
                                        Start = TimeSpan.FromSeconds(1),
                                        InPoint = TimeSpan.FromSeconds(4),
                                        OutPoint = TimeSpan.FromSeconds(6),
                                        Effects =
                                        [
                                            new TimelineEffect
                                            {
                                                Type = "brightness_contrast"
                                            }
                                        ],
                                        Transitions = new ClipTransitions
                                        {
                                            Out = new Transition
                                            {
                                                Type = "fade",
                                                Duration = 0.5
                                            }
                                        }
                                    }
                                ]
                            },
                            new TimelineTrack
                            {
                                Id = "bgm",
                                Kind = TrackKind.Audio,
                                Clips =
                                [
                                    new TimelineClip
                                    {
                                        Id = "bgm-clip",
                                        Start = TimeSpan.Zero,
                                        InPoint = TimeSpan.Zero,
                                        OutPoint = TimeSpan.FromSeconds(4)
                                    }
                                ]
                            }
                        ]
                    },
                    Output = new EditOutputPlan
                    {
                        Path = Path.Combine(tempDirectory, "final.mp4"),
                        Container = "mp4"
                    }
                },
                Format = ProjectExportFormat.Edl,
                OutputPath = outputPath,
                Title = "V2_TIMELINE"
            });

            Assert.Equal(24, result.FrameRate);
            Assert.Equal(1, result.EventCount);
            Assert.Contains(result.Warnings, item => item.Code == ProjectExportWarningCodes.AudioIgnored);
            Assert.Contains(result.Warnings, item => item.Code == ProjectExportWarningCodes.ExtraVideoTracksIgnored);
            Assert.Contains(result.Warnings, item => item.Code == ProjectExportWarningCodes.EffectsIgnored);
            Assert.Contains(result.Warnings, item => item.Code == ProjectExportWarningCodes.TransitionsIgnored);

            var edl = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("TITLE: V2_TIMELINE", edl);
            Assert.Contains("main-clip", edl);
            Assert.Contains(sourcePath, edl, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(alternatePath, edl, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("00:00:04:00 00:00:06:00 00:00:01:00 00:00:03:00", edl);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExportAsync_FailsWhenOutputExistsWithoutOverwrite()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-export-overwrite-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var sourcePath = Path.Combine(tempDirectory, "input.mp4");
            var outputPath = Path.Combine(tempDirectory, "existing.edl");
            await File.WriteAllTextAsync(sourcePath, "video");
            await File.WriteAllTextAsync(outputPath, "existing");

            var service = new EditPlanExportService();

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExportAsync(new ProjectExportRequest
            {
                Plan = new EditPlan
                {
                    Source = new EditPlanSource
                    {
                        InputPath = sourcePath
                    },
                    Clips =
                    [
                        new EditClip
                        {
                            Id = "clip-001",
                            InPoint = TimeSpan.Zero,
                            OutPoint = TimeSpan.FromSeconds(1)
                        }
                    ],
                    Output = new EditOutputPlan
                    {
                        Path = Path.Combine(tempDirectory, "final.mp4"),
                        Container = "mp4"
                    }
                },
                Format = ProjectExportFormat.Edl,
                OutputPath = outputPath
            }));

            Assert.Contains("--overwrite", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private static string NormalizeLineEndings(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
