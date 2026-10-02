using Stride.BepuPhysics;
using Stride.BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

// A drivable car from four constraints per wheel - the recipe of bepuphysics2's own car demo
// (Demos/Cars/SimpleCar.cs), in Stride's component vocabulary. Every one of the four exists as a
// component:
//
//   LinearAxisServo   the suspension spring: holds the wheel a set distance below its mount
//   PointOnLineServo  the strut: the wheel may only move along the suspension axis
//   AngularAxisMotor  drive and brake in one: a wheel-speed target with a force cap
//   AngularHinge      steering: the wheel's axle, turned about the suspension axis
//
// The chassis is one body with two box colliders; the wheels are cylinders turned a quarter
// turn so their own Y is the axle. Car parts share a collision layer that does not collide with
// itself, so wheels never fight the body. The controller does what the demo's does: Ackermann
// geometry on the front wheels, and constraint targets re-applied only when they change, so the
// car is not woken every frame.
//
// W S drive, A D steer, Space brakes, Shift doubles the speed, R puts the car back at the start.
// There is no camera controller: a chase camera on the camera entity follows the car, which is what
// frees W A S D for driving. The left mouse button, through the grabber on the same entity, picks
// the car or a crate up. Physics runs on a fixed step, so the bodies interpolate between steps or
// the car would stutter on any display faster than the simulation.

const float SuspensionLength = 0.25f;
const float MaxSteeringAngle = MathF.PI * 0.3f;      // the demo's 0.23 turns wide; 54 degrees at the lock makes the slalom drivable
const float SteeringSpeed = 2.5f;                    // radians per second towards the key
const float ForwardSpeed = 75f;
const float ForwardForce = 6f;
const float BackwardSpeed = 30f;
const float BackwardForce = 4f;
const float IdleForce = 0.25f;
const float BrakeForce = 7f;
const float ZoomMultiplier = 2f;
const float WheelBaseLength = 3.4f;
const float WheelBaseWidth = 1.8f;

var suspensionDirection = new Vector3(0, -1, 0);
var wheelAxleInChassis = new Vector3(-1, 0, 0);     // the wheel's Y after its quarter turn about Z
var wheelTurn = Quaternion.RotationZ(MathUtil.PiOverTwo);   // the quarter turn that makes a cylinder's Y the axle

Car? car = null;
var steeringAngle = 0f;
var previousTargetSpeed = float.NaN;
var previousTargetForce = float.NaN;

var start = new Vector3(0, 1.5f, 0);

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.SetupBase3D();                                 // compositor, camera, light - no camera controller, the car owns W A S D
    game.AddSkybox();
    game.AddProfiler();
    game.Add3DGround(new()
    {
        Size = new Vector3(240, 1, 240),
        Material = game.CreateMaterial(new Color(95, 105, 100), metalness: 0.3f, glossiness: 0.7f),
    });

    // Car parts on one layer that does not collide with itself; everything else stays as it was.
    var matrix = CollisionMatrix.All;
    matrix.Set(CollisionLayer.Layer1, CollisionLayer.Layer1, shouldCollide: false);
    game.GetCameraEntity().GetSimulation().CollisionMatrix = matrix;

    BuildCourse(scene);
    car = BuildCar(scene, start);

    var cameraEntity = game.GetCameraEntity();
    cameraEntity.Add(new ChaseCamera { Target = car.Chassis.Entity });
    game.AddGrabber();

    AddInstructions();
}

