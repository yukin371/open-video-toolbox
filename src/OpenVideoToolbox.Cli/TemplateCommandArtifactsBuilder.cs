namespace OpenVideoToolbox.Cli;

internal static class TemplateCommandArtifactsBuilder
{
    public static TemplateCommandBundle BuildCommandBundle(
        IReadOnlyList<string> commands,
        IReadOnlyList<object> seedCommands,
        IReadOnlyList<TemplateSignalInstruction> signalInstructions,
        IReadOnlyList<string> artifactCommands)
    {
        var variables = BuildVariables(signalInstructions);

        return new TemplateCommandBundle
        {
            Variables = variables,
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
            .Select(command => ReplaceCommandPlaceholders(command, BuildPowerShellPlaceholderMap(commandBundle.Variables)))
            .ToArray();
        var signalLines = BuildSignalScriptLines(
            commandBundle.SignalInstructions,
            BuildPowerShellPlaceholderMap(commandBundle.Variables),
            "# ");
        var artifactCommands = commandBundle.ArtifactCommands
            .Select(command => ReplaceCommandPlaceholders(command, BuildPowerShellPlaceholderMap(commandBundle.Variables)))
            .ToArray();
        var workflowCommands = commandBundle.WorkflowCommands.ToArray();
        var variableLines = BuildPowerShellVariableLines(commandBundle.Variables);
        return string.Join(
            Environment.NewLine,
            [
                "# Open Video Toolbox helper commands",
                ..variableLines,
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
            .Select(command => ReplaceCommandPlaceholders(command, BuildBatchPlaceholderMap(commandBundle.Variables)))
            .ToArray();
        var signalLines = BuildSignalScriptLines(
            commandBundle.SignalInstructions,
            BuildBatchPlaceholderMap(commandBundle.Variables),
            "REM ");
        var artifactCommands = commandBundle.ArtifactCommands
            .Select(command => ReplaceCommandPlaceholders(command, BuildBatchPlaceholderMap(commandBundle.Variables)))
            .ToArray();
        var workflowCommands = commandBundle.WorkflowCommands.ToArray();
        var variableLines = BuildBatchVariableLines(commandBundle.Variables);
        return string.Join(
            Environment.NewLine,
            [
                "@echo off",
                ..variableLines,
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
            .Select(command => ReplaceCommandPlaceholders(command, BuildShellPlaceholderMap(commandBundle.Variables)))
            .ToArray();
        var signalLines = BuildSignalScriptLines(
            commandBundle.SignalInstructions,
            BuildShellPlaceholderMap(commandBundle.Variables),
            "# ");
        var artifactCommands = commandBundle.ArtifactCommands
            .Select(command => ReplaceCommandPlaceholders(command, BuildShellPlaceholderMap(commandBundle.Variables)))
            .ToArray();
        var workflowCommands = commandBundle.WorkflowCommands.ToArray();
        var variableLines = BuildShellVariableLines(commandBundle.Variables);
        return string.Join(
            Environment.NewLine,
            [
                "#!/usr/bin/env sh",
                ..variableLines,
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

    private static string ReplaceCommandPlaceholders(string command, IReadOnlyDictionary<string, string> replacements)
    {
        var result = command;
        foreach (var pair in replacements)
        {
            result = result.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
        }

        return result;
    }

    private static IReadOnlyList<string> BuildSignalScriptLines(
        IReadOnlyList<TemplateSignalInstruction> signalInstructions,
        IReadOnlyDictionary<string, string> replacements,
        string commentPrefix)
    {
        var lines = new List<string>();
        foreach (var instruction in signalInstructions)
        {
            if (!string.IsNullOrWhiteSpace(instruction.Consumption))
            {
                lines.Add($"{commentPrefix}{instruction.Consumption}");
            }

            lines.Add(ReplaceCommandPlaceholders(instruction.Command, replacements));
        }

        return lines;
    }

    private static IReadOnlyDictionary<string, string> BuildVariables(IReadOnlyList<TemplateSignalInstruction> signalInstructions)
    {
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["inputPath"] = "<input>"
        };

        if (signalInstructions.Any(instruction => instruction.Command.Contains("<whisper-model-path>", StringComparison.Ordinal)))
        {
            variables["whisperModelPath"] = "<whisper-model-path>";
        }

        return variables;
    }

    private static IReadOnlyDictionary<string, string> BuildPowerShellPlaceholderMap(IReadOnlyDictionary<string, string> variables)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["<input>"] = "$InputPath"
        };

        if (variables.ContainsKey("whisperModelPath"))
        {
            map["<whisper-model-path>"] = "$WhisperModelPath";
        }

        return map;
    }

    private static IReadOnlyDictionary<string, string> BuildBatchPlaceholderMap(IReadOnlyDictionary<string, string> variables)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["<input>"] = "\"%INPUT_PATH%\""
        };

        if (variables.ContainsKey("whisperModelPath"))
        {
            map["<whisper-model-path>"] = "\"%WHISPER_MODEL_PATH%\"";
        }

        return map;
    }

    private static IReadOnlyDictionary<string, string> BuildShellPlaceholderMap(IReadOnlyDictionary<string, string> variables)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["<input>"] = "\"$INPUT_PATH\""
        };

        if (variables.ContainsKey("whisperModelPath"))
        {
            map["<whisper-model-path>"] = "\"$WHISPER_MODEL_PATH\"";
        }

        return map;
    }

    private static IReadOnlyList<string> BuildPowerShellVariableLines(IReadOnlyDictionary<string, string> variables)
    {
        var lines = new List<string>
        {
            "$InputPath = \"<input>\""
        };

        if (variables.ContainsKey("whisperModelPath"))
        {
            lines.Add("$WhisperModelPath = \"<whisper-model-path>\"");
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildBatchVariableLines(IReadOnlyDictionary<string, string> variables)
    {
        var lines = new List<string>
        {
            "set INPUT_PATH=<input>"
        };

        if (variables.ContainsKey("whisperModelPath"))
        {
            lines.Add("set WHISPER_MODEL_PATH=<whisper-model-path>");
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildShellVariableLines(IReadOnlyDictionary<string, string> variables)
    {
        var lines = new List<string>
        {
            "INPUT_PATH=\"<input>\""
        };

        if (variables.ContainsKey("whisperModelPath"))
        {
            lines.Add("WHISPER_MODEL_PATH=\"<whisper-model-path>\"");
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
