using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Media;

namespace OpenVideoToolbox.Cli;

internal sealed record TemplateExampleWriteResult
{
    public required string OutputDirectory { get; init; }

    public required IReadOnlyList<string> WrittenFiles { get; init; }
}

internal sealed record TemplatePlanBuildResult
{
    public required EditPlanTemplateDefinition Template { get; init; }

    public required object TemplateSource { get; init; }

    public required EditPlan Plan { get; init; }

    public MediaProbeResult? Probe { get; init; }

    public IReadOnlyDictionary<string, string> ArtifactBindings { get; init; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, string> ParameterOverrides { get; init; } = new Dictionary<string, string>();
}
