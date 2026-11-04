using Stride.BepuPhysics;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

// Debris that ignores itself. A burst of a few hundred fragments lands in a walled pit; what each
// fragment must collide with is the pit and the floor, and what it need not collide with is the other
// fragments. Fragments on a layer the collision matrix does not pair with itself pass through each
// other and settle in one flat layer on the floor; with the pair on, they pile up on each other, and
// the solver pays for every contact between them. The overlay counts those contacts and the active
// bodies, and the profiler in the corner shows the step time move with them.
//
// This is the first reason to filter: not a rule of the game but a saving, for things whose mutual
// collisions nobody would see. E05_3D_CollisionLayer is the example for the matrix itself.
//
// Space fires the burst again, F flips the debris-against-debris pair and fires again, and 1 to 3
// pick how many fragments there are.

const CollisionLayer SceneryLayer = CollisionLayer.Layer1;
const CollisionLayer DebrisLayer = CollisionLayer.Layer2;
const float PitHalfWidth = 6f;
const float WallHeight = 2.5f;
const float FragmentSize = 0.35f;
const float BurstHeight = 5f;
const float BurstSpeed = 4f;

int[] countChoices = [150, 400, 800, 1600];
string[] countKeys = [.. countChoices.Select((_, i) => $"{i + 1}")];
var countChoice = 1;
var selfCollision = false;

var fragments = new List<Entity>();
var random = new Random(11);

BepuSimulation simulation = null!;
Material fragmentMaterial = null!;

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    Camera = CameraSetup.Perspective(position: new Vector3(0f, 10f, 15f), rotation: new Vector3(0f, -32f, 0f)).WithController(),
    WindowTitle = "Debris filtering - Stride Community Toolkit",
    Resizable = true,
    DpiAware = true,
    Profiler = true,
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    simulation = game.GetCameraEntity().GetSimulation();
    fragmentMaterial = game.CreateMaterial(new Color(200, 120, 70), metalness: 0f, glossiness: 0.4f);

    // The ground the setup made is on the default layer; the pit's walls go on the scenery layer with it
    BuildPit(scene);
    ApplyMatrix();
    Burst(scene);

    TextOverlay.GetOrCreate(game).AddSection("Debris filtering", BuildOverlayLines);
}

void Update(Scene scene, GameTime time)
{
    var input = game.Input;

    if (input.IsKeyPressed(Keys.Space)) Burst(scene);

    if (input.IsKeyPressed(Keys.F))
    {
        selfCollision = !selfCollision;
        ApplyMatrix();
        Burst(scene);
    }

    for (var i = 0; i < countChoices.Length; i++)
    {
        if (input.IsKeyPressed(Keys.D1 + i) || input.IsKeyPressed(Keys.NumPad1 + i))
        {
            countChoice = i;
            Burst(scene);
        }
    }
}

// One pair of the matrix is the whole lesson: whether the debris layer collides with itself
void ApplyMatrix() => simulation.CollisionMatrix.Set(DebrisLayer, DebrisLayer, selfCollision);

void BuildPit(Scene scene)
{
    var wallMaterial = game.CreateMaterial(new Color(120, 125, 135), metalness: 0f, glossiness: 0.3f);

    for (var side = 0; side < 4; side++)
    {
        var alongX = side < 2;
        var sign = side % 2 == 0 ? 1f : -1f;
        var size = alongX ? new Vector3(0.3f, WallHeight, PitHalfWidth * 2f) : new Vector3(PitHalfWidth * 2f, WallHeight, 0.3f);
        var position = alongX ? new Vector3(sign * PitHalfWidth, WallHeight / 2f, 0f) : new Vector3(0f, WallHeight / 2f, sign * PitHalfWidth);

        var wall = game.CreatePrimitive(Primitive3D.Box(size), new BepuPhysicsOptions
        {
            EntityName = "Wall",
            Body = BodyMode.Static,
            Material = wallMaterial,
            Position = position,
        });

        wall.Get<StaticComponent>().CollisionLayer = SceneryLayer;
        wall.Scene = scene;
    }
}

/// <summary>The fragments again from nothing: a tight cloud above the pit, each thrown outwards and up.</summary>
void Burst(Scene scene)
{
    foreach (var fragment in fragments) fragment.Scene = null;

    fragments.Clear();

    for (var i = 0; i < countChoices[countChoice]; i++)
    {
        var direction = Vector3.Normalize(new Vector3(random.NextSingle() - 0.5f, 0.4f + random.NextSingle() * 0.6f, random.NextSingle() - 0.5f));

        // Every fragment draws the one shared model
        var fragment = game.CreatePrimitive(Primitive3D.Cube(FragmentSize), new BepuPhysicsOptions
        {
            EntityName = "Fragment",
            Material = fragmentMaterial,
            ShareModel = true,
            // A shell above the pit rather than a ball of overlapping cubes: fragments that start inside each other
            // cost the first step dearly, and with the pair on it shows as a stall before the burst
            Position = new Vector3(0f, BurstHeight, 0f) + direction * (1f + random.NextSingle() * 2f),
        });

        var body = fragment.Get<BodyComponent>();

        // The layer is set before the entity joins the scene, so the body is filtered from its first step
        body.CollisionLayer = DebrisLayer;
        fragment.Scene = scene;

        body.LinearVelocity = direction * (BurstSpeed * (0.5f + random.NextSingle()));
        body.AngularVelocity = new Vector3(random.NextSingle() - 0.5f, random.NextSingle() - 0.5f, random.NextSingle() - 0.5f) * 6f;

        fragments.Add(fragment);
    }
}

IReadOnlyList<TextElement> BuildOverlayLines()
{
    // Read from Bepu's own simulation: every constraint here is a contact, so the count is the contacts the solver works through
    var bepu = simulation.Simulation;
    var contacts = bepu.Solver.CountConstraints();
    var active = bepu.Bodies.ActiveSet.Count;

    return
    [
        new("Space", "Fire the burst again", Color.Gold),
        new("F", selfCollision ? "Debris collides with debris: on" : "Debris collides with debris: off", selfCollision ? Color.OrangeRed : Color.LightGreen),
        new(countKeys, $"Fragments: {countChoices[countChoice]}", Color.Gold),
        new(""),
        new($"Contact constraints in the solver: {contacts}", Color.White),
        new($"Awake bodies: {active}", Color.White),
    ];
}

/*
---example-metadata
slug: debris-filtering
title:
  en: Debris Filtering
level: Intermediate
category: Physics
complexity: 2
order: 83
description:
  en: |-
    A burst of a few hundred fragments lands in a walled pit. On a layer the collision matrix does
    not pair with itself the fragments pass through each other and settle in one flat layer; with
    the pair on they pile up, and the solver pays for every contact between them. The overlay counts
    the contact constraints and the awake bodies, and the profiler shows the step time move with
    them: the first reason to filter is a saving, for collisions nobody would see.
concepts:
  - One pair of the collision matrix, a layer against itself, flipped at runtime
  - Reading the contact constraint and active body counts from the Bepu simulation
  - The step time in the profiler as the cost of contacts
  - One shared model for every fragment, through ShareModel
  - "Using helpers: SceneSetup3D, UseBepu, UseSkybox, CreatePrimitive, CreateMaterial, GetSimulation, TextOverlay"
tags:
  - 3D
  - Physics
  - Bepu
  - Collision filtering
  - Performance
related:
  - E05_3D_CollisionLayer
  - E05_3D_CollisionGroup
  - E05_3D_CubeFountain
screenshotFrame: 240
enabled: true
created: 2026-11-01
---
*/