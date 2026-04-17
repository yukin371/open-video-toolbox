namespace OpenVideoToolbox.Cli;

internal static class TemplateCommandArtifactsBuilder
{
    public static TemplateCommandBundle BuildCommandBundle(
        IReadOnlyList<string> commands,
        IReadOnlyList<object> seedCommands,
        IReadOnlyList<string> signalCommands)
    {
        return new TemplateCommandBundle
        {
            Variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["inputPath"] = "<input>"
            },
            InitPlanCommands = commands,
            SeedCommands = seedCommands,
            SignalCommands = signalCommands,
            WorkflowCommands =
            [
                "ovt validate-plan --plan edit.json",
                "ovt render --plan edit.json --preview",
                "ovt mix-audio --plan edit.json --output mixed.wav --preview"
            ]
        };
    }

    public static string BuildPowerShellCommandScript(TemplateCommandBundle commandBundle)
    {
        var initPlanCommands = commandBundle.InitPlanCommands
            .Select(command => ReplaceCommandInputPlaceholder(command, "$InputPath"))
            .ToArray();
        var signalCommands = commandBundle.SignalCommands
            .Select(command => ReplaceCommandInputPlaceholder(command, "$InputPath"))
            .ToArray();
        var workflowCommands = commandBundle.WorkflowCommands.ToArray();
        return string.Join(
            Environment.NewLine,
            [
                "# Open Video Toolbox helper commands",
                "$InputPath = \"<input>\"",
                "",
                "# signal preparation examples",
                ..signalCommands,
                "",
                "# init-plan examples",
                ..initPlanCommands,
                "",
                "# workflow examples",
                ..workflowCommands,
                ""
            ]);
    }

    public static string BuildBatchCommandScript(TemplateCommandBundle commandBundle)
    {
        var initPlanCommands = commandBundle.InitPlanCommands
            .Select(command => ReplaceCommandInputPlaceholder(command, "\"%INPUT_PATH%\""))
            .ToArray();
        var signalCommands = commandBundle.SignalCommands
            .Select(command => ReplaceCommandInputPlaceholder(command, "\"%INPUT_PATH%\""))
            .ToArray();
        var workflowCommands = commandBundle.WorkflowCommands.ToArray();
        return string.Join(
            Environment.NewLine,
            [
                "@echo off",
                "set INPUT_PATH=<input>",
                "",
                "REM signal preparation examples",
                ..signalCommands,
                "",
                "REM init-plan example",
                ..initPlanCommands,
                "",
                "REM workflow examples",
                ..workflowCommands,
                ""
            ]);
    }

    public static string BuildShellCommandScript(TemplateCommandBundle commandBundle)
    {
        var initPlanCommands = commandBundle.InitPlanCommands
            .Select(command => ReplaceCommandInputPlaceholder(command, "\"$INPUT_PATH\""))
            .ToArray();
        var signalCommands = commandBundle.SignalCommands
            .Select(command => ReplaceCommandInputPlaceholder(command, "\"$INPUT_PATH\""))
            .ToArray();
        var workflowCommands = commandBundle.WorkflowCommands.ToArray();
        return string.Join(
            Environment.NewLine,
            [
                "#!/usr/bin/env sh",
                "INPUT_PATH=\"<input>\"",
                "",
                "# signal preparation examples",
                ..signalCommands,
                "",
                "# init-plan example",
                ..initPlanCommands,
                "",
                "# workflow examples",
                ..workflowCommands,
                ""
            ]);
    }

    private static string ReplaceCommandInputPlaceholder(string command, string replacement)
    {
        return command.Replace("<input>", replacement, StringComparison.Ordinal);
    }
}

internal sealed record TemplateCommandBundle
{
    public IReadOnlyDictionary<string, string> Variables { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<string> InitPlanCommands { get; init; } = [];

    public IReadOnlyList<object> SeedCommands { get; init; } = [];

    public IReadOnlyList<string> SignalCommands { get; init; } = [];

    public IReadOnlyList<string> WorkflowCommands { get; init; } = [];
}
