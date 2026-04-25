using System.Text.Json;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Media;
using OpenVideoToolbox.Core.Serialization;
using static OpenVideoToolbox.Cli.CliOptionParsing;

namespace OpenVideoToolbox.Cli;

internal static class NarratedSlidesPlanBuildSupport
{
    public static async Task<NarratedSlidesCommandBuildResult> BuildAsync(
        string manifestPath,
        string fullPlanOutputPath,
        IReadOnlyDictionary<string, string> options)
    {
        if (!TryGetIntOption(options, "--timeout-seconds", out var timeoutSeconds, out var error))
        {
            throw new InvalidOperationException(error!);
        }

        var fullManifestPath = Path.GetFullPath(manifestPath);
        var manifestDirectory = Path.GetDirectoryName(fullManifestPath)!;
        var content = await File.ReadAllTextAsync(fullManifestPath);
        var manifest = JsonSerializer.Deserialize<NarratedSlidesManifest>(content, OpenVideoToolboxJson.Default)
            ?? throw new InvalidOperationException($"Failed to parse narrated-slides manifest '{fullManifestPath}'.");

        if (manifest.SchemaVersion != 1)
        {
            throw new InvalidOperationException(
                $"Unsupported narrated-slides manifest schema version '{manifest.SchemaVersion}'.");
        }

        if (manifest.Sections.Count == 0)
        {
            throw new InvalidOperationException("Narrated-slides manifest must contain at least one section.");
        }

        var templateId = GetOption(options, "--template")
            ?? manifest.Template?.Id
            ?? NarratedSlidesPlanBuilder.DefaultTemplateId;

        var planDirectory = Path.GetDirectoryName(fullPlanOutputPath)!;
        var renderOutputPath = GetOption(options, "--render-output")
            ?? manifest.Video.Output
            ?? Path.Combine(planDirectory, $"{Path.GetFileNameWithoutExtension(fullManifestPath)}.mp4");
        renderOutputPath = EditPlanPathResolver.ResolvePath(planDirectory, renderOutputPath);

        var ffprobePath = GetOption(options, "--ffprobe") ?? "ffprobe";
        TimeSpan? timeout = timeoutSeconds is null ? null : TimeSpan.FromSeconds(timeoutSeconds.Value);
        var probeService = new FfprobeMediaProbeService(new DefaultProcessRunner(), new FfprobeJsonParser());

        var resolvedSections = new List<NarratedSlidesResolvedSection>(manifest.Sections.Count);
        var probedSectionCount = 0;

        for (var index = 0; index < manifest.Sections.Count; index++)
        {
            var section = manifest.Sections[index];
            if (!string.Equals(section.Visual.Kind, "video", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Section '{section.Id}' has unsupported visual kind '{section.Visual.Kind}'. Only 'video' is supported in the first narrated-slides version.");
            }

            var visualPath = ResolveManifestPath(manifestDirectory, section.Visual.Path, $"sections[{index}].visual.path");
            var voicePath = ResolveManifestPath(manifestDirectory, section.Voice.Path, $"sections[{index}].voice.path");

            var visualDuration = section.Visual.DurationMs is int configuredVisualDuration && configuredVisualDuration > 0
                ? TimeSpan.FromMilliseconds(configuredVisualDuration)
                : await ProbeDurationAsync(probeService, visualPath, ffprobePath, timeout, $"section '{section.Id}' visual");

            var voiceDuration = section.Voice.DurationMs is int configuredVoiceDuration && configuredVoiceDuration > 0
                ? TimeSpan.FromMilliseconds(configuredVoiceDuration)
                : await ProbeDurationAsync(probeService, voicePath, ffprobePath, timeout, $"section '{section.Id}' voice");

            if (section.Visual.DurationMs is null || section.Voice.DurationMs is null)
            {
                probedSectionCount++;
            }

            resolvedSections.Add(new NarratedSlidesResolvedSection
            {
                Id = section.Id,
                Title = section.Title,
                VisualPath = visualPath,
                VisualDuration = visualDuration,
                VoicePath = voicePath,
                VoiceDuration = voiceDuration
            });
        }

        var subtitlePath = string.IsNullOrWhiteSpace(manifest.Subtitles?.Path)
            ? null
            : ResolveManifestPath(manifestDirectory, manifest.Subtitles.Path, "subtitles.path");

        var bgmPath = string.IsNullOrWhiteSpace(manifest.Bgm?.Path)
            ? null
            : ResolveManifestPath(manifestDirectory, manifest.Bgm.Path, "bgm.path");

        var result = new NarratedSlidesPlanBuilder().Build(new NarratedSlidesPlanBuildRequest
        {
            Manifest = manifest,
            TemplateId = templateId,
            RenderOutputPath = renderOutputPath,
            Sections = resolvedSections,
            SubtitlePath = subtitlePath,
            SubtitleMode = manifest.Subtitles?.Mode,
            BgmPath = bgmPath,
            BgmGainDb = manifest.Bgm?.GainDb ?? -18
        });

        return new NarratedSlidesCommandBuildResult
        {
            ManifestPath = fullManifestPath,
            TemplateId = templateId,
            RenderOutputPath = renderOutputPath,
            Plan = result.Plan,
            Stats = result.Stats,
            ProbedSectionCount = probedSectionCount
        };
    }

    private static string ResolveManifestPath(string manifestDirectory, string path, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"Narrated-slides field '{fieldName}' must be a non-empty path.");
        }

        var resolvedPath = Path.GetFullPath(Path.Combine(manifestDirectory, path));
        if (!File.Exists(resolvedPath))
        {
            throw new InvalidOperationException(
                $"Narrated-slides field '{fieldName}' points to a missing file: '{resolvedPath}'.");
        }

        return resolvedPath;
    }

    private static async Task<TimeSpan> ProbeDurationAsync(
        FfprobeMediaProbeService probeService,
        string path,
        string ffprobePath,
        TimeSpan? timeout,
        string logicalName)
    {
        var probe = await probeService.ProbeAsync(path, ffprobePath, timeout);
        if (probe.Format.Duration is not { } duration || duration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"Could not resolve a positive duration for {logicalName} '{path}'.");
        }

        return duration;
    }
}
