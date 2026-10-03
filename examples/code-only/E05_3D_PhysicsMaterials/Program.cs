using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.CommunityToolkit.Windows;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// How to make something bouncy, and how to make something slippery. Every Bepu body and static has
// four numbers that decide what a contact does: SpringFrequency, SpringDampingRatio,
// FrictionCoefficient and MaximumRecoveryVelocity. Nothing else differs between the lanes below, and
// the overlay prints what each lane measured.
//
// Back row, bounce: seven identical balls dropped from the same height, numbered as in the overlay.
//   A contact is a spring. SpringDampingRatio is how much of the hit it absorbs: 0 absorbs nothing,
//   1 or more absorbs all of it, and the default is 3, which is why nothing bounces until it is
//   lowered. SpringFrequency is how stiff the spring is. The simulation takes 60 steps a second
//   and a stiff spring is over in a step or two, which loses most of the bounce: with no damping
//   a tenth of the height comes back at the default 30 Hz, and more than half at 5 Hz. A lower
//   frequency is also softer, so the ball sinks into the floor before it comes back.
//
//   Two things touch, and only ONE of their two springs is used: that of the side with the higher
//   MaximumRecoveryVelocity, and when the two are equal, the body's and not the static's. The last
//   two lanes are the same bouncy pad under the same plain ball. Only the one whose pad has the
//   higher MaximumRecoveryVelocity bounces.
//
// Front row, friction: six identical boxes pushed at the same speed along lanes that differ in
// friction.
//   A pair's friction is the two coefficients multiplied, so zero on either side is ice. The last
//   lane is the proof: a box of 4 on a lane of 0.5 stops where the box of 1 stops on the lane of 2.
//
//   The distances depend on the engine: the Bepu build that Stride 4.4 uses divides a pair's
//   friction by its number of contact points, four for a box on its face. Bepu has corrected that
//   since, and with it every box stops in a quarter of the distance. The order of the lanes, which
//   is the lesson, stays the same.

WindowsDpiManager.EnablePerMonitorV2();

const float BallRadius = 0.4f;
const float DropHeight = 3f;
const float PadHeight = 0.2f;
const float BallSpacing = 1.8f;
const float BounceRowZ = -3f;

const float BoxSize = 0.6f;
const float PushSpeed = 4.5f;
const float LaneLength = 11f;
const float LaneWidth = 0.9f;
const float LaneThickness = 0.1f;
const float FirstLaneZ = 1.5f;
const float LaneSpacing = 1.15f;

// The defaults of a Bepu collidable, written out so the lanes can be read against them
var defaultSpring = new Spring(Frequency: 30f, DampingRatio: 3f);
const float DefaultRecovery = 1000f;

var bouncy = new Spring(Frequency: 10f, DampingRatio: 0f);

BounceLane[] bounceLanes =
[
    new("30 Hz, damping 3", defaultSpring, Pad: null, DefaultRecovery, new Color(205, 205, 210)),
    new("30 Hz, damping 0", new Spring(30f, 0f), Pad: null, DefaultRecovery, new Color(215, 225, 120)),
    new("10 Hz, damping 0.2", new Spring(10f, 0.2f), Pad: null, DefaultRecovery, new Color(240, 200, 80)),
    new("10 Hz, damping 0", bouncy, Pad: null, DefaultRecovery, new Color(240, 150, 60)),
    new("5 Hz, damping 0", new Spring(5f, 0f), Pad: null, DefaultRecovery, new Color(235, 100, 80)),
    new("Pad, equal recovery", defaultSpring, Pad: bouncy, DefaultRecovery, new Color(110, 200, 110)),
    new("Pad, higher recovery", defaultSpring, Pad: bouncy, DefaultRecovery * 2f, new Color(90, 170, 240)),
];

FrictionLane[] frictionLanes =
[
    new(Lane: 0f, Box: 1f, new Color(190, 225, 250)),
    new(Lane: 0.5f, Box: 1f, new Color(150, 205, 225)),
    new(Lane: 1f, Box: 1f, new Color(175, 200, 160)),
    new(Lane: 2f, Box: 1f, new Color(225, 190, 120)),
    new(Lane: 4f, Box: 1f, new Color(235, 150, 90)),
    new(Lane: 0.5f, Box: 4f, new Color(150, 205, 225)),
];

