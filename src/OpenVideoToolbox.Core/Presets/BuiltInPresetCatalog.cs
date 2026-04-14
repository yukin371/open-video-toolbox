namespace OpenVideoToolbox.Core.Presets;

public static class BuiltInPresetCatalog
{
    private static readonly IReadOnlyDictionary<string, PresetDefinition> Presets =
        new Dictionary<string, PresetDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["h264-aac-mp4"] = new PresetDefinition
            {
                Id = "h264-aac-mp4",
                DisplayName = "H.264 + AAC / MP4",
                Kind = PresetKind.Transcode,
                Video = new VideoEncoderSettings
                {
                    Encoder = "libx264",
                    Preset = "medium",
                    Crf = 23,
                    PixelFormat = "yuv420p"
                },
                Audio = new AudioEncoderSettings
                {
                    Encoder = "aac",
                    BitrateKbps = 192,
                    Channels = 2,
                    SampleRate = 48000
                },
                Output = new OutputSettings
                {
                    ContainerExtension = ".mp4",
                    FastStart = true,
                    OverwriteExisting = false
                },
                ExtraArguments = ["-map_metadata", "-1"]
            },
            ["h265-aac-mp4"] = new PresetDefinition
            {
                Id = "h265-aac-mp4",
                DisplayName = "H.265 + AAC / MP4",
                Kind = PresetKind.Transcode,
                Video = new VideoEncoderSettings
                {
                    Encoder = "libx265",
                    Preset = "medium",
                    Crf = 26,
                    PixelFormat = "yuv420p"
                },
                Audio = new AudioEncoderSettings
                {
                    Encoder = "aac",
                    BitrateKbps = 192,
                    Channels = 2,
                    SampleRate = 48000
                },
                Output = new OutputSettings
                {
                    ContainerExtension = ".mp4",
                    FastStart = true,
                    OverwriteExisting = false
                },
                ExtraArguments = ["-map_metadata", "-1"]
            },
            ["copy-aac-mkv"] = new PresetDefinition
            {
                Id = "copy-aac-mkv",
                DisplayName = "Copy Video + AAC / MKV",
                Kind = PresetKind.Transcode,
                Video = new VideoEncoderSettings
                {
                    Encoder = "copy"
                },
                Audio = new AudioEncoderSettings
                {
                    Encoder = "aac",
                    BitrateKbps = 192,
                    Channels = 2,
                    SampleRate = 48000
                },
                Output = new OutputSettings
                {
                    ContainerExtension = ".mkv",
                    FastStart = false,
                    OverwriteExisting = false
                },
                ExtraArguments = ["-map_metadata", "-1"]
            }
        };

    public static string DefaultPresetId => "h264-aac-mp4";

    public static IReadOnlyList<PresetDefinition> GetAll()
    {
        return Presets.Values.ToArray();
    }

    public static bool TryGet(string presetId, out PresetDefinition preset)
    {
        if (Presets.TryGetValue(presetId, out var found))
        {
            preset = found;
            return true;
        }

        preset = null!;
        return false;
    }

    public static PresetDefinition GetRequired(string presetId)
    {
        if (TryGet(presetId, out var preset))
        {
            return preset;
        }

        throw new KeyNotFoundException($"Unknown preset '{presetId}'.");
    }
}

