using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenVideoToolbox.Cli;
using OpenVideoToolbox.Core.Audio;
using OpenVideoToolbox.Core.AudioSeparation;
using OpenVideoToolbox.Core.Beats;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Jobs;
using OpenVideoToolbox.Core.Media;
using OpenVideoToolbox.Core.Presets;
using OpenVideoToolbox.Core.Serialization;
using OpenVideoToolbox.Core.Speech;
using OpenVideoToolbox.Core.Subtitles;

return await MainAsync(args);

static async Task<int> MainAsync(string[] args)
{
    if (args.Length == 0)
    {
        PrintUsage();
        return 1;
    }

    var command = args[0].ToLowerInvariant();
    var remaining = args.Skip(1).ToArray();

    return command switch
    {
        "doctor" => await RunDoctorAsync(remaining),
        "init-plan" => await RunInitPlanAsync(remaining),
        "scaffold-template" => await RunScaffoldTemplateAsync(remaining),
        "extract-audio" => await RunExtractAudioAsync(remaining),
        "audio-analyze" => await RunAudioAnalyzeAsync(remaining),
        "audio-gain" => await RunAudioGainAsync(remaining),
        "transcribe" => await RunTranscribeAsync(remaining),
        "detect-silence" => await RunDetectSilenceAsync(remaining),
        "separate-audio" => await RunSeparateAudioAsync(remaining),
        "beat-track" => await RunBeatTrackAsync(remaining),
        "concat" => await RunConcatAsync(remaining),
        "cut" => await RunCutAsync(remaining),
        "mix-audio" => await RunMixAudioAsync(remaining),
        "render" => await RunRenderAsync(remaining),
        "subtitle" => await RunSubtitleAsync(remaining),
        "validate-plan" => await RunValidatePlanAsync(remaining),
        "probe" => await RunProbeAsync(remaining),
        "plan" => await RunPlanAsync(remaining),
        "run" => await RunTranscodeAsync(remaining),
        "templates" => RunTemplates(remaining),
        "presets" => RunPresets(),
        "help" or "--help" or "-h" => ShowHelp(),
        _ => Fail($"Unknown command '{args[0]}'.")
    };
}

