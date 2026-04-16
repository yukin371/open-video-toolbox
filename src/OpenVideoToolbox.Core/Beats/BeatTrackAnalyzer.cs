namespace OpenVideoToolbox.Core.Beats;

public sealed class BeatTrackAnalyzer
{
    public BeatTrackDocument Analyze(WavePcmData waveform, string sourcePath)
    {
        ArgumentNullException.ThrowIfNull(waveform);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        if (waveform.SampleRateHz <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(waveform), "Sample rate must be greater than zero.");
        }

        if (waveform.Samples.Length == 0)
        {
            return new BeatTrackDocument
            {
                SourcePath = sourcePath,
                SampleRateHz = waveform.SampleRateHz,
                FrameDuration = TimeSpan.Zero,
                Beats = []
            };
        }

        var frameSize = Math.Max(1, waveform.SampleRateHz / 20);
        var frameDuration = TimeSpan.FromSeconds((double)frameSize / waveform.SampleRateHz);
        var energies = BuildFrameEnergies(waveform.Samples, frameSize);
        var smoothingWindow = Math.Max(4, (int)Math.Round(1.0 / frameDuration.TotalSeconds));
        var minimumBeatGapFrames = Math.Max(1, (int)Math.Round(0.28 / frameDuration.TotalSeconds));

        var beats = new List<BeatMarker>();
        var lastAcceptedFrame = -minimumBeatGapFrames;

        for (var frameIndex = 1; frameIndex < energies.Length - 1; frameIndex++)
        {
            var current = energies[frameIndex];
            if (current <= 0.0025)
            {
                continue;
            }

            var average = AverageWindow(energies, Math.Max(0, frameIndex - smoothingWindow), frameIndex);
            if (average <= 0)
            {
                continue;
            }

            var ratio = current / average;
            if (ratio < 1.35)
            {
                continue;
            }

            if (current < energies[frameIndex - 1] || current < energies[frameIndex + 1])
            {
                continue;
            }

            if (frameIndex - lastAcceptedFrame < minimumBeatGapFrames)
            {
                if (beats.Count > 0 && beats[^1].Strength < NormalizeStrength(ratio))
                {
                    beats[^1] = beats[^1] with
                    {
                        Time = TimeSpan.FromSeconds((double)(frameIndex * frameSize) / waveform.SampleRateHz),
                        Strength = NormalizeStrength(ratio)
                    };
                    lastAcceptedFrame = frameIndex;
                }

                continue;
            }

            beats.Add(new BeatMarker
            {
                Index = beats.Count,
                Time = TimeSpan.FromSeconds((double)(frameIndex * frameSize) / waveform.SampleRateHz),
                Strength = NormalizeStrength(ratio)
            });
            lastAcceptedFrame = frameIndex;
        }

        return new BeatTrackDocument
        {
            SourcePath = sourcePath,
            SampleRateHz = waveform.SampleRateHz,
            FrameDuration = frameDuration,
            EstimatedBpm = EstimateBpm(beats),
            Beats = beats
        };
    }

    private static double[] BuildFrameEnergies(short[] samples, int frameSize)
    {
        var frameCount = (int)Math.Ceiling((double)samples.Length / frameSize);
        var energies = new double[frameCount];

        for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            var start = frameIndex * frameSize;
            var end = Math.Min(samples.Length, start + frameSize);
            double sumSquares = 0;
            for (var sampleIndex = start; sampleIndex < end; sampleIndex++)
            {
                var normalized = samples[sampleIndex] / 32768.0;
                sumSquares += normalized * normalized;
            }

            var sampleCount = Math.Max(1, end - start);
            energies[frameIndex] = Math.Sqrt(sumSquares / sampleCount);
        }

        return energies;
    }

    private static double AverageWindow(double[] values, int startInclusive, int endExclusive)
    {
        if (endExclusive <= startInclusive)
        {
            return 0;
        }

        double sum = 0;
        for (var index = startInclusive; index < endExclusive; index++)
        {
            sum += values[index];
        }

        return sum / (endExclusive - startInclusive);
    }

    private static double NormalizeStrength(double ratio)
    {
        return Math.Clamp((ratio - 1.0) / 1.5, 0.0, 1.0);
    }

    private static double? EstimateBpm(IReadOnlyList<BeatMarker> beats)
    {
        if (beats.Count < 2)
        {
            return null;
        }

        var intervals = new List<double>();
        for (var index = 1; index < beats.Count; index++)
        {
            var intervalSeconds = (beats[index].Time - beats[index - 1].Time).TotalSeconds;
            if (intervalSeconds <= 0)
            {
                continue;
            }

            var bpm = 60.0 / intervalSeconds;
            if (bpm is >= 40 and <= 240)
            {
                intervals.Add(bpm);
            }
        }

        if (intervals.Count == 0)
        {
            return null;
        }

        intervals.Sort();
        var middle = intervals.Count / 2;
        return intervals.Count % 2 == 0
            ? Math.Round((intervals[middle - 1] + intervals[middle]) / 2, 2)
            : Math.Round(intervals[middle], 2);
    }
}
