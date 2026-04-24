using OpenVideoToolbox.Core.Editing;
using static OpenVideoToolbox.Cli.CliCommandOutput;
using static OpenVideoToolbox.Cli.CliOptionParsing;

namespace OpenVideoToolbox.Cli;

internal static class EffectsCommandHandlers
{
    public static int RunEffects(string[] args, Func<string, int> fail)
    {
        if (args.Length == 0)
        {
            return RunList(Array.Empty<string>(), fail);
        }

        var first = args[0];
        if (string.Equals(first, "list", StringComparison.OrdinalIgnoreCase))
        {
            return RunList(args.Skip(1).ToArray(), fail);
        }

        if (string.Equals(first, "describe", StringComparison.OrdinalIgnoreCase))
        {
            return RunDescribe(args.Skip(1).ToArray(), fail);
        }

        if (first.StartsWith("--", StringComparison.Ordinal))
        {
            return RunList(args, fail);
        }

        return RunDescribe(args, fail);
    }

    private static int RunList(string[] args, Func<string, int> fail)
    {
        if (!TryParseOptions(args, out var options, out var error))
        {
            return fail(error!);
        }

        var category = GetOption(options, "--category");
        var jsonOutPath = GetOption(options, "--json-out");
        var effects = BuiltInEffectCatalog.GetAll(category)
            .Select(BuildEffectSummary)
            .ToArray();

        return WriteCommandEnvelope(
            "effects",
            preview: false,
            new
            {
                mode = "list",
                filters = new
                {
                    category
                },
                count = effects.Length,
                effects
            },
            jsonOutPath);
    }

    private static int RunDescribe(string[] args, Func<string, int> fail)
    {
        if (args.Length == 0 || args[0].StartsWith("--", StringComparison.Ordinal))
        {
            return fail("effects describe requires an effect type.");
        }

        var effectType = args[0];
        if (!TryParseOptions(args.Skip(1).ToArray(), out var options, out var error))
        {
            return fail(error!);
        }

        var jsonOutPath = GetOption(options, "--json-out");
        var effect = BuiltInEffectCatalog.CreateRegistry().Get(effectType);
        if (effect is null)
        {
            return fail($"Unknown effect '{effectType}'.");
        }

        return WriteCommandEnvelope(
            "effects",
            preview: false,
            new
            {
                mode = "describe",
                effect = BuildEffectDetail(effect)
            },
            jsonOutPath);
    }

    private static object BuildEffectSummary(IEffectDefinition definition)
    {
        return new
        {
            source = "built-in",
            definition.Type,
            definition.Category,
            definition.DisplayName,
            definition.Description,
            templateMode = GetTemplateMode(definition),
            parameterCount = definition.Parameters.Items.Count,
            parameterNames = definition.Parameters.Items.Keys
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private static object BuildEffectDetail(IEffectDefinition definition)
    {
        return new
        {
            source = "built-in",
            definition.Type,
            definition.Category,
            definition.DisplayName,
            definition.Description,
            templateMode = GetTemplateMode(definition),
            parameters = definition.Parameters.Items
                .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase),
            ffmpegTemplates = definition.FfmpegTemplates
        };
    }

    private static string GetTemplateMode(IEffectDefinition definition)
    {
        if (definition.FfmpegTemplates is null)
        {
            return "executor";
        }

        if (definition.FfmpegTemplates.Transitions is not null)
        {
            return "transitionTemplate";
        }

        return "filterTemplate";
    }
}
