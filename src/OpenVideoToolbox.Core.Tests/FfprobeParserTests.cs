using OpenVideoToolbox.Core.Media;
using Xunit;

namespace OpenVideoToolbox.Core.Tests;

public sealed class FfprobeParserTests
{
    [Fact]
    public void Parse_MapsFormatAndStreamMetadata()
    {
        var parser = new FfprobeJsonParser();

        var result = parser.Parse(
            """
            {
              "streams": [
                {
                  "index": 0,
                  "codec_type": "video",
                  "codec_name": "h264",
                  "codec_long_name": "H.264 / AVC / MPEG-4 AVC / MPEG-4 part 10",
                  "width": 1920,
                  "height": 1080,
                  "avg_frame_rate": "24000/1001",
                  "bit_rate": "7500000",
                  "duration": "120.500"
                },
                {
                  "index": 1,
                  "codec_type": "audio",
                  "codec_name": "aac",
                  "codec_long_name": "AAC (Advanced Audio Coding)",
                  "channels": 2,
                  "sample_rate": "48000",
                  "channel_layout": "stereo",
                  "bit_rate": "192000",
                  "duration": "120.500",
                  "tags": {
                    "language": "jpn"
                  }
                }
              ],
              "format": {
                "format_name": "matroska,webm",
                "format_long_name": "Matroska / WebM",
                "duration": "120.500",
                "size": "734003200",
                "bit_rate": "8000000"
              }
            }
            """,
            @"D:\Media\episode01.mkv");

        Assert.Equal("episode01.mkv", result.FileName);
        Assert.Equal("matroska,webm", result.Format.ContainerName);
        Assert.Equal(TimeSpan.FromSeconds(120.5), result.Format.Duration);
        Assert.Equal(734003200L, result.Format.SizeBytes);
        Assert.Equal(2, result.Streams.Count);

        var video = result.Streams[0];
        Assert.Equal(MediaStreamKind.Video, video.Kind);
        Assert.Equal(1920, video.Width);
        Assert.Equal(1080, video.Height);
        Assert.Equal(23.976023976023978d, video.FrameRate!.Value, 12);

        var audio = result.Streams[1];
        Assert.Equal(MediaStreamKind.Audio, audio.Kind);
        Assert.Equal("jpn", audio.Language);
        Assert.Equal(48000, audio.SampleRate);
        Assert.Equal("stereo", audio.ChannelLayout);
    }
}

