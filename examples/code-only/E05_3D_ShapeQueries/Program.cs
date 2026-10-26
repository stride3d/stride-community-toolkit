using BepuPhysics.Collidables;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.DebugShapes;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

// Two questions a raycast cannot answer: what is inside this volume right now, and what would this
// shape touch first if it moved that way. Bepu answers both for any convex shape. Overlap tests a shape
// at a pose against everything in the simulation; SweepCast throws a shape along a velocity and
// reports the first contact, with the point, the normal and how far along the path it happened. Like
// a raycast, neither moves anything: they are questions.
//
// A probe box follows the mouse over a field of bodies and lists what it holds; a left click tosses
// them, which proves the list. From a launcher on the left a ball is swept towards the mouse, and a
// ghost shows where it would first touch; T throws a real ball along the same line, without gravity,
// and it stops where the ghost was. A sweep is a straight line: gravity is the caller's to add.

WindowsDpiManager.EnablePerMonitorV2();

const float ProbeHeight = 1.5f;
const float BallRadius = 0.35f;
const float ThrowSpeed = 14f;
const float SweepReach = 16f;       // In metres, because the sweep's velocity is a unit direction
const int ThrownKept = 12;

var probeSize = new Vector3(2.2f, ProbeHeight, 2.2f);
var launcher = new Vector3(-8f, 1.2f, 0f);
var aim = new Vector3(0.5f, 0f, 1.6f);  // Where the probe sits and the sweep points until the mouse moves

List<FieldBody> bodies = [];
List<BodyComponent> inside = [];
List<Entity> thrown = [];
var probeLine = "Probe holds nothing";
var sweepLine = "Sweep: nothing within 16 m";

CameraComponent camera = null!;
BepuSimulation? simulation = null;
ImmediateDebugRenderSystem debugDraw = null!;
Material highlight = null!;
Material ballMaterial = null!;

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    // Above and in front of the field, shifted right so the launcher on the left clears the overlay
    Camera = CameraSetup.Perspective(position: new Vector3(0f, 11f, 15.5f), rotation: new Vector3(0f, -34f, 0f)).WithController(),
    Profiler = true
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Shape queries - Stride Community Toolkit";

    debugDraw = game.AddDebugShapes();
    debugDraw.Visible = true;

    game.AddPhysicsDebug();

    camera = game.GetCameraEntity().Get<CameraComponent>();
    highlight = game.CreateMaterial(new Color(255, 170, 40), metalness: 0f, glossiness: 0.5f);
    ballMaterial = game.CreateMaterial(new Color(80, 190, 255), metalness: 0f, glossiness: 0.6f);

    BuildField(scene);
    BuildLauncher(scene);

    TextOverlay.GetOrCreate(game).AddSection("Shape queries", BuildOverlayLines);
}

void Update(Scene scene, GameTime time)
{
    // The bodies hand over their simulation once they are attached, which takes a frame
    simulation ??= bodies[0].Body.Simulation;

    if (simulation is null) return;

    if (game.Input.IsKeyPressed(Keys.R)) Reset();

    FollowMouse();
    ProbeForOverlaps();
    SweepTowardsAim(scene);
}

/// <summary>The point on the ground under the mouse, once the mouse has moved; the probe and the sweep both aim there.</summary>
void FollowMouse()
{
    if (game.Input.MouseDelta == Vector2.Zero) return;

    var ray = camera.GetPickRay(game.Input.MousePosition);
    var ground = new Plane(Vector3.UnitY, 0f);

    if (ray.Intersects(ref ground, out Vector3 point)) aim = point;
}

/// <summary>
/// Overlap: a box at a pose, and every collidable it intersects comes back. The buffer overload writes
/// into stack memory and allocates nothing, so it can run every frame; the collection overload appends
/// <c>OverlapInfo</c> records with the penetration direction and depth of each.
/// </summary>
void ProbeForOverlaps()
{
    // A hair above the ground, so the ground plane itself is not in the list
    var centre = new Vector3(aim.X, ProbeHeight * 0.5f + 0.02f, aim.Z);

    Span<CollidableStack> buffer = stackalloc CollidableStack[32];

    inside.Clear();

    foreach (var collidable in simulation!.Overlap(new Box(probeSize.X, probeSize.Y, probeSize.Z), new RigidPose(centre, Quaternion.Identity), buffer))
    {
        if (collidable is BodyComponent body)
        {
            inside.Add(body);
        }
    }

    foreach (var field in bodies)
    {
        var model = field.Body.Entity.Get<ModelComponent>();

        // A material override while the body is inside; removing it shows the model's own again
        if (inside.Contains(field.Body))
        {
            model.Materials[0] = highlight;
        }
        else
        {
            model.Materials.Remove(0);
        }
    }

    debugDraw.DrawCube(centre, probeSize, color: inside.Count > 0 ? new Color(255, 170, 40) : Color.Gold);

    // The first three names, and how many more there are
    var shown = Math.Min(inside.Count, 3);
    var names = new string[shown];

    for (var i = 0; i < shown; i++) names[i] = inside[i].Entity.Name;

    probeLine = shown == 0 ? "Probe holds nothing" : $"Probe holds: {string.Join(", ", names)}{(inside.Count > shown ? $" +{inside.Count - shown}" : "")}";

    if (game.Input.IsMouseButtonPressed(MouseButton.Left))
    {
        foreach (var body in inside)
        {
            body.Awake = true;
            body.LinearVelocity = new Vector3(0f, 7f, 0f);
        }
    }
}

