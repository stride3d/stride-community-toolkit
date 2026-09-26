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
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2WheelJoints;
using static Box2D.NET.B2Worlds;

// A car on two wheel joints, over hilly terrain built from a chain. Ported from the Box2D.NET
// samples' Car struct and Driving scene (MIT, (c) 2022 Erin Catto, (c) 2025 Choi Ikpil).
//
// A wheel joint is the whole car: it pins the wheel to the chassis at a pivot, lets it turn
// freely, lets it slide along one axis - the suspension, straight up - on a spring with a travel
// limit, and drives it with a motor. Two of them, a rounded chassis and two heavy, grippy wheels,
// and it drives. The motor is on all the time: throttle sets its speed, and speed zero with the
// torque still applied is the brake.
//
// The terrain is one chain shape. A row of separate segments would have internal corners for
// the wheels to catch on; a chain smooths the joins, which is what makes the hills drivable.
//
// A and D drive, J and K change the suspension stiffness while you ride, the camera follows the
// chassis, and the left mouse button picks the car up - the quickest way to see the suspension.

const float Scale = 1f;
const float DriveSpeed = 35f;          // wheel rad/s at full throttle
const float Torque = 2.5f * Scale;

Box2DSimulation? simulation = null;
ShapeBatch? shapeBatch = null;
Basic2DCameraController? cameraController = null;

var pink = new Color(0xFF, 0xC0, 0xCB);
var paleGreen = new Color(0x98, 0xFB, 0x98);
var royalBlue = new Color(0x41, 0x69, 0xE1);
var background = new Color(0.2f, 0.2f, 0.2f);

Vector2[] terrain = [];
List<Entity> spawned = [];
Entity? chassis = null;
B2BodyId chassisBody = default;
B2JointId rearAxle = default;
B2JointId frontAxle = default;
var hertz = 5f;
var throttle = 0f;
var autoDrive = true;                  // drives itself until A or D is pressed, so the scene moves from the start

using var game = new Game();

game.Run(start: Start, update: Update);

simulation?.Dispose();

void Start(Scene rootScene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Box2D Car - Stride Community Toolkit";

    game.SetupBase2D(clearColor: background);
    var cameraEntity = game.Add2DCameraController();
    cameraController = cameraEntity.Get<Basic2DCameraController>();
    game.AddProfiler();

    var camera = rootScene.GetCamera() ?? throw new InvalidOperationException("Camera not found in scene");
    camera.OrthographicSize = 22;

    shapeBatch = game.AddShapeBatch();
    shapeBatch.BorderWidth = 1f;
    shapeBatch.Fill.Alpha = 0.4f;

    simulation = new Box2DSimulation();

    cameraEntity.Add(new Grabber2DScript { Simulation = simulation });

    BuildTerrain();
    SpawnCar(rootScene, new Vector2(0, 1));
    AddInstructions();
}

// The Driving sample's ground: a flat start, ten hills twice over, a long flat, a ramp, a drop,
// a wall - one chain. A chain collides on the right of its direction of travel, so the floor is
// built left to right here and reversed before it is attached: right to left, collision side up.
void BuildTerrain()
{
    var points = new List<Vector2> { new(-20, 20), new(-20, 0), new(20, 0) };
    float[] hills = [0.25f, 1.0f, 4.0f, 0.0f, 0.0f, -1.0f, -2.0f, -2.0f, -1.25f, 0.0f];
    var x = 20f;

    for (var pass = 0; pass < 2; pass++)
    {
        foreach (var height in hills)
        {
            x += 5;
            points.Add(new Vector2(x, height));
        }
    }

    points.Add(new Vector2(x + 40, 0));
    points.Add(new Vector2(x + 50, 5));          // the ramp
    points.Add(new Vector2(x + 54, 5));
    points.Add(new Vector2(x + 70, 0));          // and the drop
    points.Add(new Vector2(x + 110, 0));
    points.Add(new Vector2(x + 110, 20));

    points.Reverse();
    terrain = [.. points];

    var ground = simulation!.CreateStaticBody(Vector3.Zero);
    ShapeFixtureBuilder.AttachChain(terrain, ground, friction: 0.6f);
}

