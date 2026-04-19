using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Serialization;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    private static async Task<CliRunResult> RunCliAsync(params string[] args)
    {
        return await CliTestProcessHelper.RunCliAsync(args);
    }

    private static async Task<string> CreateTemplatePluginAsync(
        string workingDirectory,
        string pluginId,
        string pluginDisplayName,
        EditPlanTemplateDefinition template)
    {
        var pluginDirectory = Path.Combine(workingDirectory, pluginId);
        var templateDirectory = Path.Combine(pluginDirectory, "templates", template.Id);
        Directory.CreateDirectory(templateDirectory);

        await File.WriteAllTextAsync(
            Path.Combine(pluginDirectory, "plugin.json"),
            JsonSerializer.Serialize(
                new
                {
                    schemaVersion = 1,
                    id = pluginId,
                    displayName = pluginDisplayName,
                    version = "1.0.0",
                    description = $"{pluginDisplayName} plugin",
                    templates = new[]
                    {
                        new
                        {
                            id = template.Id,
                            path = Path.Combine("templates", template.Id).Replace('\\', '/')
                        }
                    }
                },
                OpenVideoToolboxJson.Default));

        await File.WriteAllTextAsync(
            Path.Combine(templateDirectory, "template.json"),
            JsonSerializer.Serialize(template, OpenVideoToolboxJson.Default));

        return pluginDirectory;
    }

    private static EditPlanTemplateDefinition CreatePluginTemplateDefinition(string id, string displayName)
    {
        return new EditPlanTemplateDefinition
        {
            Id = id,
            DisplayName = displayName,
            Description = $"{displayName} description",
            Category = "plugin",
            OutputContainer = "mp4",
            DefaultSubtitleMode = SubtitleMode.Sidecar,
            RecommendedSeedModes = [EditPlanSeedMode.Manual, EditPlanSeedMode.Transcript],
            RecommendedTranscriptSeedStrategies = [TranscriptSeedStrategy.Grouped],
            ArtifactSlots =
            [
                new EditPlanArtifactSlot
                {
                    Id = "subtitles",
                    Kind = "subtitle",
                    Description = "Subtitle sidecar",
                    Required = false
                }
            ],
            SupportingSignals =
            [
                new EditPlanSupportingSignalHint
                {
                    Kind = EditPlanSupportingSignalKind.Transcript,
                    Reason = "Use transcript guidance before generating subtitles."
                }
            ]
        };
    }

    private static JsonObject GetPreviewPlan(JsonArray previews, string mode)
    {
        var preview = GetPreview(previews, mode);
        return Assert.IsType<JsonObject>(preview["editPlan"]);
    }

    private static JsonObject GetPreview(JsonArray previews, string mode)
    {
        var previewNode = previews.Single(node => node!["mode"]!.GetValue<string>() == mode);
        return Assert.IsType<JsonObject>(previewNode);
    }

    private static string WriteExecutableScript(string directory, string baseName, string windowsContent, string unixContent)
    {
        return CliTestProcessHelper.WriteExecutableScript(directory, baseName, windowsContent, unixContent);
    }

    private static void WriteMonoPcmWave(string path, int sampleRateHz, int sampleCount)
    {
        const short bitsPerSample = 16;
        const short channels = 1;
        var blockAlign = (short)(channels * bitsPerSample / 8);
        var byteRate = sampleRateHz * blockAlign;
        var dataSize = sampleCount * blockAlign;

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRateHz);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);

        for (var i = 0; i < sampleCount; i++)
        {
            var sample = (short)((i % 200) < 100 ? short.MaxValue / 8 : short.MinValue / 8);
            writer.Write(sample);
        }
    }
}
