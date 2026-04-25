using System.Text.Json;
using OpenVideoToolbox.Core.Beats;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Media;
using OpenVideoToolbox.Core.Serialization;
using OpenVideoToolbox.Core.Subtitles;
using static OpenVideoToolbox.Cli.CliOptionParsing;

namespace OpenVideoToolbox.Cli;

internal static class TemplatePlanBuildSupport
{
    public static async Task<TemplatePlanBuildResult> BuildEditPlanFromTemplateAsync(
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

        var pluginCatalog = TemplatePluginCatalogLoader.Load(GetOption(options, "--plugin-dir"));
        var availableTemplates = TemplateCommandPresentation.BuildAvailableTemplates(pluginCatalog);
        var template = EditPlanTemplateCatalog.GetRequired(availableTemplates, templateId);
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
            artifactBindings = await JsonInputLoadSupport.LoadStringMapAsync(
                artifactsPath,
                "artifact bindings",
                "Expected a JSON object like {\"slotId\":\"path\"}.");
        }

        IReadOnlyDictionary<string, string> parameterOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var templateParamsPath = GetOption(options, "--template-params");
        if (!string.IsNullOrWhiteSpace(templateParamsPath))
        {
            parameterOverrides = await JsonInputLoadSupport.LoadStringMapAsync(
                templateParamsPath,
                "template parameters",
                "Expected a JSON object like {\"hookStyle\":\"hard-cut\"}.");
        }

        var plan = new EditPlanTemplateFactory().Create(
            template,
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
            },
            TemplateCommandPresentation.BuildPersistedTemplateSource(template, pluginCatalog));

        return new TemplatePlanBuildResult
        {
            Template = template,
            TemplateSource = TemplateCommandPresentation.BuildTemplateSource(template, pluginCatalog),
            Plan = plan,
            Probe = probe,
            ArtifactBindings = artifactBindings,
            ParameterOverrides = parameterOverrides
        };
    }

    public static async Task<TranscriptDocument> LoadTranscriptAsync(string transcriptPath)
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

    private static async Task<BeatTrackDocument> LoadBeatTrackAsync(string beatsPath)
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
}
