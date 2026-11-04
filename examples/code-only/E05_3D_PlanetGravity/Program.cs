using E05_3D_PlanetGravity;
using Stride.BepuPhysics;
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

// Gravity that is not "down". Bepu applies the simulation's PoseGravity to every body each step, and that
// is all gravity is: a velocity change per step. Here the simulation's gravity is zero, set through the
// setup's GameSettings, and a script that runs on every physics step adds the pull of a planet instead:
// towards its centre, falling off with the square of the distance, as a change to each body's velocity.
//
// Bodies launched sideways at the right speed orbit; too slow and they spiral in, too fast and they
// leave. Space launches a new ring at the speed the keys chose, and the trails show the paths.
//
// The pull is applied in SimulationUpdate, not in the frame's Update: a frame may run zero or several
// physics steps, and a force that must act once per step has to live where the steps are counted.
// E02_3D_GiveMeACube_SimulationUpdate is the smaller example of that.

const float PlanetRadius = 4f;
const float OrbitRadius = 9f;
const float OrbiterRadius = 0.22f;
const int OrbiterCount = 36;
const int TrailLength = 90;

// The planet's pull at the surface, in m/s², and how it scales: GM = g * R², so a = GM / r²
const float SurfaceGravity = 20f;
const float StandardGravityParameter = SurfaceGravity * PlanetRadius * PlanetRadius;

// Multiples of the circular orbit speed at the launch radius, which is the speed that makes a circle
float[] speedFactors = [0.7f, 1f, 1.25f];
string[] speedNames = ["too slow, spirals in", "circular", "fast, an ellipse out and back"];
var speedChoice = 1;
var trails = true;

var orbiters = new List<BodyComponent>();
var trailPoints = new List<Vector3[]>();
var trailHeads = new List<int>();
var trailScratch = new Vector3[TrailLength];
var gravity = new PlanetGravity { Centre = Vector3.Zero, StandardGravityParameter = StandardGravityParameter };

ShapeBatch? shapes = null;
Material? orbiterMaterial = null;

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    GameSettings = settings =>
    {
        // One simulation with no gravity of its own, so the planet's script is the only pull, and no damping: Bepu's
        // default takes a few percent of every body's speed per second, which brings an orbit down in half a minute
        var bepu = settings.GetOrCreateConfiguration<BepuConfiguration>();

        bepu.BepuSimulations.Add(new BepuSimulation { PoseGravity = Vector3.Zero, PoseLinearDamping = 0f, PoseAngularDamping = 0f });
    },
    Ground = null,
    Camera = CameraSetup.Perspective(position: new Vector3(0f, 22f, 26f), rotation: new Vector3(0f, -40f, 0f)).WithController(),
    WindowTitle = "Planet gravity - Stride Community Toolkit",
    Resizable = true,
    DpiAware = true,
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    shapes = game.AddShapeBatch();

    // The planet: a static sphere at the centre, which is also the point every orbiter is pulled towards
    var planet = game.CreatePrimitive(Primitive3D.Sphere(PlanetRadius, tessellation: 32), new BepuPhysicsOptions
    {
        EntityName = "Planet",
        Body = BodyMode.Static,
        Material = game.CreateMaterial(new Color(70, 110, 190), metalness: 0f, glossiness: 0.4f),
    });

    // The gravity script rides on the planet; it is a component, so the physics system calls it every step
    planet.Add(gravity);
    planet.Scene = scene;

    orbiterMaterial = game.CreateMaterial(new Color(255, 200, 90), metalness: 0.2f, glossiness: 0.6f);

    Launch(scene);

    TextOverlay.GetOrCreate(game).AddSection("Planet gravity", BuildOverlayLines);
}

void Update(Scene scene, GameTime time)
{
    var input = game.Input;

    if (input.IsKeyPressed(Keys.Space)) Launch(scene);
    if (input.IsKeyPressed(Keys.T)) trails = !trails;

    for (var i = 0; i < speedFactors.Length; i++)
    {
        if (input.IsKeyPressed(Keys.D1 + i) || input.IsKeyPressed(Keys.NumPad1 + i)) speedChoice = i;
    }

    RecordTrails();

    if (trails && shapes is not null) DrawTrails(shapes);
}