void SpawnCar(Scene scene, Vector2 position)
{
    // The chassis: the sample's six-point hull, rounded by 0.15, density 1, low friction so the
    // body slides off things rather than sticking.
    Vector2[] outline =
    [
        new(-1.5f, -0.5f), new(1.5f, -0.5f), new(1.5f, 0.0f), new(0.0f, 0.9f), new(-1.15f, 0.9f), new(-1.5f, 0.2f),
    ];

    for (var i = 0; i < outline.Length; i++)
        outline[i] *= 0.85f * Scale;

    var hullPoints = new B2Vec2[outline.Length];

    for (var i = 0; i < outline.Length; i++)
        hullPoints[i] = new B2Vec2(outline[i].X, outline[i].Y);

    var hull = b2ComputeHull(hullPoints, hullPoints.Length);
    var chassisShape = b2MakePolygon(hull, 0.15f * Scale);
    var chassisDef = ShapeFixtureBuilder.CreateCustomShapeDef(1f / Scale, 0.2f, 0f);

    var chassisPosition = position + new Vector2(0, 1f * Scale);
    chassis = new Entity("Chassis") { new ShapeComponent { Vertices = [.. outline], Radius = 0.15f * Scale, Color = royalBlue } };
    chassis.Transform.Position = new Vector3(chassisPosition, 0);
    chassis.Scene = scene;
    spawned.Add(chassis);

    chassisBody = simulation!.CreateDynamicBody(chassis, new Vector3(chassisPosition, 0));
    b2CreatePolygonShape(chassisBody, in chassisDef, in chassisShape);
    chassis.Add(new Box2DBodyComponent { BodyId = chassisBody });

    // The wheels: heavy, grippy, with rolling resistance so the car does not coast forever.
    var wheelDef = ShapeFixtureBuilder.CreateCustomShapeDef(2f / Scale, 1.5f, 0f);
    wheelDef.material.rollingResistance = 0.1f;

    var rearCentre = position + new Vector2(-1f * Scale, 0.35f * Scale);
    var frontCentre = position + new Vector2(1f * Scale, 0.4f * Scale);
    var rear = Wheel(scene, rearCentre, wheelDef);
    var front = Wheel(scene, frontCentre, wheelDef);

    // The wheel joints: pivot at the wheel's centre, axis straight up, spring, travel limit,
    // and a motor that is on from the start at speed zero - which is the brake.
    var options = new WheelJointOptions
    {
        EnableSpring = true,
        Hertz = hertz,
        DampingRatio = 0.7f,
        EnableLimit = true,
        LowerTranslation = -0.25f * Scale,
        UpperTranslation = 0.25f * Scale,
        EnableMotor = true,
        MaxMotorTorque = Torque,
        MotorSpeed = 0,
    };

    rearAxle = simulation.Joints.CreateWheel(chassisBody, rear, rearCentre, Vector2.UnitY, options);
    frontAxle = simulation.Joints.CreateWheel(chassisBody, front, frontCentre, Vector2.UnitY, options);

    cameraController!.FollowTarget = chassis;
}

B2BodyId Wheel(Scene scene, Vector2 centre, B2ShapeDef shapeDef)
{
    var radius = 0.4f * Scale;
    var entity = new Entity("Wheel") { new ShapeComponent { Vertices = [Vector2.Zero], Radius = radius, Color = pink } };
    entity.Transform.Position = new Vector3(centre, 0);
    entity.Scene = scene;
    spawned.Add(entity);

    var body = simulation!.CreateDynamicBody(entity, new Vector3(centre, 0));
    ShapeFixtureBuilder.AttachShape(Primitive2DModelType.Circle, new Vector2(radius, radius), body, shapeDef);
    entity.Add(new Box2DBodyComponent { BodyId = body });

    return body;
}

