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
            [],
            ["ovt transcribe <input> --model <path> --output transcript.json"],
            ["ovt subtitle <input> --transcript transcript.json --format srt --output subtitles.srt"]);

        Assert.Equal("<input>", bundle.Variables["inputPath"]);
        Assert.Single(bundle.InitPlanCommands);
        Assert.Single(bundle.SignalCommands);
        Assert.Single(bundle.ArtifactCommands);
        Assert.Contains("ovt validate-plan --plan edit.json", bundle.WorkflowCommands);
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
            [],
            ["ovt transcribe <input> --model <path> --output transcript.json"],
            ["ovt subtitle <input> --transcript transcript.json --format srt --output subtitles.srt"]);

        var script = TemplateCommandArtifactsBuilder.BuildPowerShellCommandScript(bundle);

        Assert.Contains("$InputPath = \"<input>\"", script);
        Assert.Contains("ovt transcribe $InputPath --model <path> --output transcript.json", script);
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
            ["ovt detect-silence <input> --output silence.json"],
            []);

        var batchScript = TemplateCommandArtifactsBuilder.BuildBatchCommandScript(bundle);
        var shellScript = TemplateCommandArtifactsBuilder.BuildShellCommandScript(bundle);

        Assert.Contains("ovt detect-silence \"%INPUT_PATH%\" --output silence.json", batchScript);
        Assert.Contains("ovt init-plan \"%INPUT_PATH%\" --template commentary-bgm --output edit.json --render-output final.mp4", batchScript);
        Assert.DoesNotContain("ovt init-plan <input>", batchScript);
        Assert.Contains("ovt detect-silence \"$INPUT_PATH\" --output silence.json", shellScript);
        Assert.Contains("ovt init-plan \"$INPUT_PATH\" --template commentary-bgm --output edit.json --render-output final.mp4", shellScript);
        Assert.DoesNotContain("ovt init-plan <input>", shellScript);
    }
}