var balls = new List<Ball>();
var boxes = new List<Box>();
var launched = false;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Physics materials - Stride Community Toolkit";

    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddProfiler();
    game.AddEntityTextRenderer();

    // To the right of the scene and looking straight ahead, which leaves the overlay's side clear
    game.SetCameraPosition(new Vector3(3.4f, 9.5f, 19.5f));
    game.SetCameraRotation(new Vector3(0f, -26f, 0f));

    for (var i = 0; i < bounceLanes.Length; i++) BuildBounceLane(scene, bounceLanes[i], (i - (bounceLanes.Length - 1) * 0.5f) * BallSpacing);

    for (var i = 0; i < frictionLanes.Length; i++) BuildFrictionLane(scene, frictionLanes[i], FirstLaneZ + i * LaneSpacing);

    BuildEndWall(scene);

    DebugOverlay.GetOrCreate(game).AddSection("Physics materials", OverlayLines);
}

void Update(Scene scene, GameTime time)
{
    // A velocity only takes on a body the simulation already knows, so the first push waits for it
    var ready = boxes.Count > 0 && boxes[0].Body.Simulation is not null;

    if (ready && (!launched || game.Input.IsKeyPressed(Keys.R)))
    {
        launched = true;

        Replay();
    }

    foreach (var ball in balls) ball.Measure(BallRadius);
}

/// <summary>Puts every ball and box back where it started, still, and pushes the boxes.</summary>
void Replay()
{
    foreach (var ball in balls)
    {
        Reset(ball.Body, ball.Start, Vector3.Zero);

        ball.Forget();
    }

    foreach (var box in boxes) Reset(box.Body, box.Start, new Vector3(PushSpeed, 0f, 0f));

    static void Reset(BodyComponent body, Vector3 position, Vector3 velocity)
    {
        // Teleport moves the body and nothing else: it keeps its velocity, its spin and its sleep
        body.Teleport(position, Quaternion.Identity);
        body.Awake = true;
        body.LinearVelocity = velocity;
        body.AngularVelocity = Vector3.Zero;
    }
}

/// <summary>One ball above the ground, or above a pad of its own when the lane has one.</summary>
void BuildBounceLane(Scene scene, BounceLane lane, float x)
{
    var surface = 0f;

    if (lane.Pad is { } pad)
    {
        // The pad carries the lane's spring, and the ball keeps the defaults
        var padEntity = game.Create3DPrimitive(PrimitiveModelType.Cube, new Bepu3DPhysicsOptions
        {
            Size = new Vector3(1.4f, PadHeight, 1.4f),
            Material = game.CreateMaterial(lane.Colour, metalness: 0f, glossiness: 0.3f),
            Component = new StaticComponent
            {
                Collider = new CompoundCollider(),
                SpringFrequency = pad.Frequency,
                SpringDampingRatio = pad.DampingRatio,
                MaximumRecoveryVelocity = lane.PadRecovery,
            },
        });

        padEntity.Transform.Position = new Vector3(x, PadHeight * 0.5f, BounceRowZ);
        padEntity.Scene = scene;

        surface = PadHeight;
    }

    // A sphere's size is its radius in X
    var entity = game.Create3DPrimitive(PrimitiveModelType.Sphere, new Bepu3DPhysicsOptions
    {
        Size = new Vector3(BallRadius, 0f, 0f),
        Material = game.CreateMaterial(lane.Pad is null ? lane.Colour : new Color(225, 225, 230), metalness: 0f, glossiness: 0.6f),
        Component = new BodyComponent
        {
            Collider = new CompoundCollider(),
            SpringFrequency = lane.Ball.Frequency,
            SpringDampingRatio = lane.Ball.DampingRatio,

            // Drawn between physics steps too, so the fall and the bounce are smooth
            InterpolationMode = InterpolationMode.Interpolated,
        },
    });

    var start = new Vector3(x, surface + DropHeight + BallRadius, BounceRowZ);

    entity.Transform.Position = start;
    entity.Scene = scene;

    balls.Add(new Ball(lane, entity.Get<BodyComponent>(), start, surface));

    // The ball's number in the overlay, on the ground in front of it
    AddLabel(scene, $"{balls.Count}", new Vector3(x, 0f, BounceRowZ + 1.2f), TextAnchor.MiddleCenter);
}