static async Task<int> RunDoctorAsync(string[] args)
{
    if (!TryParseOptions(args, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
    {
        return Fail(error!);
    }

    var jsonOutPath = GetOption(options, "--json-out");
    TimeSpan? timeout = timeoutSeconds is null
        ? TimeSpan.FromSeconds(5)
        : TimeSpan.FromSeconds(timeoutSeconds.Value);

    try
    {
        var inspector = new ExternalDependencyInspector(new DefaultProcessRunner());
        var report = await inspector.InspectAsync(BuildDoctorDependencyDefinitions(options, timeout));

        return WriteCommandEnvelope(
            "doctor",
            preview: false,
            report,
            jsonOutPath,
            exitCode: report.IsHealthy ? 0 : 1);
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static async Task<int> RunProbeAsync(string[] args)
{
    if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
    {
        return Fail(error!);
    }

    var ffprobePath = GetOption(options, "--ffprobe") ?? "ffprobe";
    var processRunner = new DefaultProcessRunner();
    var probeService = new FfprobeMediaProbeService(processRunner, new FfprobeJsonParser());

    try
    {
        var result = await probeService.ProbeAsync(inputPath!, ffprobePath);
        WriteJson(result);
        return 0;
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static async Task<int> RunValidatePlanAsync(string[] args)
{
    if (!TryParseOptions(args, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--plan", out var planPath, out error))
    {
        return Fail(error!);
    }

    if (!TryGetBoolOption(options, "--check-files", out var checkFiles, out error))
    {
        return Fail(error!);
    }

    var jsonOutPath = GetOption(options, "--json-out");
    var fullPlanPath = Path.GetFullPath(planPath!);
    var planDirectory = Path.GetDirectoryName(fullPlanPath)!;

    try
    {
        var validation = await ValidatePlanFileAsync(fullPlanPath, checkFiles == true);
        var report = new
        {
            planPath = fullPlanPath,
            resolvedBaseDirectory = planDirectory,
            checkFiles = checkFiles == true,
            isValid = validation.IsValid,
            issues = validation.Issues
        };

        return WriteCommandEnvelope("validate-plan", preview: false, report, jsonOutPath);
    }
    catch (Exception ex)
    {
        var report = new
        {
            planPath = fullPlanPath,
            resolvedBaseDirectory = planDirectory,
            checkFiles = checkFiles == true,
            isValid = false,
            issues = new[]
            {
                new EditPlanValidationIssue
                {
                    Severity = EditPlanValidationSeverity.Error,
                    Path = "$",
                    Code = "plan.parse.failed",
                    Message = ex.Message
                }
            }
        };

        return WriteCommandEnvelope("validate-plan", preview: false, report, jsonOutPath);
    }
}

static async Task<int> RunCutAsync(string[] args)
{
    if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredTimeSpanOption(options, "--from", out var start, out error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredTimeSpanOption(options, "--to", out var end, out error))
    {
        return Fail(error!);
    }

    if (end <= start)
    {
        return Fail("Option '--to' must be greater than '--from'.");
    }

    if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
    {
        return Fail(error!);
    }

    var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
    var jsonOutPath = GetOption(options, "--json-out");
    TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
    var request = new MediaCutRequest
    {
        InputPath = inputPath!,
        OutputPath = outputPath!,
        Start = start,
        End = end,
        OverwriteExisting = GetOption(options, "--overwrite") == "true"
    };

    var processRunner = new DefaultProcessRunner();
    var runner = new MediaCutRunner(new FfmpegCutCommandBuilder(), processRunner);

    try
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(request.OutputPath))!);

        var result = await runner.RunAsync(request, ffmpegPath, timeout);
        WriteJson(new
        {
            cut = request,
            execution = result
        });

        return result.Status == ExecutionStatus.Succeeded ? 0 : 2;
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static async Task<int> RunConcatAsync(string[] args)
{
    if (!TryParseOptions(args, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--input-list", out var inputListPath, out error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
    {
        return Fail(error!);
    }

    if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
    {
        return Fail(error!);
    }

    var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
    var jsonOutPath = GetOption(options, "--json-out");
    TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
    var request = new MediaConcatRequest
    {
        InputListPath = inputListPath!,
        OutputPath = outputPath!,
        OverwriteExisting = GetOption(options, "--overwrite") == "true"
    };

    var processRunner = new DefaultProcessRunner();
    var runner = new MediaConcatRunner(new FfmpegConcatCommandBuilder(), processRunner);

    try
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(request.OutputPath))!);

        var result = await runner.RunAsync(request, ffmpegPath, timeout);
        WriteJson(new
        {
            concat = request,
            execution = result
        });

        return result.Status == ExecutionStatus.Succeeded ? 0 : 2;
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static async Task<int> RunExtractAudioAsync(string[] args)
{
    if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredIntOption(options, "--track", out var trackIndex, out error))
    {
        return Fail(error!);
    }

    if (trackIndex < 0)
    {
        return Fail("Option '--track' must be zero or greater.");
    }

    if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
    {
        return Fail(error!);
    }

    if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
    {
        return Fail(error!);
    }

    var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
    var jsonOutPath = GetOption(options, "--json-out");
    TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
    var request = new AudioExtractRequest
    {
        InputPath = inputPath!,
        OutputPath = outputPath!,
        TrackIndex = trackIndex,
        OverwriteExisting = GetOption(options, "--overwrite") == "true"
    };

    var processRunner = new DefaultProcessRunner();
    var runner = new AudioExtractRunner(new FfmpegAudioExtractCommandBuilder(), processRunner);

    try
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(request.OutputPath))!);

        var result = await runner.RunAsync(request, ffmpegPath, timeout);
        WriteJson(new
        {
            extractAudio = request,
            execution = result
        });

        return result.Status == ExecutionStatus.Succeeded ? 0 : 2;
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static async Task<int> RunAudioAnalyzeAsync(string[] args)
{
    if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
    {
        return Fail(error!);
    }

    if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
    {
        return Fail(error!);
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

        return WriteResult(new
        {
            audioAnalyze = new
            {
                inputPath,
                outputPath = resolvedOutputPath
            },
            analysis
        }, jsonOutPath);
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static async Task<int> RunAudioGainAsync(string[] args)
{
    if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredDoubleOption(options, "--gain-db", out var gainDb, out error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
    {
        return Fail(error!);
    }

    if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
    {
        return Fail(error!);
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
        return WriteResult(new
        {
            audioGain = request,
            execution = result
        }, jsonOutPath, result.Status == ExecutionStatus.Succeeded ? 0 : 2);
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static async Task<int> RunTranscribeAsync(string[] args)
{
    if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--model", out var modelPath, out error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
    {
        return Fail(error!);
    }

    if (!TryGetBoolOption(options, "--translate", out var translateToEnglish, out error))
    {
        return Fail(error!);
    }

    if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
    {
        return Fail(error!);
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

        return WriteResult(new
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
        }, jsonOutPath);
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static async Task<int> RunDetectSilenceAsync(string[] args)
{
    if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
    {
        return Fail(error!);
    }

    if (!TryGetDoubleOption(options, "--noise-db", out var noiseDb, out error))
    {
        return Fail(error!);
    }

    if (!TryGetIntOption(options, "--min-duration-ms", out var minimumDurationMs, out error))
    {
        return Fail(error!);
    }

    if (minimumDurationMs is < 0)
    {
        return Fail("Option '--min-duration-ms' must be zero or greater.");
    }

    if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
    {
        return Fail(error!);
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

        return WriteResult(new
        {
            detectSilence = new
            {
                inputPath,
                outputPath = resolvedOutputPath,
                segmentCount = document.Segments.Count
            },
            silence = document
        }, jsonOutPath);
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static async Task<int> RunSeparateAudioAsync(string[] args)
{
    if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--output-dir", out var outputDirectory, out error))
    {
        return Fail(error!);
    }

    if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
    {
        return Fail(error!);
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

        return WriteResult(new
        {
            separateAudio = new
            {
                inputPath,
                outputDirectory = resolvedOutputDirectory,
                model = document.Model
            },
            stems = document.Stems
        }, jsonOutPath);
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static async Task<int> RunRenderAsync(string[] args)
{
    if (!TryParseOptions(args, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--plan", out var planPath, out error))
    {
        return Fail(error!);
    }

    if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
    {
        return Fail(error!);
    }

    if (!TryGetBoolOption(options, "--preview", out var previewOnly, out error))
    {
        return Fail(error!);
    }

    var jsonOutPath = GetOption(options, "--json-out");
    var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
    TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);

    try
    {
        var plan = await LoadEditPlanAsync(planPath!, GetOption(options, "--output"));
        var request = new EditPlanRenderRequest
        {
            Plan = plan,
            OverwriteExisting = GetOption(options, "--overwrite") == "true"
        };

        var builder = new FfmpegEditPlanRenderCommandBuilder();
        var previewBuilder = new EditPlanExecutionPreviewBuilder(builder, new FfmpegEditPlanAudioMixCommandBuilder());
        var preview = previewBuilder.BuildRenderPreview(request, ffmpegPath);
        if (previewOnly == true)
        {
            return WriteCommandEnvelope(
                "render",
                preview: true,
                new
                {
                    render = request.Plan,
                    executionPreview = preview
                },
                jsonOutPath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(plan.Output.Path))!);

        var processRunner = new DefaultProcessRunner();
        var runner = new EditPlanRenderRunner(builder, processRunner);
        var result = await runner.RunAsync(request, ffmpegPath, timeout);

        return WriteCommandEnvelope(
            "render",
            preview: false,
            new
            {
                render = request.Plan,
                executionPreview = preview,
                execution = result
            },
            jsonOutPath);
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static async Task<int> RunSubtitleAsync(string[] args)
{
    if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--transcript", out var transcriptPath, out error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--format", out var rawFormat, out error))
    {
        return Fail(error!);
    }

    if (!TryParseSubtitleFormat(rawFormat!, out var format, out error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
    {
        return Fail(error!);
    }

    if (!TryGetIntOption(options, "--max-line-length", out var maxLineLength, out error))
    {
        return Fail(error!);
    }

    var jsonOutPath = GetOption(options, "--json-out");

    try
    {
        var transcript = await LoadTranscriptAsync(transcriptPath!);
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
        return Fail(ex.Message);
    }
}

static async Task<int> RunMixAudioAsync(string[] args)
{
    if (!TryParseOptions(args, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--plan", out var planPath, out error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
    {
        return Fail(error!);
    }

    if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
    {
        return Fail(error!);
    }

    if (!TryGetBoolOption(options, "--preview", out var previewOnly, out error))
    {
        return Fail(error!);
    }

    var jsonOutPath = GetOption(options, "--json-out");
    var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
    TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);

    try
    {
        var fullPlanPath = Path.GetFullPath(planPath!);
        var planDirectory = Path.GetDirectoryName(fullPlanPath)!;
        var plan = await LoadEditPlanAsync(fullPlanPath, outputOverridePath: null);
        var resolvedOutputPath = EditPlanPathResolver.ResolvePath(planDirectory, outputPath!);
        Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);

        var request = new EditPlanAudioMixRequest
        {
            Plan = plan,
            OutputPath = resolvedOutputPath,
            OverwriteExisting = GetOption(options, "--overwrite") == "true"
        };

        var builder = new FfmpegEditPlanAudioMixCommandBuilder();
        var previewBuilder = new EditPlanExecutionPreviewBuilder(new FfmpegEditPlanRenderCommandBuilder(), builder);
        var preview = previewBuilder.BuildAudioMixPreview(request, ffmpegPath);
        if (previewOnly == true)
        {
            return WriteCommandEnvelope(
                "mix-audio",
                preview: true,
                new
                {
                    mixAudio = new
                    {
                        planPath = fullPlanPath,
                        request.OutputPath
                    },
                    executionPreview = preview
                },
                jsonOutPath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);

        var processRunner = new DefaultProcessRunner();
        var runner = new EditPlanAudioMixRunner(builder, processRunner);
        var result = await runner.RunAsync(request, ffmpegPath, timeout);

        return WriteCommandEnvelope(
            "mix-audio",
            preview: false,
            new
            {
                mixAudio = new
                {
                    planPath = fullPlanPath,
                    request.OutputPath
                },
                executionPreview = preview,
                execution = result
            },
            jsonOutPath);
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static async Task<int> RunBeatTrackAsync(string[] args)
{
    if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
    {
        return Fail(error!);
    }

    if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
    {
        return Fail(error!);
    }

    if (!TryGetIntOption(options, "--sample-rate", out var sampleRateHz, out error))
    {
        return Fail(error!);
    }

    if (sampleRateHz is <= 0)
    {
        return Fail("Option '--sample-rate' must be greater than zero.");
    }

    var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
    TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
    var resolvedOutputPath = Path.GetFullPath(outputPath!);
    var tempWavePath = Path.Combine(Path.GetTempPath(), $"ovt-beat-track-{Guid.NewGuid():N}.wav");

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
            WriteJson(new
            {
                beatTrack = new
                {
                    inputPath,
                    outputPath = resolvedOutputPath
                },
                extraction
            });
            return 2;
        }

        var waveform = new WavePcmReader().ReadMono16Bit(tempWavePath);
        var beatTrack = new BeatTrackAnalyzer().Analyze(waveform, inputPath!);
        await File.WriteAllTextAsync(resolvedOutputPath, JsonSerializer.Serialize(beatTrack, OpenVideoToolboxJson.Default));

        WriteJson(new
        {
            beatTrack = new
            {
                inputPath,
                outputPath = resolvedOutputPath,
                beatCount = beatTrack.Beats.Count,
                beatTrack.EstimatedBpm
            },
            extraction
        });

        return 0;
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
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

static async Task<int> RunInitPlanAsync(string[] args)
{
    if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--template", out var templateId, out error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--output", out var planOutputPath, out error))
    {
        return Fail(error!);
    }

    var fullPlanOutputPath = Path.GetFullPath(planOutputPath!);

    try
    {
        var build = await BuildEditPlanFromTemplateAsync(inputPath!, templateId!, fullPlanOutputPath, options);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPlanOutputPath)!);
        await File.WriteAllTextAsync(fullPlanOutputPath, JsonSerializer.Serialize(build.Plan, OpenVideoToolboxJson.Default));

        WriteJson(new
        {
            template = build.Template,
            planPath = fullPlanOutputPath,
            probed = build.Probe is not null,
            editPlan = build.Plan
        });

        return 0;
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static async Task<int> RunScaffoldTemplateAsync(string[] args)
{
    if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--template", out var templateId, out error))
    {
        return Fail(error!);
    }

    if (!TryGetRequiredOption(options, "--dir", out var outputDirectory, out error))
    {
        return Fail(error!);
    }

    var fullOutputDirectory = Path.GetFullPath(outputDirectory!);
    var fullPlanOutputPath = Path.Combine(fullOutputDirectory, "edit.json");
    if (!TryGetBoolOption(options, "--validate", out var validateAfterWrite, out error))
    {
        return Fail(error!);
    }

    if (!TryGetBoolOption(options, "--check-files", out var checkFiles, out error))
    {
        return Fail(error!);
    }

    try
    {
        var build = await BuildEditPlanFromTemplateAsync(inputPath!, templateId!, fullPlanOutputPath, options);
        var template = BuiltInEditPlanTemplateCatalog.GetRequired(templateId!);
        var artifactsExample = EditPlanTemplateExampleBuilder.BuildArtifactBindingsExample(template);
        var templateParamsExample = EditPlanTemplateExampleBuilder.BuildTemplateParamsExample(template);
        var previewPlans = EditPlanTemplateExampleBuilder.BuildPreviewPlans(template);
        var supportingSignals = EditPlanTemplateExampleBuilder.BuildSupportingSignalExamples(template);
        var commands = BuildTemplateExampleCommands(template, artifactsExample.Count > 0, templateParamsExample.Count > 0);
        var seedCommands = BuildTemplateSeedCommands(template);
        var signalCommands = supportingSignals.Select(signal => signal.Command).ToArray();
        var signalInstructions = supportingSignals
            .Select(signal => new TemplateSignalInstruction
            {
                Kind = signal.Kind.ToString().ToLowerInvariant(),
                Command = signal.Command,
                Consumption = signal.Consumption
            })
            .ToArray();
        var artifactCommands = BuildTemplateArtifactCommands(template, supportingSignals);
        var exampleWriteResult = WriteTemplateExamples(
            template,
            fullOutputDirectory,
            artifactsExample,
            templateParamsExample,
            previewPlans,
            supportingSignals,
            commands,
            seedCommands,
            signalInstructions,
            signalCommands,
            artifactCommands);

        await File.WriteAllTextAsync(fullPlanOutputPath, JsonSerializer.Serialize(build.Plan, OpenVideoToolboxJson.Default));

        var canonicalFiles = new List<string>(exampleWriteResult.WrittenFiles)
        {
            fullPlanOutputPath
        };

        if (build.ArtifactBindings.Count > 0)
        {
            var artifactsPath = Path.Combine(fullOutputDirectory, "artifacts.json");
            await File.WriteAllTextAsync(artifactsPath, JsonSerializer.Serialize(build.ArtifactBindings, OpenVideoToolboxJson.Default));
            canonicalFiles.Add(artifactsPath);
        }

        if (build.ParameterOverrides.Count > 0)
        {
            var templateParamsPath = Path.Combine(fullOutputDirectory, "template-params.json");
            await File.WriteAllTextAsync(templateParamsPath, JsonSerializer.Serialize(build.ParameterOverrides, OpenVideoToolboxJson.Default));
            canonicalFiles.Add(templateParamsPath);
        }

        EditPlanValidationResult? validation = null;
        if (validateAfterWrite == true)
        {
            validation = await ValidatePlanFileAsync(fullPlanOutputPath, checkFiles == true);
        }

        WriteJson(new
        {
            template = build.Template,
            scaffold = new
            {
                outputDirectory = fullOutputDirectory,
                planPath = fullPlanOutputPath,
                writtenFiles = canonicalFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            },
            probed = build.Probe is not null,
            validated = validateAfterWrite == true,
            validation = validation is null
                ? null
                : new
                {
                    checkFiles = checkFiles == true,
                    isValid = validation.IsValid,
                    issues = validation.Issues
                },
            editPlan = build.Plan
        });

        return validation?.IsValid == false ? 1 : 0;
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static Task<int> RunPlanAsync(string[] args)
{
    if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
    {
        return Task.FromResult(Fail(error!));
    }

    try
    {
        var job = BuildJob(inputPath!, options, probeSnapshot: null);
        var plan = new FfmpegCommandBuilder().Build(job, GetOption(options, "--ffmpeg") ?? "ffmpeg");
        WriteJson(new
        {
            job,
            commandPlan = plan
        });
        return Task.FromResult(0);
    }
    catch (Exception ex)
    {
        return Task.FromResult(Fail(ex.Message));
    }
}

static async Task<int> RunTranscodeAsync(string[] args)
{
    if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
    {
        return Fail(error!);
    }

    if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
    {
        return Fail(error!);
    }

    var ffprobePath = GetOption(options, "--ffprobe") ?? "ffprobe";
    var ffmpegPath = GetOption(options, "--ffmpeg") ?? "ffmpeg";
    TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);

    var processRunner = new DefaultProcessRunner();
    var probeService = new FfprobeMediaProbeService(processRunner, new FfprobeJsonParser());
    var runner = new TranscodeJobRunner(new FfmpegCommandBuilder(), processRunner);

    try
    {
        var probeResult = await probeService.ProbeAsync(inputPath!, ffprobePath, timeout);
        var job = BuildJob(inputPath!, options, probeResult);
        Directory.CreateDirectory(job.Output.OutputDirectory);

        var result = await runner.RunAsync(job, ffmpegPath, timeout);

        WriteJson(new
        {
            job,
            execution = result
        });

        return result.Status == ExecutionStatus.Succeeded ? 0 : 2;
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static int RunPresets()
{
    WriteJson(BuiltInPresetCatalog.GetAll());
    return 0;
}

static object BuildCommandEnvelope(string command, bool preview, object payload)
{
    return new
    {
        command,
        preview,
        payload
    };
}

static int WriteCommandEnvelope(string command, bool preview, object payload, string? jsonOutPath = null, int exitCode = 0)
{
    return WriteResult(BuildCommandEnvelope(command, preview, payload), jsonOutPath, exitCode);
}

static int RunTemplates(string[] args)
{
    string? templateId = null;
    IReadOnlyDictionary<string, string> options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal))
    {
        templateId = args[0];
        if (!TryParseOptions(args.Skip(1).ToArray(), out var parsedOptions, out var error))
        {
            return Fail(error!);
        }

        options = parsedOptions;
    }
    else
    {
        if (!TryParseOptions(args, out var parsedOptions, out var error))
        {
            return Fail(error!);
        }

        options = parsedOptions;
        templateId = GetOption(options, "--template");
    }

    try
    {
        if (!TryParseSeedModeOption(GetOption(options, "--seed-mode"), out var seedMode, out var error))
        {
            return Fail(error!);
        }

        if (!TryGetBoolOption(options, "--has-artifacts", out var hasArtifacts, out error))
        {
            return Fail(error!);
        }

        if (!TryGetBoolOption(options, "--has-subtitles", out var hasSubtitles, out error))
        {
            return Fail(error!);
        }

        if (!TryGetBoolOption(options, "--summary", out var summaryOnly, out error))
        {
            return Fail(error!);
        }

        var query = new EditPlanTemplateCatalogQuery
        {
            Category = GetOption(options, "--category"),
            SeedMode = seedMode,
            OutputContainer = GetOption(options, "--output-container"),
            ArtifactKind = GetOption(options, "--artifact-kind"),
            HasArtifacts = hasArtifacts,
            HasSubtitles = hasSubtitles
        };
        var jsonOutPath = GetOption(options, "--json-out");

        if (string.IsNullOrWhiteSpace(templateId))
        {
            object templates = summaryOnly == true
                ? BuiltInEditPlanTemplateCatalog.GetSummaries(query)
                : BuiltInEditPlanTemplateCatalog.GetAll(query);

            return WriteResult(new
            {
                filters = query,
                summary = summaryOnly == true,
                templates
            }, jsonOutPath);
        }

        var template = BuiltInEditPlanTemplateCatalog.GetRequired(templateId!);
        var artifactsExample = EditPlanTemplateExampleBuilder.BuildArtifactBindingsExample(template);
        var templateParamsExample = EditPlanTemplateExampleBuilder.BuildTemplateParamsExample(template);
        var previewPlans = EditPlanTemplateExampleBuilder.BuildPreviewPlans(template);
        var supportingSignals = EditPlanTemplateExampleBuilder.BuildSupportingSignalExamples(template);
        var writeExamplesDirectory = GetOption(options, "--write-examples");
        TemplateExampleWriteResult? writeResult = null;
        var commands = BuildTemplateExampleCommands(template, artifactsExample.Count > 0, templateParamsExample.Count > 0);
        var seedCommands = BuildTemplateSeedCommands(template);
        var signalCommands = supportingSignals.Select(signal => signal.Command).ToArray();
        var signalInstructions = supportingSignals
            .Select(signal => new TemplateSignalInstruction
            {
                Kind = signal.Kind.ToString().ToLowerInvariant(),
                Command = signal.Command,
                Consumption = signal.Consumption
            })
            .ToArray();
        var artifactCommands = BuildTemplateArtifactCommands(template, supportingSignals);

        if (!string.IsNullOrWhiteSpace(writeExamplesDirectory))
        {
            writeResult = WriteTemplateExamples(
                template,
                writeExamplesDirectory,
                artifactsExample,
                templateParamsExample,
                previewPlans,
                supportingSignals,
                commands,
                seedCommands,
                signalInstructions,
                signalCommands,
                artifactCommands);
        }

        return WriteResult(BuildTemplateGuide(
            template,
            writeResult,
            artifactsExample,
            templateParamsExample,
            previewPlans,
            supportingSignals,
            commands,
            seedCommands,
            signalInstructions,
            signalCommands,
            artifactCommands), jsonOutPath);
    }
    catch (Exception ex)
    {
        return Fail(ex.Message);
    }
}

static TemplateExampleWriteResult WriteTemplateExamples(
    EditPlanTemplateDefinition template,
    string outputDirectory,
    IReadOnlyDictionary<string, string> artifactsExample,
    IReadOnlyDictionary<string, string> templateParamsExample,
    IReadOnlyList<EditPlanTemplatePreview> previewPlans,
    IReadOnlyList<EditPlanSupportingSignalExample> supportingSignals,
    IReadOnlyList<string> commands,
    IReadOnlyList<object> seedCommands,
    IReadOnlyList<TemplateSignalInstruction> signalInstructions,
    IReadOnlyList<string> signalCommands,
    IReadOnlyList<string> artifactCommands)
{
    var fullOutputDirectory = Path.GetFullPath(outputDirectory);
    Directory.CreateDirectory(fullOutputDirectory);
    var writtenFiles = new List<string>();

    var artifactsPath = Path.Combine(fullOutputDirectory, "artifacts.json");
    File.WriteAllText(
        artifactsPath,
        JsonSerializer.Serialize(artifactsExample, OpenVideoToolboxJson.Default),
        Encoding.UTF8);
    writtenFiles.Add(artifactsPath);

    var templateParamsPath = Path.Combine(fullOutputDirectory, "template-params.json");
    File.WriteAllText(
        templateParamsPath,
        JsonSerializer.Serialize(templateParamsExample, OpenVideoToolboxJson.Default),
        Encoding.UTF8);
    writtenFiles.Add(templateParamsPath);

    foreach (var preview in previewPlans)
    {
        var fileName = $"preview-{preview.Mode.ToString().ToLowerInvariant()}.edit.json";
        var previewPath = Path.Combine(fullOutputDirectory, fileName);
        File.WriteAllText(
            previewPath,
            JsonSerializer.Serialize(preview.EditPlan, OpenVideoToolboxJson.Default),
            Encoding.UTF8);
        writtenFiles.Add(previewPath);
    }

    var templatePath = Path.Combine(fullOutputDirectory, "template.json");
    File.WriteAllText(
        templatePath,
        JsonSerializer.Serialize(template, OpenVideoToolboxJson.Default),
        Encoding.UTF8);
    writtenFiles.Add(templatePath);

    var guide = BuildTemplateGuide(
        template,
        new TemplateExampleWriteResult
        {
            OutputDirectory = fullOutputDirectory,
            WrittenFiles = writtenFiles
        },
        artifactsExample,
        templateParamsExample,
        previewPlans,
        supportingSignals,
        commands,
        seedCommands,
        signalInstructions,
        signalCommands,
        artifactCommands);
    var guidePath = Path.Combine(fullOutputDirectory, "guide.json");
    File.WriteAllText(
        guidePath,
        JsonSerializer.Serialize(guide, OpenVideoToolboxJson.Default),
        Encoding.UTF8);
    writtenFiles.Add(guidePath);

    var commandBundle = TemplateCommandArtifactsBuilder.BuildCommandBundle(commands, seedCommands, signalInstructions, artifactCommands);

    var commandsJsonPath = Path.Combine(fullOutputDirectory, "commands.json");
    File.WriteAllText(
        commandsJsonPath,
        JsonSerializer.Serialize(commandBundle, OpenVideoToolboxJson.Default),
        Encoding.UTF8);
    writtenFiles.Add(commandsJsonPath);

    var commandsPs1Path = Path.Combine(fullOutputDirectory, "commands.ps1");
    File.WriteAllText(
        commandsPs1Path,
        TemplateCommandArtifactsBuilder.BuildPowerShellCommandScript(commandBundle),
        Encoding.UTF8);
    writtenFiles.Add(commandsPs1Path);

    var commandsCmdPath = Path.Combine(fullOutputDirectory, "commands.cmd");
    File.WriteAllText(
        commandsCmdPath,
        TemplateCommandArtifactsBuilder.BuildBatchCommandScript(commandBundle),
        Encoding.UTF8);
    writtenFiles.Add(commandsCmdPath);

    var commandsShPath = Path.Combine(fullOutputDirectory, "commands.sh");
    File.WriteAllText(
        commandsShPath,
        TemplateCommandArtifactsBuilder.BuildShellCommandScript(commandBundle),
        Encoding.UTF8);
    writtenFiles.Add(commandsShPath);

    return new TemplateExampleWriteResult
    {
        OutputDirectory = fullOutputDirectory,
        WrittenFiles = writtenFiles
    };
}

static object BuildTemplateGuide(
    EditPlanTemplateDefinition template,
    TemplateExampleWriteResult? writeResult,
    IReadOnlyDictionary<string, string> artifactsExample,
    IReadOnlyDictionary<string, string> templateParamsExample,
    IReadOnlyList<EditPlanTemplatePreview> previewPlans,
    IReadOnlyList<EditPlanSupportingSignalExample> supportingSignals,
    IReadOnlyList<string> commands,
    IReadOnlyList<object> seedCommands,
    IReadOnlyList<TemplateSignalInstruction> signalInstructions,
    IReadOnlyList<string> signalCommands,
    IReadOnlyList<string> artifactCommands)
{
    return new
    {
        template,
        examples = new
        {
            outputDirectory = writeResult?.OutputDirectory,
            writtenFiles = writeResult?.WrittenFiles,
            artifactsFileName = "artifacts.json",
            artifacts = artifactsExample,
            templateParamsFileName = "template-params.json",
            templateParams = templateParamsExample,
            seedModes = template.RecommendedSeedModes,
            supportingSignals,
            commandFiles = new[]
            {
                "commands.json",
                "commands.ps1",
                "commands.cmd",
                "commands.sh"
            },
            commands,
            signalInstructions,
            signalCommands,
            artifactCommands,
            seedCommands,
            previewPlans
        }
    };
}

static IReadOnlyList<string> BuildTemplateArtifactCommands(
    EditPlanTemplateDefinition template,
    IReadOnlyList<EditPlanSupportingSignalExample> supportingSignals)
{
    var hasSubtitleOutput = template.DefaultSubtitleMode is not null
        || template.ArtifactSlots.Any(slot =>
            string.Equals(slot.Kind, "subtitle", StringComparison.OrdinalIgnoreCase)
            || string.Equals(slot.Id, "subtitles", StringComparison.OrdinalIgnoreCase));
    var hasTranscriptSignal = supportingSignals.Any(signal => signal.Kind == EditPlanSupportingSignalKind.Transcript);

    if (!hasSubtitleOutput || !hasTranscriptSignal)
    {
        return [];
    }

    return
    [
        "ovt subtitle <input> --transcript transcript.json --format srt --output subtitles.srt"
    ];
}

static IReadOnlyList<string> BuildTemplateExampleCommands(EditPlanTemplateDefinition template, bool hasArtifacts, bool hasTemplateParams)
{
    var commands = new List<string>
    {
        $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer}"
    };

    if (hasArtifacts)
    {
        commands.Add(
            $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer} --artifacts artifacts.json");
    }

    if (hasTemplateParams)
    {
        commands.Add(
            $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer} --template-params template-params.json");
    }

    return commands;
}

static IReadOnlyList<object> BuildTemplateSeedCommands(EditPlanTemplateDefinition template)
{
    var commands = new List<object>();
    foreach (var mode in template.RecommendedSeedModes.Distinct())
    {
        commands.Add(new
        {
            mode,
            command = mode switch
            {
                EditPlanSeedMode.Manual =>
                    $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer}",
                EditPlanSeedMode.Transcript =>
                    $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer} --transcript transcript.json --seed-from-transcript",
                EditPlanSeedMode.Beats =>
                    $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer} --beats beats.json --seed-from-beats --beat-group-size 4",
                _ => throw new InvalidOperationException($"Unsupported template seed mode '{mode}'.")
            },
            variants = mode == EditPlanSeedMode.Transcript
                ? BuildTranscriptSeedCommandVariants(template)
                : null
        });
    }

    return commands;
}

static IReadOnlyList<object> BuildTranscriptSeedCommandVariants(EditPlanTemplateDefinition template)
{
    var variants = new[]
    {
        new
        {
            strategy = TranscriptSeedStrategy.Grouped,
            key = "grouped",
            command = $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer} --transcript transcript.json --seed-from-transcript --transcript-segment-group-size 2"
        },
        new
        {
            strategy = TranscriptSeedStrategy.MinDuration,
            key = "min-duration",
            command = $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer} --transcript transcript.json --seed-from-transcript --min-transcript-segment-duration-ms 500"
        },
        new
        {
            strategy = TranscriptSeedStrategy.MaxGap,
            key = "max-gap",
            command = $"ovt init-plan <input> --template {template.Id} --output edit.json --render-output final.{template.OutputContainer} --transcript transcript.json --seed-from-transcript --transcript-segment-group-size 3 --max-transcript-gap-ms 200"
        }
    };

    var rankedStrategies = template.RecommendedTranscriptSeedStrategies
        .Select((strategy, index) => new { strategy, index })
        .ToDictionary(item => item.strategy, item => item.index);

    return variants
        .OrderBy(variant => rankedStrategies.TryGetValue(variant.strategy, out var rank) ? rank : int.MaxValue)
        .ThenBy(variant => variant.key, StringComparer.Ordinal)
        .Select(variant => (object)new
        {
            variant.key,
            variant.command,
            recommended = rankedStrategies.ContainsKey(variant.strategy)
        })
        .ToArray();
}

static int ShowHelp()
{
    PrintUsage();
    return 0;
}

static JobDefinition BuildJob(string inputPath, IReadOnlyDictionary<string, string> options, MediaProbeResult? probeSnapshot)
{
    var presetId = GetOption(options, "--preset") ?? BuiltInPresetCatalog.DefaultPresetId;
    var preset = BuiltInPresetCatalog.GetRequired(presetId);

    var outputDirectory = GetOption(options, "--output-dir")
        ?? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(inputPath)) ?? Environment.CurrentDirectory, "output");
    var outputName = GetOption(options, "--output-name") ?? Path.GetFileNameWithoutExtension(inputPath);

    return new JobDefinition
    {
        Id = $"job-{Guid.NewGuid():N}",
        CreatedAtUtc = DateTimeOffset.UtcNow,
        Source = new JobSource
        {
            InputPath = inputPath
        },
        Output = new JobOutput
        {
            OutputDirectory = outputDirectory,
            FileNameStem = outputName,
            ContainerExtension = preset.Output.ContainerExtension,
            OverwriteExisting = GetOption(options, "--overwrite") == "true" || preset.Output.OverwriteExisting
        },
        Preset = preset,
        ProbeSnapshot = probeSnapshot,
        Tags = ["cli", "phase3", presetId]
    };
}

static bool TryParseFileCommand(
    string[] args,
    out string? inputPath,
    out IReadOnlyDictionary<string, string> options,
    out string? error)
{
    inputPath = null;
    options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    error = null;

    if (args.Length == 0)
    {
        error = "Missing input file path.";
        return false;
    }

    inputPath = args[0];
    if (!TryParseOptions(args.Skip(1).ToArray(), out var parsedOptions, out error))
    {
        return false;
    }

    options = parsedOptions;
    return true;
}

static bool TryParseOptions(string[] args, out IReadOnlyDictionary<string, string> options, out string? error)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    error = null;

    for (var index = 0; index < args.Length; index++)
    {
        var token = args[index];
        if (!token.StartsWith("--", StringComparison.Ordinal))
        {
            error = $"Unexpected token '{token}'.";
            options = result;
            return false;
        }

        if (index == args.Length - 1 || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            result[token] = "true";
            continue;
        }

        result[token] = args[index + 1];
        index++;
    }

    options = result;
    return true;
}

static string? GetOption(IReadOnlyDictionary<string, string> options, string name)
{
    return options.TryGetValue(name, out var value) ? value : null;
}

static bool TryGetRequiredOption(
    IReadOnlyDictionary<string, string> options,
    string name,
    out string? value,
    out string? error)
{
    value = GetOption(options, name);
    if (!string.IsNullOrWhiteSpace(value))
    {
        error = null;
        return true;
    }

    error = $"Option '{name}' is required.";
    return false;
}

static bool TryGetIntOption(
    IReadOnlyDictionary<string, string> options,
    string name,
    out int? value,
    out string? error)
{
    value = null;
    error = null;

    if (!options.TryGetValue(name, out var rawValue))
    {
        return true;
    }

    if (int.TryParse(rawValue, out var parsed))
    {
        value = parsed;
        return true;
    }

    error = $"Option '{name}' expects an integer value.";
    return false;
}

static bool TryGetBoolOption(
    IReadOnlyDictionary<string, string> options,
    string name,
    out bool? value,
    out string? error)
{
    value = null;
    error = null;

    if (!options.TryGetValue(name, out var rawValue))
    {
        return true;
    }

    if (bool.TryParse(rawValue, out var parsed))
    {
        value = parsed;
        return true;
    }

    error = $"Option '{name}' expects a boolean value.";
    return false;
}

static bool TryGetRequiredIntOption(
    IReadOnlyDictionary<string, string> options,
    string name,
    out int value,
    out string? error)
{
    value = default;
    if (!options.TryGetValue(name, out var rawValue))
    {
        error = $"Option '{name}' is required.";
        return false;
    }

    if (int.TryParse(rawValue, out var parsed))
    {
        value = parsed;
        error = null;
        return true;
    }

    error = $"Option '{name}' expects an integer value.";
    return false;
}

static bool TryGetRequiredDoubleOption(
    IReadOnlyDictionary<string, string> options,
    string name,
    out double value,
    out string? error)
{
    value = default;
    if (!options.TryGetValue(name, out var rawValue))
    {
        error = $"Option '{name}' is required.";
        return false;
    }

    if (double.TryParse(rawValue, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
    {
        value = parsed;
        error = null;
        return true;
    }

    error = $"Option '{name}' expects a numeric value.";
    return false;
}

static bool TryGetDoubleOption(
    IReadOnlyDictionary<string, string> options,
    string name,
    out double? value,
    out string? error)
{
    value = null;
    error = null;

    if (!options.TryGetValue(name, out var rawValue))
    {
        return true;
    }

    if (double.TryParse(rawValue, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
    {
        value = parsed;
        return true;
    }

    error = $"Option '{name}' expects a numeric value.";
    return false;
}

static bool TryGetRequiredTimeSpanOption(
    IReadOnlyDictionary<string, string> options,
    string name,
    out TimeSpan value,
    out string? error)
{
    value = default;
    if (!options.TryGetValue(name, out var rawValue))
    {
        error = $"Option '{name}' is required.";
        return false;
    }

    if (TimeSpan.TryParse(rawValue, out var parsed))
    {
        value = parsed;
        error = null;
        return true;
    }

    error = $"Option '{name}' expects a time span like 00:00:12.500.";
    return false;
}

static void WriteJson<T>(T value)
{
    Console.WriteLine(JsonSerializer.Serialize(value, OpenVideoToolboxJson.Default));
}

static void WriteOutput<T>(T value, string? jsonOutPath)
{
    if (!string.IsNullOrWhiteSpace(jsonOutPath))
    {
        var fullPath = Path.GetFullPath(jsonOutPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, JsonSerializer.Serialize(value, OpenVideoToolboxJson.Default), Encoding.UTF8);
    }

    WriteJson(value);
}

static int WriteResult<T>(T value, string? jsonOutPath, int exitCode = 0)
{
    WriteOutput(value, jsonOutPath);
    return exitCode;
}

static IReadOnlyList<DependencyProbeDefinition> BuildDoctorDependencyDefinitions(
    IReadOnlyDictionary<string, string> options,
    TimeSpan? timeout)
{
    return
    [
        CreateExecutableDependency(
            id: "ffmpeg",
            required: true,
            optionValue: GetOption(options, "--ffmpeg"),
            environmentVariableName: null,
            defaultValue: "ffmpeg",
            probeArguments: ["-version"],
            timeout),
        CreateExecutableDependency(
            id: "ffprobe",
            required: true,
            optionValue: GetOption(options, "--ffprobe"),
            environmentVariableName: null,
            defaultValue: "ffprobe",
            probeArguments: ["-version"],
            timeout),
        CreateExecutableDependency(
            id: "whisper-cli",
            required: false,
            optionValue: GetOption(options, "--whisper-cli"),
            environmentVariableName: "OVT_WHISPER_CLI_PATH",
            defaultValue: "whisper-cli",
            probeArguments: ["--help"],
            timeout),
        CreateExecutableDependency(
            id: "demucs",
            required: false,
            optionValue: GetOption(options, "--demucs"),
            environmentVariableName: "OVT_DEMUCS_PATH",
            defaultValue: "demucs",
            probeArguments: ["--help"],
            timeout),
        CreateFileDependency(
            id: "whisper-model",
            required: false,
            optionValue: GetOption(options, "--whisper-model"),
            environmentVariableName: "OVT_WHISPER_MODEL_PATH")
    ];
}

static DependencyProbeDefinition CreateExecutableDependency(
    string id,
    bool required,
    string? optionValue,
    string? environmentVariableName,
    string? defaultValue,
    IReadOnlyList<string> probeArguments,
    TimeSpan? timeout)
{
    var resolution = ResolveDependencyValue(optionValue, environmentVariableName, defaultValue);
    return new DependencyProbeDefinition
    {
        Id = id,
        Kind = DependencyProbeKind.Executable,
        Required = required,
        Source = resolution.Source,
        ResolvedValue = resolution.Value,
        ProbeArguments = probeArguments,
        Timeout = timeout
    };
}

static DependencyProbeDefinition CreateFileDependency(
    string id,
    bool required,
    string? optionValue,
    string? environmentVariableName)
{
    var resolution = ResolveDependencyValue(optionValue, environmentVariableName, defaultValue: null);
    return new DependencyProbeDefinition
    {
        Id = id,
        Kind = DependencyProbeKind.File,
        Required = required,
        Source = resolution.Source,
        ResolvedValue = string.IsNullOrWhiteSpace(resolution.Value)
            ? null
            : Path.GetFullPath(resolution.Value)
    };
}

static (string? Value, DependencyValueSource Source) ResolveDependencyValue(
    string? optionValue,
    string? environmentVariableName,
    string? defaultValue)
{
    if (!string.IsNullOrWhiteSpace(optionValue))
    {
        return (optionValue, DependencyValueSource.Option);
    }

    if (!string.IsNullOrWhiteSpace(environmentVariableName))
    {
        var environmentValue = Environment.GetEnvironmentVariable(environmentVariableName);
        if (!string.IsNullOrWhiteSpace(environmentValue))
        {
            return (environmentValue.Trim(), DependencyValueSource.Environment);
        }
    }

    if (!string.IsNullOrWhiteSpace(defaultValue))
    {
        return (defaultValue, DependencyValueSource.Default);
    }

    return (null, DependencyValueSource.Unset);
}

static async Task<EditPlan> LoadEditPlanAsync(string planPath, string? outputOverridePath)
{
    var fullPlanPath = Path.GetFullPath(planPath);
    var content = await File.ReadAllTextAsync(fullPlanPath);
    var plan = JsonSerializer.Deserialize<EditPlan>(content, OpenVideoToolboxJson.Default)
        ?? throw new InvalidOperationException($"Failed to parse edit plan '{planPath}'.");

    if (plan.SchemaVersion != 1)
    {
        throw new InvalidOperationException($"Unsupported edit plan schema version '{plan.SchemaVersion}'.");
    }

    var resolvedPlan = EditPlanPathResolver.ResolvePaths(plan, Path.GetDirectoryName(fullPlanPath)!);
    if (string.IsNullOrWhiteSpace(outputOverridePath))
    {
        return resolvedPlan;
    }

    var resolvedOutputPath = EditPlanPathResolver.ResolvePath(Path.GetDirectoryName(fullPlanPath)!, outputOverridePath);
    var container = Path.GetExtension(resolvedOutputPath);
    if (container.StartsWith(".", StringComparison.Ordinal))
    {
        container = container[1..];
    }

    return resolvedPlan with
    {
        Output = resolvedPlan.Output with
        {
            Path = resolvedOutputPath,
            Container = string.IsNullOrWhiteSpace(container) ? resolvedPlan.Output.Container : container
        }
    };
}

static async Task<TranscriptDocument> LoadTranscriptAsync(string transcriptPath)
{
    var fullPath = Path.GetFullPath(transcriptPath);
    var content = await File.ReadAllTextAsync(fullPath);
    var transcript = JsonSerializer.Deserialize<TranscriptDocument>(content, OpenVideoToolboxJson.Default)
        ?? throw new InvalidOperationException($"Failed to parse transcript '{transcriptPath}'.");

    if (transcript.SchemaVersion != 1)
    {
        throw new InvalidOperationException($"Unsupported transcript schema version '{transcript.SchemaVersion}'.");
    }

    return transcript;
}

static async Task<BeatTrackDocument> LoadBeatTrackAsync(string beatsPath)
{
    var fullPath = Path.GetFullPath(beatsPath);
    var content = await File.ReadAllTextAsync(fullPath);
    var beatTrack = JsonSerializer.Deserialize<BeatTrackDocument>(content, OpenVideoToolboxJson.Default)
        ?? throw new InvalidOperationException($"Failed to parse beat track '{beatsPath}'.");

    if (beatTrack.SchemaVersion != 1)
    {
        throw new InvalidOperationException($"Unsupported beat track schema version '{beatTrack.SchemaVersion}'.");
    }

    return beatTrack;
}

static async Task<EditPlanValidationResult> ValidatePlanFileAsync(string fullPlanPath, bool checkFiles)
{
    var content = await File.ReadAllTextAsync(fullPlanPath);
    var plan = JsonSerializer.Deserialize<EditPlan>(content, OpenVideoToolboxJson.Default)
        ?? throw new InvalidOperationException($"Failed to parse edit plan '{fullPlanPath}'.");

    if (plan.SchemaVersion != 1)
    {
        throw new InvalidOperationException($"Unsupported edit plan schema version '{plan.SchemaVersion}'.");
    }

    var resolvedPlan = EditPlanPathResolver.ResolvePaths(plan, Path.GetDirectoryName(fullPlanPath)!);
    return new EditPlanValidator().Validate(resolvedPlan, checkFiles);
}

static async Task<TemplatePlanBuildResult> BuildEditPlanFromTemplateAsync(
    string inputPath,
    string templateId,
    string fullPlanOutputPath,
    IReadOnlyDictionary<string, string> options)
{
    if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out var error))
    {
        throw new InvalidOperationException(error!);
    }

    if (!TryGetIntOption(options, "--beat-group-size", out var beatGroupSize, out error))
    {
        throw new InvalidOperationException(error!);
    }

    if (!TryGetIntOption(options, "--transcript-segment-group-size", out var transcriptSegmentGroupSize, out error))
    {
        throw new InvalidOperationException(error!);
    }

    if (!TryGetIntOption(options, "--min-transcript-segment-duration-ms", out var minTranscriptSegmentDurationMs, out error))
    {
        throw new InvalidOperationException(error!);
    }

    if (!TryGetIntOption(options, "--max-transcript-gap-ms", out var maxTranscriptGapMs, out error))
    {
        throw new InvalidOperationException(error!);
    }

    if (GetOption(options, "--seed-from-transcript") == "true" && GetOption(options, "--seed-from-beats") == "true")
    {
        throw new InvalidOperationException("Options '--seed-from-transcript' and '--seed-from-beats' cannot be used together.");
    }

    if (!TryParseSubtitleModeOption(GetOption(options, "--subtitle-mode"), out var subtitleMode, out var disableSubtitles, out error))
    {
        throw new InvalidOperationException(error!);
    }

    var template = BuiltInEditPlanTemplateCatalog.GetRequired(templateId);
    var planDirectory = Path.GetDirectoryName(fullPlanOutputPath)!;
    var renderOutputPath = GetOption(options, "--render-output")
        ?? Path.Combine(planDirectory, $"{Path.GetFileNameWithoutExtension(inputPath)}.edited.{template.OutputContainer}");
    renderOutputPath = EditPlanPathResolver.ResolvePath(planDirectory, renderOutputPath);

    MediaProbeResult? probe = null;
    var shouldProbe = GetOption(options, "--probe") == "true" || !string.IsNullOrWhiteSpace(GetOption(options, "--ffprobe"));
    if (shouldProbe)
    {
        var ffprobePath = GetOption(options, "--ffprobe") ?? "ffprobe";
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
        var processRunner = new DefaultProcessRunner();
        var probeService = new FfprobeMediaProbeService(processRunner, new FfprobeJsonParser());
        probe = await probeService.ProbeAsync(inputPath, ffprobePath, timeout);
    }

    var beatsPath = GetOption(options, "--beats");
    var beatTrack = string.IsNullOrWhiteSpace(beatsPath) ? null : await LoadBeatTrackAsync(beatsPath);

    var transcriptPath = GetOption(options, "--transcript");
    var transcript = string.IsNullOrWhiteSpace(transcriptPath) ? null : await LoadTranscriptAsync(transcriptPath);

    IReadOnlyDictionary<string, string> artifactBindings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var artifactsPath = GetOption(options, "--artifacts");
    if (!string.IsNullOrWhiteSpace(artifactsPath))
    {
        artifactBindings = await LoadStringMapAsync(
            artifactsPath,
            "artifact bindings",
            "Expected a JSON object like {\"slotId\":\"path\"}.");
    }

    IReadOnlyDictionary<string, string> parameterOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var templateParamsPath = GetOption(options, "--template-params");
    if (!string.IsNullOrWhiteSpace(templateParamsPath))
    {
        parameterOverrides = await LoadStringMapAsync(
            templateParamsPath,
            "template parameters",
            "Expected a JSON object like {\"hookStyle\":\"hard-cut\"}.");
    }

    var plan = new EditPlanTemplateFactory().Create(
        templateId,
        new EditPlanTemplateRequest
        {
            InputPath = inputPath,
            RenderOutputPath = renderOutputPath,
            SourceDuration = probe?.Format.Duration,
            ParameterOverrides = parameterOverrides,
            TranscriptPath = transcriptPath,
            Transcript = transcript,
            SeedClipsFromTranscript = GetOption(options, "--seed-from-transcript") == "true",
            TranscriptSegmentGroupSize = transcriptSegmentGroupSize ?? 1,
            MinTranscriptSegmentDuration = TimeSpan.FromMilliseconds(minTranscriptSegmentDurationMs ?? 0),
            MaxTranscriptGap = maxTranscriptGapMs is null ? null : TimeSpan.FromMilliseconds(maxTranscriptGapMs.Value),
            SubtitlePath = GetOption(options, "--subtitle"),
            SubtitleModeOverride = subtitleMode,
            DisableSubtitles = disableSubtitles,
            BeatTrackPath = beatsPath,
            BeatTrack = beatTrack,
            SeedClipsFromBeats = GetOption(options, "--seed-from-beats") == "true",
            BeatGroupSize = beatGroupSize ?? 4,
            ArtifactBindings = artifactBindings,
            BgmPath = GetOption(options, "--bgm")
        });

    return new TemplatePlanBuildResult
    {
        Template = template,
        Plan = plan,
        Probe = probe,
        ArtifactBindings = artifactBindings,
        ParameterOverrides = parameterOverrides
    };
}

static async Task<IReadOnlyDictionary<string, string>> LoadStringMapAsync(
    string jsonPath,
    string logicalName,
    string shapeHint)
{
    var fullPath = Path.GetFullPath(jsonPath);
    var content = await File.ReadAllTextAsync(fullPath);
    var root = JsonNode.Parse(content) as JsonObject
        ?? throw new InvalidOperationException(
            $"Failed to parse {logicalName} '{jsonPath}'. {shapeHint}");

    var bindings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var pair in root)
    {
        if (string.IsNullOrWhiteSpace(pair.Key))
        {
            throw new InvalidOperationException($"{logicalName} '{jsonPath}' contains an empty key.");
        }

        if (pair.Value is not JsonValue value || !value.TryGetValue<string>(out var path) || string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                $"Key '{pair.Key}' in {logicalName} '{jsonPath}' must map to a non-empty string value.");
        }

        bindings[pair.Key] = path;
    }

    return bindings;
}

static bool TryParseSubtitleFormat(string rawValue, out SubtitleFormat format, out string? error)
{
    if (Enum.TryParse<SubtitleFormat>(rawValue, ignoreCase: true, out format))
    {
        error = null;
        return true;
    }

    error = "Option '--format' expects one of: srt, ass.";
    return false;
}

static bool TryParseSubtitleModeOption(string? rawValue, out SubtitleMode? mode, out bool disableSubtitles, out string? error)
{
    mode = null;
    disableSubtitles = false;
    error = null;

    if (string.IsNullOrWhiteSpace(rawValue))
    {
        return true;
    }

    if (string.Equals(rawValue, "none", StringComparison.OrdinalIgnoreCase))
    {
        disableSubtitles = true;
        return true;
    }

    if (Enum.TryParse<SubtitleMode>(rawValue, ignoreCase: true, out var parsed))
    {
        mode = parsed;
        return true;
    }

    error = "Option '--subtitle-mode' expects one of: sidecar, burnIn, none.";
    return false;
}

static bool TryParseSeedModeOption(string? rawValue, out EditPlanSeedMode? mode, out string? error)
{
    mode = null;
    error = null;

    if (string.IsNullOrWhiteSpace(rawValue))
    {
        return true;
    }

    if (Enum.TryParse<EditPlanSeedMode>(rawValue, ignoreCase: true, out var parsed))
    {
        mode = parsed;
        return true;
    }

    error = "Option '--seed-mode' expects one of: manual, transcript, beats.";
    return false;
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    Console.Error.WriteLine();
    PrintUsage();
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("Open Video Toolbox CLI");
    Console.WriteLine("Commands:");
    Console.WriteLine("  presets");
    Console.WriteLine("  templates [<template-id>] [--template <id>] [--category <id>] [--seed-mode <manual|transcript|beats>] [--output-container <ext>] [--artifact-kind <kind>] [--has-artifacts [true|false]] [--has-subtitles [true|false]] [--summary [true|false]] [--json-out <path>] [--write-examples <dir>]");
    Console.WriteLine("  doctor [--ffmpeg <path>] [--ffprobe <path>] [--whisper-cli <path>] [--whisper-model <path>] [--demucs <path>] [--json-out <path>] [--timeout-seconds <n>]");
    Console.WriteLine("  beat-track <input> --output <beats.json> [--ffmpeg <path>] [--sample-rate <hz>] [--timeout-seconds <n>]");
    Console.WriteLine("  audio-analyze <input> --output <audio.json> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>]");
    Console.WriteLine("  audio-gain <input> --gain-db <n> --output <path> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>] [--overwrite]");
    Console.WriteLine("  transcribe <input> --model <path> --output <transcript.json> [--language <id>] [--translate [true|false]] [--whisper-cli <path>] [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>]");
    Console.WriteLine("  detect-silence <input> --output <silence.json> [--noise-db <n>] [--min-duration-ms <n>] [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>]");
    Console.WriteLine("  separate-audio <input> --output-dir <path> [--model <id>] [--demucs <path>] [--json-out <path>] [--timeout-seconds <n>]");
    Console.WriteLine("  cut <input> --from <hh:mm:ss.fff> --to <hh:mm:ss.fff> --output <path> [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]");
    Console.WriteLine("  concat --input-list <path> --output <path> [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]");
    Console.WriteLine("  extract-audio <input> --track <n> --output <path> [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]");
    Console.WriteLine("  init-plan <input> --template <id> --output <edit.json> [--render-output <path>] [--probe] [--ffprobe <path>] [--transcript <transcript.json>] [--seed-from-transcript] [--transcript-segment-group-size <n>] [--min-transcript-segment-duration-ms <n>] [--max-transcript-gap-ms <n>] [--beats <beats.json>] [--seed-from-beats] [--beat-group-size <n>] [--artifacts <artifacts.json>] [--template-params <template-params.json>] [--subtitle <path>] [--subtitle-mode <sidecar|burnIn|none>] [--bgm <path>] [--timeout-seconds <n>]");
    Console.WriteLine("  scaffold-template <input> --template <id> --dir <workdir> [--validate [true|false]] [--check-files [true|false]] [--render-output <path>] [--probe] [--ffprobe <path>] [--transcript <transcript.json>] [--seed-from-transcript] [--transcript-segment-group-size <n>] [--min-transcript-segment-duration-ms <n>] [--max-transcript-gap-ms <n>] [--beats <beats.json>] [--seed-from-beats] [--beat-group-size <n>] [--artifacts <artifacts.json>] [--template-params <template-params.json>] [--subtitle <path>] [--subtitle-mode <sidecar|burnIn|none>] [--bgm <path>] [--timeout-seconds <n>]");
    Console.WriteLine("  mix-audio --plan <edit.json> --output <path> [--preview [true|false]] [--json-out <path>] [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]");
    Console.WriteLine("  render --plan <path> [--output <path>] [--preview [true|false]] [--json-out <path>] [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]");
    Console.WriteLine("  subtitle <input> --transcript <transcript.json> --format <srt|ass> --output <path> [--max-line-length <n>] [--json-out <path>]");
    Console.WriteLine("  validate-plan --plan <edit.json> [--check-files [true|false]] [--json-out <path>]");
    Console.WriteLine("  probe <input> [--ffprobe <path>]");
    Console.WriteLine("  plan <input> [--preset <id>] [--output-dir <dir>] [--output-name <name>] [--ffmpeg <path>] [--overwrite]");
    Console.WriteLine("  run <input> [--preset <id>] [--output-dir <dir>] [--output-name <name>] [--ffprobe <path>] [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]");
    Console.WriteLine();
    Console.WriteLine("Built-in presets:");
    foreach (var preset in BuiltInPresetCatalog.GetAll())
    {
        Console.WriteLine($"  {preset.Id} - {preset.DisplayName}");
    }
}

sealed record TemplateExampleWriteResult
{
    public required string OutputDirectory { get; init; }

    public required IReadOnlyList<string> WrittenFiles { get; init; }
}

sealed record TemplateCommandBundle
{
    public IReadOnlyDictionary<string, string> Variables { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<string> InitPlanCommands { get; init; } = [];

    public IReadOnlyList<object> SeedCommands { get; init; } = [];

    public IReadOnlyList<string> WorkflowCommands { get; init; } = [];
}

sealed record TemplatePlanBuildResult
{
    public required EditPlanTemplateDefinition Template { get; init; }

    public required EditPlan Plan { get; init; }

    public MediaProbeResult? Probe { get; init; }

    public IReadOnlyDictionary<string, string> ArtifactBindings { get; init; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, string> ParameterOverrides { get; init; } = new Dictionary<string, string>();
}
