using OpenVideoToolbox.Core.Execution;

namespace OpenVideoToolbox.Core.AudioSeparation;

public sealed class DemucsAudioSeparationService
{
    private readonly DemucsSeparationRunner _runner;

    public DemucsAudioSeparationService(DemucsSeparationRunner runner)
    {
        _runner = runner;
    }

    public async Task<AudioSeparationDocument> SeparateAsync(
        DemucsSeparationRequest request,
        string executablePath = "demucs",
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Model);

        var result = await _runner.RunAsync(
            new DemucsExecutionRequest
            {
                InputPath = request.InputPath,
                OutputDirectory = request.OutputDirectory,
                Model = request.Model,
                TwoStems = "vocals"
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
                ?? "Unknown audio separation failure.";

            throw new InvalidOperationException($"demucs audio separation failed: {detail}");
        }

        var trackDirectory = Path.Combine(
            Path.GetFullPath(request.OutputDirectory),
            request.Model,
            Path.GetFileNameWithoutExtension(request.InputPath));
        var vocalsPath = Path.Combine(trackDirectory, "vocals.wav");
        var accompanimentPath = Path.Combine(trackDirectory, "no_vocals.wav");

        if (!File.Exists(vocalsPath) || !File.Exists(accompanimentPath))
        {
            throw new InvalidOperationException(
                $"demucs audio separation did not produce expected stems under '{trackDirectory}'.");
        }

        return new AudioSeparationDocument
        {
            InputPath = request.InputPath,
            Model = request.Model,
            Stems = new AudioSeparationStems
            {
                Vocals = vocalsPath,
                Accompaniment = accompanimentPath
            }
        };
    }
}
