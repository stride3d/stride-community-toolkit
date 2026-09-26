using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Scripts;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

// A platformer character that is not a rigid body. Ported from the Box2D.NET samples' Mover
// scene (MIT, (c) 2022 Erin Catto, (c) 2025 Choi Ikpil).
//
// A rigid-body character fights you: it tips, it bounces, it sticks to walls, it needs its
// rotation locked and its friction faked. Box2D v3 offers another way - the mover API - where the
// character is just a capsule the game moves itself, Quake style, and the world is asked only what
// it touches: collect the contact planes, solve a translation that respects them, sweep it. A
// "pogo" shape cast down from the feet drives a spring that floats the capsule above the ground
// and tells it whether it is standing. CharacterMover2D is that recipe; this scene is the samples'
// course for it.
//
// The level is two outlines drawn in Inkscape and read with SvgPath2D into chain shapes; a
// fifty-plank bridge on sprung revolute joints sags under the character; a bouncy ball can be
// kicked; a "friendly" capsule is soft - the mover walks through it slowly - and an elevator is
// rigid and carries the mover. That softness is per shape: a push limit and a clip flag in the
// shape's user data.
//
// A and D walk, Space jumps, K kicks the ball, Z picks the pogo shape, G shows Box2D's own
// contact points, R resets. The character walks by itself, jumping walls, until A or D is pressed.

const float Scale = 0.2f;
const int PlankCount = 50;
const float BridgeStart = 48.7f;
const float BridgeHeight = 9.2f;
const float ElevatorAmplitude = 4f;

var elevatorBase = new Vector2(112, 10);
var spawn = new Vector2(2, 8);
var ballStart = new Vector2(7, 7);

Box2DSimulation? simulation = null;
ShapeBatch? shapeBatch = null;
Basic2DCameraController? cameraController = null;
Box2DDebugDraw? debugDraw = null;
CharacterMover2D? mover = null;
ShapeComponent? heroShape = null;
DebugTextDropdown? pogoMenu = null;
List<Vector2[]> terrain = [];
B2BodyId ballBody = default;
var showContacts = false;
var autoWalk = true;
var kickFlash = 0f;

var background = new Color(0.2f, 0.2f, 0.2f);
var paleGreen = new Color(0x98, 0xFB, 0x98);
var royalBlue = new Color(0x41, 0x69, 0xE1);
var orange = new Color(0xFF, 0xA5, 0x00);
var aquamarine = new Color(0x7F, 0xFF, 0xD4);
var plum = new Color(0xDD, 0xA0, 0xDD);
var goldenRod = new Color(0xDA, 0xA5, 0x20);
var purple = new Color(0x80, 0x00, 0x80);

using var game = new Game();

game.Run(start: Start, update: Update);

simulation?.Dispose();

void Start(Scene scene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Box2D Character Mover - Stride Community Toolkit";

    game.SetupBase2D(clearColor: background);
    var cameraEntity = game.Add2DCameraController();
    cameraController = cameraEntity.Get<Basic2DCameraController>();
    game.AddProfiler();

    var camera = scene.GetCamera() ?? throw new InvalidOperationException("Camera not found in scene");
    camera.OrthographicSize = 18;

    shapeBatch = game.AddShapeBatch();
    shapeBatch.BorderWidth = 1f;
    shapeBatch.Fill.Alpha = 0.4f;

    simulation = new Box2DSimulation();

    // Box2D's own view of the contacts, for when the planes the mover collects are not enough.
    debugDraw = new Box2DDebugDraw(shapeBatch) { DrawShapes = false, DrawJoints = false, DrawContactPoints = true, DrawContactNormals = true };

    cameraEntity.Add(new Grabber2DScript { Simulation = simulation });

    var ground1 = BuildGround(GroundPath1, new Vector2(0, 0), new Vector2(-50, -200));
    var ground2 = BuildGround(GroundPath2, new Vector2(98, 0), new Vector2(0, -200));

    BuildBridge(scene, ground1, ground2);
    BuildFriend(scene);
    BuildBall(scene);
    BuildElevator(scene);
    BuildHero(scene);
    SetupMenu();
    AddInstructions();
}

