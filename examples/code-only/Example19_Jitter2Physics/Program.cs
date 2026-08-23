using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

const float CubeSize = 0.5f;
const float VerticalSpacing = 2f;

// Physics runs at a fixed rate, decoupled from the render frame rate (see Update)
const float FixedTimeStep = 1f / 100f;
const int MaxStepsPerFrame = 5;

var groundSize = new Vector3(15f, 1, 15f);

// Initialize Jitter2 physics world with 4 substeps for better accuracy
var world = new World()
{
    SubstepCount = 4,
};

// Each cube's visual entity and physics body, kept together so they can never drift out of sync
var cubes = new List<CubeInstance>();

// Accumulates real elapsed time between fixed physics steps (see Update)
var accumulatedTime = 0f;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    game.Window.Title = "Jitter 2 Physics Example - Stride Community Toolkit";

    game.SetupBase3D();
    game.AddSkybox();
    game.Add3DCameraController();
    game.AddProfiler();

    CreateGround(rootScene);
    CreateCubes(rootScene, count: 150);
}

void Update(Scene scene, GameTime time)
{
    // Accumulate real elapsed time and step the simulation in fixed increments, so it advances at
    // the correct speed even when the render frame rate drifts away from FixedTimeStep. Capping the
    // number of steps per frame avoids a "spiral of death" if a frame hitches badly.
    accumulatedTime += (float)time.Elapsed.TotalSeconds;

    var steps = 0;

    while (accumulatedTime >= FixedTimeStep && steps < MaxStepsPerFrame)
    {
        world.Step(FixedTimeStep, true);

        accumulatedTime -= FixedTimeStep;
        steps++;
    }

    // If the frame rate stays below the physics rate for a sustained period rather than a single
    // hitch, the loop above can never fully drain accumulatedTime and the backlog would otherwise
    // keep growing forever. Clamping it bounds that backlog to one frame's worth of catch-up,
    // trading permanently-delayed simulation time for a value that can't grow without bound (which
    // would eventually lose precision as a float in a long-running session).
    accumulatedTime = MathF.Min(accumulatedTime, FixedTimeStep * MaxStepsPerFrame);

    // Update visual entities to match their physics body positions
    SyncPhysicsToEntities();
}

void CreateGround(Scene rootScene)
{
    // Create visual ground plane
    var groundEntity = game.Create3DPrimitive(PrimitiveModelType.Plane, new()
    {
        Size = groundSize,
    });
    groundEntity.Scene = rootScene;

    // Create physics body for the ground (static, won't move)
    var groundBody = world.CreateRigidBody();
    groundBody.MotionType = MotionType.Static;
    groundBody.AddShape(new BoxShape(groundSize.X, groundSize.Y, groundSize.Z));
    groundBody.Position = new JVector(0, -0.5f, 0);
}

void CreateCubes(Scene rootScene, int count)
{
    for (int i = 0; i < count; i++)
    {
        var cubePosition = new Vector3(0, 10 + i * VerticalSpacing, 0);

        // Create visual cube entity
        var cubeEntity = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
        {
            Material = game.CreateMaterial(Color.Red),
            Size = new Vector3(CubeSize),
        });

        cubeEntity.Transform.Position = cubePosition;
        cubeEntity.Scene = rootScene;

        // Create physics body for the cube (dynamic, affected by forces)
        var cubeBody = world.CreateRigidBody();
        cubeBody.AddShape(new BoxShape(CubeSize));
        cubeBody.SetMassInertia(1f);
        cubeBody.Position = new JVector(cubePosition.X, cubePosition.Y, cubePosition.Z);

        cubes.Add(new CubeInstance(cubeEntity, cubeBody));
    }
}

void SyncPhysicsToEntities()
{
    // Copy physics body transforms to visual entities each frame
    foreach (var cube in cubes)
    {
        var position = cube.Body.Position;
        var orientation = cube.Body.Orientation;

        cube.Entity.Transform.Position = new Vector3(position.X, position.Y, position.Z);
        cube.Entity.Transform.Rotation = new Quaternion(orientation.X, orientation.Y, orientation.Z, orientation.W);
    }
}

/// <summary>
/// Pairs a cube's visual entity with its physics body, so the two can never drift out of sync.
/// </summary>
record CubeInstance(Entity Entity, RigidBody Body);

/*
---example-metadata
slug: jitter2-physics
title:
  en: Jitter2 Physics Integration
  cs: Integrace fyziky Jitter2
level: Advanced
category: Physics
complexity: 4
order: 100
description:
  en: |-
    Demonstrates integrating Jitter2 physics engine with Stride. Shows how to create a physics world,
    synchronize physics bodies with visual entities, and simulate dynamic rigid body interactions.
    Features 150 falling cubes with proper collision detection and a static ground plane.
  cs: |-
    Ukazuje integraci fyzikálního enginu Jitter2 se Stride. Jak vytvořit fyzikální svět,
    synchronizovat fyzikální tělesa s vizuálními entitami a simulovat dynamické interakce pevných těles.
    Obsahuje 150 padajících kostek s detekcí kolizí a statickou zemní rovinou.
concepts:
  - Integrating external physics engine (Jitter2)
  - Creating and managing physics world
  - Synchronizing physics bodies with visual entities
  - Dynamic rigid body simulation
  - Static vs dynamic physics bodies
  - Fixed-timestep physics update loop, decoupled from the render frame rate
tags:
  - 3D
  - Physics
  - Jitter2
  - Rigid Body
  - Collision Detection
  - External Engine
  - Simulation
  - Cubes
related:
  - Example01_Basic3DScene
  - Example18_Box2DPhysics
  - Example19_Jitter2Physics_Constraints
  - Example_CubicleCalamity
enabled: true
created: 2025-12-13
---
*/