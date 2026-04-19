using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task ExtractAudio_RequiresTrackOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-extract-track-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "voice.m4a");

        try
        {
            var result = await RunCliAsync(
                "extract-audio",
                "input.mp4",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--track' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("extract-audio <input> --track <n>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExtractAudio_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-extract-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "voice.m4a");

        try
        {
            var result = await RunCliAsync(
                "extract-audio",
                "input.mp4",
                "--track",
                "0",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("extract-audio", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["extractAudio"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(0, envelope["extractAudio"]!["trackIndex"]!.GetValue<int>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Null(envelope["execution"]);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AudioAnalyze_RequiresOutputOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-analyze-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("audio-analyze", "input.mp4");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("audio-analyze <input> --output <audio.json>", result.StdOut, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AudioAnalyze_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-analyze-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "audio.json");

        try
        {
            var result = await RunCliAsync(
                "audio-analyze",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("audio-analyze", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["audioAnalyze"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AudioAnalyze_ReturnsFailureEnvelope_WhenProcessExitsNonZero()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-analyze-failed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "audio.json");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg-fail",
            """
            @echo off
            1>&2 echo loudnorm failed
            exit /b 7
            """,
            """
            #!/bin/sh
            echo "loudnorm failed" >&2
            exit 7
            """);

        try
        {
            var result = await RunCliAsync(
                "audio-analyze",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("ffmpeg audio analysis failed", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("audio-analyze", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["audioAnalyze"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("ffmpeg audio analysis failed", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AudioAnalyze_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-analyze-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "audio.json");
        var jsonOutPath = Path.Combine(outputDirectory, "audio-analyze.json");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg",
            """
            @echo off
            1>&2 echo {
            1>&2 echo   "input_i" : "-16.40",
            1>&2 echo   "input_lra" : "3.10",
            1>&2 echo   "input_tp" : "-1.20",
            1>&2 echo   "input_thresh" : "-27.30",
            1>&2 echo   "target_offset" : "0.50"
            1>&2 echo }
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            cat >&2 <<'EOF'
            {
              "input_i" : "-16.40",
              "input_lra" : "3.10",
              "input_tp" : "-1.20",
              "input_thresh" : "-27.30",
              "target_offset" : "0.50"
            }
            EOF
            """);

        try
        {
            var result = await RunCliAsync(
                "audio-analyze",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            var analysisFile = JsonNode.Parse(await File.ReadAllTextAsync(outputPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("audio-analyze", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["audioAnalyze"]!["outputPath"]!.GetValue<string>());
            Assert.True(JsonNode.DeepEquals(envelope["analysis"], analysisFile));
            Assert.Equal(-16.4, envelope["analysis"]!["analysis"]!["integratedLoudness"]!.GetValue<double>());
            Assert.Equal(3.1, envelope["analysis"]!["analysis"]!["loudnessRange"]!.GetValue<double>());
            Assert.Equal(-1.2, envelope["analysis"]!["analysis"]!["truePeakDb"]!.GetValue<double>());
            Assert.Equal(-27.3, envelope["analysis"]!["analysis"]!["thresholdDb"]!.GetValue<double>());
            Assert.Equal(0.5, envelope["analysis"]!["analysis"]!["targetOffset"]!.GetValue<double>());
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AudioGain_RequiresGainDbOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-gain-db-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "gain.wav");

        try
        {
            var result = await RunCliAsync(
                "audio-gain",
                "input.mp4",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--gain-db' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("audio-gain <input> --gain-db <n> --output <path>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AudioGain_RejectsInvalidGainDb()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-gain-invalid-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "gain.wav");

        try
        {
            var result = await RunCliAsync(
                "audio-gain",
                "input.mp4",
                "--gain-db",
                "oops",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--gain-db' expects a numeric value.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("audio-gain <input> --gain-db <n> --output <path>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AudioGain_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-gain-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "gain.wav");

        try
        {
            var result = await RunCliAsync(
                "audio-gain",
                "input.mp4",
                "--gain-db",
                "3",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("audio-gain", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["audioGain"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(3, envelope["audioGain"]!["gainDb"]!.GetValue<double>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Null(envelope["execution"]);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AudioGain_ReturnsFailureEnvelope_WhenProcessExitsNonZero()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-gain-failed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "gain.wav");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg-fail",
            """
            @echo off
            echo volume failed 1>&2
            exit /b 7
            """,
            """
            #!/bin/sh
            echo "volume failed" >&2
            exit 7
            """);

        try
        {
            var result = await RunCliAsync(
                "audio-gain",
                "input.mp4",
                "--gain-db",
                "-6",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath);

            Assert.Equal(2, result.ExitCode);
            Assert.Contains("Process exited with code 7.", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("audio-gain", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["audioGain"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(-6, envelope["audioGain"]!["gainDb"]!.GetValue<double>());
            Assert.Contains("Process exited with code 7.", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Equal("failed", envelope["execution"]!["status"]!.GetValue<string>());
            Assert.Equal(7, envelope["execution"]!["exitCode"]!.GetValue<int>());
            Assert.Contains("Process exited with code 7.", envelope["execution"]!["errorMessage"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AudioGain_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-audio-gain-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "gain.wav");
        var jsonOutPath = Path.Combine(outputDirectory, "audio-gain.json");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg",
            """
            @echo off
            setlocal
            set "out="
            :loop
            if "%~1"=="" goto done
            set "out=%~1"
            shift
            goto loop
            :done
            if "%out%"=="" exit /b 2
            break> "%out%"
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            out=""
            for arg in "$@"; do
              out="$arg"
            done
            if [ -z "$out" ]; then
              exit 2
            fi
            : > "$out"
            """);

        try
        {
            var result = await RunCliAsync(
                "audio-gain",
                "input.mp4",
                "--gain-db",
                "-6",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("audio-gain", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["audioGain"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(-6, envelope["audioGain"]!["gainDb"]!.GetValue<double>());
            Assert.Equal("succeeded", envelope["execution"]!["status"]!.GetValue<string>());
            Assert.Contains(Path.GetFullPath(outputPath), envelope["execution"]!["producedPaths"]!.AsArray().Select(node => node!.GetValue<string>()));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Transcribe_RequiresModelOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-transcribe-model-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "transcript.json");

        try
        {
            var result = await RunCliAsync(
                "transcribe",
                "input.mp4",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--model' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("transcribe <input> --model <path> --output <transcript.json>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Transcribe_RequiresOutputOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-transcribe-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync(
                "transcribe",
                "input.mp4",
                "--model",
                "ggml-base.bin");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("transcribe <input> --model <path> --output <transcript.json>", result.StdOut, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Transcribe_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-transcribe-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "transcript.json");

        try
        {
            var result = await RunCliAsync(
                "transcribe",
                "input.mp4",
                "--model",
                "ggml-base.bin",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("transcribe", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["transcribe"]!["outputPath"]!.GetValue<string>());
            Assert.EndsWith("ggml-base.bin", envelope["transcribe"]!["modelPath"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Transcribe_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-transcribe-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "transcript.json");
        var jsonOutPath = Path.Combine(outputDirectory, "transcribe.json");
        var modelPath = Path.Combine(outputDirectory, "ggml-base.bin");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg",
            """
            @echo off
            setlocal
            set "out="
            :loop
            if "%~1"=="" goto done
            set "out=%~1"
            shift
            goto loop
            :done
            if "%out%"=="" exit /b 2
            break> "%out%"
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            out=""
            for arg in "$@"; do
              out="$arg"
            done
            if [ -z "$out" ]; then
              exit 2
            fi
            : > "$out"
            """);
        var whisperCliPath = WriteExecutableScript(
            outputDirectory,
            "fake-whisper",
            """
            @echo off
            setlocal EnableDelayedExpansion
            set "prefix="
            :parse
            if "%~1"=="" goto done
            if "%~1"=="-of" (
              set "prefix=%~2"
              shift
            )
            shift
            goto parse
            :done
            if "%prefix%"=="" exit /b 2
            > "%prefix%.json" (
              echo {"result":{"language":"en"},"transcription":[{"text":"hello world","offsets":{"from":0,"to":1000}}]}
            )
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            prefix=""
            while [ "$#" -gt 0 ]; do
              if [ "$1" = "-of" ]; then
                prefix="$2"
                shift 2
                continue
              fi
              shift
            done
            if [ -z "$prefix" ]; then
              exit 2
            fi
            cat > "${prefix}.json" <<'EOF'
            {"result":{"language":"en"},"transcription":[{"text":"hello world","offsets":{"from":0,"to":1000}}]}
            EOF
            """);
        await File.WriteAllTextAsync(modelPath, "model");

        try
        {
            var result = await RunCliAsync(
                "transcribe",
                "input.mp4",
                "--model",
                modelPath,
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath,
                "--whisper-cli",
                whisperCliPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            var transcriptFile = JsonNode.Parse(await File.ReadAllTextAsync(outputPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("transcribe", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["transcribe"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(Path.GetFullPath(modelPath), envelope["transcribe"]!["modelPath"]!.GetValue<string>());
            Assert.Equal("en", envelope["transcribe"]!["language"]!.GetValue<string>());
            Assert.Equal(1, envelope["transcribe"]!["segmentCount"]!.GetValue<int>());
            Assert.NotNull(envelope["transcript"]);
            Assert.True(JsonNode.DeepEquals(envelope["transcript"], transcriptFile));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DetectSilence_RequiresOutputOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-detect-silence-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("detect-silence", "input.mp4");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("detect-silence <input> --output <silence.json>", result.StdOut, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DetectSilence_RejectsInvalidNoiseDb()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-detect-silence-noise-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "silence.json");

        try
        {
            var result = await RunCliAsync(
                "detect-silence",
                "input.mp4",
                "--output",
                outputPath,
                "--noise-db",
                "oops");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--noise-db' expects a numeric value.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("detect-silence <input> --output <silence.json>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DetectSilence_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-detect-silence-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "silence.json");

        try
        {
            var result = await RunCliAsync(
                "detect-silence",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("detect-silence", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["detectSilence"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DetectSilence_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-detect-silence-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "silence.json");
        var jsonOutPath = Path.Combine(outputDirectory, "detect-silence.json");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg",
            """
            @echo off
            1>&2 echo silence_start: 1.25
            1>&2 echo silence_end: 2.75 ^| silence_duration: 1.50
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            printf 'silence_start: 1.25\n' >&2
            printf 'silence_end: 2.75 | silence_duration: 1.50\n' >&2
            """);

        try
        {
            var result = await RunCliAsync(
                "detect-silence",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            var silenceFile = JsonNode.Parse(await File.ReadAllTextAsync(outputPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("detect-silence", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["detectSilence"]!["outputPath"]!.GetValue<string>());
            Assert.Equal(1, envelope["detectSilence"]!["segmentCount"]!.GetValue<int>());
            Assert.True(JsonNode.DeepEquals(envelope["silence"], silenceFile));
            Assert.Equal("00:00:01.2500000", envelope["silence"]!["segments"]![0]!["start"]!.GetValue<string>());
            Assert.Equal("00:00:02.7500000", envelope["silence"]!["segments"]![0]!["end"]!.GetValue<string>());
            Assert.Equal("00:00:01.5000000", envelope["silence"]!["segments"]![0]!["duration"]!.GetValue<string>());
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SeparateAudio_RequiresOutputDirectoryOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-separate-audio-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("separate-audio", "input.mp4");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output-dir' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("separate-audio <input> --output-dir <path>", result.StdOut, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SeparateAudio_RejectsMissingDemucsExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-separate-audio-demucs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync(
                "separate-audio",
                "input.mp4",
                "--output-dir",
                outputDirectory,
                "--demucs",
                "missing-demucs");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-demucs", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("separate-audio", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputDirectory), envelope["separateAudio"]!["outputDirectory"]!.GetValue<string>());
            Assert.Contains("missing-demucs", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SeparateAudio_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-separate-audio-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var jsonOutPath = Path.Combine(outputDirectory, "separate-audio.json");
        var demucsPath = WriteExecutableScript(
            outputDirectory,
            "fake-demucs",
            """
            @echo off
            setlocal
            set "out="
            set "model=htdemucs"
            set "input="
            :parse
            if "%~1"=="" goto done
            if "%~1"=="-o" (
              set "out=%~2"
              shift
            ) else if "%~1"=="-n" (
              set "model=%~2"
              shift
            ) else (
              set "input=%~1"
            )
            shift
            goto parse
            :done
            if "%out%"=="" exit /b 2
            if "%input%"=="" exit /b 2
            set "name=%~n1"
            if not defined name (
              for %%I in ("%input%") do set "name=%%~nI"
            )
            set "trackdir=%out%\%model%\%name%"
            mkdir "%trackdir%" >nul 2>nul
            break> "%trackdir%\vocals.wav"
            break> "%trackdir%\no_vocals.wav"
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            out=""
            model="htdemucs"
            input=""
            while [ "$#" -gt 0 ]; do
              case "$1" in
                -o)
                  out="$2"
                  shift 2
                  ;;
                -n)
                  model="$2"
                  shift 2
                  ;;
                *)
                  input="$1"
                  shift
                  ;;
              esac
            done
            if [ -z "$out" ] || [ -z "$input" ]; then
              exit 2
            fi
            name="$(basename "$input")"
            name="${name%.*}"
            trackdir="$out/$model/$name"
            mkdir -p "$trackdir"
            : > "$trackdir/vocals.wav"
            : > "$trackdir/no_vocals.wav"
            """);

        try
        {
            var result = await RunCliAsync(
                "separate-audio",
                "input.mp4",
                "--output-dir",
                outputDirectory,
                "--demucs",
                demucsPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("separate-audio", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputDirectory), envelope["separateAudio"]!["outputDirectory"]!.GetValue<string>());
            Assert.Equal("htdemucs", envelope["separateAudio"]!["model"]!.GetValue<string>());
            Assert.EndsWith(Path.Combine("htdemucs", "input", "vocals.wav"), envelope["stems"]!["vocals"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(Path.Combine("htdemucs", "input", "no_vocals.wav"), envelope["stems"]!["accompaniment"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Subtitle_RejectsUnsupportedFormat()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-subtitle-format-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var outputPath = Path.Combine(outputDirectory, "subs.srt");
        await File.WriteAllTextAsync(transcriptPath, """{"schemaVersion":1,"language":"en","segments":[]}""");

        try
        {
            var result = await RunCliAsync(
                "subtitle",
                "input.mp4",
                "--transcript",
                transcriptPath,
                "--format",
                "vtt",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--format' expects one of: srt, ass.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("subtitle <input> --transcript <transcript.json> --format <srt|ass>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Subtitle_RequiresTranscriptOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-subtitle-transcript-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "subs.srt");

        try
        {
            var result = await RunCliAsync(
                "subtitle",
                "input.mp4",
                "--format",
                "srt",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--transcript' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("subtitle <input> --transcript <transcript.json> --format <srt|ass>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Subtitle_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-subtitle-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var outputPath = Path.Combine(outputDirectory, "subs.srt");
        var jsonOutPath = Path.Combine(outputDirectory, "subtitle.json");
        await File.WriteAllTextAsync(
            transcriptPath,
            """{"schemaVersion":1,"language":"en","segments":[{"id":"seg-001","start":"00:00:00","end":"00:00:01","text":"hello world"}]}""");

        try
        {
            var result = await RunCliAsync(
                "subtitle",
                "input.mp4",
                "--transcript",
                transcriptPath,
                "--format",
                "srt",
                "--output",
                outputPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal(Path.GetFullPath(outputPath), stdout["subtitle"]!["outputPath"]!.GetValue<string>());
            Assert.Equal("srt", stdout["subtitle"]!["format"]!.GetValue<string>());
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BeatTrack_RequiresOutputOption()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("beat-track", "input.mp4");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("beat-track <input> --output <beats.json>", result.StdOut, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BeatTrack_RejectsInvalidSampleRate()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-sample-rate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "beats.json");

        try
        {
            var result = await RunCliAsync(
                "beat-track",
                "input.mp4",
                "--output",
                outputPath,
                "--sample-rate",
                "oops");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--sample-rate' expects an integer value.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("beat-track <input> --output <beats.json>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BeatTrack_RejectsMissingFfmpegExecutable()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-missing-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "beats.json");

        try
        {
            var result = await RunCliAsync(
                "beat-track",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("beat-track", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["beatTrack"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Null(envelope["extraction"]);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BeatTrack_ReturnsFailureEnvelope_WhenExtractionExitsNonZero()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-process-failed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "beats.json");
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg-fail",
            """
            @echo off
            echo fake ffmpeg failure 1>&2
            exit /b 7
            """,
            """
            #!/bin/sh
            echo "fake ffmpeg failure" >&2
            exit 7
            """);

        try
        {
            var result = await RunCliAsync(
                "beat-track",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath);

            Assert.Equal(2, result.ExitCode);
            Assert.Contains("Process exited with code 7.", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("beat-track", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["beatTrack"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("Process exited with code 7.", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Equal("failed", envelope["extraction"]!["status"]!.GetValue<string>());
            Assert.Equal(7, envelope["extraction"]!["exitCode"]!.GetValue<int>());
            Assert.Contains("Process exited with code 7.", envelope["extraction"]!["errorMessage"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BeatTrack_CanWriteEnvelopeToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "beats.json");
        var jsonOutPath = Path.Combine(outputDirectory, "beat-track.json");
        var sourceWavePath = Path.Combine(outputDirectory, "source.wav");
        WriteMonoPcmWave(sourceWavePath, sampleRateHz: 16000, sampleCount: 16000);
        var ffmpegPath = WriteExecutableScript(
            outputDirectory,
            "fake-ffmpeg-copy-wave",
            $"""
            @echo off
            set "out=%~1"
            :loop
            if "%~2"=="" goto done
            shift
            set "out=%~1"
            goto loop
            :done
            copy /Y "{sourceWavePath}" "%out%" >nul
            exit /b 0
            """,
            $"""
            #!/bin/sh
            out=""
            for arg in "$@"; do
              out="$arg"
            done
            cp "{sourceWavePath.Replace("\\", "/")}" "$out"
            """);

        try
        {
            var result = await RunCliAsync(
                "beat-track",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                ffmpegPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath));
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            var beatsFile = JsonNode.Parse(await File.ReadAllTextAsync(outputPath))!.AsObject();

            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("beat-track", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());

            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["beatTrack"]!["outputPath"]!.GetValue<string>());
            Assert.True(envelope["beatTrack"]!["beatCount"]!.GetValue<int>() >= 0);
            Assert.Equal("succeeded", envelope["extraction"]!["status"]!.GetValue<string>());
            Assert.Equal(1, beatsFile["schemaVersion"]!.GetValue<int>());
            Assert.Equal("input.mp4", beatsFile["sourcePath"]!.GetValue<string>());
            Assert.Equal(envelope["beatTrack"]!["beatCount"]!.GetValue<int>(), beatsFile["beats"]!.AsArray().Count);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Subtitle_RequiresSupportedFormat()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-subtitle-format-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        var outputPath = Path.Combine(outputDirectory, "subs.srt");
        await File.WriteAllTextAsync(transcriptPath, """{"schemaVersion":1,"language":"en","segments":[]}""");

        try
        {
            var result = await RunCliAsync(
                "subtitle",
                "input.mp4",
                "--transcript",
                transcriptPath,
                "--format",
                "vtt",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--format' expects one of: srt, ass.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("subtitle <input> --transcript <transcript.json> --format <srt|ass>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Subtitle_RequiresTranscriptPath()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-subtitle-transcript-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "subs.srt");

        try
        {
            var result = await RunCliAsync(
                "subtitle",
                "input.mp4",
                "--format",
                "srt",
                "--output",
                outputPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--transcript' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("subtitle <input> --transcript <transcript.json> --format <srt|ass>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BeatTrack_RequiresOutputPath()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var result = await RunCliAsync("beat-track", "input.mp4");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--output' is required.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("beat-track <input> --output <beats.json>", result.StdOut, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BeatTrack_RejectsNonIntegerSampleRate()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-sample-rate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "beats.json");

        try
        {
            var result = await RunCliAsync(
                "beat-track",
                "input.mp4",
                "--output",
                outputPath,
                "--sample-rate",
                "oops");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Option '--sample-rate' expects an integer value.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("beat-track <input> --output <beats.json>", result.StdOut, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BeatTrack_RejectsMissingFfmpegBinary()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-beat-ffmpeg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "beats.json");

        try
        {
            var result = await RunCliAsync(
                "beat-track",
                "input.mp4",
                "--output",
                outputPath,
                "--ffmpeg",
                "missing-ffmpeg");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("missing-ffmpeg", result.StdErr, StringComparison.Ordinal);

            var payload = JsonNode.Parse(result.StdOut)!.AsObject();
            Assert.Equal("beat-track", payload["command"]!.GetValue<string>());
            Assert.False(payload["preview"]!.GetValue<bool>());

            var envelope = payload["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputPath), envelope["beatTrack"]!["outputPath"]!.GetValue<string>());
            Assert.Contains("missing-ffmpeg", envelope["error"]!["message"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Null(envelope["extraction"]);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
}
