namespace OpenVideoToolbox.Core.Beats;

public sealed record WavePcmData
{
    public required int SampleRateHz { get; init; }

    public required short[] Samples { get; init; }
}
