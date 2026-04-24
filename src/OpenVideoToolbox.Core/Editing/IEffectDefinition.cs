// v2 Effect System — Design Draft

using System.Text.Json;
using OpenVideoToolbox.Core.Serialization;

namespace OpenVideoToolbox.Core.Editing;

/// <summary>
/// Unified effect descriptor interface. External consumers (AI, plugins, CLI)
/// only interact with this descriptor; the render engine decides internally
/// whether to use the template path or the <see cref="Execution.IEffectExecutor"/> path.
/// </summary>
public interface IEffectDefinition
{
    /// <summary>Effect type identifier, e.g. "fade", "scale", "text_overlay".</summary>
    string Type { get; }

    /// <summary>Effect category. See <see cref="EffectCategory"/> constants.</summary>
    string Category { get; }

    /// <summary>Human-readable display name.</summary>
    string DisplayName { get; }

    /// <summary>Optional longer description.</summary>
    string? Description { get; }

    /// <summary>Schema describing the parameters this effect accepts.</summary>
    EffectParameterSchema Parameters { get; }

    /// <summary>
    /// FFmpeg filter templates for simple effects.
    /// When null, the render engine falls back to <see cref="Execution.IEffectExecutor"/>.
    /// </summary>
    FfmpegFilterTemplateSet? FfmpegTemplates { get; }
}

/// <summary>
/// Describes the set of parameters an effect accepts.
/// Each key is the parameter name; the value describes its type and constraints.
/// </summary>
public sealed record EffectParameterSchema
{
    public required IReadOnlyDictionary<string, EffectParameterDescriptor> Items { get; init; }
}

/// <summary>
/// Describes a single effect parameter: its type, whether it is required,
/// default value, numeric bounds, allowed enum values, and documentation.
/// </summary>
public sealed record EffectParameterDescriptor
{
    /// <summary>Parameter type: "int", "float", "string", "bool", "array", "object".</summary>
    public required string Type { get; init; }

    /// <summary>Whether the parameter must be provided.</summary>
    public bool Required { get; init; }

    /// <summary>
    /// Default value preserved as JSON so numeric, boolean, array and object defaults
    /// do not lose type information at the descriptor layer.
    /// </summary>
    public JsonElement? DefaultValue { get; init; }

    /// <summary>Minimum numeric value (applies to int/float).</summary>
    public double? Min { get; init; }

    /// <summary>Maximum numeric value (applies to int/float).</summary>
    public double? Max { get; init; }

    /// <summary>Allowed values when the parameter is an enum-like string.</summary>
    public IReadOnlyList<string> EnumValues { get; init; } = [];

    /// <summary>Human-readable description of the parameter.</summary>
    public string? Description { get; init; }
}

/// <summary>
/// FFmpeg filter templates. For simple effects, the render engine performs
/// string substitution on the template fragments and concatenates them into
/// a filter chain. For transition effects, separate in/out templates are provided.
/// </summary>
public sealed record FfmpegFilterTemplateSet
{
    /// <summary>Ordered filter fragments for non-transition effects.</summary>
    public IReadOnlyList<string> Filters { get; init; } = [];

    /// <summary>Transition-specific templates (in/out).</summary>
    public TransitionTemplates? Transitions { get; init; }
}

/// <summary>
/// FFmpeg filter templates specific to transition effects,
/// providing separate templates for the in-phase and out-phase.
/// </summary>
public sealed record TransitionTemplates
{
    /// <summary>Template for the transition-in phase, e.g. "fade=t=in:d={duration}:alpha=1".</summary>
    public required string In { get; init; }

    /// <summary>Template for the transition-out phase, e.g. "fade=t=out:d={duration}:alpha=1".</summary>
    public required string Out { get; init; }
}

/// <summary>
/// String constants for effect categories used by <see cref="IEffectDefinition.Category"/>.
/// </summary>
public static class EffectCategory
{
    public const string Transition = "transition";
    public const string Filter = "filter";
    public const string Animation = "animation";
    public const string Text = "text";
    public const string Audio = "audio";
    public const string Layout = "layout";
}

/// <summary>
/// Global effect registry. Built-in effects are registered at startup;
/// plugin effects are loaded afterward and may override built-in entries.
/// </summary>
public sealed class EffectRegistry
{
    private readonly Dictionary<string, IEffectDefinition> _definitions =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Register or replace an effect definition by its type identifier.</summary>
    public void Register(IEffectDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definitions[definition.Type] = definition;
    }

    /// <summary>Register multiple effect definitions.</summary>
    public void RegisterRange(IEnumerable<IEffectDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        foreach (var definition in definitions)
        {
            Register(definition);
        }
    }

    /// <summary>Look up an effect definition by type. Returns null if not found.</summary>
    public IEffectDefinition? Get(string type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        return _definitions.TryGetValue(type, out var def) ? def : null;
    }

