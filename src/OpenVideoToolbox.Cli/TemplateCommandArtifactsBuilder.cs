namespace OpenVideoToolbox.Cli;

internal static class TemplateCommandArtifactsBuilder
{
    public static TemplateCommandBundle BuildCommandBundle(
        IReadOnlyList<string> commands,
        IReadOnlyList<object> seedCommands,
        IReadOnlyList<TemplateSignalInstruction> signalInstructions,
        IReadOnlyList<string> artifactCommands)
    {
        return new TemplateCommandBundle
        {
            Variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["inputPath"] = "<input>"
            },
            InitPlanCommands = commands,
            SeedCommands = seedCommands,
            SignalInstructions = signalInstructions,
            SignalCommands = signalInstructions.Select(instruction => instruction.Command).ToArray(),
            ArtifactCommands = artifactCommands,
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
        var signalLines = BuildSignalScriptLines(
            commandBundle.SignalInstructions,
            "$InputPath",
            "# ");
        var artifactCommands = commandBundle.ArtifactCommands
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
                ..signalLines,
                "",
                "# artifact preparation examples",
                ..artifactCommands,
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
        var signalLines = BuildSignalScriptLines(
            commandBundle.SignalInstructions,
            "\"%INPUT_PATH%\"",
            "REM ");
        var artifactCommands = commandBundle.ArtifactCommands
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
                ..signalLines,
                "",
                "REM artifact preparation examples",
                ..artifactCommands,
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
        var signalLines = BuildSignalScriptLines(
            commandBundle.SignalInstructions,
            "\"$INPUT_PATH\"",
            "# ");
        var artifactCommands = commandBundle.ArtifactCommands
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
                ..signalLines,
                "",
                "# artifact preparation examples",
                ..artifactCommands,
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

    private static IReadOnlyList<string> BuildSignalScriptLines(
        IReadOnlyList<TemplateSignalInstruction> signalInstructions,
        string inputReplacement,
        string commentPrefix)
    {
        var lines = new List<string>();
        foreach (var instruction in signalInstructions)
        {
            if (!string.IsNullOrWhiteSpace(instruction.Consumption))
            {
                lines.Add($"{commentPrefix}{instruction.Consumption}");
            }

            lines.Add(ReplaceCommandInputPlaceholder(instruction.Command, inputReplacement));
        }

        return lines;
    }
}

internal sealed record TemplateCommandBundle
{
    public IReadOnlyDictionary<string, string> Variables { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<string> InitPlanCommands { get; init; } = [];

    public IReadOnlyList<object> SeedCommands { get; init; } = [];

    public IReadOnlyList<TemplateSignalInstruction> SignalInstructions { get; init; } = [];

    public IReadOnlyList<string> SignalCommands { get; init; } = [];

    public IReadOnlyList<string> ArtifactCommands { get; init; } = [];

    public IReadOnlyList<string> WorkflowCommands { get; init; } = [];
}

internal sealed record TemplateSignalInstruction
{
    public required string Kind { get; init; }

    public required string Command { get; init; }

    public required string Consumption { get; init; }
}
