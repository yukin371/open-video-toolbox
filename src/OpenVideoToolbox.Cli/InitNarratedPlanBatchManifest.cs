namespace OpenVideoToolbox.Cli;

internal sealed record InitNarratedPlanBatchManifest
{
    public int SchemaVersion { get; init; } = 1;

    public IReadOnlyList<InitNarratedPlanBatchItem> Items { get; init; } = [];
}

internal sealed record InitNarratedPlanBatchItem
{
    public string Id { get; init; } = string.Empty;

    public string Manifest { get; init; } = string.Empty;

    public string? Output { get; init; }

    public string? Template { get; init; }

    public string? RenderOutput { get; init; }

    public string? Vars { get; init; }
}
