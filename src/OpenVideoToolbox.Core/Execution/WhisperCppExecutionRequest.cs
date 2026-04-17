namespace OpenVideoToolbox.Core.Execution;

public sealed record WhisperCppExecutionRequest
{
    public required string InputWavePath { get; init; }

    public required string ModelPath { get; init; }

    public required string OutputFilePrefix { get; init; }

    public string? Language { get; init; }

    public bool TranslateToEnglish { get; init; }
}