void Update(Scene scene, GameTime time)
{
    if (car is null) return;

    var input = game.Input;
    var dt = (float)time.Elapsed.TotalSeconds;

    if (input.IsKeyPressed(Keys.R)) ResetCar(car);

    // Steering eases toward the key at a fixed rate, as a wheel turned by hand would.
    var steerTarget = ((input.IsKeyDown(Keys.A) ? 1 : 0) - (input.IsKeyDown(Keys.D) ? 1 : 0)) * MaxSteeringAngle;
    var change = Math.Clamp(steerTarget - steeringAngle, -SteeringSpeed * dt, SteeringSpeed * dt);
    var previousSteering = steeringAngle;
    steeringAngle = Math.Clamp(steeringAngle + change, -MaxSteeringAngle, MaxSteeringAngle);

    if (steeringAngle != previousSteering)
        Steer(car, steeringAngle);

    var throttle = (input.IsKeyDown(Keys.W) ? 1 : 0) - (input.IsKeyDown(Keys.S) ? 1 : 0);
    var zoom = input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift);
    Drive(car, throttle, zoom, brake: input.IsKeyDown(Keys.Space));
}

// Back to the start line, wheels included. Teleporting the chassis alone leaves the wheels wherever
// they were, and the four constraints then drag chassis and wheels together across the gap - the
// car lands on its side or tumbles in. Every body goes to its resting pose, still, and the
// steering and the motor targets start over.
void ResetCar(Car car)
{
    Place(car.Chassis, start, Quaternion.Identity);

    foreach (var wheel in new[] { car.FrontLeft, car.FrontRight, car.RearLeft, car.RearRight })
    {
        Place(wheel.Body, start + wheel.Mount + suspensionDirection * SuspensionLength, wheelTurn);
    }

    steeringAngle = 0;
    Steer(car, 0);
    previousTargetSpeed = float.NaN;

    static void Place(BodyComponent body, Vector3 position, Quaternion rotation)
    {
        body.Teleport(position, rotation);
        body.LinearVelocity = Vector3.Zero;
        body.AngularVelocity = Vector3.Zero;
        body.Awake = true;
    }
}

// The demo's Ackermann steering: on a turn the inner wheel turns more than the outer one, so
// both roll around the same centre instead of scrubbing.
void Steer(Car car, float angle)
{
    float left, right;
    var magnitude = MathF.Abs(angle);

    if (magnitude > 1e-6f)
    {
        var turnRadius = MathF.Abs(WheelBaseLength * MathF.Tan(MathF.PI * 0.5f - magnitude));
        var halfWidth = WheelBaseWidth * 0.5f;
        var inner = MathF.Atan(WheelBaseLength / (turnRadius - halfWidth));
        var outer = MathF.Atan(WheelBaseLength / (turnRadius + halfWidth));

        (left, right) = angle > 0 ? (inner, outer) : (-outer, -inner);
    }
    else
    {
        left = right = 0;
    }

    car.FrontLeft.Hinge.LocalHingeAxisA = Vector3.Transform(wheelAxleInChassis, Quaternion.RotationAxis(suspensionDirection, -left));
    car.FrontRight.Hinge.LocalHingeAxisA = Vector3.Transform(wheelAxleInChassis, Quaternion.RotationAxis(suspensionDirection, -right));
}

// Drive and brake are the same motor with different targets. Front wheels drive; the rear ones
// only join in to brake or to hold the car when idle.
void Drive(Car car, int throttle, bool zoom, bool brake)
{
    float speed, force;
    bool allWheels;

    if (brake)
    {
        (speed, force, allWheels) = (0, BrakeForce, true);
    }
    else if (throttle > 0)
    {
        (speed, force, allWheels) = (zoom ? ForwardSpeed * ZoomMultiplier : ForwardSpeed, zoom ? ForwardForce * ZoomMultiplier : ForwardForce, false);
    }
    else if (throttle < 0)
    {
        (speed, force, allWheels) = (-(zoom ? BackwardSpeed * ZoomMultiplier : BackwardSpeed), zoom ? BackwardForce * ZoomMultiplier : BackwardForce, false);
    }
    else
    {
        (speed, force, allWheels) = (0, IdleForce, true);
    }

    // Setting a constraint wakes the car; only touch it when something changed.
    if (speed == previousTargetSpeed && force == previousTargetForce) return;

    previousTargetSpeed = speed;
    previousTargetForce = force;

    SetMotor(car.FrontLeft, speed, force);
    SetMotor(car.FrontRight, speed, force);
    SetMotor(car.RearLeft, allWheels ? speed : 0, allWheels ? force : 0);
    SetMotor(car.RearRight, allWheels ? speed : 0, allWheels ? force : 0);

    static void SetMotor(Wheel wheel, float speed, float force)
    {
        wheel.Motor.MotorMaximumForce = force;
        wheel.Motor.TargetVelocity = speed;
    }
}

