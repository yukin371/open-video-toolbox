using System.Text.Json;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Serialization;

namespace OpenVideoToolbox.Cli;

internal static class TemplatePlanValidationSupport
{
    public static async Task<EditPlanValidationResult> ValidatePlanFileAsync(
        string fullPlanPath,
        bool checkFiles,
        TemplatePluginCatalog? pluginCatalog = null)
    {
        var content = await File.ReadAllTextAsync(fullPlanPath);
        var plan = JsonSerializer.Deserialize<EditPlan>(content, OpenVideoToolboxJson.Default)
            ?? throw new InvalidOperationException($"Failed to parse edit plan '{fullPlanPath}'.");

        if (plan.SchemaVersion != 1)
        {
            throw new InvalidOperationException($"Unsupported edit plan schema version '{plan.SchemaVersion}'.");
        }

        var resolvedPlan = EditPlanPathResolver.ResolvePaths(plan, Path.GetDirectoryName(fullPlanPath)!);
        var validationTemplates = SelectValidationTemplates(plan, pluginCatalog);
        return new EditPlanValidator().Validate(resolvedPlan, checkFiles, validationTemplates);
    }

    private static IReadOnlyList<EditPlanTemplateDefinition>? SelectValidationTemplates(
        EditPlan plan,
        TemplatePluginCatalog? pluginCatalog)
    {
        if (pluginCatalog is null || pluginCatalog.Plugins.Count == 0)
        {
            return null;
        }

        if (string.Equals(plan.Template?.Source?.Kind, EditTemplateSourceKinds.Plugin, StringComparison.OrdinalIgnoreCase))
        {
            return pluginCatalog.LoadedTemplates
                .Select(item => item.Template)
                .ToArray();
        }

        return TemplateCommandPresentation.BuildAvailableTemplates(pluginCatalog);
    }
}
