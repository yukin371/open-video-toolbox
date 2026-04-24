using OpenVideoToolbox.Cli;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed class TemplateCommandArtifactsBuilderTests
{
    [Fact]
    public void BuildCommandBundle_ReturnsStableWorkflowCommands()
    {
        var bundle = TemplateCommandArtifactsBuilder.BuildCommandBundle(
            [
                "ovt init-plan <input> --template shorts-captioned --output edit.json --render-output final.mp4"
            ],
            [
                new TemplateSeedCommand
                {
                    Mode = "manual",
                    Command = "ovt init-plan <input> --template shorts-captioned --output edit.json --render-output final.mp4"
                }
            ],
            [
                new TemplateSignalInstruction
                {
                    Kind = "transcript",
                    Command = "ovt transcribe <input> --model <whisper-model-path> --output transcript.json",
                    Consumption = "Pass --transcript transcript.json to init-plan when dialogue should drive the first cut. If edit.json already exists, attach it with ovt attach-plan-material --plan edit.json --transcript --path transcript.json --check-files before inspect-plan / validate-plan / render."
                }
            ],
            ["ovt subtitle <input> --transcript transcript.json --format srt --output subtitles.srt"]);

        Assert.Equal("<input>", bundle.Variables["inputPath"]);
        Assert.Equal("<whisper-model-path>", bundle.Variables["whisperModelPath"]);
        Assert.Single(bundle.InitPlanCommands);
        Assert.Single(bundle.SeedCommands);
        Assert.Single(bundle.SignalInstructions);
        Assert.Single(bundle.SignalCommands);
        Assert.Single(bundle.ArtifactCommands);
        Assert.Contains("ovt inspect-plan --plan edit.json --check-files", bundle.WorkflowCommands);
        Assert.Contains("ovt validate-plan --plan edit.json --check-files", bundle.WorkflowCommands);
        Assert.Contains("ovt render --plan edit.json --preview", bundle.WorkflowCommands);
        Assert.Contains("ovt mix-audio --plan edit.json --output mixed.wav --preview", bundle.WorkflowCommands);
    }

    [Fact]
    public void BuildPowerShellCommandScript_RewritesInputPlaceholder()
    {
        var bundle = TemplateCommandArtifactsBuilder.BuildCommandBundle(
            [
                "ovt init-plan <input> --template shorts-captioned --output edit.json --render-output final.mp4"
            ],
            [
                new TemplateSeedCommand
                {
                    Mode = "transcript",
                    Command = "ovt init-plan <input> --template shorts-captioned --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript",
                    Variants =
                    [
                        new TemplateSeedVariant
                        {
                            Key = "grouped",
                            Command = "ovt init-plan <input> --template shorts-captioned --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript --transcript-segment-group-size 2",
                            Recommended = true,
                            Strategy = "grouped"
                        }
                    ]
                }
            ],
            [
                new TemplateSignalInstruction
                {
                    Kind = "transcript",
                    Command = "ovt transcribe <input> --model <whisper-model-path> --output transcript.json",
                    Consumption = "Pass --transcript transcript.json to init-plan when dialogue should drive the first cut. If edit.json already exists, attach it with ovt attach-plan-material --plan edit.json --transcript --path transcript.json --check-files before inspect-plan / validate-plan / render."
                }
            ],
            ["ovt subtitle <input> --transcript transcript.json --format srt --output subtitles.srt"]);

        var script = TemplateCommandArtifactsBuilder.BuildPowerShellCommandScript(bundle);

        Assert.Contains("$InputPath = \"<input>\"", script);
        Assert.Contains("$WhisperModelPath = \"<whisper-model-path>\"", script);
        Assert.Contains("# Pass --transcript transcript.json to init-plan when dialogue should drive the first cut. If edit.json already exists, attach it with ovt attach-plan-material --plan edit.json --transcript --path transcript.json --check-files before inspect-plan / validate-plan / render.", script);
        Assert.Contains("ovt transcribe $InputPath --model $WhisperModelPath --output transcript.json", script);
        Assert.Contains("# transcript seed example", script);
        Assert.Contains("ovt init-plan $InputPath --template shorts-captioned --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript", script);
        Assert.Contains("# transcript variant: grouped (recommended)", script);
        Assert.Contains("ovt init-plan $InputPath --template shorts-captioned --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript --transcript-segment-group-size 2", script);
        Assert.Contains("ovt subtitle $InputPath --transcript transcript.json --format srt --output subtitles.srt", script);
        Assert.Contains("ovt init-plan $InputPath --template shorts-captioned --output edit.json --render-output final.mp4", script);
        Assert.DoesNotContain("ovt init-plan <input>", script);
    }

    [Fact]
    public void BuildBatchAndShellScripts_RewriteInputPlaceholderForEachShell()
    {
        var bundle = TemplateCommandArtifactsBuilder.BuildCommandBundle(
            [
                "ovt init-plan <input> --template commentary-bgm --output edit.json --render-output final.mp4"
            ],
            [],
            [
                new TemplateSignalInstruction
                {
                    Kind = "silence",
                    Command = "ovt detect-silence <input> --output silence.json",
                    Consumption = "Review silence.json before hand-tuning edit.json clip boundaries."
                }
            ],
            []);

        var batchScript = TemplateCommandArtifactsBuilder.BuildBatchCommandScript(bundle);
        var shellScript = TemplateCommandArtifactsBuilder.BuildShellCommandScript(bundle);

        Assert.Contains("REM Review silence.json before hand-tuning edit.json clip boundaries.", batchScript);
        Assert.Contains("ovt detect-silence \"%INPUT_PATH%\" --output silence.json", batchScript);
        Assert.Contains("ovt init-plan \"%INPUT_PATH%\" --template commentary-bgm --output edit.json --render-output final.mp4", batchScript);
        Assert.DoesNotContain("ovt init-plan <input>", batchScript);
        Assert.Contains("# Review silence.json before hand-tuning edit.json clip boundaries.", shellScript);
        Assert.Contains("ovt detect-silence \"$INPUT_PATH\" --output silence.json", shellScript);
        Assert.Contains("ovt init-plan \"$INPUT_PATH\" --template commentary-bgm --output edit.json --render-output final.mp4", shellScript);
        Assert.DoesNotContain("ovt init-plan <input>", shellScript);
    }

    [Fact]
    public void BuildBatchAndShellScripts_IncludeSeedVariantsForEachShell()
    {
        var bundle = TemplateCommandArtifactsBuilder.BuildCommandBundle(
            [
                "ovt init-plan <input> --template commentary-bgm --output edit.json --render-output final.mp4"
            ],
            [
                new TemplateSeedCommand
                {
                    Mode = "transcript",
                    Command = "ovt init-plan <input> --template commentary-bgm --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript",
                    Variants =
                    [
                        new TemplateSeedVariant
                        {
                            Key = "max-gap",
                            Command = "ovt init-plan <input> --template commentary-bgm --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript --transcript-segment-group-size 3 --max-transcript-gap-ms 200",
                            Recommended = true,
                            Strategy = "max-gap"
                        }
                    ]
                }
            ],
            [],
            []);

        var batchScript = TemplateCommandArtifactsBuilder.BuildBatchCommandScript(bundle);
        var shellScript = TemplateCommandArtifactsBuilder.BuildShellCommandScript(bundle);

        Assert.Contains("REM transcript seed example", batchScript);
        Assert.Contains("ovt init-plan \"%INPUT_PATH%\" --template commentary-bgm --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript", batchScript);
        Assert.Contains("REM transcript variant: max-gap (recommended)", batchScript);
        Assert.Contains("ovt init-plan \"%INPUT_PATH%\" --template commentary-bgm --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript --transcript-segment-group-size 3 --max-transcript-gap-ms 200", batchScript);
        Assert.Contains("# transcript seed example", shellScript);
        Assert.Contains("ovt init-plan \"$INPUT_PATH\" --template commentary-bgm --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript", shellScript);
        Assert.Contains("# transcript variant: max-gap (recommended)", shellScript);
        Assert.Contains("ovt init-plan \"$INPUT_PATH\" --template commentary-bgm --output edit.json --render-output final.mp4 --transcript transcript.json --seed-from-transcript --transcript-segment-group-size 3 --max-transcript-gap-ms 200", shellScript);
    }

    [Fact]
    public void BuildCommandBundle_WithPluginContext_AddsPluginDirWorkflowAndVariables()
    {
        var bundle = TemplateCommandArtifactsBuilder.BuildCommandBundle(
            [
                "ovt init-plan <input> --template plugin-captioned --output edit.json --render-output final.mp4 --plugin-dir <plugin-dir>"
            ],
            [
                new TemplateSeedCommand
                {
                    Mode = "manual",
                    Command = "ovt init-plan <input> --template plugin-captioned --output edit.json --render-output final.mp4 --plugin-dir <plugin-dir>"
                }
            ],
            [],
            [],
            requiresPluginDir: true);

        Assert.Equal("<plugin-dir>", bundle.Variables["pluginDir"]);
        Assert.Contains("ovt inspect-plan --plan edit.json --check-files --plugin-dir <plugin-dir>", bundle.WorkflowCommands);
        Assert.Contains("ovt validate-plan --plan edit.json --check-files --plugin-dir <plugin-dir>", bundle.WorkflowCommands);

        var powerShellScript = TemplateCommandArtifactsBuilder.BuildPowerShellCommandScript(bundle);
        var batchScript = TemplateCommandArtifactsBuilder.BuildBatchCommandScript(bundle);
        var shellScript = TemplateCommandArtifactsBuilder.BuildShellCommandScript(bundle);

        Assert.Contains("$PluginDir = \"<plugin-dir>\"", powerShellScript);
        Assert.Contains("ovt inspect-plan --plan edit.json --check-files --plugin-dir $PluginDir", powerShellScript);
        Assert.Contains("ovt validate-plan --plan edit.json --check-files --plugin-dir $PluginDir", powerShellScript);
        Assert.Contains("set PLUGIN_DIR=<plugin-dir>", batchScript);
        Assert.Contains("ovt inspect-plan --plan edit.json --check-files --plugin-dir \"%PLUGIN_DIR%\"", batchScript);
        Assert.Contains("ovt validate-plan --plan edit.json --check-files --plugin-dir \"%PLUGIN_DIR%\"", batchScript);
        Assert.Contains("PLUGIN_DIR=\"<plugin-dir>\"", shellScript);
        Assert.Contains("ovt inspect-plan --plan edit.json --check-files --plugin-dir \"$PLUGIN_DIR\"", shellScript);
        Assert.Contains("ovt validate-plan --plan edit.json --check-files --plugin-dir \"$PLUGIN_DIR\"", shellScript);
    }
}
