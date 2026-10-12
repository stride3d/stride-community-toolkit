using Stride.Audio;

namespace Stride.CommunityToolkit.Audio;

/// <summary>
/// A <c>.wav</c> file decoded to 16-bit PCM and held in memory, from which any number of
/// overlapping <see cref="SoundInstance"/>s can be created.
/// </summary>
/// <remarks>
/// <para>
/// The counterpart of the asset pipeline's <c>Sound</c> for a code-only game: read the file at
/// runtime, no compilation step. Supported: RIFF/WAVE with PCM 8, 16, 24 or 32-bit integer samples
/// or 32-bit IEEE float, mono or stereo, any sample rate - the audio engine creates each source
/// at the file's own rate, so nothing is resampled. Anything else in the file (ADPCM, MP3 in a WAV
/// container, more than two channels) is rejected with a message that says what it found.
/// </para>
/// <para>
/// Everything is held in memory: three minutes of 44.1 kHz stereo is about 32 MB. That is the right
/// trade for effects and short loops; long music would want a disk-streaming source, which this
/// class does not provide.
/// </para>
/// <para>
/// Load through <c>game.GetAudioSystem().LoadWav(...)</c> to get a sound that can create instances; the static
/// <see cref="Read(Stream)"/> and <see cref="Read(string)"/> decode the data only, for inspection
/// or for tests, and a sound obtained that way cannot create instances.
/// </para>
/// </remarks>
public sealed class WavSound
{
    private readonly AudioEngine? _engine;

    internal WavSound(int sampleRate, int channels, short[] samples, AudioEngine? engine)
    {
        SampleRate = sampleRate;
        Channels = channels;
        Samples = samples;
        _engine = engine;
    }

    /// <summary>Samples per second per channel.</summary>
    public int SampleRate { get; }

    /// <summary>1 for mono, 2 for stereo.</summary>
    public int Channels { get; }

    /// <summary>The decoded samples, interleaved by channel.</summary>
    public ReadOnlyMemory<short> Samples { get; }

    /// <summary>Frames in the sound: samples per channel.</summary>
    public int FrameCount => Samples.Length / Channels;

    /// <summary>Playing time.</summary>
    public TimeSpan Duration => TimeSpan.FromSeconds((double)FrameCount / SampleRate);

    /// <summary>
    /// Decodes a <c>.wav</c> from a stream. Data only: the result cannot create instances - use
    /// <c>game.GetAudioSystem().LoadWav</c> for that.
    /// </summary>
    /// <exception cref="InvalidDataException">The stream is not a WAVE file, or holds a format this class does not decode.</exception>
    public static WavSound Read(Stream stream) => WavReader.Read(stream, engine: null);

    /// <summary>
    /// Decodes a <c>.wav</c> file. Data only: the result cannot create instances - use
    /// <c>game.GetAudioSystem().LoadWav</c> for that.
    /// </summary>
    /// <exception cref="InvalidDataException">The file is not a WAVE file, or holds a format this class does not decode.</exception>
    public static WavSound Read(string path)
    {
        using var stream = File.OpenRead(path);

        return Read(stream);
    }

    /// <summary>
    /// Creates a new instance of this sound. Each instance plays independently, so several can
    /// overlap - the usual case for a sound effect.
    /// </summary>
    /// <param name="spatialized">Whether the instance can be positioned with <see cref="SoundInstance.Apply3D"/>. Mono sounds only.</param>
    /// <param name="useHrtf">Head-related transfer function spatialisation; needs <c>AudioEngineSettings.HrtfSupport</c> and works on Windows only.</param>
    /// <param name="listener">The listener the instance is heard by; the engine's default listener when omitted.</param>
    /// <returns>A stopped instance. Set <see cref="SoundInstance.IsLooping"/> before <see cref="SoundInstance.Play()"/> for a loop.</returns>
    /// <exception cref="InvalidOperationException">The sound was decoded with <see cref="Read(Stream)"/> rather than loaded through the audio system.</exception>
    /// <exception cref="ArgumentException"><paramref name="spatialized"/> on a stereo sound.</exception>
    public SoundInstance CreateInstance(bool spatialized = false, bool useHrtf = false, AudioListener? listener = null)
    {
        if (_engine is null)
            throw new InvalidOperationException($"This {nameof(WavSound)} holds data only. Load it through game.GetAudioSystem().LoadWav(...) to create instances.");

        if (spatialized && Channels != 1)
            throw new ArgumentException("A spatialised sound must be mono: the engine positions a single channel in space. Export the file as mono.", nameof(spatialized));

        return PcmSoundSource.Create(_engine, listener ?? _engine.DefaultListener, Samples, SampleRate, Channels, spatialized, useHrtf);
    }
}