namespace OpenVideoToolbox.Core.Execution;

internal static class FfmpegAudioOutputCodecArguments
{
    public static IReadOnlyList<string> Resolve(string outputPath, string operationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        var extension = Path.GetExtension(outputPath).ToLowerInvariant();
        return extension switch
        {
            ".wav" => ["-c:a", "pcm_s16le"],
            ".flac" => ["-c:a", "flac"],
            ".mp3" => ["-c:a", "libmp3lame", "-b:a", "192k"],
            ".aac" or ".m4a" => ["-c:a", "aac", "-b:a", "192k"],
            _ => throw new InvalidOperationException(
                $"Unsupported {operationName} output extension '{extension}'. Use .wav, .flac, .mp3, .aac, or .m4a.")
        };
    }
}
