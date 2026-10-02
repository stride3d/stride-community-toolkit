using Stride.BepuPhysics;
using Stride.BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// This example demonstrates these constraints: DistanceLimit, DistanceServo, BallSocket, PointOnLineServo
// Any body can be picked up, carried and thrown with the left mouse button (GrabberScript on the
// camera - see E05_3D_Grabber); a middle click on a stacked cube removes it so the stack above falls
// The scene can be reset by pressing R
// DistanceLimit: Connects two spheres with a minimum and maximum distance
// DistanceServo: Connects two spheres with a target distance and spring settings
// BallSocket: Connects two entities with a ball-and-socket joint
// PointOnLineServo: Connects a cube to a line with a servo constraint

const string GoldenSphereName = "Golden Sphere";
const string ConnectedEntityName = "Connected Sphere";

// Enhanced settings for better sliding
const float CubeSpringDampingRatio = 50; // Reduced from 100
const float SpringFrequency = 20;         // Reduced from 40
const float FrictionCoefficient = 0.1f;   // Reduced from 0.5f for smoother sliding
const float ServoMaxForce = 500;          // Reduced from 1000 for softer constraints

// Game entities and components
CameraComponent? mainCamera = null;

List<Entity?> entities = [];
List<BodyComponent?> bodies = [];

var lineLayer = CollisionLayer.Layer1;
var cubeLayer = CollisionLayer.Layer2;
var groundLayer = CollisionLayer.Layer3;
var otherLayer = CollisionLayer.Layer5;

var collisionMatrix = new CollisionMatrix();
collisionMatrix.Set(lineLayer, cubeLayer, shouldCollide: false);
collisionMatrix.Set(lineLayer, groundLayer, shouldCollide: true);
collisionMatrix.Set(lineLayer, otherLayer, shouldCollide: true);
collisionMatrix.Set(groundLayer, otherLayer, shouldCollide: true);
collisionMatrix.Set(otherLayer, otherLayer, shouldCollide: true);
collisionMatrix.Set(cubeLayer, groundLayer, shouldCollide: true);
collisionMatrix.Set(cubeLayer, otherLayer, shouldCollide: true);
collisionMatrix.Set(cubeLayer, cubeLayer, shouldCollide: true);

// Initialize the game instance
using var game = new Game();

// Run the game loop with the Start and Update methods
game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    // Set up a basic 3D scene with skybox, profiler, and a ground gizmo
    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddProfiler();
    game.AddGroundGizmo(new(-5, 0, -5), showAxisName: true);

    SetupCollisionMatrix(scene);
    SetupGroundCollisionLayer(scene);

    InitializeDebugOverlay();
    InitializeEntities(scene);

    // Retrieve the active camera from the scene
    mainCamera = scene.GetCamera();

    // Pick up, carry and throw any body with the left mouse button - two servo constraints, so the
    // held body still collides and the connected constraints still pull on it.
    game.AddGrabber();
}

void Update(Scene scene, GameTime time)
{
    if (mainCamera == null) return;

    if (game.Input.IsKeyPressed(Keys.R))
    {
        ResetTheScene(scene);
    }

    // The left button belongs to the grabber; removing a stacked cube is a middle click.
    if (game.Input.IsMouseButtonPressed(MouseButton.Middle))
    {
        TryRemoveCubeStack(game.Input.MousePosition);
    }
}

void SetupCollisionMatrix(Scene scene)
{
    var camera = scene.GetCamera();

    var simulation = camera?.Entity.GetSimulation();

    if (simulation == null) return;

    simulation.CollisionMatrix = collisionMatrix;
}

void SetupGroundCollisionLayer(Scene scene)
{
    var groundEntity = scene.Entities.FirstOrDefault(e => e.Name == "Ground");

    if (groundEntity == null) return;

    var groundBody = groundEntity.GetComponent<StaticComponent>();

    groundBody!.CollisionLayer = groundLayer;
}