    /// <summary>Enumerate registered effects, optionally filtered by category.</summary>
    public IReadOnlyList<IEffectDefinition> GetAll(string? category = null)
    {
        IEnumerable<IEffectDefinition> values = _definitions.Values;
        if (!string.IsNullOrWhiteSpace(category))
        {
            values = values.Where(d => string.Equals(d.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        return values
            .OrderBy(d => d.Type, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

/// <summary>
/// Built-in effect catalog for schema v2 effect discovery. This catalog exposes
/// stable effect descriptors but does not own render execution semantics.
/// </summary>
public static class BuiltInEffectCatalog
{
    private static readonly IReadOnlyList<IEffectDefinition> Definitions = BuildDefinitions();

    public static IReadOnlyList<IEffectDefinition> GetAll(string? category = null)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return Definitions;
        }

        return Definitions
            .Where(definition => string.Equals(definition.Category, category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(definition => definition.Type, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static EffectRegistry CreateRegistry()
    {
        var registry = new EffectRegistry();
        registry.RegisterRange(Definitions);
        return registry;
    }

    private static IReadOnlyList<IEffectDefinition> BuildDefinitions()
    {
        return new IEffectDefinition[]
        {
            CreateTransitionEffect(
                type: "dissolve",
                displayName: "交叉溶解",
                description: "视频交叉溶解转场。",
                transitionIn: "fade=t=in:d={duration}:alpha=1",
                transitionOut: "fade=t=out:d={duration}:alpha=1"),
            CreateTransitionEffect(
                type: "fade",
                displayName: "淡入淡出",
                description: "视频淡入淡出转场。",
                transitionIn: "fade=t=in:d={duration}:alpha=1",
                transitionOut: "fade=t=out:d={duration}:alpha=1"),
            CreateDefinition(
                type: "brightness_contrast",
                category: EffectCategory.Filter,
                displayName: "亮度对比度",
                description: "亮度与对比度调节。",
                parameters: Schema(
                    ("brightness", FloatParameter(defaultValue: 0.0, min: -1.0, max: 1.0, description: "亮度偏移")),
                    ("contrast", FloatParameter(defaultValue: 1.0, min: 0.0, max: 3.0, description: "对比度倍率"))),
                templates: new FfmpegFilterTemplateSet
                {
                    Filters = ["eq=brightness={brightness}:contrast={contrast}"]
                }),
            CreateDefinition(
                type: "gaussian_blur",
                category: EffectCategory.Filter,
                displayName: "高斯模糊",
                description: "对视频应用高斯模糊。",
                parameters: Schema(
                    ("sigma", FloatParameter(defaultValue: 8.0, min: 0.0, max: 100.0, description: "模糊半径"))),
                templates: new FfmpegFilterTemplateSet
                {
                    Filters = ["gblur=sigma={sigma}"]
                }),
            CreateDefinition(
                type: "scale",
                category: EffectCategory.Animation,
                displayName: "缩放",
                description: "按目标宽高缩放画面。",
                parameters: Schema(
                    ("width", IntParameter(required: true, min: 1, description: "目标宽度")),
                    ("height", IntParameter(required: true, min: 1, description: "目标高度")),
                    ("flags", EnumStringParameter(["fast_bilinear", "bilinear", "bicubic", "lanczos"], defaultValue: "lanczos", description: "缩放算法"))),
                templates: new FfmpegFilterTemplateSet
                {
                    Filters = ["scale={width}:{height}:flags={flags}"]
                }),
            CreateDefinition(
                type: "pan",
                category: EffectCategory.Animation,
                displayName: "平移裁切",
                description: "按给定区域平移裁切画面。",
                parameters: Schema(
                    ("width", IntParameter(required: true, min: 1, description: "裁切宽度")),
                    ("height", IntParameter(required: true, min: 1, description: "裁切高度")),
                    ("x", IntParameter(defaultValue: 0, description: "横向偏移")),
                    ("y", IntParameter(defaultValue: 0, description: "纵向偏移"))),
                templates: new FfmpegFilterTemplateSet
                {
                    Filters = ["crop={width}:{height}:{x}:{y}"]
                }),
            CreateDefinition(
                type: "text_overlay",
                category: EffectCategory.Text,
                displayName: "文字叠加",
                description: "在画面上叠加文字。",
                parameters: Schema(
                    ("text", StringParameter(required: true, description: "显示文本")),
                    ("fontSize", IntParameter(defaultValue: 48, min: 1, description: "字号")),
                    ("fontColor", StringParameter(defaultValue: "white", description: "字体颜色")),
                    ("x", StringParameter(defaultValue: "(w-text_w)/2", description: "横向位置表达式")),
                    ("y", StringParameter(defaultValue: "(h-text_h)/2", description: "纵向位置表达式"))),
                templates: null),
            CreateDefinition(
                type: "volume",
                category: EffectCategory.Audio,
                displayName: "音量调节",
                description: "对音频做静态增益调整。",
                parameters: Schema(
                    ("gainDb", FloatParameter(defaultValue: 0.0, min: -60.0, max: 24.0, description: "增益分贝"))),
                templates: new FfmpegFilterTemplateSet
                {
                    Filters = ["volume={gainDb}dB"]
                }),
            CreateDefinition(
                type: "fade_audio",
                category: EffectCategory.Audio,
                displayName: "音频淡入淡出",
                description: "对音频应用 afade。",
                parameters: Schema(
                    ("type", EnumStringParameter(["in", "out"], defaultValue: "in", description: "淡入或淡出")),
                    ("duration", FloatParameter(required: true, defaultValue: 0.5, min: 0.1, max: 10.0, description: "持续时间（秒）")),
                    ("curve", EnumStringParameter(["tri", "qsin", "hsin", "esin", "log"], defaultValue: "tri", description: "曲线类型"))),
                templates: new FfmpegFilterTemplateSet
                {
                    Filters = ["afade=t={type}:d={duration}:curve={curve}"]
                }),
            CreateDefinition(
                type: "auto_ducking",
                category: EffectCategory.Audio,
                displayName: "自动闪避",
                description: "根据参考轨道动态压低当前轨道音量。",
                parameters: Schema(
                    ("reference", StringParameter(required: true, description: "参考轨道 id")),
                    ("duckDb", FloatParameter(defaultValue: -10.0, min: -60.0, max: 0.0, description: "压低分贝")),
                    ("attackMs", IntParameter(defaultValue: 120, min: 1, description: "起音时间（毫秒）")),
                    ("releaseMs", IntParameter(defaultValue: 240, min: 1, description: "恢复时间（毫秒）"))),
                templates: null)
        }
            .OrderBy(definition => definition.Type, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEffectDefinition CreateTransitionEffect(
        string type,
        string displayName,
        string description,
        string transitionIn,
        string transitionOut)
    {
        return CreateDefinition(
            type,
            EffectCategory.Transition,
            displayName,
            description,
            Schema(
                ("duration", FloatParameter(required: true, defaultValue: 0.5, min: 0.1, max: 5.0, description: "转场时长（秒）"))),
            new FfmpegFilterTemplateSet
            {
                Transitions = new TransitionTemplates
                {
                    In = transitionIn,
                    Out = transitionOut
                }
            });
    }

    private static IEffectDefinition CreateDefinition(
        string type,
        string category,
        string displayName,
        string description,
        EffectParameterSchema parameters,
        FfmpegFilterTemplateSet? templates)
    {
        return new BuiltInEffectDefinition
        {
            Type = type,
            Category = category,
            DisplayName = displayName,
            Description = description,
            Parameters = parameters,
            FfmpegTemplates = templates
        };
    }

    private static EffectParameterSchema Schema(params (string Name, EffectParameterDescriptor Descriptor)[] items)
    {
        return new EffectParameterSchema
        {
            Items = items.ToDictionary(item => item.Name, item => item.Descriptor, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static EffectParameterDescriptor IntParameter(
        bool required = false,
        int? defaultValue = null,
        double? min = null,
        double? max = null,
        string? description = null)
    {
        return new EffectParameterDescriptor
        {
            Type = "int",
            Required = required,
            DefaultValue = defaultValue is null ? null : ToJsonElement(defaultValue.Value),
            Min = min,
            Max = max,
            Description = description
        };
    }

    private static EffectParameterDescriptor FloatParameter(
        bool required = false,
        double? defaultValue = null,
        double? min = null,
        double? max = null,
        string? description = null)
    {
        return new EffectParameterDescriptor
        {
            Type = "float",
            Required = required,
            DefaultValue = defaultValue is null ? null : ToJsonElement(defaultValue.Value),
            Min = min,
            Max = max,
            Description = description
        };
    }

    private static EffectParameterDescriptor StringParameter(
        bool required = false,
        string? defaultValue = null,
        string? description = null)
    {
        return new EffectParameterDescriptor
        {
            Type = "string",
            Required = required,
            DefaultValue = defaultValue is null ? null : ToJsonElement(defaultValue),
            Description = description
        };
    }

    private static EffectParameterDescriptor EnumStringParameter(
        IReadOnlyList<string> enumValues,
        string? defaultValue = null,
        bool required = false,
        string? description = null)
    {
        return new EffectParameterDescriptor
        {
            Type = "string",
            Required = required,
            DefaultValue = defaultValue is null ? null : ToJsonElement(defaultValue),
            EnumValues = enumValues,
            Description = description
        };
    }

    private static JsonElement ToJsonElement<T>(T value)
    {
        return JsonSerializer.SerializeToElement(value, OpenVideoToolboxJson.Shared);
    }

    private sealed record BuiltInEffectDefinition : IEffectDefinition
    {
        public required string Type { get; init; }

        public required string Category { get; init; }

        public required string DisplayName { get; init; }

        public string? Description { get; init; }

        public required EffectParameterSchema Parameters { get; init; }

        public FfmpegFilterTemplateSet? FfmpegTemplates { get; init; }
    }
}
