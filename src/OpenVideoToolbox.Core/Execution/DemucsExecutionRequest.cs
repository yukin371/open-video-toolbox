namespace OpenVideoToolbox.Core.Execution;

public sealed record DemucsExecutionRequest
{
    public required string InputPath { get; init; }

    public required string OutputDirectory { get; init; }

    public string Model { get; init; } = "htdemucs";

    public string TwoStems { get; init; } = "vocals";
}