void InitializeEntities(Scene scene)
{
    // Create reference entities for visual reference
    CreateReferenceCube(scene);
    CreateReferenceCapsule(scene);

    CreateDistanceLimitConstraintExamples(scene);
    CreateDistanceServoConstraintExamples(scene);
    CreateBallSocketConstraintExample(scene);
    CreatePointOnLineServoConstraintExample(scene);
}

void CreateReferenceCube(Scene scene)
{
    var referenceCube = CreateCubeEntity("Reference Cube", Color.Purple, new Vector3(3, 3, 3));

    var referenceCubeBody = referenceCube.Get<BodyComponent>();
    referenceCubeBody.FrictionCoefficient = 0.1f;
    referenceCubeBody.CollisionLayer = CollisionLayer.Layer5;

    var angularServoSetB = new OneBodyAngularServoConstraintComponent
    {
        TargetOrientation = Quaternion.Identity,
        A = referenceCubeBody,
        ServoMaximumForce = 1000,
        SpringDampingRatio = 10,
        SpringFrequency = 300,
    };

    referenceCube.Add(angularServoSetB);
    referenceCube.Scene = scene;

    // Remembered like every other entity here, so that a reset removes it before building again
    entities.Add(referenceCube);
}

void CreateReferenceCapsule(Scene scene)
{
    var referenceCapsule = CreateEntity(PrimitiveModelType.Capsule, "Reference Capsule", Color.Orange, new Vector3(0, 3, 0));

    var referenceCapsuleBody = referenceCapsule.Get<BodyComponent>();
    referenceCapsuleBody.CollisionLayer = CollisionLayer.Layer5;

    referenceCapsule.Scene = scene;

    entities.Add(referenceCapsule);
}

void CreateDistanceLimitConstraintExamples(Scene scene)
{
    // Create the golden sphere
    var goldenSphere = CreateEntity(PrimitiveModelType.Sphere, GoldenSphereName, Color.Gold, new Vector3(-2, 3, -2));
    var goldenBody = goldenSphere.Get<BodyComponent>();
    goldenBody.CollisionLayer = CollisionLayer.Layer5;

    // Create a second sphere to demonstrate a connected constraint
    var connectedSphere = CreateEntity(PrimitiveModelType.Sphere, ConnectedEntityName, Color.Blue, new Vector3(-2.1f, 3, -2.9f));
    var connectedBody = connectedSphere.Get<BodyComponent>();
    connectedBody.CollisionLayer = CollisionLayer.Layer5;

    // Set up a distance limit constraint between the golden and connected spheres
    var distanceLimit = new DistanceLimitConstraintComponent
    {
        A = goldenBody,
        B = connectedBody,
        MinimumDistance = 1,
        MaximumDistance = 3.0f
    };

    goldenSphere.Add(distanceLimit);

    // Add both entities to the scene
    goldenSphere.Scene = scene;
    connectedSphere.Scene = scene;

    entities.AddRange([goldenSphere, connectedSphere]);
    bodies.AddRange([goldenBody, connectedBody]);
}

void CreateDistanceServoConstraintExamples(Scene scene)
{
    // Create the golden sphere
    var goldenSphere = CreateEntity(PrimitiveModelType.Sphere, GoldenSphereName, Color.Gold, new Vector3(-2, 6, -2));
    var goldenBody = goldenSphere.Get<BodyComponent>();
    goldenBody.CollisionLayer = CollisionLayer.Layer5;

    var connectedSphere = CreateEntity(PrimitiveModelType.Sphere, ConnectedEntityName, Color.LightBlue, new Vector3(-2.1f, 6, -2.9f));
    var connectedBody = connectedSphere.Get<BodyComponent>();
    connectedBody.CollisionLayer = CollisionLayer.Layer5;

    // Set up a distance servo constraint between the golden and connected spheres
    var distanceServo = new DistanceServoConstraintComponent
    {
        A = goldenBody,
        B = connectedBody,
        TargetDistance = 3.0f,
        SpringDampingRatio = 2,
        //SpringFrequency = 1,
    };

    goldenSphere.Add(distanceServo);

    // Add both entities to the scene
    goldenSphere.Scene = scene;
    connectedSphere.Scene = scene;

    entities.AddRange([goldenSphere, connectedSphere]);
    bodies.AddRange([goldenBody, connectedBody]);
}