/// <summary>One lane with its own friction, and a box at its left end.</summary>
void BuildFrictionLane(Scene scene, FrictionLane lane, float z)
{
    var laneEntity = game.Create3DPrimitive(PrimitiveModelType.Cube, new Bepu3DPhysicsOptions
    {
        Size = new Vector3(LaneLength, LaneThickness, LaneWidth),
        Material = game.CreateMaterial(lane.Colour, metalness: 0f, glossiness: lane.Lane == 0f ? 0.9f : 0.2f),
        Component = new StaticComponent { Collider = new CompoundCollider(), FrictionCoefficient = lane.Lane },
    });

    laneEntity.Transform.Position = new Vector3(0f, LaneThickness * 0.5f, z);
    laneEntity.Scene = scene;

    var entity = game.Create3DPrimitive(PrimitiveModelType.Cube, new Bepu3DPhysicsOptions
    {
        Size = new Vector3(BoxSize),
        Material = game.CreateMaterial(lane.Box == 1f ? new Color(235, 235, 240) : new Color(230, 90, 70), metalness: 0f, glossiness: 0.4f),
        Component = new BodyComponent
        {
            Collider = new CompoundCollider(),
            FrictionCoefficient = lane.Box,
            InterpolationMode = InterpolationMode.Interpolated,
        },
    });

    var start = new Vector3(-LaneLength * 0.5f + BoxSize, LaneThickness + BoxSize * 0.5f, z);

    entity.Transform.Position = start;
    entity.Scene = scene;

    boxes.Add(new Box(lane, entity.Get<BodyComponent>(), start));

    AddLabel(scene, lane.Label, new Vector3(-LaneLength * 0.5f - 0.3f, LaneThickness, z), TextAnchor.MiddleRight);
}

/// <summary>A wall across the far end of the lanes, so a box on ice stays in the picture.</summary>
void BuildEndWall(Scene scene)
{
    var depth = (frictionLanes.Length - 1) * LaneSpacing + LaneWidth;

    var wall = game.Create3DPrimitive(PrimitiveModelType.Cube, new Bepu3DPhysicsOptions
    {
        Size = new Vector3(0.3f, 0.8f, depth),
        Material = game.CreateMaterial(new Color(150, 150, 158), metalness: 0f, glossiness: 0.3f),
        Component = new StaticComponent { Collider = new CompoundCollider() },
    });

    wall.Transform.Position = new Vector3(LaneLength * 0.5f + 0.15f, 0.4f, FirstLaneZ + (frictionLanes.Length - 1) * LaneSpacing * 0.5f);
    wall.Scene = scene;
}

/// <summary>Screen-space text anchored to a point in the world.</summary>
void AddLabel(Scene scene, string text, Vector3 position, TextAnchor anchor)
{
    var label = new Entity("Label")
    {
        new EntityTextComponent { Text = text, FontSize = 14, Anchor = anchor, EnableShadow = true },
    };

    label.Transform.Position = position;
    label.Scene = scene;
}

IReadOnlyList<TextElement> OverlayLines()
{
    List<TextElement> lines =
    [
        new("R", "Drop and push again", Color.Gold),
        new(""),
        new("Bounce: how high a ball came back", Color.LightGray),
    ];

    for (var i = 0; i < balls.Count; i++)
        lines.Add(new($"{i + 1}. {balls[i].Lane.Label}: {balls[i].Rebound / DropHeight:P0}", balls[i].Lane.Colour));

    lines.Add(new(""));
    lines.Add(new("Friction: lane x box, and the slide", Color.LightGray));

    foreach (var box in boxes)
        lines.Add(new($"{box.Lane.Lane:0.#} x {box.Lane.Box:0.#} = {box.Lane.Lane * box.Lane.Box:0.#}: {box.Distance:0.0} m", box.Lane.Colour));

    return lines;
}

/// <summary>The two numbers of a contact spring.</summary>
record struct Spring(float Frequency, float DampingRatio);

/// <summary>
/// A lane of the bounce row. The ball carries <paramref name="Ball"/>; a lane with a
/// <paramref name="Pad"/> lands its ball on a pad with that spring and <paramref name="PadRecovery"/>
/// as its MaximumRecoveryVelocity.
/// </summary>
sealed record BounceLane(string Label, Spring Ball, Spring? Pad, float PadRecovery, Color Colour);

