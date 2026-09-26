using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Shapes;
using Stride.CommunityToolkit.Windows;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;

// A faithful replica of the Box2D.NET sample Benchmarks/BenchmarkJunkyard (MIT, (c) 2022 Erin Catto,
// (c) 2025 Choi Ikpil): a walled yard whose
// floor and walls are rows of overlapping static squares, 8,000 small five-sided "rocks" raining
// into it, and a kinematic pusher plowing back and forth through the pile at x = 60*sin(0.2t).
//
// Rendering works exactly like the Box2D testbed: no meshes, no materials, no entities - every
// shape is submitted each frame to the toolkit's ShapeBatch, whose shader (a port of the
// testbed's solid_polygon shader) draws all of them in one instanced call, computing the 60%-alpha
// fill and the pixel-constant border per fragment. Zoom and resize cost nothing; overlapping
// shapes blend through each other; body states show as the testbed's colours - pale green statics,
// pink awake, salmon fast-movers, gray sleepers, royal blue pusher - on its dark gray background.

// --- the sample's numbers, verbatim
const float GridSize = 1.0f;
const int ColumnCount = 200;
const int RowCount = 40;
const float Radius = 0.25f;
const float YStart = 15.0f;

// The testbed paints continuous-collision candidates salmon: bodies sweeping more than roughly half
// their extent in one step. Approximated here as speed > 0.5 * radius / timestep.
const float FastSpeed = 0.5f * Radius * 60f;

// --- the testbed palette (b2HexColor values used by b2World_Draw, and the samples' GL clear colour)
var paleGreen = new Color(0x98, 0xFB, 0x98);
var pink = new Color(0xFF, 0xC0, 0xCB);
var salmon = new Color(0xFA, 0x80, 0x72);
var gray = new Color(0x80, 0x80, 0x80);
var royalBlue = new Color(0x41, 0x69, 0xE1);
var background = new Color(0.2f, 0.2f, 0.2f);

Box2DSimulation? simulation = null;
ShapeBatch? shapeBatch = null;

// Bodies are entity-less: physics is the single source of truth and the debug draw reads it
// directly every frame, exactly like the testbed
List<B2BodyId> rockIds = [];
B2BodyId pusherId = default;

// The five-sided rock outline: the sample places five points on a circle by the Fibonacci sphere
// algorithm and takes their convex hull. The hull sorts them; the shader wants them sorted too.
var pentagon = FibonacciPentagon(Radius);

// Static square outlines, reused for every floor and wall submission
var floorSquare = RectangleVertices(0.55f * GridSize, 0.5f * GridSize);
var wallSquare = RectangleVertices(0.5f * GridSize, 0.55f * GridSize);

// The pusher plow: a 4 x 8 box whose shape sits 4 units above the body origin
Vector2[] pusherBoxVertices = [new(-2, 0), new(2, 0), new(2, 8), new(-2, 8)];

// Per-monitor DPI awareness, so a 150% display gets a sharp window rather than a stretched one;
// it has to happen before the window exists
WindowsDpiManager.EnablePerMonitorV2();

using var game = new Game();

game.Run(start: Start, update: Update);

simulation?.Dispose();

void Start(Scene rootScene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Junkyard Box2D Example - Stride Community Toolkit";

    game.SetupBase2D(clearColor: background);
    game.Add2DCameraController();
    game.AddProfiler();

    // The sample's viewport: camera centered on (8, 25), zoom 60 - which in the testbed means the
    // visible world is 60 units tall. The camera controller adopts this size and scales it when the
    // mouse wheel zooms; the shader keeps borders pixel-constant at any zoom on its own.
    var camera = rootScene.GetCamera() ?? throw new InvalidOperationException("Camera not found in scene");
    camera.Entity.Transform.Position = new Vector3(8, 25, 50);
    camera.OrthographicSize = 60;

    shapeBatch = game.AddShapeBatch();
    shapeBatch.BorderWidth = 1f;
    shapeBatch.Fill.Alpha = 0.4f;

    simulation = new Box2DSimulation();

    // The testbed steps the world exactly once per rendered frame - no catch-up. This scene is the
    // heaviest benchmark in the sample suite, and letting the accumulator run three catch-up steps
    // per frame would only deepen the slow motion it plays in on a loaded machine.
    simulation.MaxStepsPerFrame = 1;

    // Left mouse picks a rock up and throws it - the one interaction the replica adds to the testbed
    game.GetCameraEntity().Add(new Grabber2DScript { Simulation = simulation });

    DebugOverlay.GetOrCreate(game).AddSection("Junkyard", static () =>
    [
        new("Left mouse", "Pick a rock up, carry it, throw it", Color.Yellow),
    ]);

    CreateGround();
    SpawnRocks();
    CreatePusher();
}

