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

// A third-person camera on a character that walks a stepped pyramid. The camera is the toolkit's follow
// controller: it hangs behind and above the character, trails its steps with a short lag, swings round
// it on a right-drag and zooms on the wheel, like the fly and orbit controllers it sits beside. What the
// controller cannot know is where the walls are, so the game lends it a raycast: before the camera
// settles it asks whether anything stands between the character and the spot it wants, and comes
// forward to stay in front of a pillar or a step instead of looking through it.
//
// The character is Bepu's CharacterComponent on a capsule, as in E05_3D_FirstPersonCharacter, and WASD
// moves it relative to the camera's yaw, so "forward" is always away from the viewer. A nose on the
// capsule turns to face the way it walks; the capsule itself keeps its orientation, since the character's
// move vector is read in that frame.

const float PyramidStep = 1.6f;
const int PyramidLevels = 6;
const float CharacterSpeed = 6f;
const float SprintMultiplier = 1.8f;
const float NoseTurnSpeed = 10f;

CharacterComponent character = null!;
Entity nose = null!;
Basic3DFollowCameraController follow = null!;
BepuSimulation simulation = null!;
var noseYaw = 0f;

// The character is on its own layer, so the camera's ray ignores the capsule and sees only the scenery
const CollisionLayer CharacterLayer = CollisionLayer.Layer2;
const CollisionMask SceneryMask = CollisionMask.Everything & ~CollisionMask.Layer2;

using var game = new Game();

// No WithController: the follow controller is the camera's script here
var sceneSetup = new SceneSetup3D
{
    // The setup's ground is a small one; the pyramid and the walk around it want a field
    Ground = null,
    Camera = CameraSetup.Perspective(),
    WindowTitle = "Third-person camera - Stride Community Toolkit",
    Resizable = true,
    DpiAware = true,
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    BuildScenery(scene);

    character = new CharacterComponent
    {
        Speed = CharacterSpeed,
        JumpForce = 7f,
        Collider = new CompoundCollider(),
        CollisionLayer = CharacterLayer,
    };

    var body = game.CreatePrimitive(Primitive3D.Capsule(0.35f, 0.5f), new BepuPhysicsOptions
    {
        EntityName = "Character",
        Component = character,
        Material = game.CreateMaterial(new Color(240, 200, 80), metalness: 0f, glossiness: 0.5f),
        Position = new Vector3(4f, 1f, 13f),
    });

    // The nose: a child that turns to face the walk direction, since the capsule does not turn
    nose = game.CreatePrimitive(Primitive3D.Box(new Vector3(0.2f, 0.2f, 0.5f)), new BepuPhysicsOptions
    {
        EntityName = "Nose",
        Body = BodyMode.None,
        Material = game.CreateMaterial(new Color(60, 60, 70)),
    });

    nose.Transform.Position = new Vector3(0f, 0.3f, -0.4f);
    body.AddChild(nose);
    body.Scene = scene;

    simulation = character.Simulation!;

    // The camera starts behind the character and a little above; the controller reads its angles from there
    var camera = game.GetCameraEntity();

    camera.Transform.Position = body.Transform.Position + new Vector3(0f, 3.5f, 7f);

    follow = new Basic3DFollowCameraController
    {
        Target = body,
        TargetOffset = new Vector3(0f, 1.2f, 0f),
        ObstacleTest = ClearLine,
    };

    camera.Add(follow);

    TextOverlay.GetOrCreate(game).AddSection("Third-person camera", BuildOverlayLines);
}

void Update(Scene scene, GameTime time)
{
    var input = game.Input;

    // Forward is away from the camera along the ground, whatever the camera's pitch
    var (sinYaw, cosYaw) = MathF.SinCos(follow.Yaw);
    var forward = new Vector3(-sinYaw, 0f, -cosYaw);
    var right = new Vector3(cosYaw, 0f, -sinYaw);
    var move = Vector3.Zero;

    if (input.IsKeyDown(Keys.W) || input.IsKeyDown(Keys.Up)) move += forward;
    if (input.IsKeyDown(Keys.S) || input.IsKeyDown(Keys.Down)) move -= forward;
    if (input.IsKeyDown(Keys.A) || input.IsKeyDown(Keys.Left)) move -= right;
    if (input.IsKeyDown(Keys.D) || input.IsKeyDown(Keys.Right)) move += right;

    if (move.LengthSquared() > 0f)
    {
        move.Normalize();

        if (input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift)) move *= SprintMultiplier;

        // The nose eases round to the walk direction; its yaw is the direction's angle in the ground plane
        noseYaw = TurnTowards(noseYaw, MathF.Atan2(-move.X, -move.Z), NoseTurnSpeed * (float)time.Elapsed.TotalSeconds);
    }

    // The move vector's Y is the world's Z: the capsule keeps the identity orientation, so the frames agree
    character.MoveVector = new Vector2(move.X, move.Z);
    nose.Transform.Rotation = Quaternion.RotationY(noseYaw);
    nose.Transform.Position = Vector3.Transform(new Vector3(0f, 0.3f, -0.4f), nose.Transform.Rotation);

    if (input.IsKeyPressed(Keys.Space)) character.TryJump();
}

