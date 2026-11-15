using Box2D.NET;
using E06_Box2D_CharacterMover;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Scripts;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;

// A platformer character that is not a rigid body. Ported from the Box2D.NET samples' Mover
// scene (MIT, (c) 2022 Erin Catto, (c) 2025 Choi Ikpil).
//
// A rigid-body character tips, bounces, sticks to walls and needs its rotation locked and its
// friction faked. Box2D v3's mover API is the alternative: the character is a capsule the game
// moves itself, Quake style, asking the world only what it touches - collect the contact planes,
// solve a translation that respects them, sweep it. A "pogo" shape cast down from the feet drives
// a spring that floats the capsule above the ground and tells it whether it is standing.
// CharacterMover2D is that recipe; this scene is the samples' course for it.
//
// The level is two Inkscape outlines read with SvgPath2D into chain shapes; a fifty-plank bridge
// on sprung revolute joints sags under the character; a bouncy ball can be kicked; a "friendly"
// capsule is soft - the mover walks through it slowly - and an elevator is rigid and carries the
// mover. That softness is per shape: a push limit and a clip flag in the shape's user data.
//
// A and D walk, Space jumps, K kicks the ball, Z picks the pogo shape, G shows Box2D's own
// contact points, R resets. The character walks by itself, jumping walls, until A or D is pressed.

// Shared by the local functions: set in Start, read every frame by Update and Draw.
ShapeBatch shapeBatch = null!;
Basic2DCameraController cameraController = null!;
Box2DDebugDraw debugDraw = null!;
CharacterMover2D mover = null!;
ShapeComponent heroShape = null!;
KeyMenu pogoMenu = null!;
IReadOnlyList<Vector2[]> terrain = [];
B2BodyId ballBody = default;
var showContacts = false;
var autoWalk = true;
var kickFlash = 0f;

// The testbed grey the samples draw on.
var background = new Color(0.2f, 0.2f, 0.2f);

using var game = new Game();
using var simulation = new Box2DSimulation();

var sceneSetup = new SceneSetup2D
{
    ClearColor = background,
    Camera = CameraSetup.Orthographic(orthographicSize: 18).WithController(),
    Ground = null,
    Profiler = true,
    WindowTitle = "Box2D Character Mover - Stride Community Toolkit",
    Resizable = true,
};

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    var cameraEntity = game.GetCameraEntity();
    cameraController = cameraEntity.Get<Basic2DCameraController>();

    shapeBatch = game.AddShapeBatch();
    shapeBatch.BorderWidth = 1f;
    shapeBatch.Fill.Alpha = 0.4f;

    // Box2D's own view of the contacts, for when the planes the mover collects are not enough.
    debugDraw = new Box2DDebugDraw(shapeBatch) { DrawShapes = false, DrawJoints = false, DrawContactPoints = true, DrawContactNormals = true, FillAlpha = shapeBatch.Fill.Alpha };

    game.AddGrabber2D(simulation);

    // The course is Course.cs: the grounds, the bridge, the friend, the ball, the elevator and the hero
    var course = Course.Build(simulation, scene);

    mover = course.Mover;
    heroShape = course.HeroShape;
    ballBody = course.Ball;
    terrain = course.Terrain;

    // The camera follows the hero, looking a little above it.
    cameraController.FollowTarget = course.Hero;
    cameraController.FollowOffset = new Vector3(0, 2, 0);

    SetupMenu();
    AddInstructions();
}

void SetupMenu()
{
    pogoMenu = new KeyMenu
    {
        Title = "Pogo shape",
        ToggleKey = Keys.Z,
        TitleColor = Color.Yellow,
        SelectedIndex = 2,
        Items =
        [
            new(Keys.D1, "point - cheapest, slips off ledges soonest", () => mover.PogoShape = PogoShape.Point),
            new(Keys.D2, "circle - smooths small steps", () => mover.PogoShape = PogoShape.Circle),
            new(Keys.D3, "segment - stands on ledges the point misses", () => mover.PogoShape = PogoShape.Segment),
        ],
    };
}

