using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(new SceneSetup3D { Profiler = true }.UseBepu().UseSkybox(), start: Start);

void Start(Scene rootScene)
{
    // Left mouse grabs any body, drags it and throws it on release
    game.AddGrabber();

    // One DebugRenderComponent turns the wireframe on for every Bepu entity in the scene, so the script
    // lives on an entity that has nothing else to do. game.AddPhysicsDebug() makes this entity, with
    // the collider gizmos too
    var debug = new Entity("Physics Debug") { new DebugRenderComponentScript { Visible = true } };
    debug.Scene = rootScene;

    var cube = game.CreatePrimitive(Primitive3D.Cube());
    cube.Transform.Position = new Vector3(1f, 0.5f, 3f);
    cube.Scene = rootScene;

    CreateSpheres(rootScene, 6);
}

void CreateSpheres(Scene rootScene, int count)
{
    var half = count / 2;

    for (var i = -half; i < half; i++)
    {
        var entity = game.CreatePrimitive(Primitive3D.Sphere());
        entity.Transform.Position = new Vector3(i * 0.99f, 1, 0);
        entity.Scene = rootScene;
    }
}
/*
---example-metadata
slug: debug-render-component
title:
  en: Debug Render Component
level: Other
category: Debug
complexity: 2
order: 20
description:
  en: |-
    The companion to the collidable gizmo: DebugRenderComponentScript draws the wireframe of every
    entity's mesh rather than its collider, from one script on an empty entity. Having both on the same scene is how you tell the two apart -
    when a body behaves oddly, the question is usually whether the mesh and the collider agree, and each
    script answers one half of that.
concepts:
  - Drawing every entity's mesh as a wireframe overlay, from one script on an empty entity
  - "How this differs from CollidableGizmoScript, which draws the collider"
  - Toggling the overlay with its Visible property
  - Comparing mesh against collider when a body misbehaves
  - "The one-call form for a whole scene: game.AddPhysicsDebug()"
  - "Using helpers: SceneSetup3D, UseSkybox, CreatePrimitive, AddGrabber"
tags:
  - 3D
  - Bepu
  - Debug
  - Wireframe
  - Mesh
  - Visualisation
related:
  - E08_3D_CollidableGizmo
  - E08_2D_DebugRender
  - E08_3D_DebugShapes
enabled: true
created: 2025-10-06
---
*/