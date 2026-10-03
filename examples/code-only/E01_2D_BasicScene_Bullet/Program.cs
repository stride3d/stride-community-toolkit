using Stride.CommunityToolkit.Bullet;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase2DScene();

    var entity = game.Create2DPrimitive(Primitive2DModelType.Capsule, new()
    {
        Material = game.CreateFlatMaterial(Color.White)
    });
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;
}

/*
---example-metadata
slug: basic-2d-scene-bullet
title:
  en: Basic2D Scene (Capsule) - Bullet Physics
  cs: Základní 2D scéna (Kapsle) - Bullet Physics
level: Getting Started
category: Shapes
complexity: 1
order: 40
description:
  en: |-
    The same first 2D scene as E01_2D_BasicScene, running on the legacy Bullet physics engine instead
    of Bepu. The scene code is character-for-character identical; the only difference is which toolkit
    package is referenced and which namespace is opened. That is the point of the example - physics is
    swapped at the project level, not by rewriting the scene.
  cs: |-
    Stejná první 2D scéna jako E01_2D_BasicScene, jen běží na starším fyzikálním enginu Bullet místo
    Bepu. Kód scény je znak po znaku totožný; liší se jen tím, na který balíček sady projekt odkazuje
    a který jmenný prostor otevírá. O to v příkladu jde - fyzika se vymění na úrovni projektu, ne
    přepsáním scény.
concepts:
  - Running the base 2D scene on the legacy Bullet physics engine
  - "Switching engine by namespace: Stride.CommunityToolkit.Bullet in place of .Bepu"
  - Why the scene code needs no change when the physics engine does
  - "Using helpers: SetupBase2DScene, Create2DPrimitive, CreateFlatMaterial"
tags:
  - 2D
  - Bullet
  - Physics
  - Shapes
  - Primitive
  - Capsule
  - Scene Setup
  - Legacy
related:
  - E01_2D_BasicScene
  - E01_3D_BasicScene_Bullet
enabled: true
created: 2025-11-30
---
*/