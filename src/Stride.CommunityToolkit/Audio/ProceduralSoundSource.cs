using Stride.Audio;

namespace Stride.CommunityToolkit.Audio;

/// <summary>
/// A sound generated on the fly by a callback, with no sound file and no compiled asset.
/// </summary>
/// <remarks>
/// <para>
/// <c>Sound</c> - the type an <c>AudioEmitterComponent</c> plays - can only come out of the asset
/// pipeline: its constructor is internal and its data is compressed at build time. The one public
/// door into the audio engine is the <see cref="SoundInstance"/> constructor that takes a
/// <see cref="DynamicSoundSource"/>, and this class is that source: the engine asks it for a block
/// of samples whenever a device buffer is free, and it asks the callback.
/// </para>
/// <para>
/// Buffering: four buffers of <see cref="FramesPerBuffer"/> frames each, the same shape as the
/// engine's own streaming source. At 44.1 kHz one buffer is about 93 ms, so a change the callback
/// picks up on its next call is heard once the buffers already queued have played, about 0.28 to 0.37 s.
/// </para>
/// <para>
/// The source and the instance reference each other and neither constructor can run first; the
/// engine's own <c>CompressedSoundSource</c> solves this by passing <see langword="null"/> to the
/// base constructor, assigning the protected <c>soundInstance</c> field afterwards, and adding
/// itself to the worker's queue last. <see cref="Create"/> does the same. One trap: the
/// <see cref="SoundInstance"/> constructor resets the instance's looping flag, which reaches
/// <see cref="SetLooped"/> before the field is assigned - so that override must not touch the
/// instance.
/// </para>
/// </remarks>
public sealed class ProceduralSoundSource : DynamicSoundSource
{
    /// <summary>Frames per buffer: samples per channel handed to the callback in one call.</summary>
    private const int FramesPerBuffer = 4096;

    private const int BufferCount = 4;

    private readonly SampleFiller _fill;
    private readonly int _sampleRate;
    private readonly int _channels;
    private readonly short[] _buffer;

    private ProceduralSoundSource(SampleFiller fill, int sampleRate, int channels)
        : base(null!, BufferCount, FramesPerBuffer * channels * sizeof(short))
    {
        _fill = fill;
        _sampleRate = sampleRate;
        _channels = channels;
        _buffer = new short[FramesPerBuffer * channels];
    }

    /// <summary>
    /// Creates a playable <see cref="SoundInstance"/> whose samples come from <paramref name="fill"/>.
    /// </summary>
    /// <param name="engine">The audio engine, <c>game.GetAudioSystem().AudioEngine</c>.</param>
    /// <param name="listener">The listener the instance is heard by; <see cref="AudioEngine.DefaultListener"/> when in doubt.</param>
    /// <param name="fill">Produces each block of samples. Runs on the audio worker thread.</param>
    /// <param name="sampleRate">Samples per second per channel.</param>
    /// <param name="mono">One channel when <see langword="true"/>, two when <see langword="false"/>. Spatialised sounds must be mono.</param>
    /// <param name="spatialized">Whether the instance can be positioned with <see cref="SoundInstance.Apply3D"/>.</param>
    /// <param name="useHrtf">Head-related transfer function spatialisation; needs <c>AudioEngineSettings.HrtfSupport</c> and works on Windows only.</param>
    /// <returns>The instance. Disposing it disposes the source.</returns>
    /// <exception cref="ArgumentException"><paramref name="spatialized"/> without <paramref name="mono"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sampleRate"/> is not positive.</exception>
    public static SoundInstance Create(AudioEngine engine, AudioListener listener, SampleFiller fill, int sampleRate = 44100, bool mono = true, bool spatialized = false, bool useHrtf = false)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(fill);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);

        if (spatialized && !mono)
            throw new ArgumentException("A spatialised sound must be mono: the engine positions a single channel in space.", nameof(spatialized));

        var source = new ProceduralSoundSource(fill, sampleRate, mono ? 1 : 2);
        var instance = new SoundInstance(engine, listener, source, sampleRate, mono, spatialized, useHrtf);

        source.soundInstance = instance;

        // An invalidated engine has no native source to feed; the instance is inert and the worker
        // must not see the source.
        if (engine.State != AudioEngineState.Invalidated)
            NewSources.Add(source);

        return instance;
    }

    /// <inheritdoc/>
    public override int MaxNumberOfBuffers => BufferCount;

    /// <summary>
    /// No effect: a generated stream has no end to loop back from.
    /// </summary>
    public override void SetLooped(bool looped)
    {
        // Reached from the SoundInstance constructor before soundInstance is assigned - see the class remarks.
    }

    /// <inheritdoc/>
    protected override void ExtractAndFillData()
    {
        _fill(_buffer, _sampleRate, _channels);

        FillBuffer(_buffer, _buffer.Length * sizeof(short), AudioLayer.BufferType.None);
    }
}