void CreateBallSocketConstraintExample(Scene scene)
{
    const float FoundationHeight = 3;
    const float FoundationWidth = 0.2f;
    const float PlatformHeight = 0.2f;
    const float PlatformWidth = 3;

    var exampleOffset = new Vector3(4, 0, -4);

    var foundationSize = new Vector3(FoundationWidth, FoundationHeight, FoundationWidth);
    var foundationPosition = new Vector3(0, FoundationHeight / 2, 0) + exampleOffset;

    var platformSize = new Vector3(PlatformWidth, PlatformHeight, PlatformWidth);
    var platformPosition = new Vector3(0, FoundationHeight + PlatformHeight / 2, 0) + exampleOffset;

    var foundationBlock = CreateCubeEntity("Foundation Block", Color.Beige, foundationPosition, foundationSize);
    var foundationBody = foundationBlock.Get<BodyComponent>();
    foundationBody.Kinematic = true;
    foundationBody.CollisionLayer = CollisionLayer.Layer5;

    var platform = CreateCubeEntity("Platform", Color.Bisque, platformPosition, platformSize);
    var platformBody = platform.Get<BodyComponent>();
    platformBody.CollisionLayer = CollisionLayer.Layer5;

    var ballSocket = new BallSocketConstraintComponent
    {
        A = foundationBody,
        B = platformBody,
        // Adjusting socket to be at the top of the foundation
        LocalOffsetA = new Vector3(0, 1.5f, 0),
        // Adjusting socket to be at the bottom of the platform
        LocalOffsetB = new Vector3(0, -0.1f, 0),
    };

    foundationBlock.Add(ballSocket);
    //foundationBlock.Add(ballSocket2);

    foundationBlock.Scene = scene;
    platform.Scene = scene;

    entities.AddRange([foundationBlock, platform]);
    bodies.AddRange([foundationBody, platformBody]);
}

void CreatePointOnLineServoConstraintExample(Scene scene)
{
    // Create two separate line entities for better control of each stack

    var lineSize = new Vector3(0.1f, 10, 0.1f);
    var lineOffset = new Vector3(-4, 5f, 0);
    var lineBOffset = new Vector3(0, 0, -1);

    var lineAPosition = lineOffset;
    var lineEntityA = CreateCubeEntity("LineA", Color.Gold, lineAPosition, lineSize);

    var lineBodyA = lineEntityA.Get<BodyComponent>();
    lineBodyA.Kinematic = true;
    lineBodyA.CollisionLayer = CollisionLayer.Layer1;
    lineEntityA.Scene = scene;

    var lineBPosition = lineOffset + lineBOffset;
    var lineEntityB = CreateCubeEntity("LineB", Color.Gold, lineBPosition, lineSize);

    var lineBodyB = lineEntityB.Get<BodyComponent>();
    lineBodyB.Kinematic = true;
    lineBodyB.CollisionLayer = CollisionLayer.Layer1;
    lineEntityB.Scene = scene;

    entities.AddRange([lineEntityA, lineEntityB]);

    var cubeSize = new Vector3(0.99f);

    for (int i = 0; i < 10; i++)
    {
        // First stack (SetA)
        var cubePositionA = lineOffset + new Vector3(0, i * 2, 0);
        var cubeEntitySetA = CreateCubeEntity("CubeStackA", Color.DarkRed, cubePositionA, cubeSize);
        var cubeBodySetA = SetupCubeBody(cubeEntitySetA);

        // Tighter constraint with the line to prevent X/Z drift
        var pointOnLineServoConstraintSetA = CreatePointOnLineServoConstraint(lineBodyA, cubeBodySetA);
        // Keep orientation aligned with world axes
        var angularServoSetA = CreateOneBodyAngularServoConstraint(cubeBodySetA);

        lineEntityA.Add(pointOnLineServoConstraintSetA);
        cubeEntitySetA.Add(angularServoSetA);
        cubeEntitySetA.Scene = scene;

        // Second stack (SetB)
        var cubePositionB = lineOffset + lineBOffset + new Vector3(0, i * 2, 0);
        var cubeEntitySetB = CreateCubeEntity("CubeStackB", Color.DarkRed, cubePositionB, cubeSize);
        var cubeBodySetB = SetupCubeBody(cubeEntitySetB);

        var pointOnLineServoConstraintSetB = CreatePointOnLineServoConstraint(lineBodyB, cubeBodySetB);
        var angularServoSetB = CreateOneBodyAngularServoConstraint(cubeBodySetB);

        lineEntityB.Add(pointOnLineServoConstraintSetB);
        cubeEntitySetB.Add(angularServoSetB);
        cubeEntitySetB.Scene = scene;

        entities.AddRange([cubeEntitySetA, cubeEntitySetB]);
    }
}

