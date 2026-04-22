using System.Text.Json;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Serialization;

namespace OpenVideoToolbox.Cli;

internal static class TemplatePluginCatalogLoader
{
    public static TemplatePluginCatalog Load(string? pluginDirectory)
    {
        if (string.IsNullOrWhiteSpace(pluginDirectory))
        {
            return TemplatePluginCatalog.Empty;
        }

        var fullPluginDirectory = Path.GetFullPath(pluginDirectory);
        if (!Directory.Exists(fullPluginDirectory))
        {
            throw new InvalidOperationException($"Template plugin directory '{fullPluginDirectory}' was not found.");
        }

        var manifestPath = Path.Combine(fullPluginDirectory, "plugin.json");
        if (!File.Exists(manifestPath))
        {
            throw new InvalidOperationException($"Template plugin manifest '{manifestPath}' was not found.");
        }

        var manifest = JsonSerializer.Deserialize<TemplatePluginManifest>(
            File.ReadAllText(manifestPath),
            OpenVideoToolboxJson.Default)
            ?? throw new InvalidOperationException($"Failed to parse template plugin manifest '{manifestPath}'.");

        ValidateManifest(manifest, manifestPath);

        var plugin = new TemplatePluginDescriptor
        {
            Id = manifest.Id!,
            DisplayName = manifest.DisplayName!,
            Version = manifest.Version!,
            Description = manifest.Description!,
            Directory = fullPluginDirectory
        };

        var loadedTemplates = manifest.Templates!
            .Select(entry => LoadTemplate(fullPluginDirectory, plugin, entry))
            .ToArray();

        return new TemplatePluginCatalog
        {
            Plugins = [plugin],
            LoadedTemplates = loadedTemplates
        };
    }

    private static TemplatePluginLoadedTemplate LoadTemplate(
        string pluginDirectory,
        TemplatePluginDescriptor plugin,
        TemplatePluginTemplateManifest entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Id))
        {
            throw new InvalidOperationException($"Template plugin '{plugin.Id}' contains a template entry without an id.");
        }

        if (string.IsNullOrWhiteSpace(entry.Path))
        {
            throw new InvalidOperationException($"Template plugin '{plugin.Id}' template '{entry.Id}' is missing a path.");
        }

        var templateDirectory = Path.GetFullPath(Path.Combine(pluginDirectory, entry.Path));
        EnsurePathIsWithinPluginRoot(pluginDirectory, templateDirectory, plugin.Id, entry.Id!);
        var templatePath = Path.Combine(templateDirectory, "template.json");
        if (!File.Exists(templatePath))
        {
            throw new InvalidOperationException($"Template definition '{templatePath}' was not found.");
        }

        var template = JsonSerializer.Deserialize<EditPlanTemplateDefinition>(
            File.ReadAllText(templatePath),
            OpenVideoToolboxJson.Default)
            ?? throw new InvalidOperationException($"Failed to parse template definition '{templatePath}'.");

        if (!string.Equals(template.Id, entry.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Template plugin '{plugin.Id}' entry '{entry.Id}' does not match template id '{template.Id}' in '{templatePath}'.");
        }

        ValidateTemplateDefinition(plugin, template, templatePath);

        return new TemplatePluginLoadedTemplate
        {
            Plugin = plugin,
            Template = template,
            TemplateDirectory = templateDirectory
        };
    }

    private static void ValidateManifest(TemplatePluginManifest manifest, string manifestPath)
    {
        if (manifest.SchemaVersion != 1)
        {
            throw new InvalidOperationException(
                $"Unsupported template plugin schema version '{manifest.SchemaVersion}' in '{manifestPath}'.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            throw new InvalidOperationException($"Template plugin manifest '{manifestPath}' is missing 'id'.");
        }

        if (string.IsNullOrWhiteSpace(manifest.DisplayName))
        {
            throw new InvalidOperationException($"Template plugin manifest '{manifestPath}' is missing 'displayName'.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            throw new InvalidOperationException($"Template plugin manifest '{manifestPath}' is missing 'version'.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Description))
        {
            throw new InvalidOperationException($"Template plugin manifest '{manifestPath}' is missing 'description'.");
        }

        if (manifest.Templates is null || manifest.Templates.Count == 0)
        {
            throw new InvalidOperationException($"Template plugin manifest '{manifestPath}' must declare at least one template.");
        }

        var duplicateTemplateId = manifest.Templates
            .Where(template => !string.IsNullOrWhiteSpace(template.Id))
            .GroupBy(template => template.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateTemplateId is not null)
        {
            throw new InvalidOperationException(
                $"Template plugin manifest '{manifestPath}' contains duplicate template id '{duplicateTemplateId.Key}'.");
        }
    }

    private static void ValidateTemplateDefinition(
        TemplatePluginDescriptor plugin,
        EditPlanTemplateDefinition template,
        string templatePath)
    {
        if (string.IsNullOrWhiteSpace(template.OutputContainer))
        {
            throw new InvalidOperationException($"Template definition '{templatePath}' is missing 'outputContainer'.");
        }

        if (template.RecommendedTranscriptSeedStrategies.Count > 0
            && !template.RecommendedSeedModes.Contains(EditPlanSeedMode.Transcript))
        {
            throw new InvalidOperationException(
                $"Template definition '{templatePath}' declares transcript seed strategies but does not include transcript in 'recommendedSeedModes'.");
        }

        var duplicateArtifactSlot = template.ArtifactSlots
            .GroupBy(slot => slot.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateArtifactSlot is not null)
        {
            throw new InvalidOperationException(
                $"Template definition '{templatePath}' contains duplicate artifact slot id '{duplicateArtifactSlot.Key}'.");
        }

        var duplicateSignal = template.SupportingSignals
            .GroupBy(signal => signal.Kind)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSignal is not null)
        {
            throw new InvalidOperationException(
                $"Template definition '{templatePath}' declares supporting signal '{duplicateSignal.Key}' more than once.");
        }
    }

    private static void EnsurePathIsWithinPluginRoot(
        string pluginDirectory,
        string candidatePath,
        string pluginId,
        string templateId)
    {
        var normalizedPluginDirectory = Path.EndsInDirectorySeparator(pluginDirectory)
            ? pluginDirectory
            : pluginDirectory + Path.DirectorySeparatorChar;

        if (!candidatePath.StartsWith(normalizedPluginDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Template plugin '{pluginId}' template '{templateId}' resolves outside plugin root.");
        }
    }
}

internal sealed record TemplatePluginCatalog
{
    public static TemplatePluginCatalog Empty { get; } = new();

    public IReadOnlyList<TemplatePluginDescriptor> Plugins { get; init; } = [];

    public IReadOnlyList<TemplatePluginLoadedTemplate> LoadedTemplates { get; init; } = [];
}

internal sealed record TemplatePluginDescriptor
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string Version { get; init; }

    public required string Description { get; init; }

    public required string Directory { get; init; }
}

internal sealed record TemplatePluginLoadedTemplate
{
    public required TemplatePluginDescriptor Plugin { get; init; }

    public required EditPlanTemplateDefinition Template { get; init; }

    public required string TemplateDirectory { get; init; }
}

internal sealed record TemplatePluginManifest
{
    public int SchemaVersion { get; init; }

    public string? Id { get; init; }

    public string? DisplayName { get; init; }

    public string? Version { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<TemplatePluginTemplateManifest>? Templates { get; init; }
}

internal sealed record TemplatePluginTemplateManifest
{
    public string? Id { get; init; }

    public string? Path { get; init; }
}