/// <summary>A ring of orbiters around the planet, each moving sideways at the chosen multiple of the circular speed.</summary>
void Launch(Scene scene)
{
    Clear();

    // v = sqrt(GM / r) is the speed of a circular orbit at radius r
    var circularSpeed = MathF.Sqrt(StandardGravityParameter / OrbitRadius);
    var speed = circularSpeed * speedFactors[speedChoice];

    for (var i = 0; i < OrbiterCount; i++)
    {
        var angle = i * MathUtil.TwoPi / OrbiterCount;
        var position = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle)) * OrbitRadius;
        var tangent = new Vector3(-MathF.Sin(angle), 0f, MathF.Cos(angle));

        // Every orbiter draws the one shared model: thirty-six bodies, one mesh
        var orbiter = game.CreatePrimitive(Primitive3D.Sphere(OrbiterRadius), new BepuPhysicsOptions
        {
            EntityName = $"Orbiter {i + 1}",
            Material = orbiterMaterial,
            ShareModel = true,
            Position = position,
        });

        orbiter.Scene = scene;

        var body = orbiter.Get<BodyComponent>();

        body.LinearVelocity = tangent * speed;
        orbiters.Add(body);
        gravity.Bodies.Add(body);

        var trail = new Vector3[TrailLength];

        Array.Fill(trail, position);
        trailPoints.Add(trail);
        trailHeads.Add(0);
    }
}

void Clear()
{
    foreach (var body in orbiters) body.Entity.Scene = null;

    orbiters.Clear();
    gravity.Bodies.Clear();
    trailPoints.Clear();
    trailHeads.Clear();
}

// Each trail is a ring buffer of the body's last positions, written once per frame
void RecordTrails()
{
    for (var i = 0; i < orbiters.Count; i++)
    {
        var head = trailHeads[i];

        trailPoints[i][head] = orbiters[i].Entity.Transform.Position;
        trailHeads[i] = (head + 1) % TrailLength;
    }
}

void DrawTrails(ShapeBatch shapes)
{
    for (var i = 0; i < orbiters.Count; i++)
    {
        var points = trailPoints[i];
        var head = trailHeads[i];

        // Unroll the ring into the scratch array, oldest first, so the polyline runs along the path
        for (var j = 0; j < TrailLength; j++)
        {
            trailScratch[j] = points[(head + j) % TrailLength];
        }

        shapes.DrawPixelPolyline(trailScratch, 1.5f, new Color(255, 220, 140, 140));
    }
}

string SpeedLine(int choice) => $"Launch {speedNames[choice]}{(choice == speedChoice ? " (chosen)" : "")}";

IReadOnlyList<TextElement> BuildOverlayLines() =>
[
    new("Space", "Launch a new ring", Color.Gold),
    new("1", SpeedLine(0), speedChoice == 0 ? Color.LightGreen : Color.Gold),
    new("2", SpeedLine(1), speedChoice == 1 ? Color.LightGreen : Color.Gold),
    new("3", SpeedLine(2), speedChoice == 2 ? Color.LightGreen : Color.Gold),
    new("T", trails ? "Trails on" : "Trails off", Color.Gold),
    new(""),
    new($"Surface gravity {SurfaceGravity:0} m/s², no gravity in the simulation itself", Color.White),
    new($"{orbiters.Count} orbiters pulled towards the centre every physics step", Color.White),
];

/*
---example-metadata
slug: planet-gravity
title:
  en: Planet Gravity
level: Intermediate
category: Physics
complexity: 2
order: 86
description:
  en: |-
    A ring of bodies orbiting a planet. The simulation's own gravity is zero, set through the setup's
    GameSettings, and a script that runs on every physics step pulls each body towards the planet's
    centre, falling off with the square of the distance. Three launch speeds give a circle, a spiral
    in and an ellipse, and trails show the paths; a frame runs zero or several physics steps, so a
    force per step lives in SimulationUpdate, not in Update.
concepts:
  - PoseGravity set to zero and PoseLinearDamping to zero for a simulation through the setup's GameSettings
  - A per-step pull applied in ISimulationUpdate as a velocity change on each body
  - The circular orbit speed from the gravitational parameter and the radius
  - One shared model for every orbiter, through ShareModel
  - "Using helpers: SceneSetup3D, UseBepu, UseSkybox, CreatePrimitive, CreateMaterial, ShapeBatch, TextOverlay"
tags:
  - 3D
  - Physics
  - Bepu
  - Gravity
  - SimulationUpdate
related:
  - E02_3D_GiveMeACube_SimulationUpdate
  - E05_3D_MultipleSimulations
  - E05_3D_TimeControl
screenshotFrame: 240
enabled: true
created: 2026-11-01
---
*/