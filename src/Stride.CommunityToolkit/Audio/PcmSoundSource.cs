using Stride.Audio;

namespace Stride.CommunityToolkit.Audio;

/// <summary>
/// Streams an in-memory block of 16-bit PCM into the audio engine, one buffer at a time, with
/// looping. The source behind every <see cref="WavSound.CreateInstance"/>.
/// </summary>
/// <remarks>
/// <para>
/// Same circular-construction dance as <see cref="ProceduralSoundSource"/>; see its remarks.
/// </para>
/// <para>
/// Ending a one-shot is the delicate part, and this class does it differently from the engine's
/// own streaming source. That one stops itself the moment it queues the last buffer, which does two
/// things wrong here: it resets the prebuffer state before the voice may have started (a sound
/// short enough to fit in one buffer then never plays), and it leaves the instance reporting
/// <c>Playing</c> for ever, because on XAudio2 the native "playing" flag is never cleared for a
/// streamed source that ends on its own. So after the last buffer is queued this source waits,
/// from <see cref="UpdateInternal"/> on the worker thread, until the device has handed every buffer
/// back - which is exactly when the last sample has played - and only then stops, explicitly.
/// </para>
/// <para>
/// Three buffers rather than the engine's four: the worker wants a third of them filled before it
/// starts the voice, so three means one buffer is enough, and a sound that fits in a single
/// buffer starts like any other.
/// </para>
/// </remarks>
internal sealed class PcmSoundSource : DynamicSoundSource
{
    private const int FramesPerBuffer = 4096;
    private const int BufferCount = 3;

    private readonly ReadOnlyMemory<short> _samples;
    private readonly short[] _buffer;
    private readonly List<AudioLayer.Buffer> _reclaimed = [];

    private int _position;
    private volatile bool _looped;
    private bool _draining;

    private PcmSoundSource(ReadOnlyMemory<short> samples, int channels)
        : base(null!, BufferCount, FramesPerBuffer * channels * sizeof(short))
    {
        _samples = samples;
        _buffer = new short[FramesPerBuffer * channels];
    }

    /// <summary>
    /// Creates an instance that plays <paramref name="samples"/>.
    /// </summary>
    internal static SoundInstance Create(AudioEngine engine, AudioListener listener, ReadOnlyMemory<short> samples, int sampleRate, int channels, bool spatialized, bool useHrtf)
    {
        var source = new PcmSoundSource(samples, channels);
        var instance = new SoundInstance(engine, listener, source, sampleRate, channels == 1, spatialized, useHrtf);

        source.soundInstance = instance;

        if (engine.State != AudioEngineState.Invalidated)
            NewSources.Add(source);

        return instance;
    }

    /// <inheritdoc/>
    public override int MaxNumberOfBuffers => BufferCount;

    /// <inheritdoc/>
    public override void SetLooped(bool looped) => _looped = looped;

    /// <inheritdoc/>
    protected override void PrepareInternal()
    {
        base.PrepareInternal();

        _position = 0;
        _draining = false;
    }

    /// <inheritdoc/>
    protected override void UpdateInternal()
    {
        if (!_draining)
            return;

        // Reclaim what the device has finished with. The native handle is internal to the engine, so
        // the reclaim goes through the base class's CanFill, which pulls one returned buffer into
        // freeBuffers per call - but only when freeBuffers is empty, hence the shuffle. The list is a
        // field because this runs on every worker tick until the last buffer is back.
        _reclaimed.Clear();

        while (true)
        {
            while (freeBuffers.Count > 0)
                _reclaimed.Add(freeBuffers.Dequeue());

            if (!CanFill)
                break;
        }

        foreach (var buffer in _reclaimed)
            freeBuffers.Enqueue(buffer);

        // When every buffer is back, the last sample has played. The stop is explicit so the native
        // layer's own flag is cleared too.
        if (freeBuffers.Count < deviceBuffers.Count)
            return;

        _draining = false;

        StopInternal(ignoreQueuedBuffer: true);
    }

    /// <inheritdoc/>
    protected override void ExtractAndFillData()
    {
        if (_draining)
            return;

        var remaining = _samples.Length - _position;
        var count = Math.Min(remaining, _buffer.Length);

        _samples.Span.Slice(_position, count).CopyTo(_buffer);
        _position += count;

        var last = _position >= _samples.Length;
        var type = last
            ? (_looped ? AudioLayer.BufferType.EndOfLoop : AudioLayer.BufferType.EndOfStream)
            : AudioLayer.BufferType.None;

        FillBuffer(_buffer, count * sizeof(short), type);

        if (!last)
            return;

        _position = 0;
        _draining = !_looped;
    }
}