// One ground outline: the sample's Inkscape path, offset and scaled into metres, as a closed chain
// on a static body at bodyPosition. Chain points are in the body's frame, so the drawn copy is
// shifted into the world.
B2BodyId BuildGround(string path, Vector2 bodyPosition, Vector2 svgOffset)
{
    var local = SvgPath2D.Parse(path, svgOffset, Scale);
    var body = simulation!.CreateStaticBody(new Vector3(bodyPosition, 0));

    ShapeFixtureBuilder.AttachChain(local, body, isLoop: true);
    terrain.Add([.. local.Select(p => p + bodyPosition)]);

    return body;
}

// Fifty planks on revolute joints with a soft spring and a small motor torque - the spring keeps
// the bridge from folding, the motor gives it a little stiffness - hung between the two grounds.
void BuildBridge(Scene scene, B2BodyId ground1, B2BodyId ground2)
{
    var options = new RevoluteJointOptions { EnableSpring = true, Hertz = 3, DampingRatio = 0.8f, EnableMotor = true, MaxMotorTorque = 10 };
    Vector2[] plank = [new(-0.5f, -0.125f), new(0.5f, -0.125f), new(0.5f, 0.125f), new(-0.5f, 0.125f)];
    var previous = ground1;

    for (var i = 0; i < PlankCount; i++)
    {
        var centre = new Vector2(BridgeStart + 0.5f + i, BridgeHeight);
        var entity = new Entity("Plank") { new ShapeComponent { Vertices = [.. plank], Color = royalBlue } };
        entity.Transform.Position = new Vector3(centre, 0);
        entity.Scene = scene;

        var body = simulation!.CreateDynamicBody(entity, new Vector3(centre, 0));
        b2Body_SetAngularDamping(body, 0.2f);
        ShapeFixtureBuilder.AttachShape(Primitive2DModelType.Rectangle, new Vector2(1f, 0.25f), body);
        entity.Add(new Box2DBodyComponent { BodyId = body });

        simulation.Joints.CreateRevolute(previous, body, new Vector2(BridgeStart + i, BridgeHeight), options);
        previous = body;
    }

    simulation!.Joints.CreateRevolute(previous, ground2, new Vector2(BridgeStart + PlankCount, BridgeHeight), options);
}

// A static capsule in the mover category: the mover overlaps it but never sweeps against it, and
// a tiny push limit with no velocity clip makes it soft - walk into it and you pass through slowly.
void BuildFriend(Scene scene)
{
    var position = new Vector2(32, 4.5f);
    var entity = new Entity("Friend") { new ShapeComponent { Vertices = [new(0, -0.5f), new(0, 0.5f)], Radius = 0.3f, Color = paleGreen } };
    entity.Transform.Position = new Vector3(position, 0);
    entity.Scene = scene;

    var body = simulation!.CreateStaticBody(entity, new Vector3(position, 0));
    var def = b2DefaultShapeDef();
    def.filter = new B2Filter(CharacterMover2D.MoverCategory, ulong.MaxValue, 0);
    var capsule = new B2Capsule(new B2Vec2(0, -0.5f), new B2Vec2(0, 0.5f), 0.3f);
    var shape = b2CreateCapsuleShape(body, in def, in capsule);

    CharacterMover2D.SetResponse(shape, maxPush: 0.025f, clipVelocity: false);
}

// A bouncy ball in the debris category: the mover walks through it and K kicks it.
void BuildBall(Scene scene)
{
    var entity = new Entity("Ball") { new ShapeComponent { Vertices = [Vector2.Zero], Radius = 0.3f, Color = goldenRod } };
    entity.Transform.Position = new Vector3(ballStart, 0);
    entity.Scene = scene;

    ballBody = simulation!.CreateDynamicBody(entity, new Vector3(ballStart, 0));
    var def = b2DefaultShapeDef();
    def.filter = new B2Filter(CharacterMover2D.DebrisCategory, ulong.MaxValue, 0);
    def.material.restitution = 0.7f;
    def.material.rollingResistance = 0.2f;
    ShapeFixtureBuilder.AttachShape(Primitive2DModelType.Circle, new Vector2(0.3f, 0.3f), ballBody, def);
    entity.Add(new Box2DBodyComponent { BodyId = ballBody });
}

