using OpenVideoToolbox.Core.Editing;

namespace OpenVideoToolbox.Core.Execution;

public sealed record EditPlanAudioMixRequest
{
    public required EditPlan Plan { get; init; }

    public required string OutputPath { get; init; }

    public bool OverwriteExisting { get; init; }
}
