using Stride.BepuPhysics;
using Stride.BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// Four Bepu constraints side by side: DistanceLimit keeps two spheres between a minimum and a maximum
// distance, DistanceServo pulls two spheres to a target distance with spring settings, BallSocket
// joins two bodies at a point, PointOnLineServo holds a cube to a line. Left mouse grabs any dynamic body
// (GrabberScript, see E05_3D_Grabber), a middle click removes a stacked cube so the stack above
// falls, R resets the scene.

const string GoldenSphereName = "Golden Sphere";
const string ConnectedEntityName = "Connected Sphere";
const string StackAName = "CubeStackA";
const string StackBName = "CubeStackB";

// Soft, heavily damped contacts and little friction, so the stacked cubes slide along their lines rather than jam
const float CubeSpringDampingRatio = 50;
const float SpringFrequency = 20;
const float FrictionCoefficient = 0.1f;
const float ServoMaxForce = 500;

CameraComponent mainCamera = null!;

// Everything the scene built, so that R can take it away before building again
List<Entity> spawned = [];

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

using var game = new Game();

game.Run(new SceneSetup3D { Profiler = true }.UseBepu().UseSkybox(), start: Start, update: Update);

void Start(Scene scene)
{
    game.AddGroundGizmo(new(-5, 0, -5), showAxisName: true);

    game.GetCameraEntity().GetSimulation().CollisionMatrix = collisionMatrix;

    // The setup's ground gets the ground layer, so the matrix applies to it too
    scene.Entities.First(e => e.Name == "Ground").Get<StaticComponent>().CollisionLayer = groundLayer;

    AddInstructions();
    InitializeEntities(scene);

    mainCamera = game.GetCameraEntity().Get<CameraComponent>();

    // Pick up, carry and throw any dynamic body with the left mouse button - two servo constraints, so the
    // held body still collides and the connected constraints still pull on it.
    game.AddGrabber();
}

void Update(Scene scene, GameTime time)
{
    if (game.Input.IsKeyPressed(Keys.R))
    {
        Reset(scene);
    }

    // The left button belongs to the grabber; removing a stacked cube is a middle click.
    if (game.Input.IsMouseButtonPressed(MouseButton.Middle))
    {
        TryRemoveCubeStack(game.Input.MousePosition);
    }
}

void InitializeEntities(Scene scene)
{
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
    referenceCubeBody.CollisionLayer = otherLayer;

    referenceCube.Add(new OneBodyAngularServoConstraintComponent
    {
        TargetOrientation = Quaternion.Identity,
        A = referenceCubeBody,
        ServoMaximumForce = 1000,
        SpringDampingRatio = 10,
        SpringFrequency = 300,
    });

    Place(referenceCube, scene);
}

void CreateReferenceCapsule(Scene scene)
{
    var referenceCapsule = CreateEntity(Primitive3D.Capsule(), "Reference Capsule", Color.Orange, new Vector3(0, 3, 0));

    referenceCapsule.Get<BodyComponent>().CollisionLayer = otherLayer;

    Place(referenceCapsule, scene);
}

// The spheres stay between one and three units apart
void CreateDistanceLimitConstraintExamples(Scene scene)
    => CreateSpherePair(scene, height: 3, Color.Blue, (a, b) => new DistanceLimitConstraintComponent { A = a, B = b, MinimumDistance = 1, MaximumDistance = 3.0f });

// The servo pulls the spheres to three units apart and holds them there
void CreateDistanceServoConstraintExamples(Scene scene)
    => CreateSpherePair(scene, height: 6, Color.LightBlue, (a, b) => new DistanceServoConstraintComponent { A = a, B = b, TargetDistance = 3.0f, SpringDampingRatio = 2 });

/// <summary>A golden sphere and a connected one at a height, joined by the constraint the caller builds from their two bodies.</summary>
void CreateSpherePair(Scene scene, float height, Color connectedColour, Func<BodyComponent, BodyComponent, EntityComponent> constraint)
{
    var goldenSphere = CreateEntity(Primitive3D.Sphere(), GoldenSphereName, Color.Gold, new Vector3(-2, height, -2));
    var goldenBody = goldenSphere.Get<BodyComponent>();
    goldenBody.CollisionLayer = otherLayer;

    var connectedSphere = CreateEntity(Primitive3D.Sphere(), ConnectedEntityName, connectedColour, new Vector3(-2.1f, height, -2.9f));
    var connectedBody = connectedSphere.Get<BodyComponent>();
    connectedBody.CollisionLayer = otherLayer;

    goldenSphere.Add(constraint(goldenBody, connectedBody));

    Place(goldenSphere, scene);
    Place(connectedSphere, scene);
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
    foundationBody.CollisionLayer = otherLayer;

    var platform = CreateCubeEntity("Platform", Color.Bisque, platformPosition, platformSize);
    var platformBody = platform.Get<BodyComponent>();
    platformBody.CollisionLayer = otherLayer;

    // The socket is at the top of the foundation and the bottom of the platform
    foundationBlock.Add(new BallSocketConstraintComponent
    {
        A = foundationBody,
        B = platformBody,
        LocalOffsetA = new Vector3(0, 1.5f, 0),
        LocalOffsetB = new Vector3(0, -0.1f, 0),
    });

    Place(foundationBlock, scene);
    Place(platform, scene);
}

