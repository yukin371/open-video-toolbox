using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Speech;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class WhisperCppTranscriptionServiceTests
{
    [Fact]
    public async Task TranscribeAsync_ExtractsWaveRunsWhisperAndParsesTranscript()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"ovt-transcribe-service-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var fakeRunner = new FakeProcessRunner(request =>
            {
                if (request.CommandPlan.ToolName == "ffmpeg")
                {
                    File.WriteAllText(request.ProducedPaths[0], "fake-wave");
                }
                else if (request.CommandPlan.ToolName == "whisper-cli")
                {
                    File.WriteAllText(
                        request.ProducedPaths[0],
                        """
                        {
                          "result": { "language": "en" },
                          "transcription": [
                            {
                              "text": " Hello from whisper",
                              "offsets": {
                                "from": 0,
                                "to": 1500
                              }
                            }
                          ]
                        }
                        """);
                }

                return Task.FromResult(new ExecutionResult
                {
                    Status = ExecutionStatus.Succeeded,
                    ExitCode = 0,
                    StartedAtUtc = DateTimeOffset.UtcNow,
                    FinishedAtUtc = DateTimeOffset.UtcNow,
                    Duration = TimeSpan.Zero,
                    CommandPlan = request.CommandPlan,
                    ProducedPaths = request.ProducedPaths
                });
            });

            var service = new WhisperCppTranscriptionService(
                new AudioWaveformExtractRunner(new FfmpegAudioWaveformExtractCommandBuilder(), fakeRunner),
                new WhisperCppTranscriptionRunner(new WhisperCppCommandBuilder(), fakeRunner),
                new WhisperCppJsonParser());

            var transcript = await service.TranscribeAsync(
                new WhisperCppTranscriptionRequest
                {
                    InputPath = Path.Combine(tempDirectory, "input.mp4"),
                    ModelPath = Path.Combine(tempDirectory, "ggml-base.bin"),
                    Language = "en"
                },
                ffmpegExecutablePath: "ffmpeg-custom",
                whisperExecutablePath: "whisper-cli-custom");

            Assert.Equal("en", transcript.Language);
            var segment = Assert.Single(transcript.Segments);
            Assert.Equal("Hello from whisper", segment.Text);
            Assert.Equal(TimeSpan.FromSeconds(1.5), segment.End);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task TranscribeAsync_ThrowsWhenAudioPreparationFails()
    {
        var fakeRunner = new FakeProcessRunner(request => Task.FromResult(new ExecutionResult
        {
            Status = ExecutionStatus.Failed,
            ExitCode = 1,
            StartedAtUtc = DateTimeOffset.UtcNow,
            FinishedAtUtc = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            CommandPlan = request.CommandPlan,
            OutputLines =
            [
                new ProcessOutputLine
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Channel = ProcessOutputChannel.StandardError,
                    IsError = true,
                    Text = "ffmpeg not found"
                }
            ],
            ErrorMessage = "Process exited with code 1."
        }));

        var service = new WhisperCppTranscriptionService(
            new AudioWaveformExtractRunner(new FfmpegAudioWaveformExtractCommandBuilder(), fakeRunner),
            new WhisperCppTranscriptionRunner(new WhisperCppCommandBuilder(), fakeRunner),
            new WhisperCppJsonParser());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.TranscribeAsync(
            new WhisperCppTranscriptionRequest
            {
                InputPath = "input.mp4",
                ModelPath = "ggml-base.bin"
            }));

        Assert.Contains("audio preparation failed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
