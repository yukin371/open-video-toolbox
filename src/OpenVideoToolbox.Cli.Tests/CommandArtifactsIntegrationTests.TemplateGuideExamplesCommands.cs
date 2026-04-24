using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task TemplatesWriteExamples_WritesStableCommandArtifacts()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-template-guide-{Guid.NewGuid():N}");

        try
        {
            var result = await RunCliAsync("templates", "commentary-bgm", "--write-examples", outputDirectory);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(Path.Combine(outputDirectory, "commands.json")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "commands.ps1")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "commands.cmd")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "commands.sh")));

            var commands = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.json")))!.AsObject();
            Assert.Equal("<input>", commands["variables"]!["inputPath"]!.GetValue<string>());

            var initPlanCommands = commands["initPlanCommands"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray();
            Assert.Contains(initPlanCommands, command => command.Contains("--template commentary-bgm", StringComparison.Ordinal));

            var seedModes = commands["seedCommands"]!.AsArray()
                .Select(node => node!["mode"]!.GetValue<string>())
                .ToArray();
            Assert.True(seedModes.SequenceEqual(["manual", "transcript"]));

            var transcriptSeed = commands["seedCommands"]!.AsArray()
                .Single(node => node!["mode"]!.GetValue<string>() == "transcript")!
                .AsObject();
            var transcriptVariants = transcriptSeed["variants"]!.AsArray();
            Assert.Equal(3, transcriptVariants.Count);
            Assert.Equal("min-duration", transcriptVariants[0]!["key"]!.GetValue<string>());
            Assert.True(transcriptVariants[0]!["recommended"]!.GetValue<bool>());
            Assert.Equal("grouped", transcriptVariants[1]!["key"]!.GetValue<string>());
            Assert.False(transcriptVariants[1]!["recommended"]!.GetValue<bool>());
            Assert.Equal("max-gap", transcriptVariants[2]!["key"]!.GetValue<string>());

            var workflowCommands = commands["workflowCommands"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray();
            Assert.True(workflowCommands.SequenceEqual(
                [
                    "ovt inspect-plan --plan edit.json --check-files",
                    "ovt validate-plan --plan edit.json --check-files",
                    "ovt render --plan edit.json --preview",
                    "ovt mix-audio --plan edit.json --output mixed.wav --preview"
                ]));
            var signalCommands = commands["signalCommands"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray();
            Assert.True(signalCommands.SequenceEqual(
                [
                    "ovt transcribe <input> --model <whisper-model-path> --output transcript.json",
                    "ovt detect-silence <input> --output silence.json"
                ]));
            var signalInstructions = commands["signalInstructions"]!.AsArray();
            Assert.Equal(2, signalInstructions.Count);
            Assert.Contains(
                signalInstructions,
                node => node!["kind"]!.GetValue<string>() == "transcript"
                    && node["command"]!.GetValue<string>().Contains("<whisper-model-path>", StringComparison.Ordinal)
                    && node["consumption"]!.GetValue<string>().Contains("attach-plan-material --plan edit.json --transcript --path transcript.json --check-files", StringComparison.Ordinal));
            Assert.Contains(
                signalInstructions,
                node => node!["kind"]!.GetValue<string>() == "silence"
                    && node["consumption"]!.GetValue<string>().Contains("edit.json clip boundaries", StringComparison.Ordinal));
            Assert.Empty(commands["artifactCommands"]!.AsArray());

            var guide = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "guide.json")))!.AsObject();
            var commandFiles = guide["examples"]!["commandFiles"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray();
            Assert.True(commandFiles.SequenceEqual(["commands.json", "commands.ps1", "commands.cmd", "commands.sh"]));
            Assert.Equal("<whisper-model-path>", commands["variables"]!["whisperModelPath"]!.GetValue<string>());
            var powerShellScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.ps1"));
            Assert.Contains("$WhisperModelPath = \"<whisper-model-path>\"", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("# transcript seed example", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("ovt init-plan $InputPath --template commentary-bgm --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("# transcript variant: min-duration (recommended)", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("ovt init-plan $InputPath --template commentary-bgm --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript --min-transcript-segment-duration-ms 500", powerShellScript, StringComparison.Ordinal);
            var batchScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.cmd"));
            Assert.Contains("REM Review silence.json before hand-tuning edit.json clip boundaries", batchScript, StringComparison.Ordinal);
            Assert.Contains("REM transcript variant: min-duration (recommended)", batchScript, StringComparison.Ordinal);
            var shellScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.sh"));
            Assert.Contains("# transcript variant: min-duration (recommended)", shellScript, StringComparison.Ordinal);
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
    public async Task TemplatesWriteExamples_ForCaptionedTemplate_WritesArtifactCommandsIntoAllScripts()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-template-captioned-{Guid.NewGuid():N}");

        try
        {
            var result = await RunCliAsync("templates", "shorts-captioned", "--write-examples", outputDirectory);

            Assert.Equal(0, result.ExitCode);

            var commands = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.json")))!.AsObject();
            Assert.Contains(
                commands["artifactCommands"]!.AsArray(),
                node => node!.GetValue<string>() == "ovt subtitle <input> --transcript transcript.json --format srt --output subtitles.srt");
            Assert.Contains(
                commands["artifactCommands"]!.AsArray(),
                node => node!.GetValue<string>() == "ovt attach-plan-material --plan edit.json --transcript --path transcript.json --check-files");
            Assert.Contains(
                commands["artifactCommands"]!.AsArray(),
                node => node!.GetValue<string>() == "ovt attach-plan-material --plan edit.json --subtitles --path subtitles.srt --check-files");

            var powerShellScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.ps1"));
            var batchScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.cmd"));
            var shellScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.sh"));

            Assert.Contains("ovt subtitle $InputPath --transcript transcript.json --format srt --output subtitles.srt", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("ovt attach-plan-material --plan edit.json --transcript --path transcript.json --check-files", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("ovt attach-plan-material --plan edit.json --subtitles --path subtitles.srt --check-files", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("ovt subtitle \"%INPUT_PATH%\" --transcript transcript.json --format srt --output subtitles.srt", batchScript, StringComparison.Ordinal);
            Assert.Contains("ovt attach-plan-material --plan edit.json --transcript --path transcript.json --check-files", batchScript, StringComparison.Ordinal);
            Assert.Contains("ovt attach-plan-material --plan edit.json --subtitles --path subtitles.srt --check-files", batchScript, StringComparison.Ordinal);
            Assert.Contains("ovt subtitle \"$INPUT_PATH\" --transcript transcript.json --format srt --output subtitles.srt", shellScript, StringComparison.Ordinal);
            Assert.Contains("ovt attach-plan-material --plan edit.json --transcript --path transcript.json --check-files", shellScript, StringComparison.Ordinal);
            Assert.Contains("ovt attach-plan-material --plan edit.json --subtitles --path subtitles.srt --check-files", shellScript, StringComparison.Ordinal);
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
    public async Task TemplatesWriteExamples_ForBeatMontage_WritesStemGuidanceIntoAllScripts()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-template-beat-scripts-{Guid.NewGuid():N}");

        try
        {
            var result = await RunCliAsync("templates", "beat-montage", "--write-examples", outputDirectory);

            Assert.Equal(0, result.ExitCode);

            var powerShellScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.ps1"));
            var batchScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.cmd"));
            var shellScript = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "commands.sh"));

            Assert.Contains("After scaffold-template writes artifacts.json, point the bgm slot at stems/htdemucs/input/no_vocals.wav", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("ovt separate-audio $InputPath --output-dir stems", powerShellScript, StringComparison.Ordinal);
            Assert.Contains("After scaffold-template writes artifacts.json, point the bgm slot at stems/htdemucs/input/no_vocals.wav", batchScript, StringComparison.Ordinal);
            Assert.Contains("ovt separate-audio \"%INPUT_PATH%\" --output-dir stems", batchScript, StringComparison.Ordinal);
            Assert.Contains("After scaffold-template writes artifacts.json, point the bgm slot at stems/htdemucs/input/no_vocals.wav", shellScript, StringComparison.Ordinal);
            Assert.Contains("ovt separate-audio \"$INPUT_PATH\" --output-dir stems", shellScript, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
}
