using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

const string CubeEntityName = "Cube";

using var game = new Game();

game.Run(new SceneSetup3D { Profiler = true }.UseBepu().UseSkybox(), start: Start);

void Start(Scene rootScene)
{
    // The script draws every collider in the scene, not its own entity's, so it lives on an entity
    // that has nothing else to do. game.AddPhysicsDebug() makes this entity, with the debug meshes too
    var debug = new Entity("Physics Debug")
    {
        new CollidableGizmoScript
        {
            Color = new Color4(0.4f, 0.843f, 0, 0.9f),
            Visible = true
        }
    };
    debug.Scene = rootScene;

    // A cube without physics beside the row: no gizmo appears for it, because physics knows nothing of it
    var cube = game.CreatePrimitive(Primitive3D.Cube(), new Bepu3DPhysicsOptions
    {
        IncludePhysics = false
    });
    cube.Transform.Position = new Vector3(1f, 0.5f, 3f);
    cube.Scene = rootScene;

    CreateCubes(rootScene, 6);
}

void CreateCubes(Scene rootScene, int count)
{
    int half = count / 2;

    for (int i = -half; i < half; i++)
    {
        var entity = game.CreatePrimitive(Primitive3D.Cube(), new() { EntityName = CubeEntityName });
        entity.Transform.Position = new Vector3(i * 0.99f, 1, 0);
        entity.Scene = rootScene;
    }
}
/*
---example-metadata
slug: collidable-gizmo
title:
  en: Collidable Gizmo
level: Other
category: Debug
complexity: 2
order: 10
description:
  en: |-
    A single-purpose demo of CollidableGizmoScript, which draws the collider Bepu is actually using so
    it can be compared against the model you think you gave it. The script draws every collider in the
    scene, so it sits on an empty entity of its own. The cube beside the row is created with
    IncludePhysics set to false, which makes the point directly: the gizmo reports what physics knows
    about, and if nothing is drawn, nothing is there.
concepts:
  - Drawing the collider a body is really using
  - Hosting a scene-wide script on an empty entity
  - "Leaving physics out with IncludePhysics = false"
  - Showing or hiding the gizmo with its Visible property
  - Diagnosing a body that does not collide as expected
  - "The one-call form for a whole scene: game.AddPhysicsDebug()"
  - "Using helpers: SceneSetup3D, AddSkybox, AddProfiler, CreatePrimitive"
tags:
  - 3D
  - Bepu
  - Debug
  - Gizmo
  - Collider
  - Physics
related:
  - E08_3D_DebugRenderComponent
  - E08_2D_DebugRender
  - E08_3D_DebugShapes
enabled: true
created: 2025-10-06
---
*/