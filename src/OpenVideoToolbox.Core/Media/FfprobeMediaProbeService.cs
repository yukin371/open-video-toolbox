using OpenVideoToolbox.Core.Execution;

namespace OpenVideoToolbox.Core.Media;

public sealed class FfprobeMediaProbeService
{
    private readonly IProcessRunner _processRunner;
    private readonly FfprobeJsonParser _parser;

    public FfprobeMediaProbeService(IProcessRunner processRunner, FfprobeJsonParser parser)
    {
        _processRunner = processRunner;
        _parser = parser;
    }

    public async Task<MediaProbeResult> ProbeAsync(
        string inputPath,
        string executablePath = "ffprobe",
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

        var plan = new CommandPlan
        {
            ToolName = "ffprobe",
            ExecutablePath = executablePath,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(inputPath)),
            Arguments =
            [
                "-v",
                "quiet",
                "-print_format",
                "json",
                "-show_format",
                "-show_streams",
                inputPath
            ],
            CommandLine = $"{executablePath} -v quiet -print_format json -show_format -show_streams \"{inputPath}\""
        };

        var result = await _processRunner.ExecuteAsync(
            new ProcessExecutionRequest
            {
                CommandPlan = plan,
                Timeout = timeout ?? TimeSpan.FromSeconds(30)
            },
            cancellationToken).ConfigureAwait(false);

        if (result.Status != ExecutionStatus.Succeeded)
        {
            throw new InvalidOperationException(BuildProbeFailureMessage(result));
        }

        var json = string.Join(
            Environment.NewLine,
            result.OutputLines
                .Where(line => line.Channel == ProcessOutputChannel.StandardOutput)
                .Select(line => line.Text));
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("ffprobe completed without emitting probe JSON.");
        }

        return _parser.Parse(json, inputPath);
    }

    private static string BuildProbeFailureMessage(ExecutionResult result)
    {
        var stderr = string.Join(
            Environment.NewLine,
            result.OutputLines
                .Where(line => line.Channel == ProcessOutputChannel.StandardError)
                .Select(line => line.Text));
        return string.IsNullOrWhiteSpace(stderr)
            ? $"ffprobe failed with status {result.Status}."
            : $"ffprobe failed with status {result.Status}:{Environment.NewLine}{stderr}";
    }
}