Car BuildCar(Scene scene, Vector3 position)
{
    // One body, two box colliders: the long hull carries the mass, the cabin sits on it.
    var chassisEntity = new Entity("Car")
    {
        new BodyComponent
        {
            Collider = new CompoundCollider
            {
                Colliders =
                {
                    new BoxCollider { Size = new Vector3(1.85f, 0.7f, 4.73f), Mass = 10 },
                    new BoxCollider { Size = new Vector3(1.85f, 0.6f, 2.5f), PositionLocal = new Vector3(0, 0.65f, -0.35f), Mass = 0.5f },
                },
            },
            FrictionCoefficient = 0.35f,
            CollisionLayer = CollisionLayer.Layer1,
            InterpolationMode = InterpolationMode.Interpolated,
        },
    };
    chassisEntity.Transform.Position = position;

    var body = game.CreateMaterial(new Color(200, 60, 50), metalness: 0.6f, glossiness: 0.8f);
    chassisEntity.AddChild(Model(PrimitiveModelType.Cube, new Vector3(1.85f, 0.7f, 4.73f), body, Vector3.Zero));
    chassisEntity.AddChild(Model(PrimitiveModelType.Cube, new Vector3(1.85f, 0.6f, 2.5f), body, new Vector3(0, 0.65f, -0.35f)));
    chassisEntity.Scene = scene;

    var chassis = chassisEntity.Get<BodyComponent>();

    return new Car(chassis,
        BuildWheel(scene, chassis, new Vector3(-0.9f, -0.1f, 1.7f)),
        BuildWheel(scene, chassis, new Vector3(0.9f, -0.1f, 1.7f)),
        BuildWheel(scene, chassis, new Vector3(-0.9f, -0.1f, -1.7f)),
        BuildWheel(scene, chassis, new Vector3(0.9f, -0.1f, -1.7f)));
}

// A wheel is a cylinder turned a quarter turn about Z, so its own Y axis is the axle; the four
// constraints hang off its entity, each naming the chassis as the other body.
Wheel BuildWheel(Scene scene, BodyComponent chassis, Vector3 mount)
{
    var entity = new Entity("Wheel")
    {
        new BodyComponent
        {
            Collider = new CompoundCollider { Colliders = { new CylinderCollider { Radius = 0.4f, Length = 0.18f, Mass = 0.25f } } },
            FrictionCoefficient = 1f,
            CollisionLayer = CollisionLayer.Layer1,
            InterpolationMode = InterpolationMode.Interpolated,
        },
    };
    entity.Transform.Position = chassis.Entity.Transform.Position + mount + suspensionDirection * SuspensionLength;
    entity.Transform.Rotation = wheelTurn;
    entity.AddChild(Model(PrimitiveModelType.Cylinder, new Vector3(0.4f, 0, 0.18f), game.CreateMaterial(new Color(40, 40, 45), metalness: 0.2f, glossiness: 0.5f), Vector3.Zero));

    var wheel = entity.Get<BodyComponent>();

    var spring = new LinearAxisServoConstraintComponent
    {
        A = chassis,
        B = wheel,
        LocalOffsetA = mount,
        LocalOffsetB = Vector3.Zero,
        LocalPlaneNormal = suspensionDirection,
        TargetOffset = SuspensionLength,
        SpringFrequency = 5,
        SpringDampingRatio = 0.7f,
        ServoMaximumSpeed = float.MaxValue,
        ServoBaseSpeed = 0,
        ServoMaximumForce = float.MaxValue,
    };

    var strut = new PointOnLineServoConstraintComponent
    {
        A = chassis,
        B = wheel,
        LocalOffsetA = mount,
        LocalOffsetB = Vector3.Zero,
        LocalDirection = suspensionDirection,
        SpringFrequency = 30,
        SpringDampingRatio = 1,
        ServoMaximumSpeed = float.MaxValue,
        ServoBaseSpeed = 0,
        ServoMaximumForce = float.MaxValue,
    };

    // Wheel first: the axis is the wheel's own. A very high damping makes it a velocity motor
    // rather than a soft one; the force cap is what limits it.
    var motor = new AngularAxisMotorConstraintComponent
    {
        A = wheel,
        B = chassis,
        LocalAxisA = new Vector3(0, -1, 0),
        TargetVelocity = 0,
        MotorMaximumForce = 0,
        MotorDamping = 1_000_000f,
    };

    var hinge = new AngularHingeConstraintComponent
    {
        A = chassis,
        B = wheel,
        LocalHingeAxisA = wheelAxleInChassis,
        LocalHingeAxisB = Vector3.UnitY,
        SpringFrequency = 30,
        SpringDampingRatio = 1,
    };

    entity.Add(spring);
    entity.Add(strut);
    entity.Add(motor);
    entity.Add(hinge);
    entity.Scene = scene;

    return new Wheel(wheel, motor, hinge, mount);
}

