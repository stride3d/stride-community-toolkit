using System.Collections.Concurrent;
using Stride.Audio;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.CommunityToolkit.Audio;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

// Sound from physics. Balls of three sizes drop onto a plate, and every landing is heard: the plate's
// contact handler reads how fast the ball came in along the contact normal, and hands the audio thread
// a note to play - its pitch from the ball's size, its loudness from the speed. A gentle roll is a tap,
// a drop from the top is a thump, a big ball is a low note.
//
// The sound is procedural, as in E12_Audio_Procedural: no sound file, a callback that fills blocks of
// samples. The callback mixes up to sixteen decaying sine tones. The contact handler runs on the game
// side during the physics step and the callback on the audio worker thread, so the notes cross between
// them through a ConcurrentQueue, and nothing else is shared.
//
// Space drops a ball of the chosen size from the chosen height; 1 to 3 pick the size, R drops a rain.

const float PlateHalfWidth = 5f;
float[] radii = [0.25f, 0.4f, 0.6f];
Color[] colours = [new(90, 200, 255), new(255, 200, 90), new(255, 110, 90)];
string[] sizeNames = ["small", "medium", "large"];
var sizeChoice = 1;
var dropHeight = 6f;

var balls = new List<Entity>();
var impacts = new Impacts();
var random = new Random(9);

SoundInstance? sound = null;
ShapeBatch? shapes = null;
Material[] materials = [];

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    Camera = CameraSetup.Perspective(position: new Vector3(0f, 6f, 14f), rotation: new Vector3(0f, -20f, 0f)).WithController(),
    WindowTitle = "Impact sounds - Stride Community Toolkit",
    Resizable = true,
    DpiAware = true,
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

// The instance holds a native voice until it is disposed
sound?.Dispose();

void Start(Scene scene)
{
    shapes = game.AddShapeBatch(depthTest: false);
    materials = [.. colours.Select(colour => game.CreateMaterial(colour, metalness: 0.1f, glossiness: 0.6f))];

    // The plate: static, and the one collidable with a contact handler, so every landing on it is reported
    var plate = game.CreatePrimitive(Primitive3D.Box(new Vector3(PlateHalfWidth * 2f, 0.4f, PlateHalfWidth * 2f)), new BepuPhysicsOptions
    {
        EntityName = "Plate",
        Component = new StaticComponent { Collider = new CompoundCollider(), ContactEventHandler = impacts },
        Material = game.CreateMaterial(new Color(150, 150, 160), metalness: 0.5f, glossiness: 0.7f),
        Position = new Vector3(0f, 0.2f, 0f),
    });

    plate.Scene = scene;

    // Throws with a reason where there is no audio engine: a headless machine, Linux without OpenAL
    sound = game.Audio.CreateProceduralSound(impacts.Fill);
    sound.Play();

    Rain(scene);

    TextOverlay.GetOrCreate(game).AddSection("Impact sounds", BuildOverlayLines);
}

void Update(Scene scene, GameTime time)
{
    var input = game.Input;

    if (input.IsKeyPressed(Keys.Space)) Drop(scene, sizeChoice, new Vector3((random.NextSingle() - 0.5f) * 4f, dropHeight, (random.NextSingle() - 0.5f) * 4f));
    if (input.IsKeyPressed(Keys.R)) Rain(scene);
    if (input.IsKeyPressed(Keys.C)) Clear();
    if (input.IsKeyPressed(Keys.PageUp)) dropHeight = MathF.Min(dropHeight + 2f, 16f);
    if (input.IsKeyPressed(Keys.PageDown)) dropHeight = MathF.Max(dropHeight - 2f, 2f);

    for (var i = 0; i < radii.Length; i++)
    {
        if (input.IsKeyPressed(Keys.D1 + i) || input.IsKeyPressed(Keys.NumPad1 + i)) sizeChoice = i;
    }

    // A ball that rolled off the plate is gone for good; put it back in the rain
    for (var i = balls.Count - 1; i >= 0; i--)
    {
        if (balls[i].Transform.Position.Y < -5f)
        {
            balls[i].Scene = null;
            balls.RemoveAt(i);
        }
    }

    if (shapes is not null) impacts.DrawMarks(shapes, (float)time.Elapsed.TotalSeconds);
}

