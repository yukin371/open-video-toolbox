using System.Text.Json;

namespace OpenVideoToolbox.Core.Editing;

public sealed class NarratedSlidesPlanBuilder
{
    public const string DefaultTemplateId = "narrated-slides-starter";

    public NarratedSlidesPlanBuildResult Build(NarratedSlidesPlanBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TemplateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RenderOutputPath);

        if (request.Manifest.SchemaVersion != SchemaVersions.V1)
        {
            throw new ArgumentException(
                $"Unsupported narrated-slides manifest schema version '{request.Manifest.SchemaVersion}'.",
                nameof(request));
        }

        if (request.Sections.Count == 0)
        {
            throw new ArgumentException("Narrated-slides manifest must contain at least one section.", nameof(request));
        }

        var resolution = request.Manifest.Video.Resolution is { W: > 0, H: > 0 } configuredResolution
            ? configuredResolution
            : new NarratedSlidesResolutionManifest
            {
                W = 1920,
                H = 1080
            };

        var frameRate = request.Manifest.Video.FrameRate is int configuredFrameRate && configuredFrameRate > 0
            ? configuredFrameRate
            : 30;

        var totalDuration = TimeSpan.Zero;
        var mainClips = new List<TimelineClip>(request.Sections.Count);
        var voiceClips = new List<TimelineClip>(request.Sections.Count);

        foreach (var section in request.Sections)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(section.Id);
            ArgumentException.ThrowIfNullOrWhiteSpace(section.VisualPath);
            ArgumentException.ThrowIfNullOrWhiteSpace(section.VoicePath);

            if (section.VisualDuration <= TimeSpan.Zero)
            {
                throw new ArgumentException(
                    $"Section '{section.Id}' visual duration must be greater than zero.",
                    nameof(request));
            }

            if (section.VoiceDuration <= TimeSpan.Zero)
            {
                throw new ArgumentException(
                    $"Section '{section.Id}' voice duration must be greater than zero.",
                    nameof(request));
            }

            if (section.VisualDuration < section.VoiceDuration)
            {
                throw new ArgumentException(
                    $"Section '{section.Id}' visual duration cannot be shorter than voice duration.",
                    nameof(request));
            }

            mainClips.Add(new TimelineClip
            {
                Id = $"{section.Id}-video",
                Src = section.VisualPath,
                Start = totalDuration,
                Duration = section.VoiceDuration
            });

            voiceClips.Add(new TimelineClip
            {
                Id = $"{section.Id}-voice",
                Src = section.VoicePath,
                Start = totalDuration,
                Duration = section.VoiceDuration
            });

            totalDuration += section.VoiceDuration;
        }

        var videoTrackEffects = new List<TimelineEffect>
        {
            CreateEffect(
                "scale",
                ("width", resolution.W),
                ("height", resolution.H),
                ("flags", "lanczos"))
        };

        if (request.Manifest.Video.ProgressBar?.Enabled == true)
        {
            videoTrackEffects.Add(CreateEffect(
                "progress_bar",
                ("durationSeconds", totalDuration.TotalSeconds),
                ("height", request.Manifest.Video.ProgressBar.Height ?? 12),
                ("margin", request.Manifest.Video.ProgressBar.Margin ?? 32),
                ("color", request.Manifest.Video.ProgressBar.Color ?? "white@0.95"),
                ("backgroundColor", request.Manifest.Video.ProgressBar.BackgroundColor ?? "black@0.28")));
        }

        var tracks = new List<TimelineTrack>
        {
            new()
            {
                Id = "main",
                Kind = TrackKind.Video,
                Effects = videoTrackEffects,
                Clips = mainClips
            },
            new()
            {
                Id = "voice",
                Kind = TrackKind.Audio,
                Clips = voiceClips
            }
        };

        if (!string.IsNullOrWhiteSpace(request.BgmPath))
        {
            tracks.Add(new TimelineTrack
            {
                Id = "bgm",
                Kind = TrackKind.Audio,
                Slot = new TrackSlot
                {
                    Name = "bgm",
                    DefaultAsset = request.BgmPath,
                    Required = false
                },
                Clips =
                [
                    new TimelineClip
                    {
                        Id = "bgm-001",
                        Src = request.BgmPath,
                        Start = TimeSpan.Zero,
                        Duration = totalDuration,
                        Effects =
                        [
                            CreateEffect("volume", ("gainDb", request.BgmGainDb))
                        ]
                    }
                ]
            });
        }

        var subtitlePath = string.IsNullOrWhiteSpace(request.SubtitlePath) ? null : request.SubtitlePath;
        var subtitleMode = request.SubtitleMode ?? request.Manifest.Subtitles?.Mode ?? SubtitleMode.Sidecar;

        var plan = new EditPlan
        {
            SchemaVersion = SchemaVersions.V2,
            Source = new EditPlanSource
            {
                InputPath = request.Sections[0].VisualPath
            },
            Template = new EditTemplateReference
            {
                Id = request.TemplateId,
                Source = new EditTemplateSourceReference
                {
                    Kind = EditTemplateSourceKinds.BuiltIn
                }
            },
            Clips = [],
            AudioTracks = [],
            Artifacts = [],
            Subtitles = subtitlePath is null
                ? null
                : new EditSubtitlePlan
                {
                    Path = subtitlePath,
                    Mode = subtitleMode
                },
            Timeline = new EditPlanTimeline
            {
                Duration = totalDuration,
                Resolution = new TimelineResolution
                {
                    W = resolution.W,
                    H = resolution.H
                },
                FrameRate = frameRate,
                Tracks = tracks
            },
            Output = new EditOutputPlan
            {
                Path = request.RenderOutputPath,
                Container = ResolveOutputContainer(request.RenderOutputPath)
            }
        };

        return new NarratedSlidesPlanBuildResult
        {
            Plan = plan,
            Stats = new NarratedSlidesPlanBuildStats
            {
                SectionCount = request.Sections.Count,
                TotalDuration = totalDuration,
                HasSubtitles = subtitlePath is not null,
                HasBgm = !string.IsNullOrWhiteSpace(request.BgmPath)
            }
        };
    }

    private static TimelineEffect CreateEffect(string type, params (string key, object value)[] parameters)
    {
        var extensions = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in parameters)
        {
            extensions[key] = System.Text.Json.JsonSerializer.SerializeToElement(value, Serialization.OpenVideoToolboxJson.Shared);
        }

        return new TimelineEffect
        {
            Type = type,
            Extensions = extensions
        };
    }

    private static string ResolveOutputContainer(string renderOutputPath)
    {
        var extension = Path.GetExtension(renderOutputPath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException(
                $"Render output path '{renderOutputPath}' must include a file extension.",
                nameof(renderOutputPath));
        }

        return extension[1..];
    }
}
