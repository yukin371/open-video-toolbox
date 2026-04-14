using System.Text.Json;
using OpenVideoToolbox.Core;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Jobs;
using OpenVideoToolbox.Core.Media;
using OpenVideoToolbox.Core.Presets;
using OpenVideoToolbox.Core.Serialization;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class SerializationTests
{
    [Fact]
    public void SharedJsonOptions_UseCamelCaseAndStringEnums()
    {
        var probe = BuildProbe();

        var json = JsonSerializer.Serialize(probe, OpenVideoToolboxJson.Shared);

        Assert.Contains("\"schemaVersion\": 1", json);
        Assert.Contains("\"sourcePath\":", json);
        Assert.Contains("\"kind\": \"video\"", json);
    }

    [Fact]
    public void JobDefinition_RoundTrips_WithNestedPresetAndProbe()
    {
        var job = BuildJob();

        var json = JsonSerializer.Serialize(job, OpenVideoToolboxJson.Shared);
        var restored = JsonSerializer.Deserialize<JobDefinition>(json, OpenVideoToolboxJson.Shared);

        Assert.NotNull(restored);
        Assert.Equal(job.Id, restored!.Id);
        Assert.Equal("sample-video.mp4", restored.ProbeSnapshot!.FileName);
        Assert.Equal("libx264", restored.Preset.Video!.Encoder);
        Assert.Equal(MediaStreamKind.Audio, restored.ProbeSnapshot.Streams[1].Kind);
    }

    [Fact]
    public void ExecutionResult_RoundTrips_WithCommandPlanAndOutputs()
    {
        var result = new ExecutionResult
        {
            SchemaVersion = SchemaVersions.V1,
            Status = ExecutionStatus.Succeeded,
            ExitCode = 0,
            StartedAtUtc = new DateTimeOffset(2026, 4, 14, 1, 0, 0, TimeSpan.Zero),
            FinishedAtUtc = new DateTimeOffset(2026, 4, 14, 1, 2, 0, TimeSpan.Zero),
            Duration = TimeSpan.FromMinutes(2),
            CommandPlan = new CommandPlan
            {
                SchemaVersion = SchemaVersions.V1,
                ToolName = "ffmpeg",
                ExecutablePath = "ffmpeg",
                Arguments = ["-i", "input.mp4", "output.mp4"],
                CommandLine = "ffmpeg -i input.mp4 output.mp4"
            },
            OutputLines =
            [
                new ProcessOutputLine
                {
                    TimestampUtc = new DateTimeOffset(2026, 4, 14, 1, 0, 30, TimeSpan.Zero),
                    Text = "frame=240",
                    IsError = false
                }
            ],
            ProducedPaths = ["output/sample-video.mp4"]
        };

        var json = JsonSerializer.Serialize(result, OpenVideoToolboxJson.Shared);
        var restored = JsonSerializer.Deserialize<ExecutionResult>(json, OpenVideoToolboxJson.Shared);

        Assert.NotNull(restored);
        Assert.Equal(ExecutionStatus.Succeeded, restored!.Status);
        Assert.Single(restored.OutputLines);
        Assert.Equal("frame=240", restored.OutputLines[0].Text);
        Assert.Equal("ffmpeg", restored.CommandPlan.ToolName);
    }

    private static PresetDefinition BuildPreset()
    {
        return new PresetDefinition
        {
            SchemaVersion = SchemaVersions.V1,
            Id = "h264-aac-mp4",
            DisplayName = "H.264 + AAC / MP4",
            Kind = PresetKind.Transcode,
            Video = new VideoEncoderSettings
            {
                Encoder = "libx264",
                Preset = "medium",
                Crf = 23,
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
                ContainerExtension = ".mp4",
                FastStart = true
            },
            MetadataTags = new Dictionary<string, string>
            {
                ["phase"] = "tests"
            }
        };
    }

    private static MediaProbeResult BuildProbe()
    {
        return new MediaProbeResult
        {
            SchemaVersion = SchemaVersions.V1,
            SourcePath = "samples/input/sample-video.mp4",
            FileName = "sample-video.mp4",
            Format = new MediaFormatInfo
            {
                ContainerName = "mov,mp4,m4a,3gp,3g2,mj2",
                Duration = TimeSpan.FromMinutes(2),
                SizeBytes = 125_000_000,
                Bitrate = 8_000_000
            },
            Streams =
            [
                new MediaStreamInfo
                {
                    Index = 0,
                    Kind = MediaStreamKind.Video,
                    CodecName = "h264",
                    Width = 1920,
                    Height = 1080,
                    FrameRate = 23.976,
                    Duration = TimeSpan.FromMinutes(2)
                },
                new MediaStreamInfo
                {
                    Index = 1,
                    Kind = MediaStreamKind.Audio,
                    CodecName = "aac",
                    Channels = 2,
                    SampleRate = 48000,
                    Duration = TimeSpan.FromMinutes(2)
                }
            ]
        };
    }

    private static JobDefinition BuildJob()
    {
        return new JobDefinition
        {
            SchemaVersion = SchemaVersions.V1,
            Id = "job-0001",
            CreatedAtUtc = new DateTimeOffset(2026, 4, 14, 1, 0, 0, TimeSpan.Zero),
            Source = new JobSource
            {
                InputPath = "samples/input/sample-video.mp4"
            },
            Output = new JobOutput
            {
                OutputDirectory = "output",
                FileNameStem = "sample-video",
                ContainerExtension = ".mp4",
                OverwriteExisting = true
            },
            Preset = BuildPreset(),
            ProbeSnapshot = BuildProbe(),
            Tags = ["tests", "phase1"]
        };
    }
}
