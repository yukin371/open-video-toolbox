using System.Text.Json;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Serialization;

namespace OpenVideoToolbox.Cli;

internal static class TemplatePlanValidationSupport
{
    public static async Task<LoadedEditPlanContext> LoadPlanContextAsync(
        string fullPlanPath,
        TemplatePluginCatalog? pluginCatalog = null)
    {
        var content = await File.ReadAllTextAsync(fullPlanPath);
        var plan = JsonSerializer.Deserialize<EditPlan>(content, OpenVideoToolboxJson.Default)
            ?? throw new InvalidOperationException($"Failed to parse edit plan '{fullPlanPath}'.");

        if (plan.SchemaVersion != 1)
        {
            throw new InvalidOperationException($"Unsupported edit plan schema version '{plan.SchemaVersion}'.");
        }

        var baseDirectory = Path.GetDirectoryName(fullPlanPath)!;
        var validationTemplates = SelectValidationTemplates(plan, pluginCatalog);

        return new LoadedEditPlanContext
        {
            Plan = plan,
            ResolvedPlan = EditPlanPathResolver.ResolvePaths(plan, baseDirectory),
            BaseDirectory = baseDirectory,
            ValidationTemplates = validationTemplates
        };
    }

    public static async Task<EditPlanValidationResult> ValidatePlanFileAsync(
        string fullPlanPath,
        bool checkFiles,
        TemplatePluginCatalog? pluginCatalog = null)
    {
        var context = await LoadPlanContextAsync(fullPlanPath, pluginCatalog);
        return new EditPlanValidator().Validate(context.ResolvedPlan, checkFiles, context.ValidationTemplates);
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

internal sealed record LoadedEditPlanContext
{
    public required EditPlan Plan { get; init; }

    public required EditPlan ResolvedPlan { get; init; }

    public required string BaseDirectory { get; init; }

    public IReadOnlyList<EditPlanTemplateDefinition>? ValidationTemplates { get; init; }
}
