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
            []);

        Assert.Equal("<input>", bundle.Variables["inputPath"]);
        Assert.Single(bundle.InitPlanCommands);
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
            []);

        var script = TemplateCommandArtifactsBuilder.BuildPowerShellCommandScript(bundle);

        Assert.Contains("$InputPath = \"<input>\"", script);
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
            []);

        var batchScript = TemplateCommandArtifactsBuilder.BuildBatchCommandScript(bundle);
        var shellScript = TemplateCommandArtifactsBuilder.BuildShellCommandScript(bundle);

        Assert.Contains("ovt init-plan \"%INPUT_PATH%\" --template commentary-bgm --output edit.json --render-output final.mp4", batchScript);
        Assert.DoesNotContain("ovt init-plan <input>", batchScript);
        Assert.Contains("ovt init-plan \"$INPUT_PATH\" --template commentary-bgm --output edit.json --render-output final.mp4", shellScript);
        Assert.DoesNotContain("ovt init-plan <input>", shellScript);
    }
}
