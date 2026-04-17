using OpenVideoToolbox.Core.AudioSeparation;
using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class DemucsAudioSeparationServiceTests
{
    [Fact]
    public async Task SeparateAsync_MapsExpectedStemPaths()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-demucs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var fakeRunner = new FakeProcessRunner(request =>
            {
                foreach (var path in request.ProducedPaths)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, "fake-stem");
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
            var service = new DemucsAudioSeparationService(
                new DemucsSeparationRunner(new DemucsCommandBuilder(), fakeRunner));

            var document = await service.SeparateAsync(
                new DemucsSeparationRequest
                {
                    InputPath = "input.mp4",
                    OutputDirectory = outputDirectory,
                    Model = "htdemucs"
                },
                executablePath: "demucs-custom");

            Assert.Equal("htdemucs", document.Model);
            Assert.True(File.Exists(document.Stems.Vocals));
            Assert.True(File.Exists(document.Stems.Accompaniment));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SeparateAsync_ThrowsWhenProcessFails()
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
                    Text = "demucs not found"
                }
            ],
            ErrorMessage = "Process exited with code 1."
        }));
        var service = new DemucsAudioSeparationService(
            new DemucsSeparationRunner(new DemucsCommandBuilder(), fakeRunner));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SeparateAsync(
            new DemucsSeparationRequest
            {
                InputPath = "input.mp4",
                OutputDirectory = "stems"
            }));

        Assert.Contains("audio separation failed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
