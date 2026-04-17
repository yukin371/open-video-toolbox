using OpenVideoToolbox.Core.Execution;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class WhisperCppCommandBuilderTests
{
    [Fact]
    public void Build_CreatesDeterministicWhisperCppCommandPlan()
    {
        var builder = new WhisperCppCommandBuilder();
        var request = new WhisperCppExecutionRequest
        {
            InputWavePath = "temp/input.wav",
            ModelPath = "models/ggml-base.bin",
            OutputFilePrefix = "temp/transcript",
            Language = "zh",
            TranslateToEnglish = true
        };

        var plan = builder.Build(request);

        Assert.Equal(
            [
                "-m",
                "models/ggml-base.bin",
                "-f",
                "temp/input.wav",
                "-ojf",
                "-of",
                "temp/transcript",
                "-np",
                "-l",
                "zh",
                "-tr"
            ],
            plan.Arguments);
    }
}
