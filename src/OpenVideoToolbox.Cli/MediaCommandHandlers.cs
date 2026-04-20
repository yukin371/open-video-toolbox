using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Media;
using static OpenVideoToolbox.Cli.CliCommandOutput;
using static OpenVideoToolbox.Cli.CliOptionParsing;

namespace OpenVideoToolbox.Cli;

internal static class MediaCommandHandlers
{
    public static async Task<int> RunCutAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--output", out var outputPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredTimeSpanOption(options, "--from", out var start, out error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredTimeSpanOption(options, "--to", out var end, out error))
        {
            return fail(error!);
        }

        if (end <= start)
        {
            return fail("Option '--to' must be greater than '--from'.");
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
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
            if (result.Status != ExecutionStatus.Succeeded)
            {
                var message = BuildExecutionFailureMessage(result);
                return FailWithCommandEnvelope(
                    "cut",
                    preview: false,
                    BuildFailedCommandPayload("cut", request, message, result),
                    message,
                    jsonOutPath,
                    exitCode: 2);
            }

            return WriteCommandEnvelope(
                "cut",
                preview: false,
                new
                {
                    cut = request,
                    execution = result
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "cut",
                preview: false,
                BuildFailedCommandPayload("cut", request, ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    public static async Task<int> RunConcatAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--input-list", out var inputListPath, out error))
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
            if (result.Status != ExecutionStatus.Succeeded)
            {
                var message = BuildExecutionFailureMessage(result);
                return FailWithCommandEnvelope(
                    "concat",
                    preview: false,
                    BuildFailedCommandPayload("concat", request, message, result),
                    message,
                    jsonOutPath,
                    exitCode: 2);
            }

            return WriteCommandEnvelope(
                "concat",
                preview: false,
                new
                {
                    concat = request,
                    execution = result
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "concat",
                preview: false,
                BuildFailedCommandPayload("concat", request, ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }

    public static async Task<int> RunExtractAudioAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredIntOption(options, "--track", out var trackIndex, out error))
        {
            return fail(error!);
        }

        if (trackIndex < 0)
        {
            return fail("Option '--track' must be zero or greater.");
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
            if (result.Status != ExecutionStatus.Succeeded)
            {
                var message = BuildExecutionFailureMessage(result);
                return FailWithCommandEnvelope(
                    "extract-audio",
                    preview: false,
                    BuildFailedCommandPayload("extractAudio", request, message, result),
                    message,
                    jsonOutPath,
                    exitCode: 2);
            }

            return WriteCommandEnvelope(
                "extract-audio",
                preview: false,
                new
                {
                    extractAudio = request,
                    execution = result
                },
                jsonOutPath);
        }
        catch (Exception ex)
        {
            return FailWithCommandEnvelope(
                "extract-audio",
                preview: false,
                BuildFailedCommandPayload("extractAudio", request, ex.Message),
                ex.Message,
                jsonOutPath);
        }
    }
}
