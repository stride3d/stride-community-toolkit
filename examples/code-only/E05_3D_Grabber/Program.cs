using BepuPhysics;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

// A gravity gun: click any body to pick it up, carry it around on the end of the camera ray, and
// let go - with whatever velocity it had, so a flick throws it.
//
// The whole thing is one line: GrabberScript on the camera entity. Under it are two constraints,
// not a teleport. A linear servo pulls the grabbed point toward the cursor and an angular servo
// holds the orientation, so the solver stays in charge: the held body still collides, still pushes
// other bodies, and cannot be forced through the wall. The force caps scale with the body's mass,
// which is why the 100 kg cube drags exactly like the 1 kg one - and why it hits harder.
//
// The scene is built to be picked up: five cubes of the same size and very different masses, some
// spheres to flick, a body whose rotation is locked (the angular servo is skipped for it, and it
// slides without turning), and a wall to throw things at.

const float SpawnHeight = 0.5f;

var masses = new[] { 1f, 3f, 10f, 30f, 100f };
var spawned = new List<Entity>();

GrabberScript? grabber = null;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddProfiler();

    game.SetCameraPosition(new Vector3(0, 3.5f, -9));
    game.SetCameraRotation(new Vector3(180, -14, 0));

    // The one line. Left mouse grabs, the wheel changes the carry distance, T + mouse turns the body.
    grabber = game.AddGrabber();

    BuildScene(scene);
    AddInstructions();
}

void Update(Scene scene, Stride.Games.GameTime time)
{
    if (game.Input.IsKeyPressed(Keys.R))
    {
        grabber?.Release();

        foreach (var entity in spawned)
            entity.Scene = null;

        spawned.Clear();
        BuildScene(scene);
    }
}

void BuildScene(Scene scene)
{
    // Five cubes, same size, masses from 1 to 100 kg. The mass is the collider's; the body sums its colliders.
    for (var i = 0; i < masses.Length; i++)
    {
        var shade = (byte)(230 - i * 35);
        var cube = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
        {
            Material = game.CreateMaterial(new Color(shade, (byte)(120 + i * 20), (byte)(80 + i * 30))),
            Component = new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider { Mass = masses[i] } } } },
            Position = new Vector3(3 - i * 1.5f, SpawnHeight, 0),
        });
        cube.Name = $"{masses[i]:0} kg";
        Place(cube, scene);
    }

    // Spheres to flick.
    for (var i = 0; i < 4; i++)
    {
        var sphere = game.Create3DPrimitive(PrimitiveModelType.Sphere, new()
        {
            Size = new Vector3(0.35f),
            Material = game.CreateMaterial(new Color(120, 200, 255)),
            Position = new Vector3(2.5f - i * 1.7f, SpawnHeight, 3),
        });
        sphere.Name = "ball";
        Place(sphere, scene);
    }

    // A capsule whose rotation is locked once it is in the simulation: it can be dragged, never turned.
    var lockedBody = game.Create3DPrimitive(PrimitiveModelType.Capsule, new()
    {
        Material = game.CreateMaterial(new Color(255, 200, 80)),
        Position = new Vector3(-4.5f, 1, 1.5f),
    });
    lockedBody.Name = "locked rotation";
    Place(lockedBody, scene);

    var locked = lockedBody.Get<BodyComponent>();
    var inertia = locked.BodyInertia;
    locked.BodyInertia = new BodyInertia { InverseMass = inertia.InverseMass };   // zero inverse inertia: rotation locked

    // A wall to throw at.
    var wall = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
    {
        Size = new Vector3(8, 3, 0.4f),
        Material = game.CreateMaterial(new Color(140, 140, 150)),
        // An empty compound: the helper adds the box that fits the wall's size. A box given here
        // would be kept as the wall's collider, at its own default size
        Component = new StaticComponent { Collider = new CompoundCollider() },
        Position = new Vector3(0, 1.5f, 7),
    });
    wall.Name = "wall";
    Place(wall, scene);

    // Remembered, so that R can take the scene away before building it again
    void Place(Entity entity, Scene scene)
    {
        entity.Scene = scene;
        spawned.Add(entity);
    }
}

void AddInstructions()
{
    var overlay = DebugOverlay.GetOrCreate(game);

    overlay.AddSection("Grabber", () =>
    {
        var held = grabber?.Held;
        var lines = new List<TextElement>
        {
            new("Left mouse", "Grab and carry; release to drop or throw"),
            new("Mouse wheel", "Carry distance"),
            new("T", "Hold and move the mouse to turn the held body"),
            new("R", "Reset the scene"),
            new(""),
        };

        if (held is null)
        {
            lines.Add(new("Holding nothing: click a cube, a ball or the capsule", Color.Gray));
        }
        else
        {
            var mass = held.BodyInertia.InverseMass > 0 ? 1 / held.BodyInertia.InverseMass : 0;
            var locked = Bodies.HasLockedInertia(held.BodyInertia.InverseInertiaTensor);

            lines.Add(new($"Holding {held.Entity.Name}, {mass:0.0} kg, at {grabber!.HoldDistance:0.0} m", Color.Yellow));
            lines.Add(new($"Servo {GrabberForce(held):0} N linear cap{(locked ? ", no angular servo" : "")}"));
        }

        return lines;
    });

    float GrabberForce(BodyComponent body)
        => body.BodyInertia.InverseMass > 0 ? grabber!.ForcePerKilogram / body.BodyInertia.InverseMass : 0;
}

/*
---example-metadata
slug: grabber
title:
  en: Grabber
level: Beginner
category: Physics
complexity: 4
order: 45
description:
  en: |-
    A gravity gun: click any body to pick it up, carry it on the end of the camera ray, and let go
    - with its velocity, so a flick throws it. GrabberScript on the camera entity does it with two
    servo constraints rather than a teleport, so the held body still collides and pushes, and the
    force caps scale with mass so a 100 kg cube drags like a 1 kg one. Cubes of five masses, balls
    to flick, a capsule with locked rotation, and a wall to throw at.
concepts:
  - Picking up and throwing bodies with GrabberScript, one line on the camera entity
  - Why servo constraints beat teleporting a kinematic body for a drag
  - Force caps scaled by mass, so heavy and light bodies feel the same in the hand
  - Locking a body's rotation through its BodyInertia, and what the grabber does about it
  - Reading the held body's mass and the servo cap from the script for the overlay
  - "Using helpers: SetupBase3DScene, Create3DPrimitive, AddGrabber, DebugOverlay"
tags:
  - 3D
  - Physics
  - Bepu
  - Constraints
  - Interaction
  - Gravity Gun
related:
  - E05_3D_Constraints
  - E05_3D_Raycast
  - E05_3D_Constraints_Simple
screenshotFrame: 60
enabled: true
created: 2026-09-05
---
*/