void TryRemoveCubeStack(Vector2 mousePosition)
{
    var hit = mainCamera.Raycast(mousePosition, 100, out var hitInfo);

    if (hit && (hitInfo.Collidable.Entity.Name == "CubeStackA" || hitInfo.Collidable.Entity.Name == "CubeStackB"))
    {
        // Get the stack name to determine which column was clicked
        string stackName = hitInfo.Collidable.Entity.Name;

        // Remember where the clicked cube was, so the cubes above it can be nudged downward
        var clickedY = hitInfo.Collidable.Entity.Transform.Position.Y;
        var clickedZ = hitInfo.Collidable.Entity.Transform.Position.Z;

        // Remove the clicked cube
        hitInfo.Collidable.Entity.Scene = null;

        // Find and nudge cubes above this position in the same stack
        foreach (var entity in game.SceneSystem.SceneInstance.RootScene.Entities)
        {
            if (entity.Name == stackName)
            {
                var pos = entity.Transform.Position;
                if (Math.Abs(pos.Z - clickedZ) < 0.1f && pos.Y > clickedY)
                {
                    // Add a tiny impulse to get things moving
                    var body = entity.Get<BodyComponent>();
                    if (body != null)
                    {
                        body.ApplyLinearImpulse(new Vector3(0, -0.01f, 0));
                        body.Awake = true;
                    }
                }
            }
        }
    }

    //if (hit && hitInfo.Collidable.Entity.Name == "CubeStack")
    //{
    //    hitInfo.Collidable.Entity.Scene = null;
    //}
}

// Resets the scene by removing all entities and reinitializing them
void ResetTheScene(Scene scene)
{
    for (int i = 0; i < entities.Count; i++)
    {
        if (entities[i] is null) continue;

        entities[i]!.Scene = null;
        entities[i] = null;
    }

    for (int i = 0; i < bodies.Count; i++)
    {
        bodies[i] = null;
    }

    InitializeEntities(scene);
}


void InitializeDebugOverlay()
{
    var overlay = DebugOverlay.GetOrCreate(game);

    overlay.AddSection("Game", static () =>
    [
        new("Left mouse", "Pick up any body, carry it, throw it", Color.Yellow),
        new("Mouse wheel", "Carry distance", Color.Yellow),
        new("T", "Hold and move the mouse to turn the held body", Color.Yellow),
        new("Middle click", "Remove a stacked cube; the ones above collapse", Color.Yellow),
        new("R", "Reset the scene", Color.Yellow),
        new(""),
        new("The constraints keep pulling on whatever you carry", Color.LightGray),
    ]);
}

