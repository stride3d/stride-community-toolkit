using Example02_GiveMeACube;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

// A cube orbits the scene and knocks a ring of smaller cubes out of its way.
// The orbit is driven by OrbitScript, a SyncScript attached to the entity, rather than by the update
// callback of game.Run(...) - that is what this example is really demonstrating.

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
/// Creates the cube that <see cref="OrbitScript"/> moves.
/// </summary>
/// <remarks>
/// The body is kinematic, so it follows the scripted path without being affected by gravity or by the
/// cubes it hits, while still pushing them aside. A dynamic body would fall and be knocked off course;
/// an entity with no body at all could be moved by writing its transform, but would pass straight
/// through everything.
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
    // the orbit, so starting three units away would demand a huge velocity on the very first tick.
    cube.Transform.Position = new Vector3(0, CubeRestingHeight, OrbitRadius);

    // Adding the script is what gives the entity its behaviour
    cube.Add(new OrbitScript
    {
        Centre = new Vector3(0, CubeRestingHeight, 0),
        Radius = OrbitRadius,
        AngularSpeed = OrbitSpeed
    });

    cube.Scene = rootScene;
}

/*
---example-metadata
slug: give-me-cube-body
title:
  en: Give Me a Cube
  cs: Dej mi kostku
level: Beginner
category: Scripts
complexity: 2
order: 40
description:
  en: |-
    Add behaviour to an entity with a SyncScript component instead of the update callback of game.Run.
    The script steers a cube around a circle each frame by setting the linear velocity of a kinematic
    Bepu body, so it really sweeps through the scene and knocks a ring of smaller dynamic cubes out of
    the way. Writing Transform.Position instead would move only the mesh and collide with nothing.
  cs: |-
    Přidání chování k entitě pomocí komponenty SyncScript namísto callbacku update v game.Run.
    Skript každý snímek vede kostku po kruhu nastavením lineární rychlosti kinematického tělesa Bepu,
    takže skutečně projíždí scénou a odráží kruh menších dynamických kostek. Zápis do Transform.Position
    by naproti tomu pohnul pouze modelem a s ničím by nekolidoval.
concepts:
  - Adding behaviour with a SyncScript component
  - The Start and Update lifecycle methods
  - Configuring a script through public properties
  - Moving a kinematic body by setting its linear velocity
  - Waking a body after changing its velocity
  - Why writing Transform.Position does not move a physics body
  - Sizing a primitive and its collider together
  - "Using helpers: SetupBase3DScene"
  - "Using helpers: AddSkybox"
  - "Using helpers: Game.DeltaTime"
tags:
  - 3D
  - Script
  - SyncScript
  - Component
  - Update Loop
  - Transform
  - Bepu
  - Physics
  - Kinematic Body
  - Collision
  - Cube
related:
  - Example01_Basic3DScene
  - Example01_Basic3DScene_SyncScript
  - Example11_ImGuiNet
media: stride-game-engine-example02-give-me-cube.webp
tocName: Give me a cube
enabled: true
created: 2023-09-11
---
*/