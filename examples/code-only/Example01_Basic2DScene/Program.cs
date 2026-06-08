using Stride.CommunityToolkit.Bepu;
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
        Material = game.CreateFlatMaterial(Color.Orange)
    });
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;
}

/*
---example-metadata
title:
  en: Basic2D Scene (Capsule)
  cs: Základní 2D scéna (Kapsle)
level: Getting Started
category: Shapes
complexity: 1
description:
  en: |
    Create a minimal 2D scene using toolkit helpers and place a single capsule primitive with a flat material.
    Demonstrates primitive creation, basic positioning, and attaching the entity to the scene.
  cs: |
    Vytvoření minimální 2D scény pomocí nástrojů sady a umístění jediné kapsle s plochým materiálem.
    Ukazuje vytvoření primitivního tvaru, základní umístění a připojení entity k scéně.
concepts:
  - Creating a 2D primitive with Create2DPrimitive
  - Applying a flat material with CreateFlatMaterial
  - Setting an entity position through primitive options
  - Adding entities to a Scene (rootScene)
  - "Using helpers: SetupBase2DScene"
related:
  - Example02_GiveMeACube
  - Example01_Basic2DScene_Primitives
  - Example01_Material
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
  - Getting Started
order: 1
enabled: true
created: 2025-11-30
---
*/