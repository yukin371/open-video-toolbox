using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenVideoToolbox.Core.Serialization;

public static class OpenVideoToolboxJson
{
    public static JsonSerializerOptions Default { get; } = CreateDefault();

    public static JsonSerializerOptions Shared => Default;

    private static JsonSerializerOptions CreateDefault()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }
}
