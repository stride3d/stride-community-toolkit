using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.DebugShapes;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

ImmediateDebugRenderSystem? debugDraw = null;

// Kept in a list, so Update does not search the scene every frame
List<Entity> spheres = [];

using var game = new Game();

game.Run(new SceneSetup3D { Profiler = true }.UseBepu().UseSkybox(), start: Start, update: Update);

void Start(Scene rootScene)
{
    debugDraw = game.AddDebugShapes();
    debugDraw.Visible = true; // force visible in non-debug builds
    debugDraw.PrimitiveColor = Color.Lime;

    CreateSpheres(rootScene, 6);
}

void CreateSpheres(Scene rootScene, int count)
{
    var half = count / 2;

    for (var i = -half; i < half; i++)
    {
        var entity = game.CreatePrimitive(Primitive3D.Sphere());
        entity.Transform.Position = new Vector3(i * 0.99f, 8, 0);
        entity.Scene = rootScene;
        spheres.Add(entity);
    }
}

void Update(Scene scene, GameTime gameTime)
{
    if (debugDraw is null) return;

    foreach (var sphere in spheres)
    {
        var position = sphere.Transform.Position;

        debugDraw.DrawSphere(position, 0.5f, Color.Red, solid: false);
        debugDraw.DrawCircle(position, 0.55f, rotation: sphere.Transform.Rotation, color: Color.Orange, solid: false);
    }
}
/*
---example-metadata
slug: debug-shapes-usage
title:
  en: Debug Shapes Usage
level: Intermediate
category: Debug
complexity: 2
order: 160
description:
  en: |-
    The short version of the debug shapes example: turn the system on, draw a sphere and a circle, done.
    If the full tour is a reference, this is the copy-paste starting point - the minimum needed to get a
    shape on screen while debugging something else.
concepts:
  - Initialising the debug shape system and enabling it for a scene
  - Drawing a sphere and a circle at given positions
  - "Requires the Stride.CommunityToolkit.DebugShapes package"
tags:
  - 3D
  - Debug
  - Debug Shapes
  - Immediate Mode
  - Visualisation
related:
  - E08_3D_DebugShapes
  - E08_3D_CollidableGizmo
enabled: true
created: 2025-08-24
---
*/