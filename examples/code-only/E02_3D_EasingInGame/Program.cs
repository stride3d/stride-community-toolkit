using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.CommunityToolkit.Windows;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// Easing doing real work in a 3D scene, four ways, each one a Tween: a kinematic platform that
// lifts a stack of bodies on a sine in-out curve and comes back down, so physics rides on an eased
// motion; a door that swings open and shut on a cubic in-out; three crystals that pop into the
// scene on a back ease-out and then bob on a sine loop; and a camera that flies between two
// viewpoints on a smoother-step. The pattern is the same every time: start a tween, feed it the
// frame time, read Lerp or Slerp, and put the result where it belongs.
//
// The platform is the one that matters for a physics game. A body owned by Bepu ignores writes to
// its transform, so the eased value becomes a target the body chases with a velocity: the target's
// own speed as feed-forward, plus a gentle pull towards it so integration error cannot build up.
//
// Keys: L pauses and resumes the lift, O opens and closes the door, P pops the crystals in again,
// V flies the camera to the other viewpoint, R drops the bodies back onto the platform. D is not
// the door: the camera controller owns W A S D.
//
// New to Tween? E02_2D_EasingBasics builds the same motion by hand first, then with a tween.

WindowsDpiManager.EnablePerMonitorV2();

const float LiftLow = 1f;
const float LiftHigh = 3.5f;
const float DoorOpenAngle = 110f;

var liftHome = new Vector3(-4f, LiftLow, 0f);
var doorHinge = new Vector3(4f, 1.2f, -1f);
var doorOffset = new Vector3(0f, 0f, 0.8f);
Vector3[] crystalSpots = [new(0f, 0.6f, 3f), new(1.6f, 0.6f, 3.6f), new(-1.6f, 0.6f, 3.6f)];
Vector3[] viewpoints = [new(0f, 7f, 15f), new(10f, 4f, 7f)];

var lift = Tween.Run(3f, EasingFunction.SineEaseInOut, TweenLoop.PingPong);
var door = new Tween(1.2f, EasingFunction.CubicEaseInOut);
var bob = Tween.Run(1.6f, EasingFunction.SineEaseInOut, TweenLoop.PingPong);
var flight = new Tween(1.5f, EasingFunction.SmootherStep);
var pops = crystalSpots.Select(_ => new Tween(0.7f, EasingFunction.BackEaseOut)).ToArray();

