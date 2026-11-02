using E05_3D_PlanetWalker;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Scripts;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// A character walking round a planet. On flat ground "down" is one direction for everyone; here it is
// towards the planet's centre, so it is different for every point of the surface and changes as the
// character walks. Three things have to follow it:
//
//   the pull   - the simulation's gravity is off, and a script pulls the character towards the centre
//                every physics step, as E05_3D_PlanetGravity pulls its orbiters;
//   the up     - the character's Orientation is set so that its own up, the LocalUp its ground test, its
//                jump and its move vector are read in, points away from the centre;
//   the camera - the follow controller's Up is set the same way, so the camera orbits and pitches over
//                the surface instead of over the world's floor.
//
// WASD walks relative to the camera, as in E05_3D_ThirdPersonCamera; walk far enough and you come back
// to where you started, upside down as far as the world is concerned and none the wiser.

const float PlanetRadius = 12f;
const float SurfaceGravity = 18f;
const float CharacterSpeed = 6f;
const float SprintMultiplier = 1.8f;

const CollisionLayer CharacterLayer = CollisionLayer.Layer2;
const CollisionMask SceneryMask = CollisionMask.Everything & ~CollisionMask.Layer2;

CharacterComponent character = null!;
Entity body = null!;
Entity nose = null!;
Basic3DFollowCameraController follow = null!;
BepuSimulation simulation = null!;
var walkDirection = -Vector3.UnitZ;

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    GameSettings = settings =>
    {
        // No gravity of its own and no damping, so the pull towards the centre is the only force on the character
        var bepu = settings.GetOrCreateConfiguration<BepuConfiguration>();

        bepu.BepuSimulations.Add(new BepuSimulation { PoseGravity = Vector3.Zero, PoseLinearDamping = 0f, PoseAngularDamping = 0f });
    },
    Ground = null,
    Camera = CameraSetup.Perspective(),
    WindowTitle = "Planet walker - Stride Community Toolkit",
    Resizable = true,
    DpiAware = true,
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    BuildPlanet(scene);

    character = new CharacterComponent
    {
        Speed = CharacterSpeed,
        JumpForce = 7f,
        Collider = new CompoundCollider(),
        CollisionLayer = CharacterLayer,
        // The world's gravity would pull "down" whatever the character stands on; the planet's pull replaces it
        Gravity = false,
    };

    body = game.CreatePrimitive(Primitive3D.Capsule(0.35f, 0.5f), new BepuPhysicsOptions
    {
        EntityName = "Character",
        Component = character,
        Material = game.CreateMaterial(new Color(240, 200, 80), metalness: 0f, glossiness: 0.5f),
        Position = new Vector3(0f, PlanetRadius + 1f, 0f),
    });

    nose = game.CreatePrimitive(Primitive3D.Box(new Vector3(0.2f, 0.2f, 0.5f)), new BepuPhysicsOptions
    {
        EntityName = "Nose",
        Body = BodyMode.None,
        Material = game.CreateMaterial(new Color(60, 60, 70)),
    });

    nose.Transform.Position = new Vector3(0f, 0.3f, -0.4f);
    body.AddChild(nose);

    // The pull rides on the character and runs every physics step
    body.Add(new PlanetPull { Centre = Vector3.Zero, SurfaceGravity = SurfaceGravity, SurfaceRadius = PlanetRadius });
    body.Scene = scene;

    simulation = character.Simulation!;

    var camera = game.GetCameraEntity();

    camera.Transform.Position = body.Transform.Position + new Vector3(0f, 3.5f, 7f);

    follow = new Basic3DFollowCameraController
    {
        Target = body,
        TargetOffset = new Vector3(0f, 1.2f, 0f),
        ObstacleTest = ClearLine,
    };

    camera.Add(follow);

    TextOverlay.GetOrCreate(game).AddSection("Planet walker", BuildOverlayLines);
}

void Update(Scene scene, GameTime time)
{
    var input = game.Input;

    // Up is away from the centre, wherever the character is: the camera, the character and its nose all follow it
    var up = Vector3.Normalize(body.Transform.Position);

    follow.Up = up;
    follow.TargetOffset = up * 1.2f;

    // The camera's ground directions are from its last frame, and up has moved since; forward is laid into this
    // frame's ground and right rebuilt from the two, so the three are at right angles and make a rotation
    var forward = follow.Forward - up * Vector3.Dot(follow.Forward, up);

    if (forward.LengthSquared() < 1e-6f) forward = Vector3.Cross(up, follow.Right);

    forward.Normalize();

    var right = Vector3.Cross(up, -forward);
    var move = Vector3.Zero;

    if (input.IsKeyDown(Keys.W) || input.IsKeyDown(Keys.Up)) move += forward;
    if (input.IsKeyDown(Keys.S) || input.IsKeyDown(Keys.Down)) move -= forward;
    if (input.IsKeyDown(Keys.A) || input.IsKeyDown(Keys.Left)) move -= right;
    if (input.IsKeyDown(Keys.D) || input.IsKeyDown(Keys.Right)) move += right;

    var walking = move.LengthSquared() > 0f;

    if (walking)
    {
        move.Normalize();
        walkDirection = move;

        if (input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift)) move *= SprintMultiplier;
    }

    // The character's own frame: up away from the centre, forward where the camera looks along the surface. The
    // move vector is read in that frame, X to its right and Y along its back, so forward is a negative Y
    var basis = Matrix.Identity;

    basis.Right = right;
    basis.Up = up;
    basis.Backward = -forward;

    character.Orientation = Quaternion.Normalize(Quaternion.RotationMatrix(basis));
    character.MoveVector = new Vector2(Vector3.Dot(move, right), -Vector3.Dot(move, forward));

    // The nose turns within that frame to the last walk direction
    var noseBasis = Matrix.Identity;
    var noseBack = -Vector3.Normalize(walkDirection - up * Vector3.Dot(walkDirection, up));

    noseBasis.Right = Vector3.Cross(up, noseBack);
    noseBasis.Up = up;
    noseBasis.Backward = noseBack;

    var noseRotation = Quaternion.Normalize(Quaternion.RotationMatrix(noseBasis) * Quaternion.Invert(character.Orientation));

    nose.Transform.Rotation = noseRotation;
    nose.Transform.Position = Vector3.Transform(new Vector3(0f, 0.3f, -0.4f), noseRotation);

    if (input.IsKeyPressed(Keys.Space)) character.TryJump();
}

