using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using static Box2D.NET.B2RevoluteJoints;
using static Box2D.NET.B2WheelJoints;

// Every Box2D joint, one rig each, in a row you can pull on.
//
// A joint definition wants its anchors as local frames - a point and a rotation in each body's
// own space - which is why raw Box2D code is full of GetLocalPoint calls. Joints2D takes the pivot
// and the axis in world space and does that once; the pose at creation is the joint's zero. The
// options records carry the per-type knobs under Box2D's own names, and anything left unset keeps
// Box2D's default.
//
// Joints are only interesting when something pulls on them, so the grabber is on the camera: pick
// any body up and drag it, then watch what its joint allows. Left to right: a hinge pendulum with a
// motor and a limit, a slider on a spring, a wheel on a suspension, a rope of distance joints, a
// soft weld, and a motor joint that spins a box and springs it back home. The joints themselves are
// drawn by Box2D's own debug draw through the shape batch - Box2DDebugDraw - which can also show
// contact points, bounding boxes and centres of mass on request.

Box2DSimulation? simulation = null;
ShapeBatch? shapeBatch = null;
Box2DDebugDraw? debugDraw = null;

var pink = new Color(0xFF, 0xC0, 0xCB);
var paleGreen = new Color(0x98, 0xFB, 0x98);
var royalBlue = new Color(0x41, 0x69, 0xE1);
var gold = new Color(0xFF, 0xD7, 0x00);
var background = new Color(0.2f, 0.2f, 0.2f);

List<B2JointId> joints = [];
List<Entity> spawned = [];

B2JointId pendulumJoint = default;
B2JointId wheelJoint = default;
var pendulumMotor = false;
var pendulumLimit = false;
var wheelMotor = true;

using var game = new Game();

game.Run(start: Start, update: Update);

simulation?.Dispose();

void Start(Scene rootScene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Box2D Joints - Stride Community Toolkit";

    game.SetupBase2D(clearColor: background);
    var cameraEntity = game.Add2DCameraController();
    game.AddProfiler();

    var camera = rootScene.GetCamera() ?? throw new InvalidOperationException("Camera not found in scene");
    camera.Entity.Transform.Position = new Vector3(0, 7, 50);
    camera.OrthographicSize = 26;

    shapeBatch = game.AddShapeBatch();
    shapeBatch.BorderWidth = 1f;
    shapeBatch.Fill.Alpha = 0.4f;

    simulation = new Box2DSimulation();

    // Box2D's own debug draw through the batch: joints, and on request contacts, bounds and mass.
    // Shapes stay off - the entities' ShapeComponents already draw the bodies.
    debugDraw = new Box2DDebugDraw(shapeBatch) { DrawShapes = false, DrawJointExtras = true };

    // The 2D grabber: left mouse picks any dynamic body up. Every rig below is built to be pulled.
    cameraEntity.Add(new Grabber2DScript { Simulation = simulation });

    BuildScene(rootScene);
    AddInstructions();
}