Entity CreateCubeEntity(string name, Color color, Vector3 position, Vector3? size = null)
    => CreateEntity(PrimitiveModelType.Cube, name, color, position, size);

Entity CreateEntity(PrimitiveModelType type, string name, Color color, Vector3 position, Vector3? size = null)
{
    var entity = game.Create3DPrimitive(type, new()
    {
        EntityName = name,
        Material = game.CreateMaterial(color),
        Size = size
    });

    entity.Transform.Position = position;

    return entity;
}

static BodyComponent SetupCubeBody(Entity cubeEntitySetA)
{
    var cubeBodySetA = cubeEntitySetA.Get<BodyComponent>();
    cubeBodySetA.SpringDampingRatio = CubeSpringDampingRatio;
    cubeBodySetA.SpringFrequency = SpringFrequency;
    cubeBodySetA.FrictionCoefficient = FrictionCoefficient;
    cubeBodySetA.CollisionLayer = CollisionLayer.Layer2;

    return cubeBodySetA;
}

static PointOnLineServoConstraintComponent CreatePointOnLineServoConstraint(BodyComponent lineBodyA, BodyComponent cubeBodySetA)
{
    return new PointOnLineServoConstraintComponent
    {
        A = lineBodyA,
        B = cubeBodySetA,
        LocalOffsetA = Vector3.Zero,     // Anchor directly on line
        LocalOffsetB = Vector3.Zero,     // Anchor at center of cube
        LocalDirection = new Vector3(0, 1, 0),
        ServoMaximumForce = ServoMaxForce,
        SpringFrequency = 15,            // Add explicit spring frequency for smoother motion
        SpringDampingRatio = 1,          // Critical damping
    };
}

static OneBodyAngularServoConstraintComponent CreateOneBodyAngularServoConstraint(BodyComponent cubeBodySetA)
{
    var angularServoSetA = new OneBodyAngularServoConstraintComponent
    {
        TargetOrientation = Quaternion.Identity,
        A = cubeBodySetA,
        ServoMaximumForce = ServoMaxForce,
        SpringDampingRatio = 5,
        SpringFrequency = 15,            // Enable frequency for more responsive rotation control
    };
    return angularServoSetA;
}
/*
---example-metadata
slug: constraints
title:
  en: Various Constraints
level: Advanced
category: Physics
complexity: 5
order: 70
description:
  en: |-
    The full tour of Bepu constraints in one interactive scene: a distance limit holding two spheres
    within a range, a distance servo actively driving a separation with spring settings, a ball socket
    pivoting a platform on a kinematic foundation, and point-on-line servos confining cubes to vertical
    tracks. It is meant to be played with - pick up any body with the mouse and throw it while its
    constraints keep pulling, middle-click a cube to remove it so the stack above collapses, R resets
    everything.
concepts:
  - Picking up and throwing constrained bodies with GrabberScript
  - "Limiting a range with DistanceLimitConstraintComponent"
  - "Driving a target separation with DistanceServoConstraintComponent"
  - "Pivoting a body with BallSocketConstraintComponent"
  - "Confining motion to an axis with PointOnLineServoConstraintComponent"
  - Tuning a servo with spring frequency and damping
  - Anchoring a constraint to a kinematic foundation
  - Filtering collisions between the connected parts
  - Removing constrained bodies at runtime and resetting the scene
  - "Using helpers: SetupBase3DScene, AddSkybox, AddProfiler"
tags:
  - 3D
  - Bepu
  - Physics
  - Constraint
  - Servo
  - Ball Socket
  - Spring
  - Input
related:
  - E05_3D_Constraints_Simple
  - E05_3D_Constraints_Motors
  - E05_3D_Constraints_Rope
tocName: Various Constraints
enabled: true
created: 2025-02-02
---
*/