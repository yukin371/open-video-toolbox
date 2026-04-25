using System.Text.Json.Nodes;

namespace OpenVideoToolbox.Cli;

internal static class JsonInputLoadSupport
{
    public static async Task<IReadOnlyDictionary<string, string>> LoadStringMapAsync(
        string jsonPath,
        string logicalName,
        string shapeHint)
    {
        var fullPath = Path.GetFullPath(jsonPath);
        var content = await File.ReadAllTextAsync(fullPath);
        var root = JsonNode.Parse(content) as JsonObject
            ?? throw new InvalidOperationException(
                $"Failed to parse {logicalName} '{jsonPath}'. {shapeHint}");

        var bindings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in root)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new InvalidOperationException($"{logicalName} '{jsonPath}' contains an empty key.");
            }

            if (pair.Value is not JsonValue value || !value.TryGetValue<string>(out var resolvedValue) || string.IsNullOrWhiteSpace(resolvedValue))
            {
                throw new InvalidOperationException(
                    $"Key '{pair.Key}' in {logicalName} '{jsonPath}' must map to a non-empty string value.");
            }

            bindings[pair.Key] = resolvedValue;
        }

        return bindings;
    }
}