float? ClearLine(Vector3 from, Vector3 to)
{
    var line = to - from;
    var length = line.Length();

    if (length < MathUtil.ZeroTolerance) return null;

    return simulation.RayCast(from, line / length, length, out var hit, SceneryMask) ? hit.Distance : null;
}

/// <summary>The planet, and a scatter of blocks standing on its surface, each turned so its top faces away from the centre.</summary>
void BuildPlanet(Scene scene)
{
    var planet = game.CreatePrimitive(Primitive3D.Sphere(PlanetRadius, tessellation: 48), new BepuPhysicsOptions
    {
        EntityName = "Planet",
        Body = BodyMode.Static,
        Material = game.CreateMaterial(new Color(110, 140, 90), metalness: 0f, glossiness: 0.2f),
    });

    planet.Scene = scene;

    var stone = game.CreateMaterial(new Color(150, 140, 130), metalness: 0f, glossiness: 0.3f);
    var random = new Random(4);

    for (var i = 0; i < 24; i++)
    {
        // A random point on the sphere, and a block half sunk into the surface there
        var direction = Vector3.Normalize(new Vector3(random.NextSingle() - 0.5f, random.NextSingle() - 0.5f, random.NextSingle() - 0.5f));
        var size = new Vector3(1.5f + random.NextSingle() * 2f, 1f + random.NextSingle() * 2.5f, 1.5f + random.NextSingle() * 2f);

        var block = game.CreatePrimitive(Primitive3D.Box(size), new BepuPhysicsOptions
        {
            EntityName = "Block",
            Body = BodyMode.Static,
            Material = stone,
            Position = direction * (PlanetRadius + size.Y * 0.25f),
        });

        block.Transform.Rotation = Upright(direction);
        block.Scene = scene;
    }
}

/// <summary>A rotation whose up is the given direction, with the rest of the frame chosen to be stable near the poles.</summary>
static Quaternion Upright(Vector3 up)
{
    var reference = MathF.Abs(up.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX;
    var right = Vector3.Normalize(Vector3.Cross(reference, up));
    var basis = Matrix.Identity;

    basis.Right = right;
    basis.Up = up;
    basis.Backward = Vector3.Cross(right, up);

    return Quaternion.RotationMatrix(basis);
}

IReadOnlyList<TextElement> BuildOverlayLines() =>
[
    new(["W", "A", "S", "D"], "Walk, relative to the camera", Color.Gold),
    new("Shift", "Sprint", Color.Gold),
    new("Space", "Jump", Color.Gold),
    new("Right drag", "Swing the camera round", Color.Gold),
    new("Mouse wheel", "Closer or further", Color.Gold),
    new(""),
    new($"Pulled towards the centre at {SurfaceGravity:0} m/s² on the surface; up is away from it", Color.White),
    new("Keep walking: the way back is round the planet", Color.LightGray),
];

/*
---example-metadata
slug: planet-walker
title:
  en: Planet Walker
level: Intermediate
category: Physics
complexity: 3
order: 122
description:
  en: |-
    A character walking round a planet with the follow camera behind it. The simulation's gravity is
    off and a script pulls the character towards the centre every physics step; the character's
    Orientation and the camera's Up are set to the direction away from the centre each frame, so the
    ground test, the jump, the move vector and the camera's orbit all follow the surface. Walk far
    enough and you come back round.
concepts:
  - A per-step pull towards a point in place of the simulation's gravity
  - "CharacterComponent.Orientation set every frame, so its LocalUp and move vector follow a curved surface"
  - The follow controller's Up, with Forward and Right read back for walking
  - Static blocks placed and turned to stand on a sphere
  - "Using helpers: SceneSetup3D, UseBepu, UseSkybox, CreatePrimitive, CreateMaterial, Basic3DFollowCameraController, RayCast, TextOverlay"
tags:
  - 3D
  - Physics
  - Bepu
  - Gravity
  - Camera
  - Character
related:
  - E05_3D_ThirdPersonCamera
  - E05_3D_PlanetGravity
  - E05_3D_FirstPersonCharacter
screenshotFrame: 120
enabled: true
created: 2026-11-01
---
*/