/// <summary>A lane of the friction row: the lane's coefficient and its box's.</summary>
sealed record FrictionLane(float Lane, float Box, Color Colour)
{
    internal string Label => Box == 1f ? $"{Lane:0.#}" : $"{Lane:0.#}, box {Box:0.#}";
}

/// <summary>A dropped ball, and the height of its first rebound.</summary>
internal sealed class Ball
{
    private readonly float _surface;
    private bool _rising;
    private bool _peaked;

    internal Ball(BounceLane lane, BodyComponent body, Vector3 start, float surface)
    {
        Lane = lane;
        Body = body;
        Start = start;
        _surface = surface;
    }

    internal BounceLane Lane { get; }

    internal BodyComponent Body { get; }

    internal Vector3 Start { get; }

    /// <summary>How far above the surface the ball's lowest point came back on its first bounce.</summary>
    internal float Rebound { get; private set; }

    internal void Forget() => (_rising, _peaked, Rebound) = (false, false, 0f);

    internal void Measure(float radius)
    {
        if (_peaked) return;

        var speed = Body.LinearVelocity.Y;

        // A ball falls, goes up, and comes down again: the going up is its first rebound. The
        // velocity says so for the whole of it. The height does not: a stiff contact is over
        // between two frames, so a test for "has touched the floor" can miss it
        if (!_rising)
        {
            _rising = speed > 0.05f;

            return;
        }

        Rebound = MathF.Max(Rebound, Body.Position.Y - radius - _surface);

        if (speed < 0f) _peaked = true;
    }
}

/// <summary>A pushed box, and how far it has slid.</summary>
internal sealed class Box
{
    internal Box(FrictionLane lane, BodyComponent body, Vector3 start)
    {
        Lane = lane;
        Body = body;
        Start = start;
    }

    internal FrictionLane Lane { get; }

    internal BodyComponent Body { get; }

    internal Vector3 Start { get; }

    internal float Distance => MathF.Max(Body.Position.X - Start.X, 0f);
}

/*
---example-metadata
slug: physics-materials
title:
  en: Physics Materials
  cs: Fyzikální materiály
level: Beginner
category: Physics
complexity: 3
order: 46
description:
  en: |-
    How to make something bouncy and how to make something slippery. Seven identical balls are
    dropped and six identical boxes are pushed, and the lanes differ only in the four numbers every
    Bepu body and static has: SpringFrequency, SpringDampingRatio, FrictionCoefficient and
    MaximumRecoveryVelocity. The defaults do not bounce at all; no damping and a low frequency do.
    Of the two springs that meet, only the one with the higher MaximumRecoveryVelocity is used, which
    two lanes show with the same pad. Friction is the two coefficients multiplied. The overlay prints
    the rebound and the slide each lane measured.
  cs: |-
    Jak udělat něco skákavého a jak něco kluzkého. Sedm stejných míčů je upuštěno a šest stejných
    krabic postrčeno; dráhy se liší jen čtyřmi čísly, která má každé těleso i statický objekt v Bepu:
    SpringFrequency, SpringDampingRatio, FrictionCoefficient a MaximumRecoveryVelocity. S výchozími
    hodnotami se neodrazí nic; bez tlumení a s nízkou frekvencí ano. Ze dvou pružin, které se potkají, se
    použije jen ta s vyšší MaximumRecoveryVelocity, což ukazují dvě dráhy se stejnou podložkou. Tření
    je součin obou koeficientů. Překryv vypisuje odraz a dráhu skluzu naměřené v každé dráze.
concepts:
  - SpringDampingRatio - why the defaults do not bounce, and what lowering it does
  - SpringFrequency - a lower frequency bounces higher, and is softer
  - Which of two springs a contact uses - the side with the higher MaximumRecoveryVelocity
  - FrictionCoefficient - a pair's friction is the product of the two
  - Setting the four numbers on a BodyComponent and on a StaticComponent
  - Replaying a scene - Teleport, then Awake, then the velocities
  - Measuring a rebound from a body's LinearVelocity, which cannot miss a short contact
  - "Using helpers: SetupBase3DScene, Create3DPrimitive, AddEntityTextRenderer, DebugOverlay"
tags:
  - 3D
  - Physics
  - Bepu
  - Friction
  - Bounce
related:
  - E05_3D_CubeFountain
  - E05_3D_Grabber
  - E05_3D_Raycast
screenshotFrame: 85
enabled: true
created: 2026-10-03
---
*/