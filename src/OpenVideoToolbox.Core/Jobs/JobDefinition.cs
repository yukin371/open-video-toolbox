using OpenVideoToolbox.Core;
using OpenVideoToolbox.Core.Media;
using OpenVideoToolbox.Core.Presets;

namespace OpenVideoToolbox.Core.Jobs;

public sealed record JobDefinition
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public required string Id { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public required JobSource Source { get; init; }

    public required JobOutput Output { get; init; }

    public required PresetDefinition Preset { get; init; }

    public MediaProbeResult? ProbeSnapshot { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];
}

public sealed record JobSource
{
    public required string InputPath { get; init; }
}

public sealed record JobOutput
{
    public required string OutputDirectory { get; init; }

    public required string FileNameStem { get; init; }

    public required string ContainerExtension { get; init; }

    public bool OverwriteExisting { get; init; }
}
