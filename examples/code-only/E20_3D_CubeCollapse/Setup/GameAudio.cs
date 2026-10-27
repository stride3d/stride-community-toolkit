using Stride.Audio;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Media;

namespace CubeCollapse.Setup;

/// <summary>
/// Loads and plays the game's sound effects.
/// </summary>
/// <remarks>
/// <para>
/// Two habits worth copying are in here. The first is <em>round-robin</em>: a clear picks from three
/// recordings of the same sound in turn, because one clip repeated quickly stops sounding like an
/// event and starts sounding like a machine gun. The second is <em>pitch variation</em>, which does
/// the same job along a different axis and doubles here as information - a bigger clear is a lower,
/// heavier sound, so the size of what just happened is audible before the number is readable.
/// </para>
/// <para>
/// Round-robin is doing more work than it looks like here. A <see cref="SoundInstance"/> plays one
/// sound at a time, so replaying one that is still sounding necessarily cuts it off - which is what
/// the previous version did with its single instance, meaning the faster the player went the less
/// they heard. Three instances means three clears can overlap; only the fourth in quick succession
/// interrupts anything.
/// </para>
/// </remarks>
public class GameAudio
{
    private const int ClearVariants = 3;

    private readonly AudioEngine _engine;
    private readonly SoundInstance?[] _clearInstances = new SoundInstance?[ClearVariants];
    private readonly SoundInstance? _bigClear;
    private readonly SoundInstance? _comboStep;
    private readonly SoundInstance? _rejected;

    private int _nextClearVariant;

    /// <summary>
    /// Loads every clip the game uses.
    /// </summary>
    /// <param name="game">The running game, whose content manager owns the audio assets.</param>
    public GameAudio(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        _engine = game.Audio.AudioEngine;

        for (var i = 0; i < ClearVariants; i++)
        {
            _clearInstances[i] = Load(game, $"cube-clear-{i + 1}");
        }

        _bigClear = Load(game, "big-clear");
        _comboStep = Load(game, "combo-step");
        _rejected = Load(game, "click-rejected");
    }

    private static SoundInstance? Load(Game game, string assetName)
        => game.Content.Load<Sound>(assetName)?.CreateInstance(game.Audio.AudioEngine.DefaultListener);

    /// <summary>
    /// Plays the sound for a successful clear.
    /// </summary>
    /// <param name="cubeCount">How many cubes were cleared, which sets the pitch and the layering.</param>
    public void PlayClear(int cubeCount)
    {
        var instance = _clearInstances[_nextClearVariant];

        _nextClearVariant = (_nextClearVariant + 1) % ClearVariants;

        if (instance is not null)
        {
            // Bigger clears drop in pitch, down to roughly two thirds, which reads as heavier
            instance.Pitch = MathUtil.Lerp(1.15f, 0.7f, Math.Clamp((cubeCount - 2) / 20f, 0f, 1f));

            Play(instance);
        }

        // Layered over the clear rather than replacing it, so a big clear still sounds like the
        // normal one happening - only more so
        if (cubeCount >= 10)
        {
            Play(_bigClear);
        }
    }

    /// <summary>
    /// Plays the rising note that marks a combo continuing.
    /// </summary>
    /// <param name="comboStep">Which step of the combo this is, starting at zero.</param>
    public void PlayComboStep(int comboStep)
    {
        if (_comboStep is null) return;

        // A semitone per step, capped, so the streak is audible as it climbs
        _comboStep.Pitch = MathF.Pow(1.06f, Math.Clamp(comboStep, 0, 8));

        Play(_comboStep);
    }

    /// <summary>
    /// Plays the dull sound for a click that cleared nothing.
    /// </summary>
    public void PlayRejected() => Play(_rejected);

    /// <summary>
    /// Plays an instance, first making sure the audio engine is not still paused.
    /// </summary>
    /// <remarks>
    /// Stride pauses the whole audio engine when the window loses focus - <c>AudioSystem</c> hooks
    /// <c>Game.Deactivated</c> and calls <c>AudioEngine.PauseAudio()</c> - and
    /// <c>SoundInstance.Play</c> returns silently while it is paused, with no error and no queued
    /// playback. Clicking back onto the window delivers that click before the matching
    /// <c>ResumeAudio</c> has run, so the first sound after coming back was being dropped. Reaching
    /// this method at all means a click was just handled, so the window has focus and resuming is the
    /// correct thing to do rather than a workaround for a race.
    /// </remarks>
    private void Play(SoundInstance? instance)
    {
        if (instance is null) return;

        if (_engine.State == AudioEngineState.Paused) _engine.ResumeAudio();

        // An instance cannot overlap itself, so restarting is the only option once it is busy. The
        // round-robin above is what keeps that from being the common case.
        if (instance.PlayState == PlayState.Playing) instance.Stop();

        instance.Play();
    }
}