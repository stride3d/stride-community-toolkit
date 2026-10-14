using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

// The five colours of the first examples, one shape after another
Color[] colours = [new(30, 70, 170), new(240, 150, 60), new(235, 100, 80), new(110, 200, 110), new(190, 130, 230)];

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
            Material = game.CreateFlatMaterial(colours[index % colours.Length]),
        });

        entity.Transform.Position = new Vector3(0, 10 + index * 1.5f, 0);
        entity.Scene = rootScene;
    }
}

/*
---example-metadata
slug: primitives-2d
title:
  en: 2D Primitives
  cs: 2D primitiva
level: Beginner
category: Shapes
complexity: 1
order: 11
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
  - E02_3D_Primitives
  - E01_2D_BasicScene
  - E08_2D_DebugRender
enabled: true
created: 2025-11-30
---
*/