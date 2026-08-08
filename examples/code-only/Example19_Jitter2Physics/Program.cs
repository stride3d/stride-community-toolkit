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

var groundSize = new Vector3(15f, 1, 15f);

// Initialize Jitter2 physics world with 4 substeps for better accuracy
var world = new World()
{
    SubstepCount = 4,
};

// Store paired entities and physics bodies for synchronization
var cubeEntities = new List<Entity>();
var cubeBodies = new List<RigidBody>();

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    game.Window.Title = "Jitter 2 Physics Example - Stride Community Toolkit";

    game.SetupBase3D();
    game.AddSkybox();
    game.Add3DCameraController();
    game.AddProfiler();
    game.SetMaxFPS(100);

    CreateGround(rootScene);
    CreateCubes(rootScene, count: 150);
}

void Update(Scene scene, GameTime time)
{
    // Step the physics simulation forward in time
    world.Step(1.0f / 100.0f, true);

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
    for (int i = 0; i <= count; i++)
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

        // Store both for later synchronization
        cubeEntities.Add(cubeEntity);
        cubeBodies.Add(cubeBody);
    }
}

void SyncPhysicsToEntities()
{
    // Copy physics body transforms to visual entities each frame
    for (int i = 0; i < cubeBodies.Count; i++)
    {
        var body = cubeBodies[i];
        var entity = cubeEntities[i];

        var position = body.Position;
        var orientation = body.Orientation;

        entity.Transform.Position = new Vector3(position.X, position.Y, position.Z);
        entity.Transform.Rotation = new Quaternion(orientation.X, orientation.Y, orientation.Z, orientation.W);
    }
}

/*
---example-metadata
title:
  en: Jitter2 Physics Integration
  cs: Integrace fyziky Jitter2
level: Beginners
category: Physics
complexity: 4
description:
  en: |
    Demonstrates integrating Jitter2 physics engine with Stride. Shows how to create a physics world,
    synchronize physics bodies with visual entities, and simulate dynamic rigid body interactions.
    Features 150 falling cubes with proper collision detection and a static ground plane.
  cs: |
    Ukazuje integraci fyzikálního enginu Jitter2 se Stride. Jak vytvořit fyzikální svět,
    synchronizovat fyzikální tělesa s vizuálními entitami a simulovat dynamické interakce pevných těles.
    Obsahuje 150 padajících kostek s detekcí kolizí a statickou zemní rovinou.
concepts:
  - Integrating external physics engine (Jitter2)
  - Creating and managing physics world
  - Synchronizing physics bodies with visual entities
  - Dynamic rigid body simulation
  - Static vs dynamic physics bodies
  - Manual physics update loop
related:
  - Example01_Basic3DScene
  - Example18_Box2DPhysics
  - Example_CubicleCalamity
tags:
  - 3D
  - Physics
  - Jitter2
  - Rigid Body
  - Collision Detection
  - External Engine
  - Simulation
  - Cubes
  - Beginners
order: 19
enabled: true
created: 2025-12-13
---
*/