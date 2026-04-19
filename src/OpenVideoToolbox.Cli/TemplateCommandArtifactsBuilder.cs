namespace OpenVideoToolbox.Cli;

internal static class TemplateCommandArtifactsBuilder
{
    public static TemplateCommandBundle BuildCommandBundle(
        IReadOnlyList<string> commands,
        IReadOnlyList<TemplateSeedCommand> seedCommands,
        IReadOnlyList<TemplateSignalInstruction> signalInstructions,
        IReadOnlyList<string> artifactCommands,
        bool requiresPluginDir = false)
    {
        var variables = BuildVariables(signalInstructions, requiresPluginDir);

        return new TemplateCommandBundle
        {
            Variables = variables,
            InitPlanCommands = commands,
            SeedCommands = seedCommands,
            SignalInstructions = signalInstructions,
            SignalCommands = signalInstructions.Select(instruction => instruction.Command).ToArray(),
            ArtifactCommands = artifactCommands,
            WorkflowCommands = requiresPluginDir
                ?
                [
                    "ovt validate-plan --plan edit.json --plugin-dir <plugin-dir>",
                    "ovt render --plan edit.json --preview",
                    "ovt mix-audio --plan edit.json --output mixed.wav --preview"
                ]
                :
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
        var seedLines = BuildSeedScriptLines(
            commandBundle.SeedCommands,
            BuildPowerShellPlaceholderMap(commandBundle.Variables),
            "# ");
        var signalLines = BuildSignalScriptLines(
            commandBundle.SignalInstructions,
            BuildPowerShellPlaceholderMap(commandBundle.Variables),
            "# ");
        var artifactCommands = commandBundle.ArtifactCommands
            .Select(command => ReplaceCommandPlaceholders(command, BuildPowerShellPlaceholderMap(commandBundle.Variables)))
            .ToArray();
        var workflowCommands = commandBundle.WorkflowCommands
            .Select(command => ReplaceCommandPlaceholders(command, BuildPowerShellPlaceholderMap(commandBundle.Variables)))
            .ToArray();
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
                "# seed mode examples",
                ..seedLines,
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
        var seedLines = BuildSeedScriptLines(
            commandBundle.SeedCommands,
            BuildBatchPlaceholderMap(commandBundle.Variables),
            "REM ");
        var signalLines = BuildSignalScriptLines(
            commandBundle.SignalInstructions,
            BuildBatchPlaceholderMap(commandBundle.Variables),
            "REM ");
        var artifactCommands = commandBundle.ArtifactCommands
            .Select(command => ReplaceCommandPlaceholders(command, BuildBatchPlaceholderMap(commandBundle.Variables)))
            .ToArray();
        var workflowCommands = commandBundle.WorkflowCommands
            .Select(command => ReplaceCommandPlaceholders(command, BuildBatchPlaceholderMap(commandBundle.Variables)))
            .ToArray();
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
                "REM seed mode examples",
                ..seedLines,
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
        var seedLines = BuildSeedScriptLines(
            commandBundle.SeedCommands,
            BuildShellPlaceholderMap(commandBundle.Variables),
            "# ");
        var signalLines = BuildSignalScriptLines(
            commandBundle.SignalInstructions,
            BuildShellPlaceholderMap(commandBundle.Variables),
            "# ");
        var artifactCommands = commandBundle.ArtifactCommands
            .Select(command => ReplaceCommandPlaceholders(command, BuildShellPlaceholderMap(commandBundle.Variables)))
            .ToArray();
        var workflowCommands = commandBundle.WorkflowCommands
            .Select(command => ReplaceCommandPlaceholders(command, BuildShellPlaceholderMap(commandBundle.Variables)))
            .ToArray();
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
                "# seed mode examples",
                ..seedLines,
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

    private static IReadOnlyList<string> BuildSeedScriptLines(
        IReadOnlyList<TemplateSeedCommand> seedCommands,
        IReadOnlyDictionary<string, string> replacements,
        string commentPrefix)
    {
        var lines = new List<string>();
        foreach (var seedCommand in seedCommands)
        {
            lines.Add($"{commentPrefix}{seedCommand.Mode} seed example");
            lines.Add(ReplaceCommandPlaceholders(seedCommand.Command, replacements));

            if (seedCommand.Variants is null)
            {
                continue;
            }

            foreach (var variant in seedCommand.Variants)
            {
                var recommendationSuffix = variant.Recommended ? " (recommended)" : string.Empty;
                lines.Add($"{commentPrefix}{seedCommand.Mode} variant: {variant.Key}{recommendationSuffix}");
                lines.Add(ReplaceCommandPlaceholders(variant.Command, replacements));
            }
        }

        return lines;
    }

    private static IReadOnlyDictionary<string, string> BuildVariables(
        IReadOnlyList<TemplateSignalInstruction> signalInstructions,
        bool requiresPluginDir)
    {
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["inputPath"] = "<input>"
        };

        if (signalInstructions.Any(instruction => instruction.Command.Contains("<whisper-model-path>", StringComparison.Ordinal)))
        {
            variables["whisperModelPath"] = "<whisper-model-path>";
        }

        if (requiresPluginDir)
        {
            variables["pluginDir"] = "<plugin-dir>";
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

        if (variables.ContainsKey("pluginDir"))
        {
            map["<plugin-dir>"] = "$PluginDir";
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

        if (variables.ContainsKey("pluginDir"))
        {
            map["<plugin-dir>"] = "\"%PLUGIN_DIR%\"";
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

        if (variables.ContainsKey("pluginDir"))
        {
            map["<plugin-dir>"] = "\"$PLUGIN_DIR\"";
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

        if (variables.ContainsKey("pluginDir"))
        {
            lines.Add("$PluginDir = \"<plugin-dir>\"");
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

        if (variables.ContainsKey("pluginDir"))
        {
            lines.Add("set PLUGIN_DIR=<plugin-dir>");
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

        if (variables.ContainsKey("pluginDir"))
        {
            lines.Add("PLUGIN_DIR=\"<plugin-dir>\"");
        }

        return lines;
    }
}

internal sealed record TemplateCommandBundle
{
    public IReadOnlyDictionary<string, string> Variables { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<string> InitPlanCommands { get; init; } = [];

    public IReadOnlyList<TemplateSeedCommand> SeedCommands { get; init; } = [];

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

internal sealed record TemplateSeedCommand
{
    public required string Mode { get; init; }

    public required string Command { get; init; }

    public IReadOnlyList<TemplateSeedVariant>? Variants { get; init; }
}

internal sealed record TemplateSeedVariant
{
    public required string Key { get; init; }

    public required string Command { get; init; }

    public bool Recommended { get; init; }

    public string? Strategy { get; init; }
}
