namespace Stride.CommunityToolkit.Audio;

/// <summary>
/// Produces the next block of PCM samples for a <see cref="ProceduralSoundSource"/>.
/// </summary>
/// <param name="samples">
/// The block to fill, interleaved by channel: for stereo, left then right for each frame. Every
/// element must be written; the buffer is reused between calls and holds the previous block.
/// </param>
/// <param name="sampleRate">Samples per second per channel, as given when the sound was created.</param>
/// <param name="channels">1 for mono, 2 for stereo.</param>
/// <remarks>
/// <para>
/// Called on the audio worker thread, never on the game thread, so anything it reads that the game
/// writes - frequency, waveform, volume - should be a field the game sets atomically (a
/// <see langword="float"/> or an <see langword="int"/> is enough) rather than a structure updated
/// in several steps.
/// </para>
/// </remarks>
public delegate void SampleFiller(Span<short> samples, int sampleRate, int channels);