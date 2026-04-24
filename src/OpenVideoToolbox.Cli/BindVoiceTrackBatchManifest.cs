using System.Text.Json.Serialization;
using OpenVideoToolbox.Core.Editing;

namespace OpenVideoToolbox.Cli;

internal sealed record BindVoiceTrackBatchManifest
{
    public int SchemaVersion { get; init; } = 1;

    public IReadOnlyList<BindVoiceTrackBatchItem> Items { get; init; } = [];
}

internal sealed record BindVoiceTrackBatchItem
{
    public required string Plan { get; init; }

    public required string Path { get; init; }

    public string? TrackId { get; init; }

    public AudioTrackRole? Role { get; init; }

    public string? WriteTo { get; init; }

    public EditPlanPathWriteStyle? PathStyle { get; init; }

    public bool? CheckFiles { get; init; }

    public bool? RequireValid { get; init; }
}
