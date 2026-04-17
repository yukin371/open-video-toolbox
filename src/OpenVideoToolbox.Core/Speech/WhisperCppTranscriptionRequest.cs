namespace OpenVideoToolbox.Core.Speech;

public sealed record WhisperCppTranscriptionRequest
{
    public required string InputPath { get; init; }

    public required string ModelPath { get; init; }

    public string? Language { get; init; }

    public bool TranslateToEnglish { get; init; }
}