BodyComponent? liftBody = null;
Entity? doorEntity = null;
Entity[] crystals = [];
BodyComponent[] riders = [];
Vector3[] riderHomes = [];
var liftTargetBefore = liftHome;
var doorOpen = false;
var viewpoint = 0;
var flightFrom = Vector3.Zero;
var flightFromRotation = Quaternion.Identity;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Easing in a 3D game - Stride Community Toolkit";

    game.SetupBase3DScene();
    game.AddSkybox();

    LookFrom(viewpoints[0]);

    // The platform: a kinematic body, so it pushes what it carries and nothing pushes it back
    var platform = game.Create3DPrimitive(PrimitiveModelType.Cube, new Bepu3DPhysicsOptions
    {
        EntityName = "Platform",
        Material = game.CreateMaterial(new Color(70, 140, 200), specular: 0.3f, microSurface: 0.6f),
        Size = new Vector3(3f, 0.4f, 3f),
        Position = liftHome,
        Component = new BodyComponent { Kinematic = true, Collider = new CompoundCollider() },
    });

    platform.Scene = rootScene;
    liftBody = platform.Get<BodyComponent>();

    // The stack it carries: ordinary dynamic bodies, dropped on from a little above
    riderHomes = [liftHome + new Vector3(-0.7f, 0.9f, -0.5f), liftHome + new Vector3(0.6f, 0.9f, 0.4f), liftHome + new Vector3(-0.1f, 1.9f, 0f)];
    riders = new BodyComponent[riderHomes.Length];

    for (var i = 0; i < riderHomes.Length; i++)
    {
        var rider = game.Create3DPrimitive(i == 2 ? PrimitiveModelType.Cube : PrimitiveModelType.Sphere, new Bepu3DPhysicsOptions
        {
            EntityName = $"Rider {i + 1}",
            Material = game.CreateMaterial(i == 2 ? new Color(235, 180, 60) : new Color(220, 90, 70), specular: 0.4f, microSurface: 0.7f),
            // A sphere's Size is its radius; a cube's is its full width
            Size = new Vector3(i == 2 ? 0.8f : 0.4f),
            Position = riderHomes[i],
        });

        rider.Scene = rootScene;
        riders[i] = rider.Get<BodyComponent>();
    }

    // The door: a frame that stays, and a leaf that swings about the frame's edge. The leaf is a
    // visual, not a body - the platform already shows how easing meets physics
    var frame = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
    {
        EntityName = "Door frame",
        Material = game.CreateMaterial(new Color(60, 62, 70), specular: 0.2f, microSurface: 0.5f),
        Size = new Vector3(0.2f, 2.6f, 0.2f),
        Position = doorHinge,
    });

    frame.Scene = rootScene;

    doorEntity = new Entity("Door hinge") { Transform = { Position = doorHinge } };

    var leaf = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
    {
        EntityName = "Door leaf",
        Material = game.CreateMaterial(new Color(170, 110, 60), specular: 0.2f, microSurface: 0.5f),
        Size = new Vector3(0.12f, 2.4f, 1.6f),
        Position = doorOffset,
    });

    doorEntity.AddChild(leaf);
    doorEntity.Scene = rootScene;

    // The crystals: visuals that arrive by scale and then bob by position
    crystals = new Entity[crystalSpots.Length];

    for (var i = 0; i < crystalSpots.Length; i++)
    {
        var crystal = game.Create3DPrimitive(PrimitiveModelType.Cone, new Primitive3DEntityOptions
        {
            EntityName = $"Crystal {i + 1}",
            Material = game.CreateMaterial(new Color(120, 230, 200), specular: 0.9f, microSurface: 0.9f),
            Size = new Vector3(0.5f, 1f, 0.5f),
            Position = crystalSpots[i],
        });

        crystal.Transform.Scale = Vector3.Zero;
        crystal.Scene = rootScene;
        crystals[i] = crystal;
    }

    Pop();

    // The door starts closed: a finished run of the closing tween is the closed pose
    door.Start();
    door.Update(door.Duration);

    DebugOverlay.GetOrCreate(game).AddSection("Easing", BuildOverlayLines);
}

void Update(Scene scene, GameTime time)
{
    HandleInput();

    var dt = (float)time.Elapsed.TotalSeconds;

    // 1. The platform: the tween says where the platform should be; the body is given the speed to get there
    lift.Update(time);

    var liftTarget = lift.Lerp(liftHome, liftHome with { Y = LiftHigh });

    if (liftBody is not null && dt > 0f)
    {
        var feedForward = (liftTarget - liftTargetBefore) / dt;
        var correction = (liftTarget - liftBody.Position) * 8f;

        liftBody.LinearVelocity = feedForward + correction;
        liftBody.Awake = true;
    }

    liftTargetBefore = liftTarget;

    // 2. The door: an angle between closed and open, written straight to a transform
    door.Update(time);

    if (doorEntity is not null)
    {
        var angle = doorOpen ? door.Lerp(0f, DoorOpenAngle) : door.Lerp(DoorOpenAngle, 0f);

        doorEntity.Transform.Rotation = Quaternion.RotationY(MathUtil.DegreesToRadians(angle));
    }

    // 3. The crystals: scale from nothing with an overshoot, then a slow rise and fall forever
    bob.Update(time);

    for (var i = 0; i < crystals.Length; i++)
    {
        pops[i].Update(time);

        crystals[i].Transform.Scale = new Vector3(pops[i].Lerp(0f, 1f));
        crystals[i].Transform.Position = crystalSpots[i] + new Vector3(0f, bob.Lerp(0f, 0.5f), 0f);
    }

    // 4. The camera: position and rotation between two viewpoints, on one clock. The tween drives the
    // camera only while it flies: a completed tween stays complete, so writing its pose every frame
    // from then on would pin the camera where it landed and the free-flight controller could never
    // move it again. The frame it completes writes the exact landing pose, then the tween is reset
    // and the controller has the camera back.
    flight.Update(time);

    if (flight.IsRunning || flight.IsComplete)
    {
        var camera = game.GetCameraEntity().Transform;
        var eye = viewpoints[viewpoint];

        camera.Position = flight.Lerp(flightFrom, eye);
        camera.Rotation = flight.Slerp(flightFromRotation, MathUtilEx.LookRotation(eye, new Vector3(0f, 1.5f, 0f), Vector3.UnitY));

        if (flight.IsComplete) flight.Reset();
    }
}