void BuildScene(Scene scene)
{
    Ground(scene);

    // 1. Hinge: an arm hanging from a fixed pivot. M switches the motor, L the +-60 degree limit.
    var pivotA = new Vector2(-15, 13);
    var post = StaticBox(scene, pivotA, new Vector2(0.6f, 0.6f));
    var arm = DynamicBox(scene, pivotA + new Vector2(0, -2), new Vector2(0.5f, 4), pink);
    pendulumJoint = simulation!.Joints.CreateRevolute(post, arm, pivotA, new RevoluteJointOptions
    {
        MaxMotorTorque = 500,
        MotorSpeed = 2,
        LowerAngle = -MathUtil.DegreesToRadians(60),
        UpperAngle = MathUtil.DegreesToRadians(60),
    });
    joints.Add(pendulumJoint);

    // 2. Slider: a block on a horizontal rail, sprung back to the middle and limited to +-3.
    var railCentre = new Vector2(-7, 9);
    var rail = StaticBox(scene, railCentre, new Vector2(7, 0.3f));
    var block = DynamicBox(scene, railCentre + new Vector2(0, 0.8f), new Vector2(1.2f, 1.2f), royalBlue);
    joints.Add(simulation.Joints.CreatePrismatic(rail, block, railCentre, Vector2.UnitX, new PrismaticJointOptions
    {
        EnableSpring = true,
        Hertz = 1,
        DampingRatio = 0.3f,
        EnableLimit = true,
        LowerTranslation = -3,
        UpperTranslation = 3,
    }));

    // 3. Wheel: hung from a post on a vertical suspension with a motor. J switches the motor.
    var hub = new Vector2(1, 9);
    var strut = StaticBox(scene, hub + new Vector2(0, 3), new Vector2(0.4f, 6));
    var wheel = DynamicCircle(scene, hub, 1.2f, gold);
    wheelJoint = simulation.Joints.CreateWheel(strut, wheel, hub, Vector2.UnitY, new WheelJointOptions
    {
        EnableSpring = true,
        Hertz = 2,
        DampingRatio = 0.5f,
        EnableLimit = true,
        LowerTranslation = -1.5f,
        UpperTranslation = 1.5f,
        EnableMotor = true,
        MaxMotorTorque = 50,
        MotorSpeed = 6,
    });
    joints.Add(wheelJoint);

    // 4. Rope: four circles hanging from an anchor on soft, limited distance joints.
    var anchor = new Vector2(8, 15);
    var previous = StaticBox(scene, anchor, new Vector2(0.6f, 0.6f));
    var previousPoint = anchor;

    for (var i = 1; i <= 4; i++)
    {
        var point = anchor + new Vector2(0, -1.6f * i);
        var link = DynamicCircle(scene, point, 0.45f, pink);

        joints.Add(simulation.Joints.CreateDistance(previous, link, previousPoint, point, new DistanceJointOptions
        {
            EnableSpring = true,
            Hertz = 4,
            DampingRatio = 0.5f,
            EnableLimit = true,
            MinLength = 1.2f,
            MaxLength = 2.0f,
        }));

        previous = link;
        previousPoint = point;
    }

    // 5. Weld: a T of two boxes, joined by a soft weld that bends when pulled and springs back.
    var stem = DynamicBox(scene, new Vector2(14, 1.6f), new Vector2(0.8f, 3.2f), royalBlue);
    var bar = DynamicBox(scene, new Vector2(14, 3.6f), new Vector2(4, 0.8f), royalBlue);
    joints.Add(simulation.Joints.CreateWeld(stem, bar, new Vector2(14, 3.2f), new WeldJointOptions
    {
        AngularHertz = 3,
        AngularDampingRatio = 0.5f,
    }));

    // 6. Motor joint: spins a box and springs it back to where it started when you throw it.
    var home = new Vector2(20, 9);
    var homePost = StaticBox(scene, home, new Vector2(0.4f, 0.4f));
    var spinner = DynamicBox(scene, home, new Vector2(2, 2), gold);
    joints.Add(simulation.Joints.CreateMotor(homePost, spinner, home, new MotorJointOptions
    {
        AngularVelocity = 2,
        MaxVelocityTorque = 100,
        LinearHertz = 1.5f,
        LinearDampingRatio = 0.4f,
        MaxSpringForce = 400,
        CollideConnected = false,
    }));
}

void Update(Scene rootScene, GameTime time)
{
    simulation?.Update(time.Elapsed);

    if (game.Input.IsKeyPressed(Keys.M))
    {
        pendulumMotor = !pendulumMotor;
        b2RevoluteJoint_EnableMotor(pendulumJoint, pendulumMotor);
    }

    if (game.Input.IsKeyPressed(Keys.L))
    {
        pendulumLimit = !pendulumLimit;
        b2RevoluteJoint_EnableLimit(pendulumJoint, pendulumLimit);
    }

    if (game.Input.IsKeyPressed(Keys.J))
    {
        wheelMotor = !wheelMotor;
        b2WheelJoint_EnableMotor(wheelJoint, wheelMotor);
    }

    if (game.Input.IsKeyPressed(Keys.N) && debugDraw is not null)
    {
        debugDraw.DrawContactPoints = !debugDraw.DrawContactPoints;
        debugDraw.DrawContactNormals = debugDraw.DrawContactPoints;
    }

    if (game.Input.IsKeyPressed(Keys.G) && debugDraw is not null)
        debugDraw.DrawBounds = !debugDraw.DrawBounds;

    if (game.Input.IsKeyPressed(Keys.T) && debugDraw is not null)
        debugDraw.DrawMass = !debugDraw.DrawMass;

    if (game.Input.IsKeyPressed(Keys.R))
        Reset(rootScene);

    // One call draws every joint - and whatever else is switched on - from Box2D's own view of the world.
    if (simulation is not null)
        debugDraw?.Draw(simulation);
}

void Reset(Scene scene)
{
    foreach (var joint in joints)
        simulation!.Joints.Destroy(joint);

    joints.Clear();

    foreach (var entity in spawned)
    {
        simulation!.RemoveBody(entity);
        entity.Scene = null;
    }

    spawned.Clear();
    pendulumMotor = false;
    pendulumLimit = false;
    wheelMotor = true;

    BuildScene(scene);
}

