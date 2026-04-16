using System.Globalization;
using System.Text;
using OpenVideoToolbox.Core.Editing;

namespace OpenVideoToolbox.Core.Execution;

public sealed class EditPlanAudioFilterGraphBuilder
{
    public EditPlanAudioFilterGraph Build(EditPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        Validate(plan);

        var filters = new List<string>();

        for (var index = 0; index < plan.Clips.Count; index++)
        {
            var clip = plan.Clips[index];
            filters.Add(
                $"[0:a]atrim=start={FormatFilterSeconds(clip.InPoint)}:end={FormatFilterSeconds(clip.OutPoint)},asetpts=PTS-STARTPTS[a{index}]");
        }

        var concatInputs = new StringBuilder();
        for (var index = 0; index < plan.Clips.Count; index++)
        {
            concatInputs.Append($"[a{index}]");
        }

        const string audioBaseLabel = "[a_base]";
        filters.Add($"{concatInputs}concat=n={plan.Clips.Count}:v=0:a=1{audioBaseLabel}");

        var audioLabel = audioBaseLabel;
        if (plan.AudioTracks.Count > 0)
        {
            var mixInputs = new StringBuilder(audioBaseLabel);
            for (var index = 0; index < plan.AudioTracks.Count; index++)
            {
                var track = plan.AudioTracks[index];
                var chain = new List<string> { "asetpts=PTS-STARTPTS" };
                if (track.Start > TimeSpan.Zero)
                {
                    var delayMs = (int)Math.Round(track.Start.TotalMilliseconds, MidpointRounding.AwayFromZero);
                    chain.Add($"adelay={delayMs}:all=1");
                }

                if (track.GainDb is { } gainDb)
                {
                    chain.Add($"volume={gainDb.ToString("0.###", CultureInfo.InvariantCulture)}dB");
                }

                var trackLabel = $"[a_mix_{index}]";
                filters.Add($"[{index + 1}:a]{string.Join(",", chain)}{trackLabel}");
                mixInputs.Append(trackLabel);
            }

            const string mixedAudioLabel = "[a_mix]";
            filters.Add($"{mixInputs}amix=inputs={plan.AudioTracks.Count + 1}:normalize=0:duration=longest{mixedAudioLabel}");
            audioLabel = mixedAudioLabel;
        }

        return new EditPlanAudioFilterGraph(filters, audioLabel);
    }

    public static void Validate(EditPlan plan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.Source.InputPath);

        if (plan.Clips.Count == 0)
        {
            throw new ArgumentException("Edit plan must contain at least one clip.", nameof(plan));
        }

        foreach (var clip in plan.Clips)
        {
            if (clip.OutPoint <= clip.InPoint)
            {
                throw new ArgumentException($"Clip '{clip.Id}' must have an out point greater than the in point.", nameof(plan));
            }
        }

        foreach (var track in plan.AudioTracks)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(track.Path);
            if (track.Start < TimeSpan.Zero)
            {
                throw new ArgumentException($"Audio track '{track.Id}' must not start before zero.", nameof(plan));
            }
        }
    }

    private static string FormatFilterSeconds(TimeSpan value)
    {
        return value.TotalSeconds.ToString("0.000", CultureInfo.InvariantCulture);
    }
}

public sealed record EditPlanAudioFilterGraph(IReadOnlyList<string> Filters, string OutputLabel);
