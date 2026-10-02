using Stride.Audio;
using Stride.CommunityToolkit.Audio;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Media;
using System.Reflection;

// 3D positional audio for a runtime sound: a pad loops on an orb that circles a pillar, and the
// listener is the camera, so the sound moves as the orb does and as you walk around.
//
// A spatialised instance is created with spatialized: true (mono only) and positioned by
// Apply3D(AudioEmitter); SoundEmitterScript does that every frame from its entity's transform.
// The other half is the listener, and this is the part that trips people up: a runtime sound is
// heard by the engine's default listener, which nothing ever moves - it sits at the origin facing
// +Z whatever the camera does. The engine only moves listeners that belong to an
// AudioListenerComponent, so AttachListener puts one on the camera and hands back its listener,
// and every instance here is created against that.
//
// HRTF - head-related transfer function, the "sounds like it is behind you" kind of spatialisation
// - takes two switches: HrtfSupport in the game settings before Run, which UseGameSettings
// provides, and useHrtf per instance. It works on Windows (XAudio2); OpenAL on Linux ignores it
// and spatialises with its own model. T recreates the instance with the flag flipped so the two
// can be compared.

const float OrbitRadius = 5f;
const float OrbitSpeed = 0.6f;         // radians per second

var angle = 0f;
var orbiting = true;
var hrtf = true;

WavSound? pad = null;
WavSound? chime = null;
AudioListener? listener = null;
SoundInstance? padInstance = null;

// Every chime that is still sounding, so each can be disposed when it has finished
var chimes = new List<SoundInstance>();
Entity? orb = null;
SoundEmitterScript? emitter = null;

var directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;

using var game = new Game();

// The first HRTF switch. Harmless where unsupported.
game.UseGameSettings(settings => settings.GetOrCreateConfiguration<AudioEngineSettings>().HrtfSupport = true);

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddProfiler();

    // Inside the orbit, so the orb passes beside and behind the listener on every lap.
    game.SetCameraPosition(new Vector3(0, 2.2f, -4));
    game.SetCameraRotation(new Vector3(180, -10, 0));

    // Scenery only, so the plain options: no physics body. A cylinder's size is its radius in X
    // and its height in Z
    var pillar = game.Create3DPrimitive(PrimitiveModelType.Cylinder, new Primitive3DEntityOptions
    {
        Size = new Vector3(0.3f, 0f, 2.5f),
        Material = game.CreateMaterial(new Color(150, 150, 160)),
        Position = new Vector3(0, 1.25f, 0),
    });
    pillar.Scene = scene;

    orb = game.Create3DPrimitive(PrimitiveModelType.Sphere, new Primitive3DEntityOptions
    {
        Size = new Vector3(0.6f),
        Material = game.CreateMaterial(new Color(255, 120, 60)),
    });
    orb.Scene = scene;

    // The listener that moves: the camera's. Everything below is created against it.
    listener = game.Audio.AttachListener(game.GetCameraEntity());

    pad = game.Audio.LoadWav(Path.Combine(directory, "pad-loop.wav"));
    chime = game.Audio.LoadWav(Path.Combine(directory, "chime.wav"));

    emitter = new SoundEmitterScript();
    orb.Add(emitter);

    StartPad();
    AddInstructions();
}

void StartPad()
{
    padInstance?.Dispose();

    // The second HRTF switch is per instance, which is why T can flip it: dispose and recreate.
    padInstance = pad!.CreateInstance(spatialized: true, useHrtf: hrtf, listener: listener);
    padInstance.IsLooping = true;
    padInstance.Volume = 0.7f;
    padInstance.Play();

    emitter!.Instance = padInstance;
}

