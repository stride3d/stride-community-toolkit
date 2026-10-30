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
/// Two habits worth copying. <em>Round-robin</em>: a clear picks from three recordings in turn,
/// because one clip repeated quickly sounds like a machine gun. <em>Pitch variation</em> does the
/// same job and doubles as information: a bigger clear is a lower, heavier sound.
/// </para>
/// <para>
/// Round-robin also lets clears overlap. A <see cref="SoundInstance"/> plays one sound at a time, so
/// replaying one that is still sounding cuts it off; with three instances only the fourth clear in
/// quick succession interrupts anything.
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

        // Layered over the clear rather than replacing it
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
    /// <c>SoundInstance.Play</c> returns silently while it is paused. Clicking back onto the window
    /// delivers that click before the matching <c>ResumeAudio</c> has run, so the first sound would
    /// be dropped. Reaching this method means a click was just handled, so the window has focus and
    /// resuming is correct.
    /// </remarks>
    private void Play(SoundInstance? instance)
    {
        if (instance is null) return;

        if (_engine.State == AudioEngineState.Paused) _engine.ResumeAudio();

        // An instance cannot overlap itself, so a busy one restarts; the round-robin keeps that rare
        if (instance.PlayState == PlayState.Playing) instance.Stop();

        instance.Play();
    }
}