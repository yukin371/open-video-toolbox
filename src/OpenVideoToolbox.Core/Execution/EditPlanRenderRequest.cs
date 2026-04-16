using OpenVideoToolbox.Core.Editing;

namespace OpenVideoToolbox.Core.Execution;

public sealed record EditPlanRenderRequest
{
    public required EditPlan Plan { get; init; }

    public bool OverwriteExisting { get; init; }
}
