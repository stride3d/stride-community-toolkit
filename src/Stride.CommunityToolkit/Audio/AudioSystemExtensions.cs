using Stride.Audio;
using Stride.Engine;

namespace Stride.CommunityToolkit.Audio;

/// <summary>
/// Sound for a game with no sound assets: generated sounds, <c>.wav</c> files read at runtime, and
/// a listener that follows an entity. All on <c>game.Audio</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every method needs the audio engine, and the engine may be missing: <c>AudioSystem.Initialize</c>
/// swallows the native initialisation failure and leaves <see cref="AudioSystem.AudioEngine"/>
/// <see langword="null"/>. On Windows the engine ships in the package (XAudio2), so this happens on
/// Linux without OpenAL and on headless machines. Each method throws
/// <see cref="InvalidOperationException"/> naming that cause rather than failing later with a
/// null reference; check <see cref="AudioSystem.AudioEngine"/> first to degrade silently instead.
/// </para>
/// <para>
/// Call these once the game is running - in the <c>start</c> callback of <c>Run</c> or later. The
/// engine does not exist before <c>Initialize</c>.
/// </para>
/// </remarks>
public static class AudioSystemExtensions
{
    /// <summary>
    /// Creates a sound whose samples come from a callback: a tone, noise, a synthesiser - anything
    /// with no file behind it.
    /// </summary>
    /// <param name="audio">The game's audio system, <c>game.Audio</c>.</param>
    /// <param name="fill">Produces each block of samples. Runs on the audio worker thread; see <see cref="SampleFiller"/>.</param>
    /// <param name="sampleRate">Samples per second per channel.</param>
    /// <param name="mono">One channel when <see langword="true"/>, two when <see langword="false"/>.</param>
    /// <param name="spatialized">Whether the instance can be positioned with <see cref="SoundInstance.Apply3D"/>. Mono only.</param>
    /// <param name="useHrtf">Head-related transfer function spatialisation; needs <c>AudioEngineSettings.HrtfSupport</c> through <c>UseGameSettings</c>, and works on Windows only.</param>
    /// <param name="listener">The listener the instance is heard by; the engine's default listener when omitted. See <see cref="AttachListener"/>.</param>
    /// <returns>A stopped instance; call <see cref="SoundInstance.Play()"/>. Dispose it when done.</returns>
    /// <exception cref="InvalidOperationException">The audio engine is not available.</exception>
    /// <example>
    /// A 440 Hz sine:
    /// <code>
    /// var phase = 0.0;
    /// var tone = game.GetAudioSystem().CreateProceduralSound((samples, rate, channels) =>
    /// {
    ///     for (var i = 0; i &lt; samples.Length; i++)
    ///     {
    ///         samples[i] = (short)(Math.Sin(phase) * short.MaxValue * 0.25);
    ///         phase += 2 * Math.PI * 440 / rate;
    ///     }
    /// });
    /// tone.Play();
    /// </code>
    /// </example>
    public static SoundInstance CreateProceduralSound(this AudioSystem audio, SampleFiller fill, int sampleRate = 44100, bool mono = true, bool spatialized = false, bool useHrtf = false, AudioListener? listener = null)
    {
        var engine = RequireEngine(audio);

        return ProceduralSoundSource.Create(engine, listener ?? engine.DefaultListener, fill, sampleRate, mono, spatialized, useHrtf);
    }

    /// <summary>
    /// Reads a <c>.wav</c> file into memory, ready to create instances from.
    /// </summary>
    /// <param name="audio">The game's audio system, <c>game.Audio</c>.</param>
    /// <param name="path">The file. Relative paths resolve against the working directory, so prefer a path built from the executable's folder.</param>
    /// <returns>The decoded sound. Keep it and call <see cref="WavSound.CreateInstance"/> per playback.</returns>
    /// <exception cref="InvalidOperationException">The audio engine is not available.</exception>
    /// <exception cref="InvalidDataException">The file is not a WAVE file, or holds a format <see cref="WavSound"/> does not decode.</exception>
    public static WavSound LoadWav(this AudioSystem audio, string path)
    {
        var engine = RequireEngine(audio);

        using var stream = File.OpenRead(path);

        return WavReader.Read(stream, engine);
    }

    /// <summary>
    /// Reads a <c>.wav</c> from a stream into memory, ready to create instances from.
    /// </summary>
    /// <param name="audio">The game's audio system, <c>game.Audio</c>.</param>
    /// <param name="stream">The WAVE data, read from its current position to the end of the data chunk. Not disposed.</param>
    /// <exception cref="InvalidOperationException">The audio engine is not available.</exception>
    /// <exception cref="InvalidDataException">The stream is not a WAVE file, or holds a format <see cref="WavSound"/> does not decode.</exception>
    public static WavSound LoadWav(this AudioSystem audio, Stream stream)
    {
        var engine = RequireEngine(audio);

        return WavReader.Read(stream, engine);
    }

    /// <summary>
    /// Gives <paramref name="entity"/> an <see cref="AudioListenerComponent"/> and returns its
    /// listener, which the engine then moves with the entity every frame. Put it on the camera and
    /// pass the result as the <c>listener</c> of every spatialised sound.
    /// </summary>
    /// <param name="audio">The game's audio system, <c>game.Audio</c>.</param>
    /// <param name="entity">The entity to listen from. Must already be in the scene: the engine creates the listener when the component enters one.</param>
    /// <returns>The entity's listener. Calling again for the same entity returns the same listener.</returns>
    /// <remarks>
    /// Without this, a runtime sound is heard by <see cref="AudioEngine.DefaultListener"/>, which
    /// nothing ever moves: it sits at the origin facing +Z however the camera turns. The engine only
    /// updates listeners that belong to a component, and this is the way to get one of those for a
    /// runtime sound.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The audio engine is not available, or the entity is not in a scene.</exception>
    public static AudioListener AttachListener(this AudioSystem audio, Entity entity)
    {
        RequireEngine(audio);
        ArgumentNullException.ThrowIfNull(entity);

        return AudioInternals.GetListener(entity.GetOrCreate<AudioListenerComponent>())
            ?? throw new InvalidOperationException($"'{entity.Name}' has no listener yet: the engine creates one when the entity is in a scene. Add the entity to the scene first.");
    }

    private static AudioEngine RequireEngine(AudioSystem audio)
    {
        ArgumentNullException.ThrowIfNull(audio);

        return audio.AudioEngine
            ?? throw new InvalidOperationException("The audio engine is not available: it failed to initialise (no OpenAL on Linux, or a headless machine) or the game has not started yet. Check game.GetAudioSystem().AudioEngine before creating sounds.");
    }
}