void HandleInput()
{
    var input = game.Input;

    if (input.IsKeyPressed(Keys.L))
    {
        if (lift.IsRunning) lift.Stop();
        else lift.Resume();
    }

    if (input.IsKeyPressed(Keys.O))
    {
        doorOpen = !doorOpen;
        door.Start();
    }

    if (input.IsKeyPressed(Keys.P)) Pop();

    if (input.IsKeyPressed(Keys.V))
    {
        var camera = game.GetCameraEntity().Transform;

        flightFrom = camera.Position;
        flightFromRotation = camera.Rotation;
        viewpoint = (viewpoint + 1) % viewpoints.Length;
        flight.Start();
    }

    if (input.IsKeyPressed(Keys.R))
    {
        for (var i = 0; i < riders.Length; i++)
        {
            riders[i].Teleport(riderHomes[i] with { Y = riderHomes[i].Y + 3f }, Quaternion.Identity);
            riders[i].LinearVelocity = Vector3.Zero;
            riders[i].AngularVelocity = Vector3.Zero;
            riders[i].Awake = true;
        }
    }
}

// Each crystal starts a little after the one before, which is all a stagger is
void Pop()
{
    for (var i = 0; i < pops.Length; i++)
    {
        pops[i].Start();
        pops[i].Update(-0.15f * i);
    }
}

void LookFrom(Vector3 eye)
{
    var camera = game.GetCameraEntity().Transform;

    camera.Position = eye;
    camera.Rotation = MathUtilEx.LookRotation(eye, new Vector3(0f, 1.5f, 0f), Vector3.UnitY);
}

IReadOnlyList<TextElement> BuildOverlayLines() =>
[
    new("L", "Pause and resume the lift", Color.Gold),
    new("O", "Open and close the door", Color.Gold),
    new("P", "Pop the crystals in again", Color.Gold),
    new("V", "Fly to the other viewpoint", Color.Gold),
    new("R", "Drop the bodies back onto the platform", Color.Gold),
    new(""),
    new($"Lift {(lift.IsRunning ? "running" : "paused")}, sine in-out ping-pong at {lift.Progress:0.00}", Color.LightGreen),
    new($"Door {(doorOpen ? "opening" : "closing")} on a cubic in-out", Color.LightGreen),
    new("Platform: a kinematic body chasing an eased", Color.LightGray),
    new("target with a velocity.", Color.LightGray),
    new("Door, crystals, camera: transforms from tweens.", Color.LightGray),
];

/*
---example-metadata
slug: easing-3d-game
title:
  en: Easing in a 3D Game
  cs: Easing ve 3D hře
level: Beginner
category: Mathematics
complexity: 3
order: 66
description:
  en: |-
    Easing doing real work in a 3D scene, four ways, each one a Tween: a kinematic platform lifts
    a stack of physics bodies on a sine curve and comes back down, a door swings on a cubic curve,
    crystals pop in with an overshoot and bob forever, and the camera flies between two viewpoints
    on a smoother-step. The platform shows how an eased value drives a Bepu body: as a target the
    body chases with a velocity, never as a transform write.
  cs: |-
    Easing při skutečné práci ve 3D scéně, čtyřmi způsoby, pokaždé jako Tween: kinematická
    plošina zvedá hromádku fyzikálních těles po sinusové křivce a vrací se dolů, dveře se otáčejí
    po kubické křivce, krystaly vyskočí s přestřelením a navždy se pohupují a kamera přelétá mezi
    dvěma stanovišti po křivce smoother-step. Plošina ukazuje, jak zjemněná hodnota řídí těleso
    Bepu: jako cíl, za kterým těleso jede rychlostí, nikdy zápisem do transformace.
concepts:
  - Tween - start, feed the frame time, read Lerp or Slerp
  - Driving a kinematic Bepu body towards an eased target with feed-forward velocity plus a correction
  - TweenLoop.PingPong for a lift and a bob, TweenLoop.None for a door and a camera flight
  - A staggered pop-in by starting tweens with a negative head start
  - A camera flight as one tween over position and rotation
tags:
  - 3D
  - Mathematics
  - Easing
  - Animation
  - Physics
related:
  - E02_2D_Easing
  - E02_2D_EasingBasics
  - E02_2D_EasingInGame
  - E02_3D_GiveMeACube
enabled: true
created: 2026-09-14
---
*/