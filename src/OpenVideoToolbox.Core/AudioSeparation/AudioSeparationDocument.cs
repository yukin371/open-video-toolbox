using OpenVideoToolbox.Core;

namespace OpenVideoToolbox.Core.AudioSeparation;

public sealed record AudioSeparationDocument
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public required string InputPath { get; init; }

    public required string Model { get; init; }

    public required AudioSeparationStems Stems { get; init; }
}

public sealed record AudioSeparationStems
{
    public required string Vocals { get; init; }

    public required string Accompaniment { get; init; }
}
