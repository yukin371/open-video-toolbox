using OpenVideoToolbox.Core.Editing;

namespace OpenVideoToolbox.Core.Execution;

public enum ProjectExportFormat
{
    Edl = 0
}

public sealed record ProjectExportRequest
{
    public required EditPlan Plan { get; init; }

    public required ProjectExportFormat Format { get; init; }

    public required string OutputPath { get; init; }

    public int? FrameRate { get; init; }

    public string? Title { get; init; }

    public bool Overwrite { get; init; }
}

public sealed record ProjectExportResult
{
    public required string Format { get; init; }

    public required string FidelityLevel { get; init; }

    public required string OutputPath { get; init; }

    public required string Title { get; init; }

    public required int FrameRate { get; init; }

    public required int EventCount { get; init; }

    public required IReadOnlyList<ProjectExportWarning> Warnings { get; init; }
}

public sealed record ProjectExportWarning
{
    public required string Code { get; init; }

    public string? Target { get; init; }

    public required string Message { get; init; }
}

public static class ProjectExportFormats
{
    public const string Edl = "edl";
}

public static class ProjectExportFidelityLevels
{
    public const string L1 = "L1";
}

public static class ProjectExportWarningCodes
{
    public const string V1Wrapped = "export.plan.v1Wrapped";

    public const string FrameRateDefaulted = "export.frameRate.defaulted";

    public const string AudioIgnored = "export.timeline.audioIgnored";

    public const string ExtraVideoTracksIgnored = "export.timeline.extraVideoTracksIgnored";

    public const string EffectsIgnored = "export.timeline.effectsIgnored";

    public const string TransitionsIgnored = "export.timeline.transitionsIgnored";
}