/// <summary>
/// One static body carrying every floor and wall shape: 161 slightly overlapping squares across the
/// bottom and 50 up each side wall at x = -80 and x = +80, exactly as the sample builds them.
/// </summary>
void CreateGround()
{
    var groundId = simulation!.CreateStaticBody(Vector3.Zero);
    var shapeDef = ShapeFixtureBuilder.CreateDefaultShapeDef();

    var y = 0.0f;
    var x = -80.0f * GridSize;

    for (var i = 0; i < 161; ++i)
    {
        var box = b2MakeOffsetBox(0.55f * GridSize, 0.5f * GridSize, new B2Vec2(x, y), b2Rot_identity);
        b2CreatePolygonShape(groundId, in shapeDef, in box);
        x += GridSize;
    }

    y = GridSize;
    x = -80.0f * GridSize;

    for (var i = 0; i < 50; ++i)
    {
        var box = b2MakeOffsetBox(0.5f * GridSize, 0.55f * GridSize, new B2Vec2(x, y), b2Rot_identity);
        b2CreatePolygonShape(groundId, in shapeDef, in box);
        y += GridSize;
    }

    y = GridSize;
    x = 80.0f * GridSize;

    for (var i = 0; i < 50; ++i)
    {
        var box = b2MakeOffsetBox(0.5f * GridSize, 0.55f * GridSize, new B2Vec2(x, y), b2Rot_identity);
        b2CreatePolygonShape(groundId, in shapeDef, in box);
        y += GridSize;
    }
}

/// <summary>
/// The junk: 200 columns x 40 rows of five-sided rocks stacked from y = 15 upward, each column
/// zig-zagged sideways by the alternating 0.1 offset, exactly as the sample spawns them.
/// </summary>
void SpawnRocks()
{
    var side = -0.1f;

    for (var i = 0; i < ColumnCount; ++i)
    {
        var x = 1.5f * (2.0f * i - ColumnCount) * Radius;

        for (var j = 0; j < RowCount; ++j)
        {
            var y = 4.0f * j * Radius + YStart;
            var position = new Vector3(x + side, y, 0);
            side = -side;

            var bodyId = simulation!.CreateDynamicBody(position);

            ShapeFixtureBuilder.AttachPolygon(pentagon, bodyId);

            rockIds.Add(bodyId);
        }
    }
}

/// <summary>
/// The pusher: a kinematic 4 x 8 plow whose shape sits 4 units above its body origin. Its sweep is
/// driven per fixed step by <see cref="PusherDriver"/> through the simulation update hook, exactly
/// like the sample's StepJunkyard.
/// </summary>
void CreatePusher()
{
    pusherId = simulation!.CreateKinematicBody(Vector3.Zero);

    var shapeDef = ShapeFixtureBuilder.CreateDefaultShapeDef();
    var pusherBox = b2MakeOffsetBox(2.0f, 4.0f, new B2Vec2(0.0f, 4.0f), b2Rot_identity);
    b2CreatePolygonShape(pusherId, in shapeDef, in pusherBox);

    simulation.RegisterSimulationUpdate(new PusherDriver(pusherId));
}

void Update(Scene rootScene, GameTime time)
{
    // Box2D is stepped by hand: fixed-timestep accumulation and the pusher drive happen inside
    simulation?.Update(time.Elapsed);

    SubmitShapes();
}

/// <summary>
/// Submits every shape for this frame straight from the physics world, in the testbed's draw order:
/// the static yard first, then the rocks coloured by their state, the pusher last on top.
/// </summary>
void SubmitShapes()
{
    if (shapeBatch is null) return;

    var x = -80.0f * GridSize;

    for (var i = 0; i < 161; ++i)
    {
        shapeBatch.DrawSolidPolygon(floorSquare, new Vector2(x, 0f), 0f, paleGreen);
        x += GridSize;
    }

    var y = GridSize;

    for (var i = 0; i < 50; ++i)
    {
        shapeBatch.DrawSolidPolygon(wallSquare, new Vector2(-80.0f * GridSize, y), 0f, paleGreen);
        shapeBatch.DrawSolidPolygon(wallSquare, new Vector2(80.0f * GridSize, y), 0f, paleGreen);
        y += GridSize;
    }

    foreach (var bodyId in rockIds)
    {
        var transform = b2Body_GetTransform(bodyId);

        Color color;

        if (!b2Body_IsAwake(bodyId))
        {
            color = gray;
        }
        else
        {
            var velocity = b2Body_GetLinearVelocity(bodyId);
            var fast = velocity.X * velocity.X + velocity.Y * velocity.Y > FastSpeed * FastSpeed;

            color = fast ? salmon : pink;
        }

        SubmitBodyPolygon(pentagon, transform, color);
    }

    SubmitBodyPolygon(pusherBoxVertices, b2Body_GetTransform(pusherId), royalBlue);
}