void Update(Scene scene, GameTime time)
{
    simulation.Update(time.Elapsed);

    var input = game.Input;

    // A frame whose key press the pogo menu claims gets no play input, but is drawn like any other
    if (!pogoMenu.Update(input)) HandlePlayInput(input);

    kickFlash = MathF.Max(0, kickFlash - (float)time.Elapsed.TotalSeconds);

    Draw();
}

void HandlePlayInput(InputManager input)
{
    // The first press of A or D hands the character to the player until R.
    if (input.IsKeyDown(Keys.A) || input.IsKeyDown(Keys.D))
    {
        autoWalk = false;
    }

    mover.Throttle = autoWalk ? 1 : (input.IsKeyDown(Keys.D) ? 1 : 0) - (input.IsKeyDown(Keys.A) ? 1 : 0);

    // On auto-walk the character jumps at any wall plane in its way, so the course plays through untouched.
    if (autoWalk && mover.IsOnGround && BlockedByWall())
    {
        mover.Jump();
    }

    if (input.IsKeyPressed(Keys.Space)) mover.Jump();
    if (input.IsKeyPressed(Keys.K)) Kick();
    if (input.IsKeyPressed(Keys.G)) showContacts = !showContacts;
    if (input.IsKeyPressed(Keys.R)) Reset();
}

// The sample's kick: an overlap just below the feet, restricted to debris, and an impulse away
// from the mover on whatever dynamic body it finds.
void Kick()
{
    var centre = KickCentre();
    var filter = new B2QueryFilter(CharacterMover2D.MoverCategory, CharacterMover2D.DebrisCategory);

    foreach (var body in simulation.OverlapCircle(centre, 0.5f, filter))
    {
        if (b2Body_GetType(body) != B2BodyType.b2_dynamicBody) continue;

        // Away from the mover and up, so the ball lifts rather than skids.
        var target = b2Body_GetWorldCenterOfMass(body);
        var direction = b2Normalize(target - new B2Vec2(mover.Position.X, mover.Position.Y));

        BodyForces.ApplyImpulse(body, new Vector2(2f * direction.X, 2f));
    }

    kickFlash = 0.2f;
}

Vector2 KickCentre() => mover.Bottom - new Vector2(0, 3f * mover.Radius);

// A contact plane facing mostly sideways, against the direction of travel, is a wall.
bool BlockedByWall()
{
    foreach (var plane in mover.Planes)
    {
        if (MathF.Abs(plane.plane.normal.X) > 0.7f && MathF.Sign(plane.plane.normal.X) != MathF.Sign(mover.Throttle))
        {
            return true;
        }
    }

    return false;
}

void Reset()
{
    mover.Teleport(Course.HeroStart);
    BodyForces.TeleportAtRest(ballBody, Course.BallStart);
    autoWalk = true;
}

