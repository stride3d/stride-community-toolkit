using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();
    game.AddSkybox();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;
}

/*
---example-metadata
title:
  en: Basic3D Scene (Capsule)
  cs: Základní 3D scéna (Kapsle)
level: Getting Started
category: Shapes
complexity: 1
description:
  en: |
    Create a minimal 3D scene using toolkit helpers and place a single capsule primitive.
    Demonstrates entity creation, basic positioning, and attaching the entity to the scene.
  cs: |
    Vytvoření minimální 3D scény pomocí nástrojů sady a umístění jediné kapsle jako primitivního tvaru.
    Ukazuje vytvoření entity, základní umístění a připojení entity k scéně.
concepts:
  - Creating a 3D primitive (Capsule)
  - Positioning an entity with Transform.Position
  - Adding entities to a Scene (rootScene)
  - "Using helpers: SetupBase3DScene and AddSkybox"
related:
  - Example02_GiveMeACube
  - Example01_Basic3DScene_Primitives
  - Example01_Material
tags:
  - 3D
  - Shapes
  - Primitive
  - Capsule
  - Scene Setup
  - Skybox
  - Transform
  - Position
  - Getting Started
order: 1
enabled: true
created: 2023-09-11
---
*/