namespace OpenVideoToolbox.Core.Editing;

internal static class EditPlanTemplateResolution
{
    public static EditPlanTemplateDefinition? ResolveTemplate(
        EditPlan plan,
        IReadOnlyList<EditPlanTemplateDefinition>? availableTemplates,
        ICollection<EditPlanValidationIssue>? issues = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.Template is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(plan.Template.Id))
        {
            AddIssue(
                issues,
                "template.id",
                "template.id.required",
                "Template id is required when template metadata is present.");
            return null;
        }

        ValidateTemplateSource(plan.Template, issues);

        var templates = SelectTemplatesForValidation(plan.Template, availableTemplates, issues);
        if (templates is null)
        {
            return null;
        }

        try
        {
            return EditPlanTemplateCatalog.GetRequired(templates, plan.Template.Id);
        }
        catch (InvalidOperationException)
        {
            AddIssue(
                issues,
                "template.id",
                "template.id.unknown",
                $"Unknown edit plan template '{plan.Template.Id}'.");
            return null;
        }
    }

    private static IReadOnlyList<EditPlanTemplateDefinition>? SelectTemplatesForValidation(
        EditTemplateReference template,
        IReadOnlyList<EditPlanTemplateDefinition>? availableTemplates,
        ICollection<EditPlanValidationIssue>? issues)
    {
        if (template.Source is null)
        {
            return availableTemplates ?? BuiltInEditPlanTemplateCatalog.GetAll();
        }

        if (string.Equals(template.Source.Kind, EditTemplateSourceKinds.Plugin, StringComparison.OrdinalIgnoreCase))
        {
            if (availableTemplates is not null)
            {
                return availableTemplates;
            }

            AddIssue(
                issues,
                "template.source",
                "template.source.catalog.required",
                $"Template '{template.Id}' is marked as a plugin template and requires plugin catalog context for validation. Re-run validate-plan with '--plugin-dir'.");
            return null;
        }

        return BuiltInEditPlanTemplateCatalog.GetAll();
    }

    private static void ValidateTemplateSource(
        EditTemplateReference template,
        ICollection<EditPlanValidationIssue>? issues)
    {
        if (template.Source is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(template.Source.Kind))
        {
            AddIssue(
                issues,
                "template.source.kind",
                "template.source.kind.required",
                "Template source kind is required when template source metadata is present.");
            return;
        }

        if (string.Equals(template.Source.Kind, EditTemplateSourceKinds.Plugin, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(template.Source.PluginId))
            {
                AddIssue(
                    issues,
                    "template.source.pluginId",
                    "template.source.pluginId.required",
                    "Plugin template source requires a plugin id.");
            }

            return;
        }

        if (string.Equals(template.Source.Kind, EditTemplateSourceKinds.BuiltIn, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(template.Source.PluginId))
            {
                AddIssue(
                    issues,
                    "template.source.pluginId",
                    "template.source.pluginId.unexpected",
                    "Built-in template source must not declare a plugin id.");
            }

            return;
        }

        AddIssue(
            issues,
            "template.source.kind",
            "template.source.kind.invalid",
            $"Unsupported template source kind '{template.Source.Kind}'.");
    }

    private static void AddIssue(
        ICollection<EditPlanValidationIssue>? issues,
        string path,
        string code,
        string message)
    {
        if (issues is null)
        {
            return;
        }

        issues.Add(new EditPlanValidationIssue
        {
            Severity = EditPlanValidationSeverity.Error,
            Path = path,
            Code = code,
            Message = message
        });
    }
}
