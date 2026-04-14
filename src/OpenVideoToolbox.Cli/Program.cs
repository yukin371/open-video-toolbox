using System.Text.Json;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Jobs;
using OpenVideoToolbox.Core.Media;
using OpenVideoToolbox.Core.Presets;
using OpenVideoToolbox.Core.Serialization;

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
        "probe" => await RunProbeAsync(remaining),
        "plan" => await RunPlanAsync(remaining),
        "run" => await RunTranscodeAsync(remaining),
        "presets" => RunPresets(),
        "help" or "--help" or "-h" => ShowHelp(),
        _ => Fail($"Unknown command '{args[0]}'.")
    };
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

static void WriteJson<T>(T value)
{
    Console.WriteLine(JsonSerializer.Serialize(value, OpenVideoToolboxJson.Default));
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