/// <summary>Balls of every size from a spread of heights, so the first seconds are a scale of notes.</summary>
void Rain(Scene scene)
{
    for (var i = 0; i < 12; i++)
    {
        Drop(scene, i % radii.Length, new Vector3((random.NextSingle() - 0.5f) * 7f, 4f + i * 1.2f, (random.NextSingle() - 0.5f) * 7f));
    }
}

void Drop(Scene scene, int size, Vector3 position)
{
    var ball = game.CreatePrimitive(Primitive3D.Sphere(radii[size]), new BepuPhysicsOptions
    {
        EntityName = $"{sizeNames[size]} ball",
        Material = materials[size],
        ShareModel = true,
        Position = position,
    });

    ball.Scene = scene;
    balls.Add(ball);
}

void Clear()
{
    foreach (var ball in balls) ball.Scene = null;

    balls.Clear();
}

IReadOnlyList<TextElement> BuildOverlayLines() =>
[
    new("Space", $"Drop a {sizeNames[sizeChoice]} ball from {dropHeight:0} m", Color.Gold),
    new(["1", "2", "3"], "Small, medium or large", Color.Gold),
    new(["PageUp", "PageDown"], "Drop height", Color.Gold),
    new("R", "A rain of balls", Color.Gold),
    new("C", "Clear the balls", Color.Gold),
    new(""),
    new($"Landings heard: {impacts.Count}, last at {impacts.LastSpeed:0.0} m/s", Color.White),
    new("Pitch from the size, loudness from the speed", Color.LightGray),
];

/// <summary>
/// The plate's contact handler and the sound's sample filler in one. A landing becomes a note - a
/// frequency from the ball's radius, a loudness from its speed along the contact normal - queued for the
/// audio thread, which mixes the notes that are still ringing into every block it is asked for.
/// </summary>
public sealed class Impacts : IContactHandler
{
    private const int MaxVoices = 16;
    private const float Gain = 0.4f;
    private const float LoudSpeed = 12f;
    private const float QuietSpeed = 0.6f;
    private const float Decay = 0.35f;
    private const float MarkLife = 0.6f;

    // Four buffers of 4096 samples at 44.1 kHz sit between a note and the speaker, about a third of a second;
    // the ring waits as long, so the eye and the ear agree on when the ball landed
    private const float SoundDelay = 0.37f;

    private readonly ConcurrentQueue<(float Frequency, float Amplitude)> _notes = new();
    private readonly Voice[] _voices = new Voice[MaxVoices];
    private readonly (Vector3 Point, float Age)[] _marks = new (Vector3, float)[12];
    private int _nextMark;
    private int _nextVoice;

    public Impacts()
    {
        for (var i = 0; i < _marks.Length; i++) _marks[i].Age = MarkLife;
    }

    public bool NoContactResponse => false;

    public int Count { get; private set; }

    public float LastSpeed { get; private set; }

    /// <summary>The first contact of a pair: how fast the other body approaches along the normal says how hard it landed.</summary>
    void IContactHandler.OnStartedTouching<TManifold>(Contacts<TManifold> contacts)
    {
        if (contacts.Other is not BodyComponent body) return;

        var contact = contacts.GetEnumerator();

        if (!contact.MoveNext()) return;

        // The event arrives after the step has solved the contact, when the ball's velocity is already the bounce; the
        // velocity from before the step is the one it hit with. Along the normal, whichever way the normal points; a
        // ball rolling across the plate approaches at nearly nothing
        var speed = MathF.Abs(Vector3.Dot(body.PreviousLinearVelocity, contact.Current.Normal));

        if (speed < QuietSpeed) return;

        // The radius is the sphere collider's; the ball's note falls with its size, its loudness rises with the speed
        var radius = body.Collider is CompoundCollider { Colliders: [SphereCollider sphere] } ? sphere.Radius : 0.4f;
        var amplitude = Math.Clamp(speed / LoudSpeed, 0.08f, 1f);

        _notes.Enqueue((180f / radius, amplitude));
        Count++;
        LastSpeed = speed;

        _marks[_nextMark] = (contact.Current.Point, -SoundDelay);
        _nextMark = (_nextMark + 1) % _marks.Length;
    }

