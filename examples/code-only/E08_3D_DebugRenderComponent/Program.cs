using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

const string SphereEntityName = "Sphere";

using var game = new Game();

game.Run(new SceneSetup3D { Profiler = true }.UseBepu().UseSkybox(), start: Start);

void Start(Scene rootScene)
{
    var entity = game.CreatePrimitive(Primitive3D.Cube());
    entity.Transform.Position = new Vector3(1f, 0.5f, 3f);
    entity.Add(new DebugRenderComponentScript() { Visible = true });
    entity.Scene = rootScene;

    CreateSpheres(rootScene, 6);
}

void CreateSpheres(Scene rootScene, int count)
{
    int half = count / 2;

    for (int i = -half; i < half; i++)
    {
        var entity = game.CreatePrimitive(Primitive3D.Sphere(), new() { EntityName = SphereEntityName });
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
    The companion to the collidable gizmo: DebugRenderComponentScript draws the wireframe of an entity's
    own mesh rather than its collider. Having both on the same scene is how you tell the two apart -
    when a body behaves oddly, the question is usually whether the mesh and the collider agree, and each
    script answers one half of that.
concepts:
  - Drawing an entity's mesh as a wireframe overlay
  - "How this differs from CollidableGizmoScript, which draws the collider"
  - Toggling the overlay with its Visible property
  - Comparing mesh against collider when a body misbehaves
  - "The one-call form for a whole scene: game.AddPhysicsDebug()"
  - "Using helpers: SceneSetup3D, AddSkybox, AddProfiler, CreatePrimitive"
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