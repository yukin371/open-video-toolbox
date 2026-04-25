using OpenVideoToolbox.Cli;
using OpenVideoToolbox.Core.Presets;
using static OpenVideoToolbox.Cli.CliCommandOutput;

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
        "doctor" => await FoundationCommandHandlers.RunDoctorAsync(remaining, Fail),
        "effects" => EffectsCommandHandlers.RunEffects(remaining, Fail),
        "validate-plugin" => FoundationCommandHandlers.RunValidatePlugin(remaining, Fail),
        "init-plan" => await TemplateCommandHandlers.RunInitPlanAsync(remaining, Fail),
        "init-narrated-plan" => await TemplateCommandHandlers.RunInitNarratedPlanAsync(remaining, Fail),
        "scaffold-template" => await TemplateCommandHandlers.RunScaffoldTemplateAsync(remaining, Fail),
        "scaffold-template-batch" => await TemplateCommandHandlers.RunScaffoldTemplateBatchAsync(remaining, Fail),
        "extract-audio" => await MediaCommandHandlers.RunExtractAudioAsync(remaining, Fail),
        "audio-analyze" => await AudioCommandHandlers.RunAudioAnalyzeAsync(remaining, Fail),
        "audio-gain" => await AudioCommandHandlers.RunAudioGainAsync(remaining, Fail),
        "audio-normalize" => await AudioCommandHandlers.RunAudioNormalizeAsync(remaining, Fail),
        "transcribe" => await AudioCommandHandlers.RunTranscribeAsync(remaining, Fail),
        "detect-silence" => await AudioCommandHandlers.RunDetectSilenceAsync(remaining, Fail),
        "auto-cut-silence" => await AudioCommandHandlers.RunAutoCutSilenceAsync(remaining, Fail),
        "separate-audio" => await AudioCommandHandlers.RunSeparateAudioAsync(remaining, Fail),
        "beat-track" => await AudioCommandHandlers.RunBeatTrackAsync(remaining, Fail),
        "concat" => await MediaCommandHandlers.RunConcatAsync(remaining, Fail),
        "cut" => await MediaCommandHandlers.RunCutAsync(remaining, Fail),
        "export" => await RenderCommandHandlers.RunExportAsync(remaining, Fail),
        "mix-audio" => await RenderCommandHandlers.RunMixAudioAsync(remaining, Fail),
        "render" => await RenderCommandHandlers.RunRenderAsync(remaining, Fail),
        "render-batch" => await RenderCommandHandlers.RunRenderBatchAsync(remaining, Fail),
        "subtitle" => await AudioCommandHandlers.RunSubtitleAsync(remaining, Fail),
        "inspect-plan" => await EditPlanCommandHandlers.RunInspectPlanAsync(remaining, Fail),
        "replace-plan-material" => await EditPlanCommandHandlers.RunReplacePlanMaterialAsync(remaining, Fail),
        "replace-plan-material-batch" => await EditPlanCommandHandlers.RunReplacePlanMaterialBatchAsync(remaining, Fail),
        "attach-plan-material" => await EditPlanCommandHandlers.RunAttachPlanMaterialAsync(remaining, Fail),
        "attach-plan-material-batch" => await EditPlanCommandHandlers.RunAttachPlanMaterialBatchAsync(remaining, Fail),
        "bind-voice-track" => await EditPlanCommandHandlers.RunBindVoiceTrackAsync(remaining, Fail),
        "bind-voice-track-batch" => await EditPlanCommandHandlers.RunBindVoiceTrackBatchAsync(remaining, Fail),
        "validate-plan" => await EditPlanCommandHandlers.RunValidatePlanAsync(remaining, Fail),
        "probe" => await FoundationCommandHandlers.RunProbeAsync(remaining, Fail),
        "plan" => await FoundationCommandHandlers.RunPlanAsync(remaining, Fail),
        "run" => await FoundationCommandHandlers.RunTranscodeAsync(remaining, Fail),
        "templates" => TemplateCommandHandlers.RunTemplates(remaining, Fail),
        "presets" => RunPresets(remaining),
        "help" or "--help" or "-h" => ShowHelp(),
        _ => Fail($"Unknown command '{args[0]}'.")
    };
}

