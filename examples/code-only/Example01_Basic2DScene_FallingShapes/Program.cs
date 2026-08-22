using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

const int ShapeCount = 30;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase2DScene();
    game.AddProfiler();

    var otherShape = game.Create2DPrimitive(Primitive2DModelType.Rectangle, new()
    {
        Material = game.CreateFlatMaterial(Color.Gold),
        Size = new Vector2(0.5f, 0.8f)
    });
    otherShape.Transform.Position = new Vector3(0.2f, 4f, 0);
    otherShape.Scene = rootScene;

    for (int i = 0; i <= ShapeCount; i++)
    {
        var shape = game.Create2DPrimitive(Primitive2DModelType.Capsule, new()
        {
            Material = game.CreateFlatMaterial(Color.White),
            Component = new Body2DComponent()
            {
                Collider = new CompoundCollider(),
                FrictionCoefficient = 0.35f
            }
        });
        shape.Transform.Position = new Vector3(0.001f * i, 10 + i * 2, 0);
        shape.Scene = rootScene;
    }
}

/*
---example-metadata
slug: falling-shapes-2d
title:
  en: Basic2D Scene (Falling Shapes)
  cs: Základní 2D scéna (Padající tvary)
level: Beginner
category: Physics
complexity: 1
order: 90
description:
  en: |-
    Create a minimal 2D scene using toolkit helpers and place multiple capsule primitives with flat materials.
    Demonstrates primitive creation, basic positioning, and attaching the entities to the scene.
    The shapes will fall due to physics, showcasing the integration of Bepu physics in a 2D scene.
  cs: |-
    Vytvoření minimální 2D scény pomocí nástrojů sady a umístění několika kapslí s plochými materiály.
    Ukazuje vytvoření primitivních tvarů, základní umístění a připojení entit k scéně.
    Tvary budou padat díky fyzice, což ukazuje integraci Bepu fyziky v 2D scéně.
concepts:
  - Creating a 2D primitive with Create2DPrimitive
  - Applying a flat material with CreateFlatMaterial
  - Setting an entity position through primitive options
  - Adding entities to a Scene (rootScene)
  - "Using helpers: SetupBase2DScene"
tags:
  - 2D
  - Bepu
  - Flat Material
  - Shapes
  - Primitive
  - Capsule
  - Scene Setup
  - Transform
  - Position
related:
  - Example02_GiveMeACube
  - Example01_Basic2DScene_Primitives
  - Example01_Material
enabled: true
created: 2026-06-11
---
*/