using System.Text.Json;
using OpenVideoToolbox.Core.Serialization;

namespace OpenVideoToolbox.Cli;

internal static class BatchCommandArtifacts
{
    public static string ResolveSummaryPath(string manifestBaseDirectory)
    {
        return Path.Combine(manifestBaseDirectory, "summary.json");
    }

    public static string ResolveResultPath(string manifestBaseDirectory, string itemId)
    {
        return Path.Combine(manifestBaseDirectory, "results", $"{itemId}.json");
    }

    public static string ResolveItemId(string? itemId, int index)
    {
        return string.IsNullOrWhiteSpace(itemId)
            ? $"item-{index + 1:000}"
            : itemId;
    }

    public static async Task<string> WriteResultAsync(string manifestBaseDirectory, string itemId, object payload)
    {
        var resultPath = ResolveResultPath(manifestBaseDirectory, itemId);
        Directory.CreateDirectory(Path.GetDirectoryName(resultPath)!);
        await File.WriteAllTextAsync(resultPath, JsonSerializer.Serialize(payload, OpenVideoToolboxJson.Default));
        return resultPath;
    }

    public static async Task<string> WriteSummaryAsync(string manifestBaseDirectory, object payload)
    {
        var summaryPath = ResolveSummaryPath(manifestBaseDirectory);
        await File.WriteAllTextAsync(summaryPath, JsonSerializer.Serialize(payload, OpenVideoToolboxJson.Default));
        return summaryPath;
    }
}