void SubmitBodyPolygon(Vector2[] vertices, B2Transform transform, Color color)
{
    // The instance transform is the body's, in the same (x, y, cos, sin) form the testbed uses
    shapeBatch!.DrawSolidPolygon(vertices, new Vector2(transform.p.X, transform.p.Y), MathF.Atan2(transform.q.s, transform.q.c), color);
}

static Vector2[] RectangleVertices(float halfWidth, float halfHeight) =>
[
    new(-halfWidth, -halfHeight),
    new(halfWidth, -halfHeight),
    new(halfWidth, halfHeight),
    new(-halfWidth, halfHeight),
];

/// <summary>
/// The sample's rock outline: five points on a circle of the given radius by the Fibonacci sphere
/// algorithm, sorted by angle so the convex hull and the shader's SDF see them in CCW order.
/// </summary>
static Vector2[] FibonacciPentagon(float radius)
{
    var phi = MathF.PI * (MathF.Sqrt(5.0f) - 1.0f);
    var points = new Vector2[5];

    for (var i = 0; i < 5; ++i)
    {
        var theta = phi * i;
        points[i] = new Vector2(radius * MathF.Cos(theta), radius * MathF.Sin(theta));
    }

    return [.. points.OrderBy(p => MathF.Atan2(p.Y, p.X))];
}

/// <summary>
/// Drives the kinematic pusher along the sample's sweep, x = 60*sin(0.2t), by setting its target
/// transform once per fixed step - Box2D derives the kinematic velocity from target and time step.
/// </summary>
public sealed class PusherDriver : IBox2DSimulationUpdate
{
    private readonly B2BodyId _pusherId;
    private int _stepCount;

    public PusherDriver(B2BodyId pusherId) => _pusherId = pusherId;

    public void SimulationUpdate(Box2DSimulation simulation, float deltaTime)
    {
        var time = deltaTime * _stepCount;
        _stepCount++;

        var cosSin = b2ComputeCosSin(0.2f * time);
        var target = new B2Transform(new B2Vec2(60.0f * cosSin.sine, 0.0f), b2Rot_identity);

        b2Body_SetTargetTransform(_pusherId, in target, deltaTime, true);
    }

    public void AfterSimulationUpdate(Box2DSimulation simulation, float deltaTime)
    {
    }
}

/*
---example-metadata
slug: junkyard-box2d
title:
  en: Junkyard (Box2D)
  cs: Vrakoviště (Box2D)
level: Advanced
category: Performance
complexity: 4
order: 62
description:
  en: |-
    A faithful replica of the Box2D.NET BenchmarkJunkyard sample: 8,000 small five-sided rocks rain
    into a walled yard and a kinematic plow sweeps back and forth through the pile, driven by a
    target transform once per fixed step. Rendering works exactly like the Box2D testbed: no meshes,
    materials or entities - every shape is submitted each frame to the toolkit's ShapeBatch,
    whose shader (a port of the testbed's solid_polygon shader) draws them all in one instanced
    call with the 60%-alpha fill and pixel-constant border computed per fragment. Body states show
    as the testbed's colours - pink awake, salmon fast-movers, gray sleepers.
  cs: |-
    Věrná replika ukázky BenchmarkJunkyard z Box2D.NET: 8 000 malých pětiúhelníkových kamenů prší
    do ohrazeného dvora a kinematická radlice se prohrnuje hromadou tam a zpět. Vykreslování funguje
    přesně jako Box2D testbed: žádné meshe, materiály ani entity - každý tvar se každý snímek předá
    do ShapeBatch, jehož shader (port testbed shaderu solid_polygon) je vykreslí jedním
    instancovaným voláním s výplní o 60% průhlednosti a okrajem stálé šířky v pixelech.
concepts:
  - Replicating a Box2D testbed benchmark scene in Stride, rendering included
  - Immediate-mode shape drawing with ShapeBatch - no meshes, materials or entities
  - An SDF shader computing fill, border and transparency per fragment, stable under any zoom
  - Entity-less physics bodies as the single source of truth, read directly each frame
  - Driving a kinematic body with SetTargetTransform once per fixed step
  - Hooking per-fixed-step logic through IBox2DSimulationUpdate
  - Colour-coding awake, fast and sleeping bodies straight from body state
tags:
  - 2D
  - Box2D
  - Physics
  - Shader
  - Performance
  - Kinematic
  - Third Party
related:
  - E10_2D_StressPile_Box2D
  - E06_Box2D
enabled: true
screenshotFrame: 600
created: 2026-08-31
---
*/