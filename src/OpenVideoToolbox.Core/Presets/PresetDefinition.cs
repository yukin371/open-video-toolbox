namespace OpenVideoToolbox.Core.Presets;

public sealed record PresetDefinition
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public PresetKind Kind { get; init; } = PresetKind.Transcode;

    public VideoEncoderSettings? Video { get; init; }

    public AudioEncoderSettings? Audio { get; init; }

    public required OutputSettings Output { get; init; }

    public IReadOnlyDictionary<string, string> MetadataTags { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<string> ExtraArguments { get; init; } = [];
}

public enum PresetKind
{
    Transcode,
    Remux,
    AudioOnly
}

public sealed record VideoEncoderSettings
{
    public required string Encoder { get; init; }

    public string? Preset { get; init; }

    public int? Crf { get; init; }

    public string? PixelFormat { get; init; }

    public IReadOnlyList<string> ExtraArguments { get; init; } = [];
}

public sealed record AudioEncoderSettings
{
    public required string Encoder { get; init; }

    public int? BitrateKbps { get; init; }

    public int? Channels { get; init; }

    public int? SampleRate { get; init; }

    public IReadOnlyList<string> ExtraArguments { get; init; } = [];
}

public sealed record OutputSettings
{
    public required string ContainerExtension { get; init; }

    public bool FastStart { get; init; }

    public bool OverwriteExisting { get; init; }
}
