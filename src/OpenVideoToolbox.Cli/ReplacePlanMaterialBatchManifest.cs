using OpenVideoToolbox.Core.Editing;

namespace OpenVideoToolbox.Cli;

internal sealed record ReplacePlanMaterialBatchManifest
{
    public int SchemaVersion { get; init; } = 1;

    public IReadOnlyList<ReplacePlanMaterialBatchItem> Items { get; init; } = [];
}

internal sealed record ReplacePlanMaterialBatchItem
{
    public string Id { get; init; } = string.Empty;

    public string Plan { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public bool SourceInput { get; init; }

    public bool Transcript { get; init; }

    public bool Beats { get; init; }

    public bool Subtitles { get; init; }

    public string? AudioTrackId { get; init; }

    public string? ArtifactSlot { get; init; }

    public string? WriteTo { get; init; }

    public EditPlanPathWriteStyle? PathStyle { get; init; }

    public bool? CheckFiles { get; init; }

    public bool? RequireValid { get; init; }

    public SubtitleMode? SubtitleMode { get; init; }
}