static int RunPresets(string[] args)
{
    CliOptionParsing.TryParseOptions(args, out var options, out _);
    var jsonOutPath = CliOptionParsing.GetOption(options, "--json-out");
    return WriteCommandEnvelope("presets", preview: false, BuiltInPresetCatalog.GetAll(), jsonOutPath);
}

static int ShowHelp()
{
    PrintUsage();
    return 0;
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
    Console.WriteLine("  presets [--json-out <path>]");
    Console.WriteLine("  templates [<template-id>] [--template <id>] [--category <id>] [--seed-mode <manual|transcript|beats>] [--output-container <ext>] [--artifact-kind <kind>] [--has-artifacts [true|false]] [--has-subtitles [true|false]] [--summary [true|false]] [--plugin-dir <path>] [--json-out <path>] [--write-examples <dir>]");
    Console.WriteLine("  effects [list [--category <id>] | describe <type> | <type>] [--json-out <path>]");
    Console.WriteLine("  doctor [--ffmpeg <path>] [--ffprobe <path>] [--whisper-cli <path>] [--whisper-model <path>] [--demucs <path>] [--json-out <path>] [--timeout-seconds <n>]");
    Console.WriteLine("  validate-plugin --plugin-dir <path> [--json-out <path>]");
    Console.WriteLine("  beat-track <input> --output <beats.json> [--ffmpeg <path>] [--sample-rate <hz>] [--json-out <path>] [--timeout-seconds <n>]");
    Console.WriteLine("  audio-analyze <input> --output <audio.json> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>]");
    Console.WriteLine("  audio-gain <input> --gain-db <n> --output <path> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>] [--overwrite]");
    Console.WriteLine("  audio-normalize <input> --output <path> [--target-lufs <n>] [--lra <n>] [--true-peak-db <n>] [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>] [--overwrite]");
    Console.WriteLine("  transcribe <input> --model <path> --output <transcript.json> [--language <id>] [--translate [true|false]] [--whisper-cli <path>] [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>]");
    Console.WriteLine("  detect-silence <input> --output <silence.json> [--noise-db <n>] [--min-duration-ms <n>] [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>]");
    Console.WriteLine("  auto-cut-silence --silence <silence.json> [--clips-only] [--padding-ms <n>] [--merge-gap-ms <n>] [--min-clip-duration-ms <n>] [--source-duration-ms <n>] [--ffprobe <path>] [--template <id>] [--render-output <path>] [--output <path>] [--json-out <path>]");
    Console.WriteLine("  separate-audio <input> --output-dir <path> [--model <id>] [--demucs <path>] [--json-out <path>] [--timeout-seconds <n>]");
    Console.WriteLine("  cut <input> --from <hh:mm:ss.fff> --to <hh:mm:ss.fff> --output <path> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>] [--overwrite]");
    Console.WriteLine("  concat --input-list <path> --output <path> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>] [--overwrite]");
    Console.WriteLine("  extract-audio <input> --track <n> --output <path> [--ffmpeg <path>] [--json-out <path>] [--timeout-seconds <n>] [--overwrite]");
    Console.WriteLine("  init-plan <input> --template <id> --output <edit.json> [--render-output <path>] [--probe] [--ffprobe <path>] [--transcript <transcript.json>] [--seed-from-transcript] [--transcript-segment-group-size <n>] [--min-transcript-segment-duration-ms <n>] [--max-transcript-gap-ms <n>] [--beats <beats.json>] [--seed-from-beats] [--beat-group-size <n>] [--artifacts <artifacts.json>] [--template-params <template-params.json>] [--subtitle <path>] [--subtitle-mode <sidecar|burnIn|none>] [--bgm <path>] [--plugin-dir <path>] [--timeout-seconds <n>]");
    Console.WriteLine("  init-narrated-plan --manifest <narrated.json> --output <edit.json> [--template <id>] [--render-output <path>] [--vars <vars.json>] [--ffprobe <path>] [--timeout-seconds <n>] [--json-out <path>]");
    Console.WriteLine("  scaffold-template <input> --template <id> --dir <workdir> [--validate [true|false]] [--check-files [true|false]] [--render-output <path>] [--probe] [--ffprobe <path>] [--transcript <transcript.json>] [--seed-from-transcript] [--transcript-segment-group-size <n>] [--min-transcript-segment-duration-ms <n>] [--max-transcript-gap-ms <n>] [--beats <beats.json>] [--seed-from-beats] [--beat-group-size <n>] [--artifacts <artifacts.json>] [--template-params <template-params.json>] [--subtitle <path>] [--subtitle-mode <sidecar|burnIn|none>] [--bgm <path>] [--plugin-dir <path>] [--timeout-seconds <n>]");
    Console.WriteLine("  scaffold-template-batch --manifest <batch.json> [--plugin-dir <path>] [--json-out <path>]");
    Console.WriteLine("  export --plan <edit.json> --format <edl> --output <path> [--frame-rate <fps>] [--title <name>] [--json-out <path>] [--overwrite]");
    Console.WriteLine("  mix-audio --plan <edit.json> --output <path> [--preview [true|false]] [--json-out <path>] [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]");
    Console.WriteLine("  render --plan <path> [--output <path>] [--preview [true|false]] [--json-out <path>] [--ffmpeg <path>] [--timeout-seconds <n>] [--overwrite]");
    Console.WriteLine("  render-batch --manifest <batch.json> [--preview [true|false]] [--ffmpeg <path>] [--timeout-seconds <n>] [--json-out <path>]");
    Console.WriteLine("  subtitle <input> --transcript <transcript.json> --format <srt|ass> --output <path> [--max-line-length <n>] [--json-out <path>]");
    Console.WriteLine("  inspect-plan --plan <edit.json> [--check-files [true|false]] [--plugin-dir <path>] [--json-out <path>]");
    Console.WriteLine("  replace-plan-material --plan <edit.json> [--write-to <path>] [--in-place [true|false]] --path <new-file> (--source-input | --audio-track-id <id> | --artifact-slot <slotId> | --transcript | --beats | --subtitles) [--subtitle-mode <sidecar|burnIn>] [--path-style <auto|relative|absolute>] [--check-files [true|false]] [--plugin-dir <path>] [--require-valid [true|false]] [--json-out <path>]");
    Console.WriteLine("  replace-plan-material-batch --manifest <batch.json> [--plugin-dir <path>] [--json-out <path>]");
    Console.WriteLine("  attach-plan-material --plan <edit.json> [--write-to <path>] [--in-place [true|false]] --path <new-file> (--transcript | --beats | --subtitles | --audio-track-id <id> [--audio-track-role <original|voice|bgm|effects>] | --artifact-slot <slotId>) [--subtitle-mode <sidecar|burnIn>] [--path-style <auto|relative|absolute>] [--check-files [true|false]] [--plugin-dir <path>] [--require-valid [true|false]] [--json-out <path>]");
    Console.WriteLine("  attach-plan-material-batch --manifest <batch.json> [--plugin-dir <path>] [--json-out <path>]");
    Console.WriteLine("  bind-voice-track --plan <edit.json> --path <audio-file> [--track-id <id>] [--role <original|voice|bgm|effects>] [--write-to <path>] [--in-place [true|false]] [--path-style <auto|relative|absolute>] [--check-files [true|false]] [--plugin-dir <path>] [--require-valid [true|false]] [--json-out <path>]");
    Console.WriteLine("  bind-voice-track-batch --manifest <batch.json> [--plugin-dir <path>] [--json-out <path>]");
    Console.WriteLine("  validate-plan --plan <edit.json> [--check-files [true|false]] [--plugin-dir <path>] [--json-out <path>]");
    Console.WriteLine("  probe <input> [--ffprobe <path>] [--json-out <path>]");
    Console.WriteLine("  plan <input> [--preset <id>] [--output-dir <dir>] [--output-name <name>] [--ffmpeg <path>] [--json-out <path>] [--overwrite]");
    Console.WriteLine("  run <input> [--preset <id>] [--output-dir <dir>] [--output-name <name>] [--ffprobe <path>] [--ffmpeg <path>] [--timeout-seconds <n>] [--json-out <path>] [--overwrite]");
    Console.WriteLine();
    Console.WriteLine("Built-in presets:");
    foreach (var preset in BuiltInPresetCatalog.GetAll())
    {
        Console.WriteLine($"  {preset.Id} - {preset.DisplayName}");
    }
}
