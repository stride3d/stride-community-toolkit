using E02_3D_SyncScript;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

void Start(Scene scene)
{
    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddProfiler();

    // The cube starts on its circle; the script works the centre out from where it starts
    var entity = game.Create3DPrimitive(PrimitiveModelType.Cube);
    entity.Transform.Position = new Vector3(1f, 0.5f, 3f);
    entity.Add(new RotationComponentScript());
    entity.Scene = scene;

    var entityCone = game.Create3DPrimitive(PrimitiveModelType.Cone, new() { Size = new(0.5f, 5, 0) });
    entityCone.Transform.Position = new Vector3(0, 6, 0);
    entityCone.Scene = scene;
}
/*
---example-metadata
slug: sync-script
title:
  en: SyncScript - moving a body every frame
level: Beginner
category: Scripts
complexity: 2
order: 50
description:
  en: |-
    A cube driven in a circle by a SyncScript, which is the ordinary way to run code every frame. The
    part worth copying is how it moves: the body is made kinematic and given a velocity, rather than
    a position written to Transform.Position. With a physics body attached the simulation owns the
    transform, so a position written there moves the mesh and leaves the collider behind; a velocity
    moves both, which is what lets the cube push dynamic bodies out of the way. SetTargetPose is the
    call for once-per-physics-step code, not for a per-frame Update.
concepts:
  - Running per-frame logic by deriving from SyncScript
  - Attaching a script to an entity with Entity.Add
  - Fetching a sibling component with Entity.Get
  - Why physics owns the transform once a body is attached
  - "Moving a kinematic body with LinearVelocity, not Transform.Position"
  - Why SetTargetPose belongs in ISimulationUpdate and not in a per-frame Update
  - Framerate independence with Game.DeltaTime
  - "Using helpers: SetupBase3DScene, AddSkybox, AddProfiler, Create3DPrimitive"
tags:
  - 3D
  - Bepu
  - Scripts
  - SyncScript
  - Kinematic Body
  - Transform
related:
  - E02_3D_GiveMeACube
  - E02_3D_GiveMeACube_SimulationUpdate
  - E01_3D_BasicScene
enabled: true
created: 2025-10-06
---
*/