/// <summary>
/// SweepCast: a shape thrown from a pose along a velocity, and the first thing it would touch comes
/// back as a hit with the point, the normal and the distance along the path. The velocity here is a
/// unit direction, so the distance and the reach are in metres; with a real velocity they are seconds.
/// </summary>
void SweepTowardsAim(Scene scene)
{
    var direction = new Vector3(aim.X, launcher.Y, aim.Z) - launcher;

    if (direction.LengthSquared() < 1e-4f) return;

    direction.Normalize();

    var found = simulation!.SweepCast(new Sphere(BallRadius), new RigidPose(launcher, Quaternion.Identity), new BodyVelocity(direction, Vector3.Zero), SweepReach, out var hit);
    var touch = launcher + direction * (found ? hit.Distance : SweepReach);
    var colour = found ? Color.DeepSkyBlue : Color.Gray;

    debugDraw.DrawLine(launcher, touch, colour);
    debugDraw.DrawSphere(touch, BallRadius, colour);

    if (found)
    {
        debugDraw.DrawArrow(hit.Point, hit.Point + hit.Normal * 0.6f, color: Color.Red);

        sweepLine = $"Sweep: {hit.Collidable.Entity.Name} after {hit.Distance:0.00} m";
    }
    else
    {
        sweepLine = $"Sweep: nothing within {SweepReach:0} m";
    }

    if (game.Input.IsKeyPressed(Keys.T))
    {
        Throw(scene, direction);
    }
}

/// <summary>A real ball along the sweep's line, with no gravity so it follows the straight path the sweep tested.</summary>
void Throw(Scene scene, Vector3 direction)
{
    // Per-body gravity is honoured only once the simulation is told to look at per-body settings
    simulation!.UsePerBodyAttributes = true;

    var ball = game.CreatePrimitive(Primitive3D.Sphere(BallRadius), new BepuPhysicsOptions
    {
        EntityName = $"Ball {thrown.Count + 1}",
        Material = ballMaterial,
        Component = new BodyComponent { Collider = new CompoundCollider(), InterpolationMode = InterpolationMode.Interpolated, Gravity = false },
        Position = launcher,
    });

    ball.Scene = scene;
    ball.Get<BodyComponent>().LinearVelocity = direction * ThrowSpeed;

    thrown.Add(ball);

    if (thrown.Count > ThrownKept)
    {
        thrown[0].Scene = null;
        thrown.RemoveAt(0);
    }
}

/// <summary>Fourteen bodies in two rows, and two pillars, one of them on the line the sweep starts out on.</summary>
void BuildField(Scene scene)
{
    var random = new Random(3);
    Color[] colours = [new(200, 90, 80), new(90, 170, 110), new(120, 120, 220)];

    for (var i = 0; i < 14; i++)
    {
        var column = i % 7;
        var row = i / 7;
        var position = new Vector3(-5f + column * 1.8f + random.NextSingle() * 0.6f, 1.5f, (row - 0.5f) * 3.2f + random.NextSingle() * 0.8f);

        var (shape, name) = (i % 3) switch
        {
            0 => (Primitive3D.Cube(0.8f), "Cube"),
            1 => (Primitive3D.Sphere(0.45f), "Sphere"),
            _ => (Primitive3D.Capsule(0.3f, 0.6f), "Capsule"),
        };

        var entity = game.CreatePrimitive(shape, new BepuPhysicsOptions
        {
            EntityName = $"{name} {i + 1}",
            Material = game.CreateMaterial(colours[i % colours.Length], metalness: 0f, glossiness: 0.4f),
            Position = position,
        });

        entity.Transform.Rotation = Quaternion.RotationY(random.NextSingle() * MathUtil.TwoPi);
        entity.Scene = scene;

        bodies.Add(new FieldBody(entity.Get<BodyComponent>(), position, entity.Transform.Rotation));
    }

    foreach (var (x, z, number) in new[] { (4.5f, 0f, 1), (-2.5f, -4f, 2) })
    {
        var pillar = game.CreatePrimitive(Primitive3D.Box(new Vector3(1f, 3f, 1f)), new BepuPhysicsOptions
        {
            EntityName = $"Pillar {number}",
            Material = game.CreateMaterial(new Color(150, 150, 160), metalness: 0f, glossiness: 0.3f),
            Body = BodyMode.Static,
            Position = new Vector3(x, 1.5f, z),
        });

        pillar.Scene = scene;
    }
}