Entity Model(PrimitiveModelType type, Vector3 size, Material material, Vector3 localPosition)
{
    // The core, physics-free primitive: the body is on the parent, this is only the look.
    var entity = game.Create3DPrimitive(type, new Primitive3DEntityOptions { Size = size, Material = material });
    entity.Transform.Position = localPosition;

    return entity;
}

void BuildCourse(Scene scene)
{
    // A ramp, a slalom of pillars, and a wall of crates to drive through or throw with the grabber.
    var ramp = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
    {
        Size = new Vector3(6, 0.4f, 10),
        Material = game.CreateMaterial(new Color(150, 150, 160)),
        // An empty compound: the helper adds the box that fits the ramp's size
        Component = new StaticComponent { Collider = new CompoundCollider() },
        Position = new Vector3(0, 0.9f, 30),
    });
    ramp.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(12));
    ramp.Scene = scene;

    for (var i = 0; i < 6; i++)
    {
        var pillar = game.Create3DPrimitive(PrimitiveModelType.Cylinder, new()
        {
            // A cylinder's size is its radius in X and its height in Z
            Size = new Vector3(0.5f, 0, 3),
            Material = game.CreateMaterial(new Color(120, 170, 220)),
            Component = new StaticComponent { Collider = new CompoundCollider() },
            Position = new Vector3(i % 2 == 0 ? -4 : 4, 1.5f, -20 - i * 8),
        });
        pillar.Scene = scene;
    }

    for (var row = 0; row < 3; row++)
    {
        for (var i = 0; i < 6; i++)
        {
            var crate = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
            {
                Size = new Vector3(1.2f),
                Material = game.CreateMaterial(new Color(220, 180, 90)),
                // The crate's own collider, for its mass: a compound that already holds a shape is
                // kept as it is, so the size has to be given too
                Component = new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider { Mass = 2, Size = new Vector3(1.2f) } } } },
                Position = new Vector3(-3.6f + i * 1.3f, 0.6f + row * 1.25f, 60),
            });
            crate.Scene = scene;
        }
    }
}

