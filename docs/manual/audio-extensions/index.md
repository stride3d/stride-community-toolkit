# Audio Extensions

Sound for a game with no sound assets: tones generated in a callback, `.wav` files read at runtime, and a listener that follows the camera. Everything lives in `Stride.CommunityToolkit.Audio` and hangs off `game.Audio`.

## Why this exists

The type an `AudioEmitterComponent` plays, `Sound`, only comes out of the asset pipeline: its constructor is internal and its data is compressed by ffmpeg at build time. A code-only game has no `.sdpkg`, so it has no `Sound` and no other way to make a sound.

The door that is open is the `SoundInstance` constructor that takes a `DynamicSoundSource`. The engine asks the source for a block of PCM whenever a device buffer is free; the toolkit provides two sources - one backed by a callback, one by an array decoded from a `.wav` - and hides the constructor dance behind three methods.

## Making sound

- [`CreateProceduralSound()`](xref:Stride.CommunityToolkit.Audio.AudioSystemExtensions.CreateProceduralSound(Stride.Audio.AudioSystem,Stride.CommunityToolkit.Audio.SampleFiller,System.Int32,System.Boolean,System.Boolean,System.Boolean,Stride.Audio.AudioListener)) - A sound whose samples come from a [`SampleFiller`](xref:Stride.CommunityToolkit.Audio.SampleFiller) callback: a tone, noise, a synthesiser. Four buffers of 4096 samples, so a change is heard within about a tenth of a second. The callback runs on the audio worker thread; hand it single `int` or `float` fields, not structures.
- [`LoadWav(path)`](xref:Stride.CommunityToolkit.Audio.AudioSystemExtensions.LoadWav(Stride.Audio.AudioSystem,System.String)) / [`LoadWav(stream)`](xref:Stride.CommunityToolkit.Audio.AudioSystemExtensions.LoadWav(Stride.Audio.AudioSystem,System.IO.Stream)) - Decodes a `.wav` (8/16/24/32-bit PCM or 32-bit float, mono or stereo, any sample rate) into a [`WavSound`](xref:Stride.CommunityToolkit.Audio.WavSound) held in memory. Compressed formats are rejected with a message naming them.
- [`WavSound.CreateInstance()`](xref:Stride.CommunityToolkit.Audio.WavSound.CreateInstance(System.Boolean,System.Boolean,Stride.Audio.AudioListener)) - One `WavSound`, many instances: each plays independently, so effects overlap. Set `IsLooping` before `Play()` for music. Dispose an instance once it reports `Stopped`; each holds a native source.

```csharp
var chime = game.Audio.LoadWav(Path.Combine(directory, "chime.wav"));

var instance = chime.CreateInstance();
instance.Volume = 0.8f;
instance.Play();
```

## Positioning sound

- [`AttachListener()`](xref:Stride.CommunityToolkit.Audio.AudioSystemExtensions.AttachListener(Stride.Audio.AudioSystem,Stride.Engine.Entity)) - Puts an `AudioListenerComponent` on an entity - the camera, almost always - and returns its listener, which the engine then moves every frame. Pass it as the `listener` of every spatialised instance.
- [`SoundEmitterScript`](xref:Stride.CommunityToolkit.Audio.SoundEmitterScript) - A `SyncScript` that positions a spatialised instance at its entity every frame: position, orientation and velocity from the world transform, through `Apply3D`.

```csharp
var listener = game.Audio.AttachListener(game.GetCameraEntity());

var pad = game.Audio.LoadWav(Path.Combine(directory, "pad-loop.wav"));
var instance = pad.CreateInstance(spatialized: true, listener: listener);

orb.Add(new SoundEmitterScript { Instance = instance });
instance.Play();
```

> [!WARNING]
> Without `AttachListener`, a runtime sound is heard by the engine's default listener, which nothing ever moves: it sits at the origin facing +Z however the camera turns. The engine only updates listeners that belong to an `AudioListenerComponent`, and the listener on that component is internal - `AttachListener` reads it for you.

Spatialised instances must be mono, and `Pan` and spatialisation are exclusive: both files the examples ship are mono for that reason.

## HRTF

Head-related transfer function spatialisation takes two switches: `HrtfSupport` in the game settings before `Run()`, which [`UseGameSettings()`](../game-extensions/index.md) provides, then `useHrtf: true` per instance. It works on Windows, where the engine's XAudio2 backend carries the HRTF processor; OpenAL on Linux and macOS ignores the flag and uses its own model.

```csharp
game.UseGameSettings(settings => settings.GetOrCreateConfiguration<AudioEngineSettings>().HrtfSupport = true);
```

## When there is no audio engine

`AudioSystem.Initialize` swallows the native initialisation failure and leaves `game.Audio.AudioEngine` null - on Linux without OpenAL, and on headless machines. Every method here throws `InvalidOperationException` naming that cause; check `game.Audio.AudioEngine` first to degrade silently instead. The methods also need the engine to exist, so call them from the `start` callback of `Run()` or later, never before.

One more engine rule worth knowing: the audio engine pauses whenever the game window loses focus, and while paused a `Play()` call is silently ignored. A game launched into the background therefore never starts its music until something calls `Play()` again with the window focused.

## Examples

- [Procedural Sound](../code-only/examples/procedural-sound.md) - a tone with no file, waveform and pitch changed live.
- [Wav File](../code-only/examples/wav-file.md) - a chime that overlaps with itself and a looping pad, from files on disk.
- [Spatial Sound](../code-only/examples/spatial-sound.md) - a sound on an orbiting orb, heard from the camera, with an HRTF toggle.