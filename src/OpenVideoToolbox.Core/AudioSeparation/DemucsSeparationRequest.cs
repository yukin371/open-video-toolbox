namespace OpenVideoToolbox.Core.AudioSeparation;

public sealed record DemucsSeparationRequest
{
    public required string InputPath { get; init; }

    public required string OutputDirectory { get; init; }

    public string Model { get; init; } = "htdemucs";
}
