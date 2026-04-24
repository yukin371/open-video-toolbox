using System.Text.Json;
using OpenVideoToolbox.Core.Serialization;

namespace OpenVideoToolbox.Cli;

internal static class BatchCommandArtifacts
{
    public static string ResolveResultPath(string manifestBaseDirectory, string itemId)
    {
        return Path.Combine(manifestBaseDirectory, "results", $"{itemId}.json");
    }

    public static async Task<string> WriteResultAsync(string manifestBaseDirectory, string itemId, object payload)
    {
        var resultPath = ResolveResultPath(manifestBaseDirectory, itemId);
        Directory.CreateDirectory(Path.GetDirectoryName(resultPath)!);
        await File.WriteAllTextAsync(resultPath, JsonSerializer.Serialize(payload, OpenVideoToolboxJson.Default));
        return resultPath;
    }
}
