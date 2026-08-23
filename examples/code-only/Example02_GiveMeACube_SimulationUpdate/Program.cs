using Example02_GiveMeACube_SimulationUpdate;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

// The same scene as Example02_GiveMeACube, with one difference: the orbit is driven from the physics
// clock instead of the render loop.
//
// Example02 sets a velocity every frame, which is safe because the velocity it sets is bounded. This
// version calls SetTargetPose, which is only correct when exactly one physics step consumes it - so the
// script implements ISimulationUpdate and is called once per fixed step rather than once per frame.

// Half of a 1x1x1 cube, so it sits exactly on the ground
const float CubeRestingHeight = 0.5f;

const float OrbitRadius = 3f;
const float OrbitSpeed = 1f;

const float LooseCubeSize = 0.4f;
const int LooseCubeCount = 8;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    // Camera, directional light and a ground plane
    game.SetupBase3DScene();
    game.AddSkybox();

    CreateCentreCube(rootScene);
    CreateLooseCubes(rootScene);
    CreateOrbitingCube(rootScene);
}

/// <summary>
/// Creates the cube the orbit is centred on, using the toolkit's default Bepu physics: a dynamic body
/// with a matching box collider. It rests on the ground and the orbit never reaches it.
/// </summary>
void CreateCentreCube(Scene rootScene)
{
    var cube = game.Create3DPrimitive(PrimitiveModelType.Cube, new Bepu3DPhysicsOptions());

    cube.Transform.Position = new Vector3(0, CubeRestingHeight, 0);
    cube.Scene = rootScene;
}

/// <summary>
/// Scatters small dynamic cubes evenly around the orbit path, so there is something for the orbiting
/// cube to collide with.
/// </summary>
void CreateLooseCubes(Scene rootScene)
{
    for (var i = 0; i < LooseCubeCount; i++)
    {
        // The half step keeps the first cube clear of the orbiting cube's spawn point
        var angle = MathF.Tau * (i + 0.5f) / LooseCubeCount;

        // Size drives both the generated model and the generated collider
        var cube = game.Create3DPrimitive(PrimitiveModelType.Cube, new Bepu3DPhysicsOptions
        {
            Size = new Vector3(LooseCubeSize)
        });

        cube.Transform.Position = new Vector3(
            MathF.Sin(angle) * OrbitRadius,
            LooseCubeSize / 2,
            MathF.Cos(angle) * OrbitRadius);

        cube.Scene = rootScene;
    }
}

/// <summary>
/// Creates the cube that <see cref="OrbitSimulationScript"/> moves.
/// </summary>
/// <remarks>
/// The body is kinematic, so it follows the scripted path without being affected by gravity or by the
/// cubes it hits, while still pushing them aside.
/// </remarks>
void CreateOrbitingCube(Scene rootScene)
{
    var cube = game.Create3DPrimitive(PrimitiveModelType.Cube, new Bepu3DPhysicsOptions
    {
        Component = new BodyComponent
        {
            Kinematic = true,
            Collider = new CompoundCollider()
        }
    });

    // Spawn on the circle, not at its centre: the script steers the body towards the next point on
    // the orbit, so starting three units away would demand a huge velocity on the very first step.
    cube.Transform.Position = new Vector3(0, CubeRestingHeight, OrbitRadius);

    cube.Add(new OrbitSimulationScript
    {
        Centre = new Vector3(0, CubeRestingHeight, 0),
        Radius = OrbitRadius,
        AngularSpeed = OrbitSpeed
    });

    cube.Scene = rootScene;
}

/*
---example-metadata
slug: simulation-update
title:
  en: Give Me a Cube (SimulationUpdate)
  cs: Dej mi kostku (SimulationUpdate)
level: Intermediate
category: Physics
complexity: 3
order: 60
description:
  en: |-
    Drive an entity from the physics clock instead of the render loop. The script is a StartupScript with
    no per-frame Update at all: it implements ISimulationUpdate, so Bepu calls it once per fixed physics
    step and Stride registers it automatically. That is what makes SetTargetPose safe, because exactly one
    step consumes the velocity it sets. Compare with Example02_GiveMeACube, which sets a bounded velocity
    every frame instead.
  cs: |-
    Řízení entity podle hodin fyziky namísto vykreslovací smyčky. Skript je StartupScript zcela bez metody
    Update: implementuje rozhraní ISimulationUpdate, takže jej Bepu volá jednou za pevný fyzikální krok
    a Stride jej zaregistruje automaticky. Právě díky tomu je metoda SetTargetPose bezpečná, protože
    nastavenou rychlost spotřebuje přesně jeden krok. Porovnejte s Example02_GiveMeACube, který místo toho
    nastavuje omezenou rychlost každý snímek.
concepts:
  - Implementing ISimulationUpdate to run on the physics clock
  - The difference between a fixed physics step and a frame delta time
  - Using SetTargetPose safely, once per physics step
  - StartupScript as a component with no per-frame Update
  - Moving a kinematic body so it pushes dynamic bodies
  - "Using helpers: SetupBase3DScene"
  - "Using helpers: AddSkybox"
tags:
  - 3D
  - Physics
  - Bepu
  - ISimulationUpdate
  - Fixed Timestep
  - Kinematic Body
  - Collision
  - Script
  - StartupScript
  - Cube
related:
  - Example02_GiveMeACube
  - Example01_Basic3DScene_SyncScript
  - Example20_BepuFirstPersonCharacter
enabled: true
created: 2026-08-07
---
*/