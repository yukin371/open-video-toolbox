using OpenVideoToolbox.Core.Beats;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class BeatTrackAnalyzerTests
{
    [Fact]
    public void Analyze_DetectsSyntheticPulseTrainAndEstimatesBpm()
    {
        const int sampleRate = 1000;
        var samples = new short[sampleRate * 5];

        for (var second = 0; second < 5; second++)
        {
            var pulseStart = second * sampleRate;
            for (var offset = 0; offset < 20; offset++)
            {
                samples[pulseStart + offset] = 28_000;
            }
        }

        var analyzer = new BeatTrackAnalyzer();
        var result = analyzer.Analyze(
            new WavePcmData
            {
                SampleRateHz = sampleRate,
                Samples = samples
            },
            "pulse.wav");

        Assert.True(result.Beats.Count >= 4);
        Assert.Equal(60, result.EstimatedBpm);
        Assert.All(result.Beats, beat => Assert.True(beat.Strength > 0));
    }

    [Fact]
    public void Analyze_ReturnsNoBeatsForFlatSignal()
    {
        var analyzer = new BeatTrackAnalyzer();
        var result = analyzer.Analyze(
            new WavePcmData
            {
                SampleRateHz = 16000,
                Samples = new short[16000]
            },
            "flat.wav");

        Assert.Empty(result.Beats);
        Assert.Null(result.EstimatedBpm);
    }
}
