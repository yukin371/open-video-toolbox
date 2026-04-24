namespace OpenVideoToolbox.Cli;

internal sealed record RenderOperationResult
{
    public required object Payload { get; init; }

    public int ExitCode { get; init; }

    public string? ErrorMessage { get; init; }
}
