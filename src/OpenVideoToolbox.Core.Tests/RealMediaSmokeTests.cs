using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Media;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class RealMediaSmokeTests
{
    [Fact]
    public async Task ProbeAsync_ParsesRealMediaFile_WhenFfprobeAvailable()
    {
        if (!RealMediaSmokeTestHelper.IsToolAvailable("ffmpeg") || !RealMediaSmokeTestHelper.IsToolAvailable("ffprobe"))
        {
            return;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-real-probe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var inputPath = Path.Combine(tempDirectory, "sample.mp4");
            await RealMediaSmokeTestHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(2));

            var service = new FfprobeMediaProbeService(new DefaultProcessRunner(), new FfprobeJsonParser());
            var result = await service.ProbeAsync(inputPath, "ffprobe", TimeSpan.FromSeconds(15));

            Assert.Equal("sample.mp4", result.FileName);
            Assert.True(result.Format.Duration >= TimeSpan.FromSeconds(1.9));
            Assert.Contains(result.Streams, stream => stream.Kind == MediaStreamKind.Video);
            Assert.Contains(result.Streams, stream => stream.Kind == MediaStreamKind.Audio);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RenderRunner_ProducesVideoAndSubtitleSidecar_WhenFfmpegAvailable()
    {
        if (!RealMediaSmokeTestHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-real-render-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var inputPath = Path.Combine(tempDirectory, "input.mp4");
            var subtitlePath = Path.Combine(tempDirectory, "captions.srt");
            var outputDirectory = Path.Combine(tempDirectory, "out");
            var outputPath = Path.Combine(outputDirectory, "final.mp4");

            await RealMediaSmokeTestHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(2));
            await File.WriteAllTextAsync(subtitlePath, "1\n00:00:00,000 --> 00:00:01,000\nhello world\n");
            Directory.CreateDirectory(outputDirectory);

            var request = new EditPlanRenderRequest
            {
                Plan = new EditPlan
                {
                    Source = new EditPlanSource
                    {
                        InputPath = inputPath
                    },
                    Clips =
                    [
                        new EditClip
                        {
                            Id = "clip-001",
                            InPoint = TimeSpan.Zero,
                            OutPoint = TimeSpan.FromSeconds(1.2)
                        }
                    ],
                    Subtitles = new EditSubtitlePlan
                    {
                        Path = subtitlePath,
                        Mode = SubtitleMode.Sidecar
                    },
                    Output = new EditOutputPlan
                    {
                        Path = outputPath,
                        Container = "mp4"
                    }
                },
                OverwriteExisting = true
            };

            var runner = new EditPlanRenderRunner(new FfmpegEditPlanRenderCommandBuilder(), new DefaultProcessRunner());
            var result = await runner.RunAsync(request, "ffmpeg", TimeSpan.FromSeconds(30));

            Assert.Equal(ExecutionStatus.Succeeded, result.Status);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "final.srt")));
            Assert.Contains(outputPath, result.ProducedPaths);
            Assert.Contains(Path.Combine(outputDirectory, "final.srt"), result.ProducedPaths);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task AudioMixRunner_ProducesMixedAudio_WhenFfmpegAvailable()
    {
        if (!RealMediaSmokeTestHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-real-mix-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var inputPath = Path.Combine(tempDirectory, "input.mp4");
            var bgmPath = Path.Combine(tempDirectory, "bgm.wav");
            var outputPath = Path.Combine(tempDirectory, "mixed.wav");

            await RealMediaSmokeTestHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(2));
            await RealMediaSmokeTestHelper.CreateSampleAudioAsync(bgmPath, TimeSpan.FromSeconds(2));

            var request = new EditPlanAudioMixRequest
            {
                Plan = new EditPlan
                {
                    Source = new EditPlanSource
                    {
                        InputPath = inputPath
                    },
                    Clips =
                    [
                        new EditClip
                        {
                            Id = "clip-001",
                            InPoint = TimeSpan.Zero,
                            OutPoint = TimeSpan.FromSeconds(1.5)
                        }
                    ],
                    AudioTracks =
                    [
                        new AudioTrackMix
                        {
                            Id = "bgm-01",
                            Role = AudioTrackRole.Bgm,
                            Path = bgmPath,
                            Start = TimeSpan.Zero,
                            GainDb = -12
                        }
                    ],
                    Output = new EditOutputPlan
                    {
                        Path = Path.Combine(tempDirectory, "final.mp4"),
                        Container = "mp4"
                    }
                },
                OutputPath = outputPath,
                OverwriteExisting = true
            };

            var runner = new EditPlanAudioMixRunner(new FfmpegEditPlanAudioMixCommandBuilder(), new DefaultProcessRunner());
            var result = await runner.RunAsync(request, "ffmpeg", TimeSpan.FromSeconds(30));

            Assert.Equal(ExecutionStatus.Succeeded, result.Status);
            Assert.True(File.Exists(outputPath));
            Assert.Contains(outputPath, result.ProducedPaths);
            Assert.True(new FileInfo(outputPath).Length > 0);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task MediaCutRunner_ProducesCutClip_WhenFfmpegAvailable()
    {
        if (!RealMediaSmokeTestHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-real-cut-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var inputPath = Path.Combine(tempDirectory, "input.mp4");
            var outputPath = Path.Combine(tempDirectory, "clip.mp4");

            await RealMediaSmokeTestHelper.CreateSampleVideoAsync(inputPath, TimeSpan.FromSeconds(3));

            var runner = new MediaCutRunner(new FfmpegCutCommandBuilder(), new DefaultProcessRunner());
            var result = await runner.RunAsync(
                new MediaCutRequest
                {
                    InputPath = inputPath,
                    OutputPath = outputPath,
                    Start = TimeSpan.FromSeconds(0.5),
                    End = TimeSpan.FromSeconds(1.5),
                    OverwriteExisting = true
                },
                "ffmpeg",
                TimeSpan.FromSeconds(30));

            Assert.Equal(ExecutionStatus.Succeeded, result.Status);
            Assert.True(File.Exists(outputPath));
            Assert.Contains(outputPath, result.ProducedPaths);
            Assert.True(new FileInfo(outputPath).Length > 0);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task MediaConcatRunner_ProducesMergedClip_WhenFfmpegAvailable()
    {
        if (!RealMediaSmokeTestHelper.IsToolAvailable("ffmpeg"))
        {
            return;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-real-concat-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var sourcePath = Path.Combine(tempDirectory, "input.mp4");
            var clip1Path = Path.Combine(tempDirectory, "clip-01.mp4");
            var clip2Path = Path.Combine(tempDirectory, "clip-02.mp4");
            var listPath = Path.Combine(tempDirectory, "clips.txt");
            var outputPath = Path.Combine(tempDirectory, "merged.mp4");

            await RealMediaSmokeTestHelper.CreateSampleVideoAsync(sourcePath, TimeSpan.FromSeconds(4));

            var cutRunner = new MediaCutRunner(new FfmpegCutCommandBuilder(), new DefaultProcessRunner());
            var firstCut = await cutRunner.RunAsync(
                new MediaCutRequest
                {
                    InputPath = sourcePath,
                    OutputPath = clip1Path,
                    Start = TimeSpan.FromSeconds(0),
                    End = TimeSpan.FromSeconds(1),
                    OverwriteExisting = true
                },
                "ffmpeg",
                TimeSpan.FromSeconds(30));
            var secondCut = await cutRunner.RunAsync(
                new MediaCutRequest
                {
                    InputPath = sourcePath,
                    OutputPath = clip2Path,
                    Start = TimeSpan.FromSeconds(1),
                    End = TimeSpan.FromSeconds(2),
                    OverwriteExisting = true
                },
                "ffmpeg",
                TimeSpan.FromSeconds(30));

            Assert.Equal(ExecutionStatus.Succeeded, firstCut.Status);
            Assert.Equal(ExecutionStatus.Succeeded, secondCut.Status);

            await File.WriteAllLinesAsync(
                listPath,
                [
                    $"file '{clip1Path.Replace("'", "'\\''", StringComparison.Ordinal)}'",
                    $"file '{clip2Path.Replace("'", "'\\''", StringComparison.Ordinal)}'"
                ]);

            var concatRunner = new MediaConcatRunner(new FfmpegConcatCommandBuilder(), new DefaultProcessRunner());
            var result = await concatRunner.RunAsync(
                new MediaConcatRequest
                {
                    InputListPath = listPath,
                    OutputPath = outputPath,
                    OverwriteExisting = true
                },
                "ffmpeg",
                TimeSpan.FromSeconds(30));

            Assert.Equal(ExecutionStatus.Succeeded, result.Status);
            Assert.True(File.Exists(outputPath));
            Assert.Contains(outputPath, result.ProducedPaths);
            Assert.True(new FileInfo(outputPath).Length > 0);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