void Update(Scene scene, Stride.Games.GameTime time)
{
    var input = game.Input;

    if (input.IsKeyPressed(Keys.G))
        orbiting = !orbiting;

    if (input.IsKeyPressed(Keys.T))
    {
        hrtf = !hrtf;
        StartPad();
    }

    if (input.IsKeyPressed(Keys.Space) && padInstance is not null)
    {
        if (padInstance.PlayState == PlayState.Playing)
            padInstance.Stop();
        else
            padInstance.Play();
    }

    if (input.IsKeyPressed(Keys.N) && chime is not null && emitter is not null)
    {
        // A one-shot needs positioning once: it is over before the orb has moved far.
        var instance = chime.CreateInstance(spatialized: true, useHrtf: hrtf, listener: listener);
        instance.Apply3D(emitter.Emitter);
        instance.Play();

        chimes.Add(instance);
    }

    // A finished instance still holds a native source: dispose it once it reports Stopped
    chimes.RemoveAll(instance =>
    {
        if (instance.PlayState != PlayState.Stopped)
            return false;

        instance.Dispose();
        return true;
    });

    if (orbiting)
        angle += OrbitSpeed * (float)time.Elapsed.TotalSeconds;

    if (orb is not null)
        orb.Transform.Position = new Vector3(MathF.Cos(angle) * OrbitRadius, 1.2f, MathF.Sin(angle) * OrbitRadius);
}

void AddInstructions()
{
    var overlay = DebugOverlay.GetOrCreate(game);

    overlay.AddSection("Spatial sound", () =>
    {
        var camera = game.GetCameraEntity().Transform.WorldMatrix;
        var toOrb = orb!.Transform.Position - camera.TranslationVector;
        var side = Vector3.Dot(toOrb, (Vector3)camera.Row1);          // the camera's +X is screen-right
        var ahead = Vector3.Dot(toOrb, -(Vector3)camera.Row3);        // it looks down its -Z
        var playing = padInstance?.PlayState == PlayState.Playing;

        return
        [
            new("T", $"HRTF {(hrtf ? "on" : "off")} (Windows only; OpenAL ignores it)", hrtf ? Color.LightGreen : Color.Yellow),
            new("G", $"Orbit {(orbiting ? "running" : "paused")}", Color.Yellow),
            new("Space", $"Pad {(playing ? "playing" : "stopped")}", Color.Yellow),
            new("N", "Chime at the orb", Color.Yellow),
            new(""),
            new($"Orb {toOrb.Length():0.0} m away, {(side < 0 ? "left" : "right")}, {(ahead < 0 ? "behind" : "ahead")}", Color.LightGreen),
            new("Listener: the camera (AttachListener)", Color.LightGray),
            new("Emitter: the orb (SoundEmitterScript)", Color.LightGray),
            new("Walk around with the camera keys: the sound follows", Color.LightGray),
        ];
    });
}

/*
---example-metadata
slug: spatial-sound
title:
  en: Spatial Sound
level: Intermediate
category: Audio
complexity: 4
order: 78
description:
  en: |-
    3D positional audio for a runtime sound: a looping pad on an orb that circles a pillar, heard
    from the camera. The instance is created spatialised and positioned every frame by
    SoundEmitterScript; the listener is the camera's, obtained with AttachListener, because the
    engine's default listener never moves. T recreates the instance with HRTF on or off to compare
    the two, G pauses the orbit, N fires a one-shot chime at the orb.
concepts:
  - Creating a spatialised instance and positioning it with Apply3D through SoundEmitterScript
  - "Why the default listener is useless for a moving camera, and AttachListener as the fix"
  - HRTF as two switches - HrtfSupport through UseGameSettings, then useHrtf per instance
  - Positioning a one-shot once versus tracking a moving emitter every frame
  - Reading the emitter's side and distance relative to the camera for the overlay
  - "Using helpers: UseGameSettings, SetupBase3DScene, Create3DPrimitive, DebugOverlay"
tags:
  - 3D
  - Audio
  - Spatial
  - HRTF
  - Listener
related:
  - E12_Audio_WavFile
  - E12_Audio_Procedural
  - E05_3D_MultipleSimulations
screenshotFrame: 90
enabled: true
created: 2026-09-05
---
*/