using OpenVideoToolbox.Core;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Jobs;
using OpenVideoToolbox.Core.Presets;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfmpegCommandBuilderTests
{
    [Fact]
    public void Build_CreatesDeterministicTranscodeCommandPlan()
    {
        var builder = new FfmpegCommandBuilder();
        var job = BuildJob();

        var plan = builder.Build(job);

        Assert.Equal("ffmpeg", plan.ToolName);
        Assert.Equal("output", plan.WorkingDirectory);
        Assert.Equal(
            [
                "-y",
                "-i",
                "samples/input/sample-video.mp4",
                "-c:v",
                "libx264",
                "-preset",
                "medium",
                "-crf",
                "21",
                "-pix_fmt",
                "yuv420p",
                "-c:a",
                "aac",
                "-b:a",
                "192k",
                "-ac",
                "2",
                "-ar",
                "48000",
                "-movflags",
                "+faststart",
                "-map_metadata",
                "-1",
                Path.Combine("output", "sample-video.release.mp4")
            ],
            plan.Arguments);
    }

    [Fact]
    public void Build_UsesNoOverwriteSwitch_WhenOutputDisallowsOverwrite()
    {
        var builder = new FfmpegCommandBuilder();
        var job = BuildJob() with
        {
            Output = new JobOutput
            {
                OutputDirectory = "encoded",
                FileNameStem = "episode-01",
                ContainerExtension = "mkv",
                OverwriteExisting = false
            }
        };

        var plan = builder.Build(job, executablePath: "ffmpeg-custom");

        Assert.Equal("ffmpeg-custom", plan.ExecutablePath);
        Assert.Equal("-n", plan.Arguments[0]);
        Assert.Equal(Path.Combine("encoded", "episode-01.mkv"), plan.Arguments[^1]);
        Assert.Contains("samples/input/sample-video.mp4", plan.CommandLine);
    }

    [Fact]
    public void Build_AddsVideoDisableFlag_ForAudioOnlyPreset()
    {
        var builder = new FfmpegCommandBuilder();
        var job = BuildJob() with
        {
            Output = new JobOutput
            {
                OutputDirectory = "output",
                FileNameStem = "sample-video.release",
                ContainerExtension = ".m4a",
                OverwriteExisting = true
            },
            Preset = new PresetDefinition
            {
                SchemaVersion = SchemaVersions.V1,
                Id = "aac-audio-only",
                DisplayName = "AAC audio only",
                Kind = PresetKind.AudioOnly,
                Audio = new AudioEncoderSettings
                {
                    Encoder = "aac",
                    BitrateKbps = 128,
                    Channels = 2,
                    SampleRate = 44100
                },
                Output = new OutputSettings
                {
                    ContainerExtension = ".m4a",
                    FastStart = false,
                    OverwriteExisting = true
                }
            }
        };

        var plan = builder.Build(job);

        Assert.Contains("-vn", plan.Arguments);
        Assert.DoesNotContain("-c:v", plan.Arguments);
        Assert.Equal(Path.Combine("output", "sample-video.release.m4a"), plan.Arguments[^1]);
    }

    [Fact]
    public void Build_DoesNotDuplicateFastStart_WhenAlreadyProvidedInExtraArguments()
    {
        var builder = new FfmpegCommandBuilder();
        var job = BuildJob() with
        {
            Preset = BuildJob().Preset with
            {
                ExtraArguments = ["-movflags", "+faststart"]
            }
        };

        var plan = builder.Build(job);

        Assert.Equal(1, plan.Arguments.Count(argument => argument == "-movflags"));
    }

    private static JobDefinition BuildJob()
    {
        return new JobDefinition
        {
            SchemaVersion = SchemaVersions.V1,
            Id = "job-0002",
            CreatedAtUtc = new DateTimeOffset(2026, 4, 15, 0, 0, 0, TimeSpan.Zero),
            Source = new JobSource
            {
                InputPath = "samples/input/sample-video.mp4"
            },
            Output = new JobOutput
            {
                OutputDirectory = "output",
                FileNameStem = "sample-video.release",
                ContainerExtension = "mp4",
                OverwriteExisting = true
            },
            Preset = new PresetDefinition
            {
                SchemaVersion = SchemaVersions.V1,
                Id = "h264-aac-mp4",
                DisplayName = "H.264 + AAC / MP4",
                Kind = PresetKind.Transcode,
                Video = new VideoEncoderSettings
                {
                    Encoder = "libx264",
                    Preset = "medium",
                    Crf = 21,
                    PixelFormat = "yuv420p"
                },
                Audio = new AudioEncoderSettings
                {
                    Encoder = "aac",
                    BitrateKbps = 192,
                    Channels = 2,
                    SampleRate = 48000
                },
                Output = new OutputSettings
                {
                    ContainerExtension = "mp4",
                    FastStart = true,
                    OverwriteExisting = true
                },
                ExtraArguments = ["-map_metadata", "-1"]
            },
            Tags = ["phase2"]
        };
    }
}