// A kinematic platform in the dynamic category, rigid to the mover: a push limit of ten
// centimetres a step and velocity clipping, so it carries the mover rather than passing through.
void BuildElevator(Scene scene)
{
    var start = elevatorBase - new Vector2(0, ElevatorAmplitude);
    Vector2[] slab = [new(-2, -0.1f), new(2, -0.1f), new(2, 0.1f), new(-2, 0.1f)];
    var entity = new Entity("Elevator") { new ShapeComponent { Vertices = [.. slab], Color = plum } };
    entity.Transform.Position = new Vector3(start, 0);
    entity.Scene = scene;

    var body = simulation!.CreateKinematicBody(entity, new Vector3(start, 0));
    var def = b2DefaultShapeDef();
    def.filter = new B2Filter(CharacterMover2D.DynamicCategory, ulong.MaxValue, 0);
    var box = b2MakeBox(2f, 0.1f);
    var shape = b2CreatePolygonShape(body, in def, in box);

    CharacterMover2D.SetResponse(shape, maxPush: 0.1f, clipVelocity: true);
    entity.Add(new Box2DBodyComponent { BodyId = body });

    simulation.RegisterSimulationUpdate(new Elevator(body, elevatorBase, ElevatorAmplitude));
}

// The character: an entity with a capsule outline, and the mover that drives it. Registered with
// the simulation, the mover steps itself after every fixed physics step.
void BuildHero(Scene scene)
{
    heroShape = new ShapeComponent { Vertices = [new(0, -0.5f), new(0, 0.5f)], Radius = 0.3f, Color = orange };
    var hero = new Entity("Hero") { heroShape };
    hero.Transform.Position = new Vector3(spawn, 0);
    hero.Scene = scene;

    mover = new CharacterMover2D(spawn) { Entity = hero };
    simulation!.RegisterSimulationUpdate(mover);

    cameraController!.FollowTarget = hero;
    cameraController.FollowOffset = new Vector3(0, 2, 0);
}

void SetupMenu()
{
    pogoMenu = new DebugTextDropdown
    {
        Title = "Pogo shape",
        ToggleKey = Keys.Z,
        TitleColor = Color.Yellow,
        SelectedIndex = 2,
        Items =
        [
            new(Keys.D1, "point - cheapest, slips off ledges soonest", () => mover!.PogoShape = PogoShape.Point),
            new(Keys.D2, "circle - smooths small steps", () => mover!.PogoShape = PogoShape.Circle),
            new(Keys.D3, "segment - stands on ledges the point misses", () => mover!.PogoShape = PogoShape.Segment),
        ],
    };
}

void Update(Scene scene, GameTime time)
{
    simulation?.Update(time.Elapsed);

    var input = game.Input;

    if (pogoMenu is not null && pogoMenu.Update(input)) return;

    if (mover is null) return;

    if (input.IsKeyDown(Keys.A) || input.IsKeyDown(Keys.D))
        autoWalk = false;

    mover.Throttle = autoWalk ? 1 : (input.IsKeyDown(Keys.D) ? 1 : 0) - (input.IsKeyDown(Keys.A) ? 1 : 0);

    // Walking by itself, the character jumps whenever a wall plane is in its way - the pillar, the
    // stairs up to the bridge, the far ground's steps - so the whole course plays through untouched.
    if (autoWalk && mover.IsOnGround && BlockedByWall())
        mover.Jump();

    if (input.IsKeyPressed(Keys.Space))
        mover.Jump();

    if (input.IsKeyPressed(Keys.K))
        Kick();

    if (input.IsKeyPressed(Keys.G))
        showContacts = !showContacts;

    if (input.IsKeyPressed(Keys.R))
        Reset();

    kickFlash = MathF.Max(0, kickFlash - (float)time.Elapsed.TotalSeconds);

    Draw();
}