void Draw()
{
    // The level outlines, closed, in the samples' pale green.
    foreach (var outline in terrain)
    {
        for (var i = 0; i < outline.Length; i++)
        {
            shapeBatch.DrawPixelLine(outline[i], outline[(i + 1) % outline.Length], 2f, Color.PaleGreen);
        }
    }

    // Orange on the ground, aquamarine in the air: the pogo's verdict, painted on the capsule.
    heroShape.Color = mover.IsOnGround ? Color.Orange : Color.Aquamarine;

    // The contact planes the mover collected: a dot on the capsule's surface and the normal.
    foreach (var plane in mover.Planes)
    {
        var normal = new Vector2(plane.plane.normal.X, plane.plane.normal.Y);
        var p1 = mover.Position + (plane.plane.offset - mover.Radius) * normal;
        var p2 = p1 + 0.1f * normal;

        shapeBatch.DrawPixelDisc(new Vector3(p1, 0), 4f, Color.Yellow);
        shapeBatch.DrawPixelLine(p1, p2, 2f, Color.Yellow);
    }

    // The pogo: grey while it reaches nothing, plum when it stands on something.
    var pogoColor = mover.PogoHit ? Color.Plum : Color.Gray;
    var end = mover.PogoEnd;

    shapeBatch.DrawPixelLine(mover.PogoOrigin, end, 2f, pogoColor);

    switch (mover.PogoShape)
    {
        case PogoShape.Point:
            shapeBatch.DrawPixelDisc(new Vector3(end, 0), 5f, pogoColor);
            break;
        case PogoShape.Circle:
            shapeBatch.DrawRing(end, 0.5f * mover.Radius, pogoColor);
            break;
        default:
            var half = new Vector2(0.75f * mover.Radius, 0);
            shapeBatch.DrawPixelLine(end - half, end + half, 2f, pogoColor);
            break;
    }

    // Velocity, drawn as a line from the centre.
    shapeBatch.DrawPixelLine(mover.Position, mover.Position + mover.Velocity, 2f, Color.Purple);

    if (kickFlash > 0)
    {
        shapeBatch.DrawRing(KickCentre(), 0.5f, Color.Goldenrod);
    }

    if (showContacts) debugDraw.Draw(simulation);
}

void AddInstructions()
{
    TextOverlay.GetOrCreate(game).AddSection("Mover", () =>
    {
        var m = mover;

        return
        [
            new(["A", "D"], "Walk", Color.Yellow),
            new("Space", "Jump", Color.Yellow),
            new("K", "Kick the ball", Color.Yellow),
            new("G", $"Box2D contact points {(showContacts ? "on" : "off")}", Color.Yellow),
            new("R", "Reset", Color.Yellow),
            new("Left mouse", "Throw things at the character", Color.Yellow),
            .. pogoMenu.GetLines(),
            new(""),
            new($"Position {m.Position.X:0.00}, {m.Position.Y:0.00}, {(m.IsOnGround ? "on ground" : "in the air")}", Color.LightGreen),
            new($"Velocity {m.Velocity.X:0.00}, {m.Velocity.Y:0.00}", Color.LightGreen),
            new($"Planes {m.Planes.Length}, solver iterations {m.IterationsLastStep}"),
        ];
    });
}

/*
---example-metadata
slug: box2d-character-mover
title:
  en: Box2D Character Mover
level: Intermediate
category: Physics
complexity: 4
order: 94
description:
  en: |-
    A platformer character with no rigid body, on Box2D v3's mover API: a capsule the game moves
    itself, Quake style, asking the world only what it touches - collect the contact planes, solve
    a translation that respects them, sweep it - with a pogo shape cast from the feet that floats
    it above the ground. The course is the samples' own: two Inkscape outlines read into chains,
    a fifty-plank bridge that sags under the character, a ball to kick, a soft capsule to walk
    through and a rigid elevator to ride. A and D walk, Space jumps, K kicks, Z picks the pogo
    shape, and the grabber throws things at the character.
concepts:
  - "A character as a capsule plus a transform, not a body: why a rigid-body character fights you"
  - "The mover loop: CollideMover for planes, SolvePlanes for a translation, CastMover for the sweep"
  - The pogo spring from a shape cast, and how it decides whether the character is on the ground
  - "Per-shape softness: a push limit and a velocity clip flag in the shape's user data"
  - Category bits so the mover overlaps some shapes, sweeps against others and kicks the rest
  - Level outlines from SVG paths with SvgPath2D, as closed chain shapes
  - A kinematic elevator driven by target transforms inside the fixed step
  - "Using helpers: CharacterMover2D, SvgPath2D, Joints2D.CreateRevolute, Grabber2DScript, Box2DDebugDraw, KeyMenu"
tags:
  - 2D
  - Box2D
  - Physics
  - Character
  - Platformer
  - Terrain
  - Third Party
related:
  - E06_Box2D_Car
  - E06_Box2D_Joints
  - E06_Box2D
screenshotFrame: 600
enabled: true
created: 2026-09-06
---
*/