/// <summary>An angle eased towards another the short way round, by a fraction of the gap clamped to the whole of it.</summary>
static float TurnTowards(float current, float target, float fraction)
{
    var gap = MathF.IEEERemainder(target - current, MathUtil.TwoPi);

    return current + gap * MathF.Min(fraction, 1f);
}

/// <summary>The camera's wall test: the distance to the first piece of scenery on the line, or null for a clear one.</summary>
float? ClearLine(Vector3 from, Vector3 to)
{
    var line = to - from;
    var length = line.Length();

    if (length < MathUtil.ZeroTolerance) return null;

    return simulation.RayCast(from, line / length, length, out var hit, SceneryMask) ? hit.Distance : null;
}

/// <summary>A stepped pyramid of static blocks to climb and hide behind, and a ring of pillars to walk between.</summary>
void BuildScenery(Scene scene)
{
    var stone = game.CreateMaterial(new Color(150, 140, 130), metalness: 0f, glossiness: 0.3f);
    var pillarMaterial = game.CreateMaterial(new Color(110, 130, 170), metalness: 0.1f, glossiness: 0.4f);

    game.Add3DGround(new Vector2(70f), new() { EntityName = "Ground", Material = game.CreateMaterial(new Color(120, 125, 120), metalness: 0f, glossiness: 0.2f) });

    // Each level is a square of blocks one block narrower on each side than the one below
    for (var level = 0; level < PyramidLevels; level++)
    {
        var half = PyramidLevels - level;

        for (var x = -half; x < half; x++)
        {
            for (var z = -half; z < half; z++)
            {
                // Only the rim of each level is a block: the inside is never seen or touched
                if (x > -half && x < half - 1 && z > -half && z < half - 1) continue;

                var block = game.CreatePrimitive(Primitive3D.Cube(PyramidStep), new BepuPhysicsOptions
                {
                    EntityName = "Step",
                    Body = BodyMode.Static,
                    Material = stone,
                    ShareModel = true,
                    Position = new Vector3((x + 0.5f) * PyramidStep, (level + 0.5f) * PyramidStep, (z + 0.5f) * PyramidStep),
                });

                block.Scene = scene;
            }
        }
    }

    // Eight pillars round the pyramid, none of them straight behind the character's start
    for (var i = 0; i < 8; i++)
    {
        var angle = (i + 0.5f) * MathUtil.TwoPi / 8f;
        var pillar = game.CreatePrimitive(Primitive3D.Cylinder(0.5f, 4f), new BepuPhysicsOptions
        {
            EntityName = "Pillar",
            Body = BodyMode.Static,
            Material = pillarMaterial,
            ShareModel = true,
            Position = new Vector3(MathF.Cos(angle) * 21f, 2f, MathF.Sin(angle) * 21f),
        });

        pillar.Scene = scene;
    }
}

IReadOnlyList<TextElement> BuildOverlayLines() =>
[
    new(["W", "A", "S", "D"], "Walk, relative to the camera", Color.Gold),
    new("Shift", "Sprint", Color.Gold),
    new("Space", "Jump", Color.Gold),
    new("Right drag", "Swing the camera round", Color.Gold),
    new("Mouse wheel", "Closer or further", Color.Gold),
    new(""),
    new("Walk behind a pillar or down a step: the camera comes forward", Color.White),
];

/*
---example-metadata
slug: third-person-camera
title:
  en: Third-Person Camera
level: Intermediate
category: Physics
complexity: 2
order: 121
description:
  en: |-
    A character on a stepped pyramid with the toolkit's follow camera behind it: the camera trails
    the character's steps with a short lag, swings round it on a right-drag, zooms on the wheel, and
    comes forward rather than look through a pillar or a step, through a raycast the game lends it.
    WASD walks the character relative to the camera's yaw, with a nose that turns to face the way it
    goes.
concepts:
  - Basic3DFollowCameraController on the camera entity, with a Target and a TargetOffset
  - An ObstacleTest from a Bepu raycast, with the character on a layer the ray ignores
  - Walking relative to the camera's yaw through CharacterComponent.MoveVector
  - A child entity that turns while the physics capsule keeps its orientation
  - "Using helpers: SceneSetup3D, UseBepu, UseSkybox, Add3DGround, CreatePrimitive, CreateMaterial, GetCameraEntity, RayCast, TextOverlay"
tags:
  - 3D
  - Physics
  - Bepu
  - Camera
  - Character
related:
  - E05_3D_FirstPersonCharacter
  - E11_3D_Charts
screenshotFrame: 120
enabled: true
created: 2026-11-01
---
*/