void Update(Scene rootScene, GameTime time)
{
    simulation?.Update(time.Elapsed);

    var input = game.Input;

    // Throttle: the motor's speed. A negative wheel speed - clockwise - rolls the car to the right.
    if (input.IsKeyDown(Keys.A) || input.IsKeyDown(Keys.D))
        autoDrive = false;

    throttle = autoDrive ? 1 : (input.IsKeyDown(Keys.D) ? 1 : 0) - (input.IsKeyDown(Keys.A) ? 1 : 0);
    SetMotorSpeed(-throttle * DriveSpeed);

    var stiffer = (input.IsKeyDown(Keys.K) ? 1 : 0) - (input.IsKeyDown(Keys.J) ? 1 : 0);

    if (stiffer != 0)
    {
        hertz = Math.Clamp(hertz + stiffer * 4 * (float)time.Elapsed.TotalSeconds, 1, 12);
        b2WheelJoint_SetSpringHertz(rearAxle, hertz);
        b2WheelJoint_SetSpringHertz(frontAxle, hertz);
    }

    if (input.IsKeyPressed(Keys.R))
        Reset(rootScene);

    DrawTerrain();
}

void SetMotorSpeed(float speed)
{
    if (!simulation!.Joints.IsValid(rearAxle)) return;

    b2WheelJoint_SetMotorSpeed(rearAxle, speed);
    b2WheelJoint_SetMotorSpeed(frontAxle, speed);
    b2Joint_WakeBodies(rearAxle);
}

void DrawTerrain()
{
    if (shapeBatch is null) return;

    for (var i = 0; i + 1 < terrain.Length; i++)
        shapeBatch.DrawPixelLine(new Vector3(terrain[i], 0), new Vector3(terrain[i + 1], 0), 2f, paleGreen);
}

void Reset(Scene scene)
{
    simulation!.Joints.Destroy(rearAxle);
    simulation.Joints.Destroy(frontAxle);

    foreach (var entity in spawned)
    {
        simulation.RemoveBody(entity);
        entity.Scene = null;
    }

    spawned.Clear();
    SpawnCar(scene, new Vector2(0, 1));
}

void AddInstructions()
{
    var overlay = DebugOverlay.GetOrCreate(game);

    overlay.Position = DisplayPosition.BottomLeft;

    overlay.AddSection("Car", () =>
    {
        var speed = simulation is not null && b2Body_IsValid(chassisBody) ? b2Body_GetLinearVelocity(chassisBody).X : 0;

        return
        [
            new($"A / D  drive      throttle {throttle,2:+0;-0; 0}   speed {speed,6:0.0} m/s", Color.Yellow),
            new($"J / K  suspension  {hertz:0.0} Hz"),
            new("R      new car        Left mouse  pick the car up"),
        ];
    });
}

/*
---example-metadata
slug: box2d-car
title:
  en: Box2D Car
level: Intermediate
category: Physics
complexity: 4
order: 93
description:
  en: |-
    A car on two wheel joints over hilly terrain: the wheel joint pins the wheel, lets it turn,
    springs it along the suspension axis with a travel limit, and drives it with a motor whose
    speed is the throttle and whose zero is the brake. The terrain is one chain shape, so the
    wheels never catch on a corner. A and D drive, J and K tune the suspension while riding, the
    camera follows, and the grabber lifts the car to show the suspension working.
concepts:
  - "A drivable car from two wheel joints: pivot, suspension axis, spring, travel limit, motor"
  - Throttle as motor speed and braking as motor speed zero with torque still applied
  - Terrain as one chain shape with ShapeFixtureBuilder.AttachChain, and why not a row of segments
  - Retuning a joint while it runs through the Box2D wheel-joint functions
  - Camera follow with Basic2DCameraController.FollowTarget, which frees the driving keys
  - "Using helpers: Joints2D.CreateWheel, Grabber2DScript, ShapeBatch, ShapeComponent"
tags:
  - 2D
  - Box2D
  - Physics
  - Car
  - Joints
  - Terrain
  - Third Party
related:
  - E06_Box2D_Joints
  - E05_3D_Car
  - E06_Box2D
screenshotFrame: 150
enabled: true
created: 2026-09-06
---
*/