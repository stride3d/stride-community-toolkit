using Stride.CommunityToolkit.Audio;
using System.Buffers.Binary;
using System.Text;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Audio;

/// <summary>
/// Pins the <c>.wav</c> decoder behind <see cref="WavSound.Read(Stream)"/>: every supported sample
/// format lands as 16-bit PCM with the right values, chunk quirks are tolerated, and unsupported
/// content is rejected with a message that names it. Pure data, no audio engine.
/// </summary>
public class WavSoundTests
{
    [Fact]
    public void Read_Pcm16Mono_KeepsSamplesAndDescribesTheSound()
    {
        var samples = new short[] { 0, 1000, -1000, short.MaxValue, short.MinValue, 7 };
        var bytes = Wav(channels: 1, sampleRate: 22050, bits: 16, Pcm16(samples));

        var sound = WavSound.Read(new MemoryStream(bytes));

        Assert.Equal(22050, sound.SampleRate);
        Assert.Equal(1, sound.Channels);
        Assert.Equal(samples, sound.Samples.ToArray());
        Assert.Equal(6, sound.FrameCount);
        Assert.Equal(TimeSpan.FromSeconds(6.0 / 22050), sound.Duration);
    }

    [Fact]
    public void Read_Stereo_CountsFramesPerChannel()
    {
        var bytes = Wav(channels: 2, sampleRate: 44100, bits: 16, Pcm16([1, 2, 3, 4, 5, 6, 7, 8]));

        var sound = WavSound.Read(new MemoryStream(bytes));

        Assert.Equal(2, sound.Channels);
        Assert.Equal(4, sound.FrameCount);
        Assert.Equal(8, sound.Samples.Length);
    }

    [Fact]
    public void Read_Pcm8_IsUnsignedAndScaledTo16Bit()
    {
        // 8-bit WAVE is unsigned with silence at 128; 0 is full negative, 255 nearly full positive.
        var bytes = Wav(channels: 1, sampleRate: 8000, bits: 8, [128, 0, 255, 192]);

        var sound = WavSound.Read(new MemoryStream(bytes));

        Assert.Equal(new short[] { 0, -32768, 32512, 16384 }, sound.Samples.ToArray());
    }

    [Fact]
    public void Read_Pcm24_TakesTheTopTwoBytes()
    {
        // 0x123456 little-endian is 56 34 12; the 16-bit result is 0x1234.
        var bytes = Wav(channels: 1, sampleRate: 48000, bits: 24, [0x56, 0x34, 0x12, 0x00, 0x00, 0x80]);

        var sound = WavSound.Read(new MemoryStream(bytes));

        Assert.Equal(new short[] { 0x1234, unchecked((short)0x8000) }, sound.Samples.ToArray());
    }

    [Fact]
    public void Read_Pcm32_TakesTheTopTwoBytes()
    {
        var data = new byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(data, 0x12345678);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(4), int.MinValue);

        var sound = WavSound.Read(new MemoryStream(Wav(channels: 1, sampleRate: 48000, bits: 32, data)));

