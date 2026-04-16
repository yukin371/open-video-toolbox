using System.Text.Json;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Serialization;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class EditPlanSerializationTests
{
    [Fact]
    public void EditPlan_RoundTrips_WithSchemaV1Shape()
    {
        var plan = new EditPlan
        {
            Source = new EditPlanSource
            {
                InputPath = "input.mp4"
            },
            Template = new EditTemplateReference
            {
                Id = "shorts-captioned",
                Version = "1.0.0",
                Parameters = new Dictionary<string, string>
                {
                    ["hookStyle"] = "hard-cut"
                }
            },
            Clips =
            [
                new EditClip
                {
                    Id = "clip-001",
                    InPoint = TimeSpan.FromSeconds(12.5),
                    OutPoint = TimeSpan.FromSeconds(27.5),
                    Label = "intro-hook"
                }
            ],
            AudioTracks =
            [
                new AudioTrackMix
                {
                    Id = "bgm-01",
                    Role = AudioTrackRole.Bgm,
                    Path = "bgm.wav",
                    Start = TimeSpan.Zero,
                    GainDb = -8
                }
            ],
            Artifacts =
            [
                new EditArtifactReference
                {
                    SlotId = "subtitles",
                    Kind = "subtitle",
                    Path = "subtitles.srt"
                }
            ],
            Transcript = new EditTranscriptPlan
            {
                Path = "transcript.json",
                Language = "en",
                SegmentCount = 2
            },
            Beats = new EditBeatTrackPlan
            {
                Path = "beats.json",
                EstimatedBpm = 120
            },
            Subtitles = new EditSubtitlePlan
            {
                Path = "subtitles.srt",
                Mode = SubtitleMode.Sidecar
            },
            Output = new EditOutputPlan
            {
                Path = "final.mp4",
                Container = "mp4"
            },
            Extensions = new Dictionary<string, JsonElement>
            {
                ["x-toolbox"] = JsonDocument.Parse("{\"beats\":\"beats.json\"}").RootElement.Clone()
            }
        };

        var json = JsonSerializer.Serialize(plan, OpenVideoToolboxJson.Shared);
        var restored = JsonSerializer.Deserialize<EditPlan>(json, OpenVideoToolboxJson.Shared);

        Assert.Contains("\"schemaVersion\": 1", json);
        Assert.Contains("\"template\":", json);
        Assert.Contains("\"in\":", json);
        Assert.Contains("\"out\":", json);
        Assert.NotNull(restored);
        Assert.Equal("input.mp4", restored!.Source.InputPath);
        Assert.Equal("shorts-captioned", restored.Template!.Id);
        Assert.Equal("hard-cut", restored.Template.Parameters["hookStyle"]);
        Assert.Equal(TimeSpan.FromSeconds(12.5), restored.Clips[0].InPoint);
        Assert.Equal(AudioTrackRole.Bgm, restored.AudioTracks[0].Role);
        Assert.Equal("subtitles", restored.Artifacts[0].SlotId);
        Assert.Equal("transcript.json", restored.Transcript!.Path);
        Assert.Equal("beats.json", restored.Beats!.Path);
        Assert.Equal(SubtitleMode.Sidecar, restored.Subtitles!.Mode);
        Assert.True(restored.Extensions?.ContainsKey("x-toolbox"));
    }
}