void Ground(Scene scene)
{
    var entity = new Entity("Ground") { new ShapeComponent { Vertices = [.. Rectangle(24, 0.5f)], Color = paleGreen } };
    entity.Transform.Position = new Vector3(0, -0.5f, 0);
    entity.Scene = scene;
    spawned.Add(entity);

    var body = simulation!.CreateStaticBody(entity, new Vector3(0, -0.5f, 0));
    ShapeFixtureBuilder.AttachShape(Primitive2DModelType.Rectangle, new Vector2(48, 1), body);
}

Entity StaticBox(Scene scene, Vector2 centre, Vector2 size)
{
    var entity = new Entity("Static") { new ShapeComponent { Vertices = [.. Rectangle(size.X / 2, size.Y / 2)], Color = paleGreen } };
    entity.Transform.Position = new Vector3(centre, 0);
    entity.Scene = scene;
    spawned.Add(entity);

    var body = simulation!.CreateStaticBody(entity, new Vector3(centre, 0));
    ShapeFixtureBuilder.AttachShape(Primitive2DModelType.Rectangle, size, body);

    return entity;
}

Entity DynamicBox(Scene scene, Vector2 centre, Vector2 size, Color color)
{
    var entity = new Entity("Box") { new ShapeComponent { Vertices = [.. Rectangle(size.X / 2, size.Y / 2)], Color = color } };
    entity.Transform.Position = new Vector3(centre, 0);
    entity.Scene = scene;
    spawned.Add(entity);

    var body = simulation!.CreateDynamicBody(entity, new Vector3(centre, 0));
    ShapeFixtureBuilder.AttachShape(Primitive2DModelType.Rectangle, size, body);
    entity.Add(new Box2DBodyComponent { BodyId = body });

    return entity;
}

Entity DynamicCircle(Scene scene, Vector2 centre, float radius, Color color)
{
    var entity = new Entity("Circle") { new ShapeComponent { Vertices = [Vector2.Zero], Radius = radius, Color = color } };
    entity.Transform.Position = new Vector3(centre, 0);
    entity.Scene = scene;
    spawned.Add(entity);

    var body = simulation!.CreateDynamicBody(entity, new Vector3(centre, 0));
    ShapeFixtureBuilder.AttachShape(Primitive2DModelType.Circle, new Vector2(radius, radius), body);
    entity.Add(new Box2DBodyComponent { BodyId = body });

    return entity;
}

void AddInstructions()
{
    var overlay = DebugOverlay.GetOrCreate(game);

    overlay.Position = DisplayPosition.BottomLeft;

    overlay.AddSection("Joints", () =>
    [
        new("Left to right: hinge, slider, wheel, rope, weld, motor joint. Left mouse pulls on anything."),
        new($"M  hinge motor {(pendulumMotor ? "ON " : "off")}   L  hinge limit {(pendulumLimit ? "ON " : "off")}   J  wheel motor {(wheelMotor ? "ON " : "off")}", Color.Yellow),
        new($"Box2D debug draw:  N  contacts {(debugDraw?.DrawContactPoints == true ? "ON " : "off")}   G  bounds {(debugDraw?.DrawBounds == true ? "ON " : "off")}   T  mass {(debugDraw?.DrawMass == true ? "ON " : "off")}"),
        new("R  rebuild the rigs"),
    ]);
}

static Vector2[] Rectangle(float halfWidth, float halfHeight) =>
[
    new(-halfWidth, -halfHeight),
    new(halfWidth, -halfHeight),
    new(halfWidth, halfHeight),
    new(-halfWidth, halfHeight),
];

/*
---example-metadata
slug: box2d-joints
title:
  en: Box2D Joints
level: Intermediate
category: Physics
complexity: 5
order: 92
description:
  en: |-
    Every Box2D joint, one rig each, in a row you can pull on: a hinge pendulum with a motor and a
    limit, a slider on a spring, a wheel on a suspension, a rope of distance joints, a soft weld and
    a motor joint that springs its box back home. Joints2D takes world-space pivots and axes and
    turns them into the local frames Box2D wants, with options records for the per-type settings.
    The grabber on the camera picks any body up, and the joints are drawn from their anchors.
concepts:
  - Creating every Box2D joint type through Joints2D with world-space pivots and axes
  - Options records that mirror the Box2D definition and leave the rest at Box2D's defaults
  - Toggling a joint's motor and limit at runtime through the Box2D joint functions
  - Drawing joints, contacts, bounds and mass with Box2DDebugDraw, Box2D's debug draw through ShapeBatch
  - Pulling on constrained bodies with Grabber2DScript
  - "Using helpers: SetupBase2D, Add2DCameraController, AddShapeBatch, ShapeComponent"
tags:
  - 2D
  - Box2D
  - Physics
  - Joints
  - Constraint
  - Interaction
  - Third Party
related:
  - E06_Box2D
  - E06_Box2D_Explosion
  - E05_3D_Constraints
screenshotFrame: 120
enabled: true
created: 2026-09-06
---
*/