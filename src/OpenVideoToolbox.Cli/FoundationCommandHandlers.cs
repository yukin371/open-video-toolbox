using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Media;
using static OpenVideoToolbox.Cli.CliCommandOutput;
using static OpenVideoToolbox.Cli.CliOptionParsing;

namespace OpenVideoToolbox.Cli;

internal static class FoundationCommandHandlers
{
    public static async Task<int> RunDoctorAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
        }

        var jsonOutPath = GetOption(options, "--json-out");
        TimeSpan? timeout = timeoutSeconds is null
            ? TimeSpan.FromSeconds(5)
            : TimeSpan.FromSeconds(timeoutSeconds.Value);

        try
        {
            var inspector = new ExternalDependencyInspector(new DefaultProcessRunner());
            var report = await inspector.InspectAsync(FoundationCommandSupport.BuildDoctorDependencyDefinitions(options, timeout));

            return WriteCommandEnvelope(
                "doctor",
                preview: false,
                report,
                jsonOutPath,
                exitCode: report.IsHealthy ? 0 : 1);
        }
        catch (Exception ex)
        {
            return fail(ex.Message);
        }
    }

    public static async Task<int> RunProbeAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
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
            return fail(ex.Message);
        }
    }

    public static async Task<int> RunValidatePlanAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetRequiredOption(options, "--plan", out var planPath, out error))
        {
            return fail(error!);
        }

        if (!TryGetBoolOption(options, "--check-files", out var checkFiles, out error))
        {
            return fail(error!);
        }

        var jsonOutPath = GetOption(options, "--json-out");
        var pluginCatalog = TemplatePluginCatalogLoader.Load(GetOption(options, "--plugin-dir"));
        var fullPlanPath = Path.GetFullPath(planPath!);
        var planDirectory = Path.GetDirectoryName(fullPlanPath)!;

        try
        {
            var validation = await TemplatePlanValidationSupport.ValidatePlanFileAsync(fullPlanPath, checkFiles == true, pluginCatalog);
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

    public static Task<int> RunPlanAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return Task.FromResult(fail(error!));
        }

        try
        {
            var job = FoundationCommandSupport.BuildJob(inputPath!, options, probeSnapshot: null);
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
            return Task.FromResult(fail(ex.Message));
        }
    }

    public static async Task<int> RunTranscodeAsync(string[] args, Func<string, int> fail)
    {
        if (!TryParseFileCommand(args, out var inputPath, out var options, out var error))
        {
            return fail(error!);
        }

        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out error))
        {
            return fail(error!);
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
            var job = FoundationCommandSupport.BuildJob(inputPath!, options, probeResult);
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
            return fail(ex.Message);
        }
    }
}