/// <summary>The launcher is only a marker: it has no collider, or every sweep would start inside it.</summary>
void BuildLauncher(Scene scene)
{
    var marker = game.CreatePrimitive(Primitive3D.Sphere(BallRadius), new BepuPhysicsOptions
    {
        EntityName = "Launcher",
        Material = ballMaterial,
        Body = BodyMode.None,
        Position = launcher,
    });

    marker.Scene = scene;

    var label = new Entity("Label")
    {
        new EntityTextComponent { Text = "Launcher", FontSize = 14, Anchor = TextAnchor.MiddleCenter, EnableShadow = true },
    };

    label.Transform.Position = launcher - new Vector3(0f, 1.2f, 0f);
    label.Scene = scene;
}

/// <summary>Every body back where it started, and the thrown balls gone.</summary>
void Reset()
{
    foreach (var field in bodies)
    {
        // Teleport moves the body and nothing else: it keeps its velocity, its spin and its sleep
        field.Body.Teleport(field.Start, field.Rotation);
        field.Body.Awake = true;
        field.Body.LinearVelocity = Vector3.Zero;
        field.Body.AngularVelocity = Vector3.Zero;
    }

    foreach (var ball in thrown) ball.Scene = null;

    thrown.Clear();
}

IReadOnlyList<TextElement> BuildOverlayLines() =>
[
    new("Mouse", "Move the probe and aim the sweep", Color.Gold),
    new("Left click", "Toss what the probe holds", Color.Gold),
    new("T", "Throw a ball along the sweep", Color.Gold),
    new("R", "Put everything back", Color.Gold),
    new("P", "Draw the collider gizmos", Color.Gold),
    new(""),
    new(probeLine, new Color(255, 170, 40)),
    new(sweepLine, Color.DeepSkyBlue),
    new($"Balls thrown: {thrown.Count}", Color.LightGray),
];

/// <summary>A body of the field and where it started, for the reset.</summary>
sealed record FieldBody(BodyComponent Body, Vector3 Start, Quaternion Rotation);

/*
---example-metadata
slug: shape-queries
title:
  en: Shape Queries
  cs: Dotazy tvarem
level: Intermediate
category: Physics
complexity: 5
order: 71
description:
  en: |-
    Two questions a raycast cannot answer: what is inside this volume right now, and what would this
    shape touch first if it moved that way. A probe box follows the mouse over a field of bodies and
    lists what it holds through Overlap; a click tosses them to prove the list. A ball is swept from a
    launcher towards the mouse through SweepCast, a ghost marks where it would first touch, and T throws
    a real ball along the same line, without gravity, to land where the ghost was.
  cs: |-
    Dvě otázky, na které raycast neodpoví: co je právě teď uvnitř tohoto objemu, a čeho by se tento
    tvar dotkl jako prvního, kdyby se pohnul tím směrem. Sonda ve tvaru kvádru sleduje myš nad polem
    těles a přes Overlap vypisuje, co obsahuje; kliknutí je vyhodí do vzduchu, aby seznam potvrdilo.
    Koule je přes SweepCast vržena od odpalovače směrem k myši, přízrak ukáže, kde by se dotkla jako
    první, a T hodí skutečnou kouli po stejné přímce, bez gravitace, aby přistála tam, kde byl přízrak.
concepts:
  - Overlap with a convex shape at a pose, into a stack buffer that allocates nothing per frame
  - SweepCast of a shape along a direction, and the hit's point, normal and distance along the path
  - A unit direction makes the sweep's reach and distance metres; a real velocity makes them seconds
  - A sweep is a straight line, so the thrown ball has gravity switched off per body
  - Turning the mouse into a point on the ground with the camera's pick ray and a plane
  - Highlighting a body with a material override, and removing it to restore the model's own
  - "Using helpers: SceneSetup3D, CreatePrimitive, AddDebugShapes, AddPhysicsDebug, TextOverlay"
tags:
  - 3D
  - Physics
  - Bepu
  - Overlap
  - Sweep
  - Queries
  - Debug Shapes
related:
  - E05_3D_Raycast
  - E05_3D_Grabber
  - E05_3D_MeshColliders
screenshotFrame: 150
enabled: true
created: 2026-10-17
---
*/