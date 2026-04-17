using OpenVideoToolbox.Core.Execution;

namespace OpenVideoToolbox.Core.Audio;

public sealed class FfmpegAudioAnalysisService
{
    private readonly AudioAnalysisRunner _runner;
    private readonly AudioAnalysisParser _parser;

    public FfmpegAudioAnalysisService(AudioAnalysisRunner runner, AudioAnalysisParser parser)
    {
        _runner = runner;
        _parser = parser;
    }

    public async Task<AudioAnalysisDocument> AnalyzeAsync(
        string inputPath,
        string executablePath = "ffmpeg",
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

        var result = await _runner.RunAsync(
            new AudioAnalysisRequest
            {
                InputPath = inputPath
            },
            executablePath,
            timeout,
            cancellationToken).ConfigureAwait(false);

        if (result.Status != ExecutionStatus.Succeeded)
        {
            var detail = result.OutputLines
                .Where(line => line.IsError)
                .Select(line => line.Text)
                .FirstOrDefault()
                ?? result.ErrorMessage
                ?? "Unknown audio analysis failure.";

            throw new InvalidOperationException($"ffmpeg audio analysis failed: {detail}");
        }

        return _parser.Parse(result, inputPath);
    }
}
