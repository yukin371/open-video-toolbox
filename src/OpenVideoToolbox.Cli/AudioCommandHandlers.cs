using System.Text;
using System.Text.Json;
using OpenVideoToolbox.Core.Audio;
using OpenVideoToolbox.Core.AudioSeparation;
using OpenVideoToolbox.Core.Beats;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Media;
using OpenVideoToolbox.Core.Serialization;
using OpenVideoToolbox.Core.Speech;
using OpenVideoToolbox.Core.Subtitles;
using static OpenVideoToolbox.Cli.CliCommandOutput;
using static OpenVideoToolbox.Cli.CliOptionParsing;

namespace OpenVideoToolbox.Cli;

internal static class AudioCommandHandlers
{
    public static async Task<int> RunAudioAnalyzeAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
        var jsonOutPath = GetOption(options, "--json-out");
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
        var resolvedOutputPath = Path.GetFullPath(outputPath!);
        var service = new FfmpegAudioAnalysisService(
            new AudioAnalysisRunner(new FfmpegAudioAnalysisCommandBuilder(), new DefaultProcessRunner()),
            new AudioAnalysisParser());

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);

            var analysis = await service.AnalyzeAsync(inputPath!, ffmpegPath, timeout);
            await File.WriteAllTextAsync(
                resolvedOutputPath,
                JsonSerializer.Serialize(analysis, OpenVideoToolboxJson.Default),
                Encoding.UTF8);

            return WriteCommandEnvelope(
                "audio-analyze",
                preview: false,
                new
                {
                    audioAnalyze = new
                    {
                        inputPath,
                        outputPath = resolvedOutputPath
                    },
                    analysis
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "audio-analyze",
                preview: false,
                BuildFailedCommandPayload(
                    "audioAnalyze",
                    new
                    {
                        inputPath,
                        outputPath = resolvedOutputPath
                    },
                    ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    public static async Task<int> RunAudioGainAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredDoubleOption(options, "--gain-db", out var gainDb, out error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
        var jsonOutPath = GetOption(options, "--json-out");
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
        var request = new AudioGainRequest
        {
            InputPath = inputPath!,
            OutputPath = outputPath!,
            GainDb = gainDb,
            OverwriteExisting = GetOption(options, "--overwrite") == "true"
        };

        var processRunner = new DefaultProcessRunner();
        var runner = new AudioGainRunner(new FfmpegAudioGainCommandBuilder(), processRunner);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(request.OutputPath))!);

            var result = await runner.RunAsync(request, ffmpegPath, timeout);
            if (result.Status != ExecutionStatus.Succeeded)
            {
                var message = BuildExecutionFailureMessage(result);
                return FailWithCommandEnvelope(
                    "audio-gain",
                    preview: false,
                    BuildFailedCommandPayload("audioGain", request, message, result),
                    message,
                    jsonOutPath,
                    exitCode: 2);
            }

            return WriteCommandEnvelope(
                "audio-gain",
                preview: false,
                new
                {
                    audioGain = request,
                    execution = result
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "audio-gain",
                preview: false,
                BuildFailedCommandPayload("audioGain", request, ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    public static async Task<int> RunAudioNormalizeAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetDoubleOption(options, "--target-lufs", out var targetLufs, out error))
        {
            return fail(error!);
        }

        if (!TryGetDoubleOption(options, "--lra", out var loudnessRangeTarget, out error))
        {
            return fail(error!);
        }

        if (!TryGetDoubleOption(options, "--true-peak-db", out var truePeakDb, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
        var jsonOutPath = GetOption(options, "--json-out");
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
        var request = new AudioNormalizeRequest
        {
            InputPath = inputPath!,
            OutputPath = outputPath!,
            TargetLufs = targetLufs ?? AudioNormalizeRequest.DefaultTargetLufs,
            LoudnessRangeTarget = loudnessRangeTarget ?? AudioNormalizeRequest.DefaultLoudnessRangeTarget,
            TruePeakDb = truePeakDb ?? AudioNormalizeRequest.DefaultTruePeakDb,
            OverwriteExisting = GetOption(options, "--overwrite") == "true"
        };

        var processRunner = new DefaultProcessRunner();
        var runner = new AudioNormalizeRunner(new FfmpegAudioNormalizeCommandBuilder(), processRunner);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(request.OutputPath))!);

            var result = await runner.RunAsync(request, ffmpegPath, timeout);
            if (result.Status != ExecutionStatus.Succeeded)
            {
                var message = BuildExecutionFailureMessage(result);
                return FailWithCommandEnvelope(
                    "audio-normalize",
                    preview: false,
                    BuildFailedCommandPayload("audioNormalize", request, message, result),
                    message,
                    jsonOutPath,
                    exitCode: 2);
            }

            return WriteCommandEnvelope(
                "audio-normalize",
                preview: false,
                new
                {
                    audioNormalize = request,
                    execution = result
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "audio-normalize",
                preview: false,
                BuildFailedCommandPayload("audioNormalize", request, ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    public static async Task<int> RunTranscribeAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--model", out var modelPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--translate", out var translateToEnglish, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
        var whisperPath = GetOption(options, "--whisper-cli") ?? "whisper-cli";
        var jsonOutPath = GetOption(options, "--json-out");
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
        var resolvedOutputPath = Path.GetFullPath(outputPath!);
        var service = new WhisperCppTranscriptionService(
            new AudioWaveformExtractRunner(new FfmpegAudioWaveformExtractCommandBuilder(), new DefaultProcessRunner()),
            new WhisperCppTranscriptionRunner(new WhisperCppCommandBuilder(), new DefaultProcessRunner()),
            new WhisperCppJsonParser());

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);

            var transcript = await service.TranscribeAsync(
                new WhisperCppTranscriptionRequest
                {
                    InputPath = inputPath!,
                    ModelPath = modelPath!,
                    Language = GetOption(options, "--language"),
                    TranslateToEnglish = translateToEnglish == true
                },
                ffmpegPath,
                whisperPath,
                timeout);

            await File.WriteAllTextAsync(
                resolvedOutputPath,
                JsonSerializer.Serialize(transcript, OpenVideoToolboxJson.Default),
                Encoding.UTF8);

            return WriteCommandEnvelope(
                "transcribe",
                preview: false,
                new
                {
                    transcribe = new
                    {
                        inputPath,
                        modelPath = Path.GetFullPath(modelPath!),
                        outputPath = resolvedOutputPath,
                        language = transcript.Language,
                        segmentCount = transcript.Segments.Count,
                        translate = translateToEnglish == true
                    },
                    transcript
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "transcribe",
                preview: false,
                BuildFailedCommandPayload(
                    "transcribe",
                    new
                    {
                        inputPath,
                        modelPath = Path.GetFullPath(modelPath!),
                        outputPath = resolvedOutputPath,
                        translate = translateToEnglish == true
                    },
                    ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    public static async Task<int> RunDetectSilenceAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetDoubleOption(options, "--noise-db", out var noiseDb, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--min-duration-ms", out var minimumDurationMs, out error))
        {
            return fail(error!);
        }

        if (minimumDurationMs is < 0)
        {
            return fail("Option '--min-duration-ms' must be zero or greater.");
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
        var jsonOutPath = GetOption(options, "--json-out");
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
        var resolvedOutputPath = Path.GetFullPath(outputPath!);
        var service = new FfmpegSilenceDetectionService(
            new SilenceDetectionRunner(new FfmpegSilenceDetectionCommandBuilder(), new DefaultProcessRunner()),
            new SilenceDetectionParser());

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);

            var document = await service.DetectAsync(
                inputPath!,
                noiseDb ?? -30,
                minimumDurationMs is null ? null : TimeSpan.FromMilliseconds(minimumDurationMs.Value),
                ffmpegPath,
                timeout);

            await File.WriteAllTextAsync(
                resolvedOutputPath,
                JsonSerializer.Serialize(document, OpenVideoToolboxJson.Default),
                Encoding.UTF8);

            return WriteCommandEnvelope(
                "detect-silence",
                preview: false,
                new
                {
                    detectSilence = new
                    {
                        inputPath,
                        outputPath = resolvedOutputPath,
                        segmentCount = document.Segments.Count
                    },
                    silence = document
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "detect-silence",
                preview: false,
                BuildFailedCommandPayload(
                    "detectSilence",
                    new
                    {
                        inputPath,
                        outputPath = resolvedOutputPath
                    },
                    ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    public static async Task<int> RunAutoCutSilenceAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--silence", out var silencePath, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--clips-only", out var clipsOnly, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--padding-ms", out var paddingMs, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--merge-gap-ms", out var mergeGapMs, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--min-clip-duration-ms", out var minClipDurationMs, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--source-duration-ms", out var sourceDurationMs, out error))
        {
            return fail(error!);
        }

        if (paddingMs is < 0)
        {
            return fail("Option '--padding-ms' must be zero or greater.");
        }

        if (mergeGapMs is < 0)
        {
            return fail("Option '--merge-gap-ms' must be zero or greater.");
        }

        if (minClipDurationMs is < 0)
        {
            return fail("Option '--min-clip-duration-ms' must be zero or greater.");
        }

        if (sourceDurationMs is <= 0)
        {
            return fail("Option '--source-duration-ms' must be greater than zero.");
        }

        var clipsOnlyMode = clipsOnly == true;
        var templateId = GetOption(options, "--template");
        var renderOutputPath = GetOption(options, "--render-output");
        if (clipsOnlyMode)
        {
            if (!string.IsNullOrWhiteSpace(templateId))
            {
                return fail("Option '--template' cannot be used with '--clips-only'.");
            }

            if (!string.IsNullOrWhiteSpace(renderOutputPath))
            {
                return fail("Option '--render-output' cannot be used with '--clips-only'.");
            }
        }
        else if (string.IsNullOrWhiteSpace(renderOutputPath))
        {
            return fail("Option '--render-output' is required unless '--clips-only' is specified.");
        }

        var outputPath = GetOption(options, "--output");
        var jsonOutPath = GetOption(options, "--json-out");
        var ffprobePath = GetOption(options, "--ffprobe") ?? "ffprobe";
        var fullSilencePath = Path.GetFullPath(silencePath!);
        var mode = clipsOnlyMode ? "clipsOnly" : "plan";
        var commandContext = new
        {
            silencePath = fullSilencePath,
            mode,
            outputPath = outputPath is null ? null : Path.GetFullPath(outputPath),
            templateId,
            renderOutputPath,
            ffprobePath,
            paddingMs = paddingMs ?? 200,
            mergeGapMs = mergeGapMs ?? 500,
            minClipDurationMs = minClipDurationMs ?? 1000,
            sourceDurationMs
        };

        try
        {
            var silence = await LoadSilenceDocumentAsync(fullSilencePath);
            var sourcePath = ResolveSourcePath(fullSilencePath, silence.InputPath);
            var sourceDuration = sourceDurationMs is { } explicitDuration
                ? TimeSpan.FromMilliseconds(explicitDuration)
                : await ProbeSourceDurationAsync(sourcePath, ffprobePath);

            var request = new AutoCutSilenceRequest
            {
                SourcePath = sourcePath,
                SourceDuration = sourceDuration,
                Silence = silence,
                Padding = TimeSpan.FromMilliseconds(paddingMs ?? 200),
                MergeGap = TimeSpan.FromMilliseconds(mergeGapMs ?? 500),
                MinClipDuration = TimeSpan.FromMilliseconds(minClipDurationMs ?? 1000),
                TemplateId = templateId,
                RenderOutputPath = renderOutputPath
            };

            var planner = new AutoCutSilencePlanner();
            var result = clipsOnlyMode
                ? planner.BuildClips(request)
                : planner.BuildPlan(request);

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                var fullOutputPath = Path.GetFullPath(outputPath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
                var filePayload = clipsOnlyMode
                    ? JsonSerializer.Serialize(result.Clips, OpenVideoToolboxJson.Default)
                    : JsonSerializer.Serialize(result.Plan, OpenVideoToolboxJson.Default);
                await File.WriteAllTextAsync(fullOutputPath, filePayload, Encoding.UTF8);
            }

            return WriteCommandEnvelope(
                "auto-cut-silence",
                preview: false,
                new
                {
                    autoCutSilence = new
                    {
                        commandContext.silencePath,
                        sourcePath,
                        sourceDuration,
                        commandContext.mode,
                        commandContext.outputPath,
                        commandContext.templateId,
                        commandContext.renderOutputPath,
                        commandContext.ffprobePath,
                        commandContext.paddingMs,
                        commandContext.mergeGapMs,
                        commandContext.minClipDurationMs,
                        usedExplicitSourceDuration = sourceDurationMs is not null
                    },
                    result
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "auto-cut-silence",
                preview: false,
                BuildFailedCommandPayload("autoCutSilence", commandContext, ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    public static async Task<int> RunSeparateAudioAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--output-dir", out var outputDirectory, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        var demucsPath = GetOption(options, "--demucs") ?? "demucs";
        var jsonOutPath = GetOption(options, "--json-out");
        var model = GetOption(options, "--model") ?? "htdemucs";
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
        var resolvedOutputDirectory = Path.GetFullPath(outputDirectory!);
        var service = new DemucsAudioSeparationService(
            new DemucsSeparationRunner(new DemucsCommandBuilder(), new DefaultProcessRunner()));

        try
        {
            Directory.CreateDirectory(resolvedOutputDirectory);

            var document = await service.SeparateAsync(
                new DemucsSeparationRequest
                {
                    InputPath = inputPath!,
                    OutputDirectory = resolvedOutputDirectory,
                    Model = model
                },
                demucsPath,
                timeout);

            return WriteCommandEnvelope(
                "separate-audio",
                preview: false,
                new
                {
                    separateAudio = new
                    {
                        inputPath,
                        outputDirectory = resolvedOutputDirectory,
                        model = document.Model
                    },
                    stems = document.Stems
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "separate-audio",
                preview: false,
                BuildFailedCommandPayload(
                    "separateAudio",
                    new
                    {
                        inputPath,
                        outputDirectory = resolvedOutputDirectory,
                        model
                    },
                    ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    public static async Task<int> RunSubtitleAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--transcript", out var transcriptPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--format", out var rawFormat, out error))
        {
            return fail(error!);
        }

        if (!TryParseSubtitleFormat(rawFormat!, out var format, out error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--max-line-length", out var maxLineLength, out error))
        {
            return fail(error!);
        }

        var jsonOutPath = GetOption(options, "--json-out");

        try
        {
            var transcript = await TemplatePlanBuildSupport.LoadTranscriptAsync(transcriptPath!);
            var request = new SubtitleRenderRequest
            {
                Transcript = transcript,
                Format = format,
                OutputPath = Path.GetFullPath(outputPath!),
                MaxLineLength = maxLineLength ?? 24
            };

            var renderer = new SubtitleRenderer();
            var result = renderer.Render(request);
            Directory.CreateDirectory(Path.GetDirectoryName(result.OutputPath)!);
            await File.WriteAllTextAsync(result.OutputPath, result.Content, Encoding.UTF8);

            return WriteResult(new
            {
                source = new
                {
                    inputPath,
                    transcriptPath = Path.GetFullPath(transcriptPath!)
                },
                subtitle = new
                {
                    result.OutputPath,
                    result.Format,
                    result.SegmentCount,
                    result.MaxLineLength,
                    transcript.Language
                }
            }, jsonOutPath);
        }
        catch (Exception ex)
        {
            return fail(ex.Message);
        }
    }

    public static async Task<int> RunBeatTrackAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--sample-rate", out var sampleRateHz, out error))
        {
            return fail(error!);
        }

        if (sampleRateHz is <= 0)
        {
            return fail("Option '--sample-rate' must be greater than zero.");
        }

        var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
        var jsonOutPath = GetOption(options, "--json-out");
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
        var resolvedOutputPath = Path.GetFullPath(outputPath!);
        var tempWavePath = Path.Combine(Path.GetTempPath(), $"ovt-beat-track-{Guid.NewGuid():N}.wav");
        var beatTrackContext = new
        {
            inputPath,
            outputPath = resolvedOutputPath
        };

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);

            var processRunner = new DefaultProcessRunner();
            var extractRequest = new AudioWaveformExtractRequest
            {
                InputPath = inputPath!,
                OutputPath = tempWavePath,
                SampleRateHz = sampleRateHz ?? 16000,
                OverwriteExisting = true
            };
            var extractRunner = new AudioWaveformExtractRunner(new FfmpegAudioWaveformExtractCommandBuilder(), processRunner);
            var extraction = await extractRunner.RunAsync(extractRequest, ffmpegPath, timeout);
            if (extraction.Status != ExecutionStatus.Succeeded)
            {
                var message = BuildExecutionFailureMessage(extraction);
                Console.Error.WriteLine(message);
                return WriteCommandEnvelope(
                    "beat-track",
                    preview: false,
                    new
                    {
                        beatTrack = beatTrackContext,
                        extraction,
                        error = new
                        {
                            message
                        }
                    },
                    jsonOutPath,
                    exitCode: 2);
            }

            var waveform = new WavePcmReader().ReadMono16Bit(tempWavePath);
            var beatTrack = new BeatTrackAnalyzer().Analyze(waveform, inputPath!);
            await File.WriteAllTextAsync(resolvedOutputPath, JsonSerializer.Serialize(beatTrack, OpenVideoToolboxJson.Default));

            return WriteCommandEnvelope(
                "beat-track",
                preview: false,
                new
                {
                    beatTrack = new
                    {
                        inputPath,
                        outputPath = resolvedOutputPath,
                        beatCount = beatTrack.Beats.Count,
                        beatTrack.EstimatedBpm
                    },
                    extraction
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return WriteCommandEnvelope(
                "beat-track",
                preview: false,
                new
                {
                    beatTrack = beatTrackContext,
                    error = new
                    {
                        message = ex.Message
                    }
                },
                jsonOutPath,
                exitCode: 1);
        }
        finally
        {
            try
            {
                if (File.Exists(tempWavePath))
                {
                    File.Delete(tempWavePath);
                }
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }
    }

    private static async Task<SilenceDetectionDocument> LoadSilenceDocumentAsync(string fullSilencePath)
    {
        var content = await File.ReadAllTextAsync(fullSilencePath);
        return JsonSerializer.Deserialize<SilenceDetectionDocument>(content, OpenVideoToolboxJson.Default)
            ?? throw new InvalidOperationException($"Failed to parse silence document '{fullSilencePath}'.");
    }

    private static string ResolveSourcePath(string fullSilencePath, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new InvalidOperationException(
                $"Silence document '{fullSilencePath}' does not contain a usable inputPath.");
        }

        if (Path.IsPathRooted(sourcePath))
        {
            return Path.GetFullPath(sourcePath);
        }

        var silenceDirectory = Path.GetDirectoryName(fullSilencePath)!;
        return Path.GetFullPath(Path.Combine(silenceDirectory, sourcePath));
    }

    private static async Task<TimeSpan> ProbeSourceDurationAsync(string sourcePath, string ffprobePath)
    {
        var probeService = new FfprobeMediaProbeService(new DefaultProcessRunner(), new FfprobeJsonParser());
        var probeResult = await probeService.ProbeAsync(sourcePath, ffprobePath);
        if (probeResult.Format.Duration is not { } duration || duration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"Unable to determine source duration for '{sourcePath}'. Re-run with '--source-duration-ms'.");
        }

        return duration;
    }
}