// Two kinematic lines, each with a stack of ten cubes held to it by a point-on-line servo and kept
// upright by an angular servo, so a cube taken out of the stack lets the ones above slide down
void CreatePointOnLineServoConstraintExample(Scene scene)
{
    var lineSize = new Vector3(0.1f, 10, 0.1f);
    var lineOffset = new Vector3(-4, 5f, 0);
    var lineBOffset = new Vector3(0, 0, -1);
    var cubeSize = new Vector3(0.99f);

    var lineA = CreateLine("LineA", lineOffset);
    var lineB = CreateLine("LineB", lineOffset + lineBOffset);

    for (var i = 0; i < 10; i++)
    {
        CreateStackCube(StackAName, lineA, lineOffset + new Vector3(0, i * 2, 0));
        CreateStackCube(StackBName, lineB, lineOffset + lineBOffset + new Vector3(0, i * 2, 0));
    }

    Entity CreateLine(string name, Vector3 position)
    {
        var line = CreateCubeEntity(name, Color.Gold, position, lineSize);
        var body = line.Get<BodyComponent>();

        body.Kinematic = true;
        body.CollisionLayer = lineLayer;

        Place(line, scene);

        return line;
    }

    void CreateStackCube(string name, Entity line, Vector3 position)
    {
        var cube = CreateCubeEntity(name, Color.DarkRed, position, cubeSize);
        var body = cube.Get<BodyComponent>();

        body.SpringDampingRatio = CubeSpringDampingRatio;
        body.SpringFrequency = SpringFrequency;
        body.FrictionCoefficient = FrictionCoefficient;
        body.CollisionLayer = cubeLayer;

        // Anchored at the line's centre and the cube's, along the line's up: the cube may only slide
        line.Add(new PointOnLineServoConstraintComponent
        {
            A = line.Get<BodyComponent>(),
            B = body,
            LocalOffsetA = Vector3.Zero,
            LocalOffsetB = Vector3.Zero,
            LocalDirection = new Vector3(0, 1, 0),
            ServoMaximumForce = ServoMaxForce,
            SpringFrequency = 15,
            SpringDampingRatio = 1,
        });

        // Kept aligned with the world axes
        cube.Add(new OneBodyAngularServoConstraintComponent
        {
            TargetOrientation = Quaternion.Identity,
            A = body,
            ServoMaximumForce = ServoMaxForce,
            SpringDampingRatio = 5,
            SpringFrequency = 15,
        });

        Place(cube, scene);
    }
}

void TryRemoveCubeStack(Vector2 mousePosition)
{
    if (!mainCamera.Raycast(mousePosition, 100, out var hitInfo) || hitInfo.Collidable.Entity.Name is not (StackAName or StackBName)) return;

    var clicked = hitInfo.Collidable.Entity;
    var clickedPosition = clicked.Transform.Position;

    clicked.Scene = null;

    // A nudge to the cubes above it in the same stack, so they start moving at once
    foreach (var entity in game.SceneSystem.SceneInstance.RootScene.Entities)
    {
        var position = entity.Transform.Position;

        if (entity.Name != clicked.Name || Math.Abs(position.Z - clickedPosition.Z) >= 0.1f || position.Y <= clickedPosition.Y) continue;

        var body = entity.Get<BodyComponent>();

        body.ApplyLinearImpulse(new Vector3(0, -0.01f, 0));
        body.Awake = true;
    }
}

void Reset(Scene scene)
{
    foreach (var entity in spawned) entity.Scene = null;

    spawned.Clear();
    InitializeEntities(scene);
}

void AddInstructions()
{
    TextOverlay.GetOrCreate(game).AddSection("Game", static () =>
    [
        new("Left mouse", "Pick up a dynamic body, carry it, throw it", Color.Yellow),
        new("Mouse wheel", "Carry distance", Color.Yellow),
        new("T", "Hold and move the mouse to turn the held body", Color.Yellow),
        new("Middle click", "Remove a stacked cube; the ones above collapse", Color.Yellow),
        new("R", "Reset the scene", Color.Yellow),
        new(""),
        new("The constraints keep pulling on whatever you carry", Color.LightGray),
    ]);
}

// Into the scene, and onto the list the reset empties
void Place(Entity entity, Scene scene)
{
    entity.Scene = scene;
    spawned.Add(entity);
}

Entity CreateCubeEntity(string name, Color color, Vector3 position, Vector3? size = null)
    => CreateEntity(Primitive3D.Box(size ?? Vector3.One), name, color, position);

Entity CreateEntity(Primitive3D shape, string name, Color color, Vector3 position)
{
    var entity = game.CreatePrimitive(shape, new()
    {
        EntityName = name,
        Material = game.CreateMaterial(color),
    });

    entity.Transform.Position = position;

    return entity;
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
  - "Using helpers: SceneSetup3D, UseSkybox"
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