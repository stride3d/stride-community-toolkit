using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

const string SphereEntityName = "Sphere";

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddProfiler();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
    {
        IncludeCollider = false
    });
    entity.Transform.Position = new Vector3(1f, 0.5f, 3f);
    entity.Add(new CollidableGizmoScript()
    {
        Color = new Color4(0.4f, 0.843f, 0, 0.9f),
        Visible = true
    });
    entity.Scene = rootScene;

    CreateSpheres(rootScene, 6);
}

void CreateSpheres(Scene rootScene, int count)
{
    int half = count / 2;

    for (int i = -half; i < half; i++)
    {
        var entity = game.Create3DPrimitive(PrimitiveModelType.Cube, new() { EntityName = SphereEntityName });
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
    it can be compared against the model you think you gave it. The cube here is created with
    IncludeCollider set to false and the gizmo left visible, which makes the point directly: the gizmo
    reports what physics knows about, and if nothing is drawn, nothing is there.
concepts:
  - Drawing the collider a body is really using
  - "Suppressing the generated collider with IncludeCollider = false"
  - Showing or hiding the gizmo with its Visible property
  - Diagnosing a body that does not collide as expected
  - "Using helpers: SetupBase3DScene, AddSkybox, AddProfiler, Create3DPrimitive"
tags:
  - 3D
  - Bepu
  - Debug
  - Gizmo
  - Collider
  - Physics
related:
  - Example08_DebugRenderComponent
  - Example01_Basic2DScene_DebugRender
  - Example08_DebugShapes
enabled: true
created: 2025-10-06
---
*/
