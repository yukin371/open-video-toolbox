namespace OpenVideoToolbox.Cli;

internal sealed record RenderBatchManifest
{
    public int SchemaVersion { get; init; } = 1;

    public IReadOnlyList<RenderBatchItem> Items { get; init; } = [];
}

internal sealed record RenderBatchItem
{
    public string Id { get; init; } = string.Empty;

    public string Plan { get; init; } = string.Empty;

    public string? Output { get; init; }

    public bool? Overwrite { get; init; }
}