        Assert.Equal(new short[] { 0x1234, short.MinValue }, sound.Samples.ToArray());
    }

    [Fact]
    public void Read_Float32_ScalesAndClamps()
    {
        var data = new byte[16];
        BinaryPrimitives.WriteSingleLittleEndian(data, 0f);
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(4), 0.5f);
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(8), -1f);
        BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(12), 2f);           // over full scale: clamped, not wrapped

        var sound = WavSound.Read(new MemoryStream(Wav(channels: 1, sampleRate: 44100, bits: 32, data, formatTag: 3)));

        Assert.Equal(new short[] { 0, 16383, -32767, short.MaxValue }, sound.Samples.ToArray());
    }

    [Fact]
    public void Read_ExtensibleFormat_ReadsTheSubFormatTag()
    {
        var bytes = Wav(channels: 2, sampleRate: 44100, bits: 16, Pcm16([1, 2]), formatTag: 0xFFFE);

        var sound = WavSound.Read(new MemoryStream(bytes));

        Assert.Equal(new short[] { 1, 2 }, sound.Samples.ToArray());
    }

    [Fact]
    public void Read_SkipsUnknownChunksAndOddSizePadding()
    {
        // A LIST chunk of odd length before the data chunk, as many editors write; the pad byte must be skipped.
        var bytes = Wav(channels: 1, sampleRate: 44100, bits: 16, Pcm16([9, 8]), extraChunk: ("LIST", [1, 2, 3]));

        var sound = WavSound.Read(new MemoryStream(bytes));

        Assert.Equal(new short[] { 9, 8 }, sound.Samples.ToArray());
    }

    [Fact]
    public void Read_NotRiff_Throws()
    {
        var ex = Assert.Throws<InvalidDataException>(() => WavSound.Read(new MemoryStream(Encoding.ASCII.GetBytes("OggS....not a wav at all"))));

        Assert.Contains("Not a WAVE file", ex.Message);
    }

    [Fact]
    public void Read_CompressedFormat_NamesIt()
    {
        var bytes = Wav(channels: 1, sampleRate: 44100, bits: 16, Pcm16([1]), formatTag: 0x0055);

        var ex = Assert.Throws<InvalidDataException>(() => WavSound.Read(new MemoryStream(bytes)));

        Assert.Contains("MP3", ex.Message);
    }

    [Fact]
    public void Read_ThreeChannels_Throws()
    {
        var bytes = Wav(channels: 3, sampleRate: 44100, bits: 16, Pcm16([1, 2, 3]));

        var ex = Assert.Throws<InvalidDataException>(() => WavSound.Read(new MemoryStream(bytes)));

        Assert.Contains("3 channels", ex.Message);
    }

    [Fact]
    public void Read_EmptyData_Throws()
    {
        var bytes = Wav(channels: 1, sampleRate: 44100, bits: 16, []);

        Assert.Throws<InvalidDataException>(() => WavSound.Read(new MemoryStream(bytes)));
    }

    [Fact]
    public void CreateInstance_OnDataOnlySound_ExplainsHowToLoadProperly()
    {
        var sound = WavSound.Read(new MemoryStream(Wav(channels: 1, sampleRate: 44100, bits: 16, Pcm16([1]))));

        var ex = Assert.Throws<InvalidOperationException>(() => sound.CreateInstance());

        Assert.Contains("LoadWav", ex.Message);
    }

    private static byte[] Pcm16(short[] samples)
    {
        var bytes = new byte[samples.Length * 2];

        for (var i = 0; i < samples.Length; i++)
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i * 2), samples[i]);

        return bytes;
    }

    /// <summary>
    /// Builds a WAVE file: RIFF header, fmt chunk (16 bytes, or the 40-byte extensible form when
    /// <paramref name="formatTag"/> is 0xFFFE with PCM as the sub-format), an optional extra chunk,
    /// then the data chunk.
    /// </summary>
    internal static byte[] Wav(int channels, int sampleRate, int bits, byte[] data, ushort formatTag = 1, (string Tag, byte[] Body)? extraChunk = null)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII);

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(0);                                       // size, patched below
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));

        var extensible = formatTag == 0xFFFE;
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(extensible ? 40 : 16);
        writer.Write(formatTag);
        writer.Write((ushort)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bits / 8);
        writer.Write((ushort)(channels * bits / 8));
        writer.Write((ushort)bits);

        if (extensible)
        {
            writer.Write((ushort)22);                          // cbSize
            writer.Write((ushort)bits);                        // valid bits
            writer.Write(3u);                                  // channel mask
            writer.Write((ushort)1);                           // sub-format: PCM
            writer.Write(new byte[14]);                        // rest of the GUID
        }

        if (extraChunk is { } extra)
        {
            writer.Write(Encoding.ASCII.GetBytes(extra.Tag));
            writer.Write(extra.Body.Length);
            writer.Write(extra.Body);

            if ((extra.Body.Length & 1) == 1) writer.Write((byte)0);
        }

        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(data.Length);
        writer.Write(data);

        writer.Flush();
        stream.Position = 4;
        writer.Write((int)stream.Length - 8);

        return stream.ToArray();
    }
}