using System.Text;
using OpenVideoToolbox.Core.Beats;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class WavePcmReaderTests
{
    [Fact]
    public void ReadMono16Bit_ReadsExpectedSamples()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ovt-wave-{Guid.NewGuid():N}.wav");

        try
        {
            var bytes = BuildMono16BitWave(
                sampleRateHz: 8000,
                samples: [0, 1000, -1000, 2000]);
            File.WriteAllBytes(path, bytes);

            var reader = new WavePcmReader();
            var waveform = reader.ReadMono16Bit(path);

            Assert.Equal(8000, waveform.SampleRateHz);
            Assert.Equal([0, 1000, -1000, 2000], waveform.Samples);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static byte[] BuildMono16BitWave(int sampleRateHz, short[] samples)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        var dataSize = samples.Length * sizeof(short);

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRateHz);
        writer.Write(sampleRateHz * sizeof(short));
        writer.Write((short)sizeof(short));
        writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            writer.Write(sample);
        }

        writer.Flush();
        return stream.ToArray();
    }
}