    /// <summary>
    /// Called on the audio worker thread for every block. New notes are taken from the queue first, then
    /// every ringing voice adds its decaying sine to the block.
    /// </summary>
    public void Fill(Span<short> samples, int sampleRate, int channels)
    {
        while (_notes.TryDequeue(out var note))
        {
            // The oldest voice is replaced when all sixteen are ringing
            _voices[_nextVoice] = new Voice { Step = note.Frequency / sampleRate, Amplitude = note.Amplitude, Fade = MathF.Exp(-1f / (Decay * sampleRate)) };
            _nextVoice = (_nextVoice + 1) % MaxVoices;
        }

        samples.Clear();

        for (var v = 0; v < _voices.Length; v++)
        {
            ref var voice = ref _voices[v];

            if (voice.Amplitude < 0.001f) continue;

            for (var i = 0; i < samples.Length; i += channels)
            {
                var value = MathF.Sin(MathUtil.TwoPi * voice.Phase) * voice.Amplitude * Gain;

                // Every channel of the frame gets the same mono sample; the sum of voices is clamped to full scale
                for (var c = 0; c < channels; c++)
                {
                    samples[i + c] = (short)Math.Clamp(samples[i + c] + value * short.MaxValue, short.MinValue, short.MaxValue);
                }

                voice.Phase += voice.Step;

                if (voice.Phase >= 1f) voice.Phase -= 1f;

                voice.Amplitude *= voice.Fade;
            }
        }
    }

    /// <summary>A ring growing out of each recent landing once its note is heard, so the eye finds what the ear hears.</summary>
    public void DrawMarks(ShapeBatch shapes, float elapsed)
    {
        shapes.StrokeWidth = 2f;

        for (var i = 0; i < _marks.Length; i++)
        {
            ref var mark = ref _marks[i];

            if (mark.Age >= MarkLife) continue;

            mark.Age += elapsed;

            if (mark.Age < 0f) continue;

            var t = mark.Age / MarkLife;

            shapes.Opacity = 1f - t;
            shapes.DrawRing(mark.Point + Vector3.UnitY * 0.02f, Vector3.UnitY, 0.2f + t * 1.2f, Color.White);
        }

        shapes.Opacity = 1f;
    }

    /// <summary>One ringing note: where it is in its cycle, how far along the cycle a sample moves it, and how loud it still is.</summary>
    private struct Voice
    {
        internal float Phase { get; set; }
        internal float Step { get; set; }
        internal float Amplitude { get; set; }
        internal float Fade { get; set; }
    }
}

/*
---example-metadata
slug: audio-impacts
title:
  en: Impact Sounds
level: Intermediate
category: Audio
complexity: 2
order: 79
description:
  en: |-
    Balls of three sizes drop onto a plate and every landing is heard. The plate's contact handler
    reads the ball's pre-step speed along the contact normal and queues a note for the audio thread, its
    pitch from the ball's size and its loudness from the speed; a procedural sound mixes up to
    sixteen decaying tones. The handler runs with the physics step and the mixer on the audio
    worker thread, so the notes cross between them through a ConcurrentQueue.
concepts:
  - A contact handler on a static plate, reading the other body's PreviousLinearVelocity along the contact normal
  - A note's pitch from the collider's radius and its loudness from the speed
  - A procedural sound that mixes several decaying tones per block
  - A ConcurrentQueue as the one thing the physics side and the audio thread share
  - "Using helpers: SceneSetup3D, UseBepu, UseSkybox, CreatePrimitive, CreateMaterial, CreateProceduralSound, ShapeBatch, TextOverlay"
tags:
  - 3D
  - Audio
  - Physics
  - Bepu
  - Contacts
related:
  - E12_Audio_Procedural
  - E12_Audio_Spatial
  - E05_3D_CollisionLayer
screenshotFrame: 200
enabled: true
created: 2026-11-01
---
*/