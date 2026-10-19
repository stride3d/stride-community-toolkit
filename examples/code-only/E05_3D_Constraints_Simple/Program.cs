using Stride.BepuPhysics;
using Stride.BepuPhysics.Constraints;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(new SceneSetup3D { Profiler = true }.UseBepu().UseSkybox(), start: Start);

void Start(Scene scene)
{
    // A ground gizmo on top of the setup's scene
    game.AddGroundGizmo(new(-5, 0, -5), showAxisName: true);

    // Left mouse picks either sphere up; the servo drags the other one along (GrabberScript, see E05_3D_Grabber).
    game.GetCameraEntity().Add(new GrabberScript());

    var overlay = TextOverlay.GetOrCreate(game);
    overlay.SectionGap = 0;
    overlay.AddSection("Constraint", static () =>
    [
        new("Left mouse", "Pick a sphere up, carry it, throw it", Color.Yellow),
        new("Mouse wheel", "Carry distance", Color.Yellow),
        new("T", "Hold and move the mouse to turn the held sphere", Color.Yellow),
        new(""),
        new("A distance servo holds the spheres 3 units apart", Color.LightGray),
        new("Carry one and the other follows", Color.LightGray),
    ]);

    var entity = game.CreatePrimitive(Primitive3D.Capsule());
    entity.Transform.Position = new Vector3(0, 3, 0);
    entity.Scene = scene;

    var sphere = game.CreatePrimitive(Primitive3D.Sphere(), new()
    {
        Material = game.CreateMaterial(Color.Gold)
    });
    sphere.Transform.Position = new Vector3(0.1f, 5, -0.3f);

    var connectedSphere = game.CreatePrimitive(Primitive3D.Sphere());
    connectedSphere.Transform.Position = new Vector3(-2f, 1, -2f);

    // A distance servo holds the two spheres at a target distance: grab one and the other follows
    var distanceServo = new DistanceServoConstraintComponent
    {
        A = sphere.Get<BodyComponent>(),
        B = connectedSphere.Get<BodyComponent>(),
        TargetDistance = 3.0f,
    };

    sphere.Add(distanceServo);

    sphere.Scene = scene;
    connectedSphere.Scene = scene;
}
/*
---example-metadata
slug: simple-constraint
title:
  en: Simple Constraint
level: Intermediate
category: Physics
complexity: 3
order: 100
description:
  en: |-
    One constraint, doing one thing: a distance servo holding two spheres three units apart, pulling
    them together or pushing them apart until they settle there. It is the gentlest possible
    introduction to Bepu constraints, and the shortest - everything else in the constraint family builds
    on the same pattern of naming two bodies and describing the relationship you want between them.
concepts:
  - Connecting two bodies with DistanceServoConstraintComponent
  - Setting a target distance and watching the solver reach it
  - How a constraint component attaches to an entity
  - Reading the result against a ground reference grid
  - Picking a sphere up with GrabberScript and feeling the servo pull the other along
  - "Using helpers: SceneSetup3D, UseSkybox, AddGroundGizmo"
tags:
  - 3D
  - Bepu
  - Physics
  - Constraint
  - Servo
  - Distance
related:
  - E05_3D_Constraints
  - E05_3D_Constraints_Motors
  - E05_3D_Constraints_Rope
enabled: true
created: 2025-03-09
---
*/