// The sample's kick: an overlap just below the feet, restricted to debris, and an impulse away
// from the mover on whatever dynamic body it finds.
void Kick()
{
    var centre = KickCentre();
    var filter = new B2QueryFilter(CharacterMover2D.MoverCategory, CharacterMover2D.DebrisCategory);

    foreach (var body in simulation!.OverlapCircle(centre, 0.5f, filter))
    {
        if (b2Body_GetType(body) != B2BodyType.b2_dynamicBody) continue;

        var target = b2Body_GetWorldCenterOfMass(body);
        var direction = b2Normalize(target - new B2Vec2(mover!.Position.X, mover.Position.Y));

        b2Body_ApplyLinearImpulseToCenter(body, new B2Vec2(2f * direction.X, 2f), true);
    }

    kickFlash = 0.2f;
}

Vector2 KickCentre() => mover!.Bottom - new Vector2(0, 3f * mover.Radius);

// A contact plane facing mostly sideways, against the direction of travel, is a wall.
bool BlockedByWall()
{
    foreach (var plane in mover!.Planes)
    {
        if (MathF.Abs(plane.plane.normal.X) > 0.7f && MathF.Sign(plane.plane.normal.X) != MathF.Sign(mover.Throttle))
            return true;
    }

    return false;
}

void Reset()
{
    mover!.Teleport(spawn);
    b2Body_SetTransform(ballBody, new B2Vec2(ballStart.X, ballStart.Y), b2Rot_identity);
    b2Body_SetLinearVelocity(ballBody, b2Vec2_zero);
    b2Body_SetAngularVelocity(ballBody, 0f);
    autoWalk = true;
}

void Draw()
{
    if (shapeBatch is null || mover is null) return;

    foreach (var outline in terrain)
    {
        for (var i = 0; i < outline.Length; i++)
            shapeBatch.DrawPixelLine(new Vector3(outline[i], 0), new Vector3(outline[(i + 1) % outline.Length], 0), 2f, paleGreen);
    }

    heroShape!.Color = mover.IsOnGround ? orange : aquamarine;

    // The contact planes the mover collected: a dot on the capsule's surface and the normal.
    foreach (var plane in mover.Planes)
    {
        var normal = new Vector2(plane.plane.normal.X, plane.plane.normal.Y);
        var p1 = mover.Position + (plane.plane.offset - mover.Radius) * normal;
        var p2 = p1 + 0.1f * normal;

        shapeBatch.DrawPixelDisc(new Vector3(p1, 0), 4f, Color.Yellow);
        shapeBatch.DrawPixelLine(new Vector3(p1, 0), new Vector3(p2, 0), 2f, Color.Yellow);
    }

    // The pogo: grey while it reaches nothing, plum when it stands on something.
    var pogoColor = mover.PogoHit ? plum : Color.Gray;
    var end = mover.PogoEnd;

    shapeBatch.DrawPixelLine(new Vector3(mover.PogoOrigin, 0), new Vector3(end, 0), 2f, pogoColor);

    switch (mover.PogoShape)
    {
        case PogoShape.Point:
            shapeBatch.DrawPixelDisc(new Vector3(end, 0), 5f, pogoColor);
            break;
        case PogoShape.Circle:
            shapeBatch.DrawRing(new Vector3(end, 0), Vector3.UnitZ, 0.5f * mover.Radius, pogoColor);
            break;
        default:
            var half = new Vector2(0.75f * mover.Radius, 0);
            shapeBatch.DrawPixelLine(new Vector3(end - half, 0), new Vector3(end + half, 0), 2f, pogoColor);
            break;
    }

    shapeBatch.DrawPixelLine(new Vector3(mover.Position, 0), new Vector3(mover.Position + mover.Velocity, 0), 2f, purple);

    if (kickFlash > 0)
        shapeBatch.DrawRing(new Vector3(KickCentre(), 0), Vector3.UnitZ, 0.5f, goldenRod);

    if (showContacts && simulation is not null)
        debugDraw?.Draw(simulation);
}

