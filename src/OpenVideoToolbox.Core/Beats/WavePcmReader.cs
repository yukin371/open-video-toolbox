using System.Text;

namespace OpenVideoToolbox.Core.Beats;

public sealed class WavePcmReader
{
    public WavePcmData ReadMono16Bit(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);

        if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF")
        {
            throw new InvalidOperationException("Expected RIFF wave file.");
        }

        reader.ReadInt32();
        if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE")
        {
            throw new InvalidOperationException("Expected WAVE header.");
        }

        int? sampleRateHz = null;
        short? bitsPerSample = null;
        short? channels = null;
        byte[]? data = null;

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var chunkIdBytes = reader.ReadBytes(4);
            if (chunkIdBytes.Length < 4)
            {
                break;
            }

            var chunkId = Encoding.ASCII.GetString(chunkIdBytes);
            var chunkSize = reader.ReadInt32();

            switch (chunkId)
            {
                case "fmt ":
                    var audioFormat = reader.ReadInt16();
                    channels = reader.ReadInt16();
                    sampleRateHz = reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadInt16();
                    bitsPerSample = reader.ReadInt16();

                    if (audioFormat != 1)
                    {
                        throw new InvalidOperationException("Only PCM wave files are supported.");
                    }

                    SkipRemainingChunkBytes(reader, chunkSize, bytesAlreadyRead: 16);
                    break;

                case "data":
                    data = reader.ReadBytes(chunkSize);
                    break;

                default:
                    reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                    break;
            }

            if ((chunkSize & 1) == 1 && reader.BaseStream.Position < reader.BaseStream.Length)
            {
                reader.BaseStream.Seek(1, SeekOrigin.Current);
            }
        }

        if (sampleRateHz is null || bitsPerSample is null || channels is null || data is null)
        {
            throw new InvalidOperationException("Wave file is missing required fmt or data chunks.");
        }

        if (channels != 1 || bitsPerSample != 16)
        {
            throw new InvalidOperationException("Expected mono 16-bit PCM wave data.");
        }

        var samples = new short[data.Length / 2];
        Buffer.BlockCopy(data, 0, samples, 0, data.Length);

        return new WavePcmData
        {
            SampleRateHz = sampleRateHz.Value,
            Samples = samples
        };
    }

    private static void SkipRemainingChunkBytes(BinaryReader reader, int chunkSize, int bytesAlreadyRead)
    {
        var remainingBytes = chunkSize - bytesAlreadyRead;
        if (remainingBytes > 0)
        {
            reader.BaseStream.Seek(remainingBytes, SeekOrigin.Current);
        }
    }
}
