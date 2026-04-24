using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class EditPlanRenderRunnerTests
{
    [Fact]
    public async Task RunAsync_CopiesSubtitleSidecarAndReturnsProducedPaths()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var subtitleSourcePath = Path.Combine(tempDirectory, "captions.srt");
            await File.WriteAllTextAsync(subtitleSourcePath, "1\n00:00:00,000 --> 00:00:01,000\nhello\n");

            var fakeRunner = new FakeProcessRunner(request => Task.FromResult(new ExecutionResult
            {
                Status = ExecutionStatus.Succeeded,
                ExitCode = 0,
                StartedAtUtc = DateTimeOffset.UtcNow,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero,
                CommandPlan = request.CommandPlan,
                ProducedPaths = request.ProducedPaths
            }));

            var request = new EditPlanRenderRequest
            {
                Plan = new EditPlan
                {
                    Source = new EditPlanSource
                    {
                        InputPath = Path.Combine(tempDirectory, "input.mp4")
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
                    Subtitles = new EditSubtitlePlan
                    {
                        Path = subtitleSourcePath,
                        Mode = SubtitleMode.Sidecar
                    },
                    Output = new EditOutputPlan
                    {
                        Path = Path.Combine(tempDirectory, "render", "final.mp4"),
                        Container = "mp4"
                    }
                },
                OverwriteExisting = true
            };

            var runner = new EditPlanRenderRunner(new FfmpegEditPlanRenderCommandBuilder(), fakeRunner);

            var result = await runner.RunAsync(request, executablePath: "ffmpeg-custom");

            var sidecarPath = Path.Combine(tempDirectory, "render", "final.srt");
            Assert.Equal(ExecutionStatus.Succeeded, result.Status);
            Assert.Equal("ffmpeg-custom", fakeRunner.LastRequest!.CommandPlan.ExecutablePath);
            Assert.Equal(
                [
                    Path.Combine(tempDirectory, "render", "final.mp4"),
                    sidecarPath
                ],
                result.ProducedPaths);
            Assert.True(File.Exists(sidecarPath));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_UsesTimelineBuilderForSchemaV2()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-render-v2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var fakeRunner = new FakeProcessRunner(request => Task.FromResult(new ExecutionResult
            {
                Status = ExecutionStatus.Succeeded,
                ExitCode = 0,
                StartedAtUtc = DateTimeOffset.UtcNow,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                Duration = TimeSpan.Zero,
                CommandPlan = request.CommandPlan,
                ProducedPaths = request.ProducedPaths
            }));

            var request = new EditPlanRenderRequest
            {
                Plan = new EditPlan
                {
                    SchemaVersion = SchemaVersions.V2,
                    Source = new EditPlanSource
                    {
                        InputPath = Path.Combine(tempDirectory, "input.mp4")
                    },
                    Timeline = new EditPlanTimeline
                    {
                        Duration = TimeSpan.FromSeconds(2),
                        Resolution = new TimelineResolution
                        {
                            W = 1920,
                            H = 1080
                        },
                        FrameRate = 30,
                        Tracks =
                        [
                            new TimelineTrack
                            {
                                Id = "main",
                                Kind = TrackKind.Video,
                                Clips =
                                [
                                    new TimelineClip
                                    {
                                        Id = "clip-001",
                                        Start = TimeSpan.Zero,
                                        InPoint = TimeSpan.Zero,
                                        OutPoint = TimeSpan.FromSeconds(2)
                                    }
                                ]
                            }
                        ]
                    },
                    Output = new EditOutputPlan
                    {
                        Path = Path.Combine(tempDirectory, "render", "final.mp4"),
                        Container = "mp4"
                    }
                },
                OverwriteExisting = true
            };

            var runner = new EditPlanRenderRunner(new FfmpegEditPlanRenderCommandBuilder(), fakeRunner);

            var result = await runner.RunAsync(request, executablePath: "ffmpeg-custom");

            Assert.Equal(ExecutionStatus.Succeeded, result.Status);
            Assert.Equal(SchemaVersions.V2, fakeRunner.LastRequest!.CommandPlan.SchemaVersion);
            Assert.Equal("ffmpeg-custom", fakeRunner.LastRequest.CommandPlan.ExecutablePath);
            Assert.Equal([Path.Combine(tempDirectory, "render", "final.mp4")], result.ProducedPaths);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
