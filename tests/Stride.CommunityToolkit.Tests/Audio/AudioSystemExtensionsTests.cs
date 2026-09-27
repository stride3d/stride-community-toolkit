using Stride.Audio;
using Stride.CommunityToolkit.Audio;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Engine;
using Stride.Games;
using Stride.Media;
using System.Diagnostics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Audio;

/// <summary>
/// Pins the runtime-audio helpers against a real, headless <see cref="Game"/>: a procedural sound
/// is fed from its callback while it plays, a <c>.wav</c> loaded through the audio system can create
/// instances, and <see cref="AudioSystemExtensions.AttachListener"/> hands back the listener the
/// engine moves.
/// </summary>
/// <remarks>
/// <para>
/// The audio engine is created inside <c>Run</c>, and a machine may not be able to play: without a
/// native audio backend (Linux without OpenAL) <c>AudioSystem.Initialize</c> swallows the failure and
/// leaves <see cref="AudioSystem.AudioEngine"/> null; without an audio device (a CI runner) the engine
/// exists but is <see cref="AudioEngineState.Invalidated"/>, every instance is inert and the worker
/// never feeds a source. The playback tests record whether the engine could play and skip with that
/// reason rather than fail. The listener test needs no device and runs wherever there is an engine;
/// the helpful exception before <c>Run</c> holds everywhere.
/// </para>
/// <para>
/// Same collection as <see cref="GameExtensionsRunTests"/>: one headless game at a time.
/// </para>
/// </remarks>
[Collection(GameExtensionsRunTests.Name)]
public class AudioSystemExtensionsTests
{
    private const string NoEngine = "No audio engine on this machine (AudioSystem.Initialize failed silently).";
    private const string NoPlayback = "No audio playback on this machine: no engine, or no audio device (the engine is invalidated).";

    /// <summary>Whether the machine can play sound: an engine, and a device behind it.</summary>
    private static bool CanPlay(AudioSystem audio) => audio.AudioEngine is { State: not AudioEngineState.Invalidated };

    [Fact]
    public void CreateProceduralSound_BeforeRun_ExplainsThatTheEngineIsMissing()
    {
        using var game = new Game();

        var ex = Assert.Throws<InvalidOperationException>(() => game.Audio.CreateProceduralSound((s, _, _) => s.Clear()));

        Assert.Contains("audio engine", ex.Message);
    }

    [SkippableFact]
    public void CreateProceduralSound_FeedsTheCallbackWhilePlaying()
    {
        using var game = new Game();

        var fills = 0;
        var canPlay = false;
        var clock = Stopwatch.StartNew();
        SoundInstance? instance = null;

        game.Run(update: (_, _) =>
        {
            canPlay = CanPlay(game.Audio);

            if (!canPlay)
            {
                game.Exit();
                return;
            }

            if (instance is null)
            {
                clock.Restart();
                instance = game.Audio.CreateProceduralSound((samples, _, _) =>
                {
                    samples.Clear();
                    Interlocked.Increment(ref fills);
                });

                instance.Play();
            }

            // The worker fills on its own thread at the playback rate, about one buffer per 93 ms; the
            // headless loop runs unthrottled, so the deadline is wall-clock, not frames.
            if (clock.ElapsedMilliseconds > 3000 || Volatile.Read(ref fills) >= 4)
            {
                instance?.Dispose();
                game.Exit();
            }
        }, context: new GameContextHeadless());

        Skip.If(!canPlay, NoPlayback);

        Assert.True(fills >= 4, $"Expected the callback to fill at least the four prebuffered blocks; it ran {fills} times.");
    }

    [SkippableFact]
    public void LoadWav_ThenCreateInstance_PlaysAndEnds()
    {
        using var game = new Game();

        var canPlay = false;
        var clock = Stopwatch.StartNew();
        WavSound? sound = null;
        SoundInstance? instance = null;
        var ended = false;

        // A quarter of a second of silence, mono, 8 kHz: 2000 frames.
        var wav = WavSoundTests.Wav(channels: 1, sampleRate: 8000, bits: 16, new byte[4000]);

        game.Run(update: (_, _) =>
        {
            canPlay = CanPlay(game.Audio);

            if (!canPlay)
            {
                game.Exit();
                return;
            }

            if (instance is null)
            {
                clock.Restart();
                sound = game.Audio.LoadWav(new MemoryStream(wav));
                instance = sound.CreateInstance();
                instance.Play();
                return;
            }

            ended = instance.PlayState == PlayState.Stopped;

            if (ended || clock.ElapsedMilliseconds > 4000)
            {
                instance.Dispose();
                game.Exit();
            }
        }, context: new GameContextHeadless());

        Skip.If(!canPlay, NoPlayback);

        Assert.NotNull(sound);
        Assert.Equal(2000, sound.FrameCount);
        Assert.True(ended, "A quarter-second one-shot should report Stopped within four seconds.");
    }

    [SkippableFact]
    public void AttachListener_OnAnEntityInTheScene_ReturnsTheListenerTheEngineMoves()
    {
        using var game = new Game();

        var engineAvailable = false;
        AudioListener? first = null;
        AudioListener? second = null;
        InvalidOperationException? outsideScene = null;

        game.Run(start: scene =>
        {
            engineAvailable = game.Audio.AudioEngine is not null;

            if (!engineAvailable)
                return;

            var entity = new Entity("ears") { Scene = scene };

            first = game.Audio.AttachListener(entity);
            second = game.Audio.AttachListener(entity);

            outsideScene = Assert.Throws<InvalidOperationException>(() => game.Audio.AttachListener(new Entity("nowhere")));
        }, update: (_, _) => game.Exit(), context: new GameContextHeadless());

        Skip.If(!engineAvailable, NoEngine);

        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.NotSame(game.Audio.AudioEngine?.DefaultListener, first);
        Assert.Contains("in a scene", outsideScene?.Message);
    }
}