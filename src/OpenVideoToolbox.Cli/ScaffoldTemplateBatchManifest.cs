namespace OpenVideoToolbox.Cli;

internal sealed record ScaffoldTemplateBatchManifest
{
    public int SchemaVersion { get; init; } = 1;

    public IReadOnlyList<ScaffoldTemplateBatchItem> Items { get; init; } = [];
}

internal sealed record ScaffoldTemplateBatchItem
{
    public string Id { get; init; } = string.Empty;

    public string Input { get; init; } = string.Empty;

    public string Template { get; init; } = string.Empty;

    public string? Workdir { get; init; }

    public bool? Validate { get; init; }

    public bool? CheckFiles { get; init; }
}
