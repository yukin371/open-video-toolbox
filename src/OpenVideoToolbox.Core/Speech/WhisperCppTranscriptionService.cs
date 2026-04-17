using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Subtitles;

namespace OpenVideoToolbox.Core.Speech;

public sealed class WhisperCppTranscriptionService
{
    private readonly AudioWaveformExtractRunner _audioWaveformExtractRunner;
    private readonly WhisperCppTranscriptionRunner _transcriptionRunner;
    private readonly WhisperCppJsonParser _parser;

    public WhisperCppTranscriptionService(
        AudioWaveformExtractRunner audioWaveformExtractRunner,
        WhisperCppTranscriptionRunner transcriptionRunner,
        WhisperCppJsonParser parser)
    {
        _audioWaveformExtractRunner = audioWaveformExtractRunner;
        _transcriptionRunner = transcriptionRunner;
        _parser = parser;
    }

    public async Task<TranscriptDocument> TranscribeAsync(
        WhisperCppTranscriptionRequest request,
        string ffmpegExecutablePath = "ffmpeg",
        string whisperExecutablePath = "whisper-cli",
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ModelPath);

        var tempWavePath = Path.Combine(Path.GetTempPath(), $"ovt-transcribe-{Guid.NewGuid():N}.wav");
        var tempOutputPrefix = Path.Combine(Path.GetTempPath(), $"ovt-transcribe-{Guid.NewGuid():N}");
        var tempJsonPath = $"{tempOutputPrefix}.json";

        try
        {
            var extraction = await _audioWaveformExtractRunner.RunAsync(
                new AudioWaveformExtractRequest
                {
                    InputPath = request.InputPath,
                    OutputPath = tempWavePath,
                    SampleRateHz = 16000,
                    OverwriteExisting = true
                },
                ffmpegExecutablePath,
                timeout,
                cancellationToken).ConfigureAwait(false);

            if (extraction.Status != ExecutionStatus.Succeeded)
            {
                throw new InvalidOperationException($"audio preparation failed: {ResolveFailureDetail(extraction)}");
            }

            var transcription = await _transcriptionRunner.RunAsync(
                new WhisperCppExecutionRequest
                {
                    InputWavePath = tempWavePath,
                    ModelPath = request.ModelPath,
                    OutputFilePrefix = tempOutputPrefix,
                    Language = request.Language,
                    TranslateToEnglish = request.TranslateToEnglish
                },
                whisperExecutablePath,
                timeout,
                cancellationToken).ConfigureAwait(false);

            if (transcription.Status != ExecutionStatus.Succeeded)
            {
                throw new InvalidOperationException($"whisper transcription failed: {ResolveFailureDetail(transcription)}");
            }

            if (!File.Exists(tempJsonPath))
            {
                throw new InvalidOperationException($"whisper transcription did not produce expected JSON output '{tempJsonPath}'.");
            }

            var jsonContent = await File.ReadAllTextAsync(tempJsonPath, cancellationToken).ConfigureAwait(false);
            return _parser.Parse(jsonContent);
        }
        finally
        {
            TryDeleteFile(tempWavePath);
            TryDeleteFile(tempJsonPath);
        }
    }

    private static string ResolveFailureDetail(ExecutionResult result)
    {
        return result.OutputLines
            .Where(line => line.IsError)
            .Select(line => line.Text)
            .FirstOrDefault()
            ?? result.ErrorMessage
            ?? "Unknown failure.";
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
}
