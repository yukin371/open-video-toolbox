using OpenVideoToolbox.Core;

namespace OpenVideoToolbox.Core.Execution;

public sealed class ExternalDependencyInspector
{
    private readonly IProcessRunner _processRunner;

    public ExternalDependencyInspector(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task<DependencyInspectionReport> InspectAsync(
        IReadOnlyList<DependencyProbeDefinition> definitions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        var dependencies = new List<DependencyProbeResult>(definitions.Count);
        foreach (var definition in definitions)
        {
            dependencies.Add(await InspectDependencyAsync(definition, cancellationToken).ConfigureAwait(false));
        }

        var missingRequiredCount = dependencies.Count(static dependency => dependency.Required && !dependency.IsAvailable);
        var missingOptionalCount = dependencies.Count(static dependency => !dependency.Required && !dependency.IsAvailable);

        return new DependencyInspectionReport
        {
            IsHealthy = missingRequiredCount == 0,
            MissingRequiredCount = missingRequiredCount,
            MissingOptionalCount = missingOptionalCount,
            Dependencies = dependencies
        };
    }

    private async Task<DependencyProbeResult> InspectDependencyAsync(
        DependencyProbeDefinition definition,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return definition.Kind switch
        {
            DependencyProbeKind.Executable => await InspectExecutableAsync(definition, cancellationToken).ConfigureAwait(false),
            DependencyProbeKind.File => InspectFile(definition),
            _ => throw new InvalidOperationException($"Unsupported dependency kind '{definition.Kind}'.")
        };
    }

    private async Task<DependencyProbeResult> InspectExecutableAsync(
        DependencyProbeDefinition definition,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(definition.ResolvedValue))
        {
            return CreateUnavailableResult(definition, "Dependency executable is not configured.");
        }

        var executablePath = definition.ResolvedValue;
        var commandPlan = new CommandPlan
        {
            ToolName = definition.Id,
            ExecutablePath = executablePath,
            Arguments = definition.ProbeArguments,
            CommandLine = BuildCommandLine(executablePath, definition.ProbeArguments)
        };

        try
        {
            var execution = await _processRunner.ExecuteAsync(
                new ProcessExecutionRequest
                {
                    CommandPlan = commandPlan,
                    Timeout = definition.Timeout
                },
                cancellationToken).ConfigureAwait(false);

            return new DependencyProbeResult
            {
                Id = definition.Id,
                Kind = definition.Kind,
                Required = definition.Required,
                Source = definition.Source,
                ResolvedValue = executablePath,
                IsAvailable = true,
                ProbeSucceeded = execution.Status == ExecutionStatus.Succeeded && execution.ExitCode == 0,
                ExitCode = execution.ExitCode,
                Detail = BuildExecutionDetail(execution)
            };
        }
        catch (Exception ex)
        {
            return CreateUnavailableResult(definition, ex.Message);
        }
    }

    private static DependencyProbeResult InspectFile(DependencyProbeDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.ResolvedValue))
        {
            return CreateUnavailableResult(definition, "Dependency path is not configured.");
        }

        var fullPath = Path.GetFullPath(definition.ResolvedValue);
        var exists = File.Exists(fullPath);

        return new DependencyProbeResult
        {
            Id = definition.Id,
            Kind = definition.Kind,
            Required = definition.Required,
            Source = definition.Source,
            ResolvedValue = fullPath,
            IsAvailable = exists,
            ProbeSucceeded = exists,
            Detail = exists
                ? "File exists."
                : $"File not found: {fullPath}"
        };
    }

    private static DependencyProbeResult CreateUnavailableResult(DependencyProbeDefinition definition, string detail)
    {
        return new DependencyProbeResult
        {
            Id = definition.Id,
            Kind = definition.Kind,
            Required = definition.Required,
            Source = definition.Source,
            ResolvedValue = definition.Kind == DependencyProbeKind.File && !string.IsNullOrWhiteSpace(definition.ResolvedValue)
                ? Path.GetFullPath(definition.ResolvedValue)
                : definition.ResolvedValue,
            IsAvailable = false,
            ProbeSucceeded = false,
            Detail = detail
        };
    }

    private static string BuildExecutionDetail(ExecutionResult execution)
    {
        var firstOutputLine = execution.OutputLines
            .Select(static line => line.Text)
            .FirstOrDefault(static text => !string.IsNullOrWhiteSpace(text));

        if (execution.Status == ExecutionStatus.Succeeded && execution.ExitCode == 0)
        {
            return firstOutputLine ?? "Probe succeeded.";
        }

        if (!string.IsNullOrWhiteSpace(execution.ErrorMessage) && !string.IsNullOrWhiteSpace(firstOutputLine))
        {
            return $"{execution.ErrorMessage} {firstOutputLine}";
        }

        if (!string.IsNullOrWhiteSpace(execution.ErrorMessage))
        {
            return execution.ErrorMessage;
        }

        if (!string.IsNullOrWhiteSpace(firstOutputLine))
        {
            return firstOutputLine;
        }

        return execution.ExitCode is { } exitCode
            ? $"Probe exited with code {exitCode}."
            : "Probe failed.";
    }

    private static string BuildCommandLine(string executablePath, IReadOnlyList<string> arguments)
    {
        var parts = new List<string>(arguments.Count + 1)
        {
            QuoteArgument(executablePath)
        };

        parts.AddRange(arguments.Select(QuoteArgument));
        return string.Join(' ', parts);
    }

    private static string QuoteArgument(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "\"\"";
        }

        return value.IndexOfAny([' ', '\t', '"']) >= 0
            ? $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : value;
    }
}

public sealed record DependencyInspectionReport
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public required bool IsHealthy { get; init; }

    public int MissingRequiredCount { get; init; }

    public int MissingOptionalCount { get; init; }

    public IReadOnlyList<DependencyProbeResult> Dependencies { get; init; } = [];
}

public sealed record DependencyProbeDefinition
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public required string Id { get; init; }

    public required DependencyProbeKind Kind { get; init; }

    public bool Required { get; init; }

    public required DependencyValueSource Source { get; init; }

    public string? ResolvedValue { get; init; }

    public IReadOnlyList<string> ProbeArguments { get; init; } = [];

    public TimeSpan? Timeout { get; init; }
}

public sealed record DependencyProbeResult
{
    public int SchemaVersion { get; init; } = SchemaVersions.V1;

    public required string Id { get; init; }

    public required DependencyProbeKind Kind { get; init; }

    public bool Required { get; init; }

    public required DependencyValueSource Source { get; init; }

    public string? ResolvedValue { get; init; }

    public bool IsAvailable { get; init; }

    public bool ProbeSucceeded { get; init; }

    public int? ExitCode { get; init; }

    public string? Detail { get; init; }
}

public enum DependencyProbeKind
{
    Executable,
    File
}

public enum DependencyValueSource
{
    Option,
    Environment,
    Default,
    Unset
}