void AddInstructions()
{
    var overlay = DebugOverlay.GetOrCreate(game);

    overlay.AddSection("Car", () =>
    {
        var speed = car?.Chassis.LinearVelocity.Length() ?? 0;

        return
        [
            new(["W", "S"], "Drive", Color.Yellow),
            new(["A", "D"], "Steer", Color.Yellow),
            new("Space", "Brake", Color.Yellow),
            new("Shift", "Hold for double speed", Color.Yellow),
            new("R", "Back to the start", Color.Yellow),
            new("Left mouse", "Pick up the car or a crate", Color.Yellow),
            new(""),
            new($"Speed {speed * 3.6f:0} km/h, steering {MathUtil.RadiansToDegrees(steeringAngle):0}°"),
            new($"Motor cap {previousTargetForce:0.00}"),
            new("Four constraints per wheel: suspension spring,", Color.Gray),
            new("strut, drive motor, steering hinge", Color.Gray),
        ];
    });
}

/// <summary>The chassis body and its four wheels, front pair first.</summary>
sealed record Car(BodyComponent Chassis, Wheel FrontLeft, Wheel FrontRight, Wheel RearLeft, Wheel RearRight);

/// <summary>A wheel body, the two constraints the controller drives - the motor and the steering hinge - and where it mounts on the chassis.</summary>
sealed record Wheel(BodyComponent Body, AngularAxisMotorConstraintComponent Motor, AngularHingeConstraintComponent Hinge, Vector3 Mount);

/// <summary>
/// Sits behind the target and looks at it, easing into place. On the camera entity in place of the
/// camera controller, so the driving keys are free.
/// </summary>
public sealed class ChaseCamera : SyncScript
{
    public Entity? Target { get; set; }
    public float Distance { get; set; } = 9f;
    public float Height { get; set; } = 3.5f;
    public float Smoothing { get; set; } = 4f;

    public override void Update()
    {
        if (Target is null) return;

        var targetPosition = Target.Transform.WorldMatrix.TranslationVector;
        var forward = Vector3.Normalize(Vector3.Transform(Vector3.UnitZ, Target.Transform.Rotation) with { Y = 0 });
        var wanted = targetPosition - forward * Distance + Vector3.UnitY * Height;

        var blend = 1 - MathF.Exp(-Smoothing * (float)Game.UpdateTime.Elapsed.TotalSeconds);
        var position = Vector3.Lerp(Entity.Transform.Position, wanted, blend);

        // A view matrix looks down -Z; its inverse is the camera's world pose.
        var view = Matrix.LookAtRH(position, targetPosition + Vector3.UnitY * 0.8f, Vector3.UnitY);
        Matrix.Invert(ref view, out var world);
        world.Decompose(out _, out Quaternion rotation, out _);

        Entity.Transform.Position = position;
        Entity.Transform.Rotation = rotation;
    }
}

/*
---example-metadata
slug: car
title:
  en: Car
level: Intermediate
category: Physics
complexity: 5
order: 96
description:
  en: |-
    A drivable car from four constraints per wheel, the recipe of bepuphysics2's own car demo in
    Stride's components: a linear axis servo as the suspension spring, a point-on-line servo as the
    strut, an angular axis motor for drive and brake, and an angular hinge turned about the
    suspension axis for steering, with Ackermann geometry on the front wheels. W S A D drive, a
    chase camera follows, and the grabber lifts the car to show the suspension settle.
concepts:
  - "The four-constraint wheel: LinearAxisServo, PointOnLineServo, AngularAxisMotor, AngularHinge"
  - Steering by turning a hinge axis about the suspension direction, with Ackermann geometry
  - Drive and brake as one motor with a velocity target and a force cap
  - Re-applying constraint targets only when they change, so the car sleeps when idle
  - Filtering chassis-wheel collisions with a collision layer that does not collide with itself
  - A chase camera in place of the camera controller, to free the driving keys
  - "Using helpers: SetupBase3D, Add3DGround, Create3DPrimitive (both overloads), GrabberScript"
tags:
  - 3D
  - Bepu
  - Physics
  - Car
  - Constraint
  - Servo
  - Motor
related:
  - E06_Box2D_Car
  - E05_3D_Constraints_Motors
  - E05_3D_Grabber
screenshotFrame: 90
enabled: true
created: 2026-09-06
---
*/