void AddInstructions()
{
    var overlay = DebugOverlay.GetOrCreate(game);

    overlay.Position = DisplayPosition.BottomLeft;

    overlay.AddSection("Mover", () =>
    {
        var m = mover!;

        return
        [
            new($"position {m.Position.X,7:0.00} {m.Position.Y,6:0.00}   velocity {m.Velocity.X,6:0.00} {m.Velocity.Y,6:0.00}   {(m.IsOnGround ? "on ground" : "in the air")}", Color.LightGreen),
            new($"planes {m.Planes.Length}   solver iterations {m.IterationsLastStep}"),
            new("A / D  walk      Space  jump      K  kick the ball      Left mouse  throw things at the character", Color.Yellow),
            new($"G  Box2D contact points {(showContacts ? "on" : "off")}      R  reset"),
            .. pogoMenu?.GetLines() ?? [],
        ];
    });
}

// The kinematic elevator: a cosine ride set as a target transform each fixed step, so it carries
// a velocity the mover's planes and the contact solver both feel.
sealed class Elevator(B2BodyId body, Vector2 origin, float amplitude) : IBox2DSimulationUpdate
{
    private float _time;

    public void SimulationUpdate(Box2DSimulation simulation, float deltaTime)
    {
        _time += deltaTime;

        var y = amplitude * MathF.Cos(_time + MathF.PI) + origin.Y;
        var target = new B2Transform(new B2Vec2(origin.X, y), b2Rot_identity);

        b2Body_SetTargetTransform(body, in target, deltaTime, true);
    }

    public void AfterSimulationUpdate(Box2DSimulation simulation, float deltaTime) { }
}

// The two grounds of the samples' Mover scene, as drawn in Inkscape.
static partial class Program
{
    public const string GroundPath1 =
        "M 2.6458333,201.08333 H 293.68751 v -47.625 h -2.64584 l -10.58333,7.9375 -13.22916,7.9375 -13.24648,5.29167 "
        + "-31.73269,7.9375 -21.16667,2.64583 -23.8125,10.58333 H 142.875 v -5.29167 h -5.29166 v 5.29167 H 119.0625 v "
        + "-2.64583 h -2.64583 v -2.64584 h -2.64584 v -2.64583 H 111.125 v -2.64583 H 84.666668 v -2.64583 h -5.291666 v "
        + "-2.64584 h -5.291667 v -2.64583 H 68.791668 V 174.625 h -5.291666 v -2.64584 H 52.916669 L 39.6875,177.27083 H "
        + "34.395833 L 23.8125,185.20833 H 15.875 L 5.2916669,187.85416 V 153.45833 H 2.6458333 v 47.625";

    public const string GroundPath2 =
        "M 2.6458333,201.08333 H 293.68751 l 0,-23.8125 h -23.8125 l 21.16667,21.16667 h -23.8125 l -39.68751,-13.22917 "
        + "-26.45833,7.9375 -23.8125,2.64583 h -13.22917 l -0.0575,2.64584 h -5.29166 v -2.64583 l -7.86855,-1e-5 "
        + "-0.0114,-2.64583 h -2.64583 l -2.64583,2.64584 h -7.9375 l -2.64584,2.64583 -2.58891,-2.64584 h -13.28609 v "
        + "-2.64583 h -2.64583 v -2.64584 l -5.29167,1e-5 v -2.64583 h -2.64583 v -2.64583 l -5.29167,-1e-5 v -2.64583 h "
        + "-2.64583 v -2.64584 h -5.291667 v -2.64583 H 92.60417 V 174.625 h -5.291667 v -2.64584 l -34.395835,1e-5 "
        + "-7.9375,-2.64584 -7.9375,-2.64583 -5.291667,-5.29167 H 21.166667 L 13.229167,158.75 5.2916668,153.45833 H "
        + "2.6458334 l -10e-8,47.625";
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
  - "Using helpers: CharacterMover2D, SvgPath2D, Joints2D.CreateRevolute, Grabber2DScript, Box2DDebugDraw, DebugTextDropdown"
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