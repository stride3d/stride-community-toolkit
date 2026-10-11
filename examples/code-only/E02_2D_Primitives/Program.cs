using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

var random = new Random(1);
var parallelogramVertices = new Vector2[]
{
    new(-0.5f, -0.25f),
    new(0.5f, -0.25f),
    new(0.75f, 0.25f),
    new(-0.25f, 0.25f),
};

List<Primitive2D> shapes = [
    Primitive2D.Circle(),
    Primitive2D.Capsule(),
    Primitive2D.Rectangle(),
    Primitive2D.Square(),
    Primitive2D.RegularPolygon(),
    Primitive2D.Triangle(),
    Primitive2D.Polygon(parallelogramVertices),
];

using var game = new Game();

game.Run(new SceneSetup2D().UseBepu(), start: Start);

void Start(Scene rootScene)
{
    foreach (var (index, shape) in shapes.Index())
    {
        var entity = game.CreatePrimitive(shape, new()
        {
            Material = game.CreateFlatMaterial(random.NextColor()),
        });

        entity.Transform.Position = new Vector3(0, 10 + index * 1.5f, 0);
        entity.Scene = rootScene;
    }
}

/*
---example-metadata
slug: primitives-2d
title:
  en: Basic 2D Scene (Multiple Primitives)
  cs: Základní 2D scéna (Více primitiv)
level: Beginner
category: Shapes
complexity: 1
order: 80
description:
  en: |-
    Create a minimal 2D scene using toolkit helpers and place multiple different primitive shapes.
    Demonstrates entity creation, basic positioning, and attaching the entities to the scene.
  cs: |-
    Vytvoření minimální 2D scény pomocí nástrojů sady a umístění více různých primitivních tvarů.
    Ukazuje vytvoření entity, základní umístění a připojení entit k scéně.
concepts:
  - Creating multiple 2D primitives (Circle, Capsule, Rectangle, Square, Polygon, Triangle)
  - Positioning entities with Transform.Position
  - Adding entities to a Scene (rootScene)
  - "Using helpers: SceneSetup2D"
tags:
  - 2D
  - Bepu
  - Shapes
  - Primitives
  - Scene Setup
  - Transform
  - Position
related:
  - E08_2D_DebugRender
  - E01_2D_BasicScene_Bullet
  - E01_2D_BasicScene
enabled: true
created: 2025-11-30
---
*/