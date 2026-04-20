using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Jobs;
using OpenVideoToolbox.Core.Media;
using OpenVideoToolbox.Core.Presets;
using static OpenVideoToolbox.Cli.CliOptionParsing;

namespace OpenVideoToolbox.Cli;

internal static class FoundationCommandSupport
{
    public static JobDefinition BuildJob(string inputPath, IReadOnlyDictionary<string, string> options, MediaProbeResult? probeSnapshot)
    {
        var presetId = GetOption(options, "--preset") ?? BuiltInPresetCatalog.DefaultPresetId;
        var preset = BuiltInPresetCatalog.GetRequired(presetId);

        var outputDirectory = GetOption(options, "--output-dir")
            ?? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(inputPath)) ?? Environment.CurrentDirectory, "output");
        var outputName = GetOption(options, "--output-name") ?? Path.GetFileNameWithoutExtension(inputPath);

        return new JobDefinition
        {
            Id = $"job-{Guid.NewGuid():N}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Source = new JobSource
            {
                InputPath = inputPath
            },
            Output = new JobOutput
            {
                OutputDirectory = outputDirectory,
                FileNameStem = outputName,
                ContainerExtension = preset.Output.ContainerExtension,
                OverwriteExisting = GetOption(options, "--overwrite") == "true" || preset.Output.OverwriteExisting
            },
            Preset = preset,
            ProbeSnapshot = probeSnapshot,
            Tags = ["cli", "phase3", presetId]
        };
    }

    public static IReadOnlyList<DependencyProbeDefinition> BuildDoctorDependencyDefinitions(
        IReadOnlyDictionary<string, string> options,
        TimeSpan? timeout)
    {
        return
        [
            CreateExecutableDependency(
                id: "ffmpeg",
                required: true,
                optionValue: GetOption(options, "--ffmpeg"),
                environmentVariableName: null,
                defaultValue: "ffmpeg",
                probeArguments: ["-version"],
                timeout),
            CreateExecutableDependency(
                id: "ffprobe",
                required: true,
                optionValue: GetOption(options, "--ffprobe"),
                environmentVariableName: null,
                defaultValue: "ffprobe",
                probeArguments: ["-version"],
                timeout),
            CreateExecutableDependency(
                id: "whisper-cli",
                required: false,
                optionValue: GetOption(options, "--whisper-cli"),
                environmentVariableName: "OVT_WHISPER_CLI_PATH",
                defaultValue: "whisper-cli",
                probeArguments: ["--help"],
                timeout),
            CreateExecutableDependency(
                id: "demucs",
                required: false,
                optionValue: GetOption(options, "--demucs"),
                environmentVariableName: "OVT_DEMUCS_PATH",
                defaultValue: "demucs",
                probeArguments: ["--help"],
                timeout),
            CreateFileDependency(
                id: "whisper-model",
                required: false,
                optionValue: GetOption(options, "--whisper-model"),
                environmentVariableName: "OVT_WHISPER_MODEL_PATH")
        ];
    }

    private static DependencyProbeDefinition CreateExecutableDependency(
        string id,
        bool required,
        string? optionValue,
        string? environmentVariableName,
        string? defaultValue,
        IReadOnlyList<string> probeArguments,
        TimeSpan? timeout)
    {
        var resolution = ResolveDependencyValue(optionValue, environmentVariableName, defaultValue);
        return new DependencyProbeDefinition
        {
            Id = id,
            Kind = DependencyProbeKind.Executable,
            Required = required,
            Source = resolution.Source,
            ResolvedValue = resolution.Value,
            ProbeArguments = probeArguments,
            Timeout = timeout
        };
    }

    private static DependencyProbeDefinition CreateFileDependency(
        string id,
        bool required,
        string? optionValue,
        string? environmentVariableName)
    {
        var resolution = ResolveDependencyValue(optionValue, environmentVariableName, defaultValue: null);
        return new DependencyProbeDefinition
        {
            Id = id,
            Kind = DependencyProbeKind.File,
            Required = required,
            Source = resolution.Source,
            ResolvedValue = string.IsNullOrWhiteSpace(resolution.Value)
                ? null
                : Path.GetFullPath(resolution.Value)
        };
    }

    private static (string? Value, DependencyValueSource Source) ResolveDependencyValue(
        string? optionValue,
        string? environmentVariableName,
        string? defaultValue)
    {
        if (!string.IsNullOrWhiteSpace(optionValue))
        {
            return (optionValue, DependencyValueSource.Option);
        }

        if (!string.IsNullOrWhiteSpace(environmentVariableName))
        {
            var environmentValue = Environment.GetEnvironmentVariable(environmentVariableName);
            if (!string.IsNullOrWhiteSpace(environmentValue))
            {
                return (environmentValue.Trim(), DependencyValueSource.Environment);
            }
        }

        if (!string.IsNullOrWhiteSpace(defaultValue))
        {
            return (defaultValue, DependencyValueSource.Default);
        }

        return (null, DependencyValueSource.Unset);
    }
}
