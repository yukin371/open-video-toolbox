namespace OpenVideoToolbox.Core.Execution;

public sealed record AudioAnalysisRequest
{
    public required string InputPath { get; init; }
}
