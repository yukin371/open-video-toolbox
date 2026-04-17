using OpenVideoToolbox.Core.Execution;

namespace OpenVideoToolbox.Core.Audio;

public sealed class FfmpegSilenceDetectionService
{
    private readonly SilenceDetectionRunner _runner;
    private readonly SilenceDetectionParser _parser;

    public FfmpegSilenceDetectionService(SilenceDetectionRunner runner, SilenceDetectionParser parser)
    {
        _runner = runner;
        _parser = parser;
    }

    public async Task<SilenceDetectionDocument> DetectAsync(
        string inputPath,
        double noiseDb = -30,
        TimeSpan? minimumDuration = null,
        string executablePath = "ffmpeg",
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

        var result = await _runner.RunAsync(
            new SilenceDetectionRequest
            {
                InputPath = inputPath,
                NoiseDb = noiseDb,
                MinimumDuration = minimumDuration ?? TimeSpan.FromSeconds(0.5)
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
                ?? "Unknown silence detection failure.";

            throw new InvalidOperationException($"ffmpeg silence detection failed: {detail}");
        }

        return _parser.Parse(result, inputPath);
    }
}
