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

    for (int i = 0; i < ShapeCount; i++)
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
slug: falling-shapes-physics-2d
title:
  en: Falling Shapes with Physics Options (2D)
  cs: Padající tvary s nastavením fyziky (2D)
level: Beginner
category: Physics
complexity: 1
order: 90
description:
  en: |-
    Thirty capsules and a rectangle dropped in a column, as in E01_2D_FallingShapes, with one thing
    added: each capsule is given its own Body2DComponent with a lower FrictionCoefficient, so the pile
    spreads wider than with the default. It shows where a body's physics options go when a primitive
    is created.
  cs: |-
    Třicet kapslí a obdélník spuštěné ve sloupci jako v E01_2D_FallingShapes, s jedním přídavkem:
    každá kapsle dostane vlastní Body2DComponent s nižším FrictionCoefficient, takže se hromada
    rozprostře víc než s výchozí hodnotou. Ukazuje, kam patří fyzikální nastavení tělesa při
    vytváření primitivního tvaru.
concepts:
  - Passing a Body2DComponent to Create2DPrimitive to set physics options
  - FrictionCoefficient - a lower value lets the pile spread
  - An empty CompoundCollider that the helper fills with the fitted shape
  - "Using helpers: SetupBase2DScene, AddProfiler, Create2DPrimitive, CreateFlatMaterial"
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
  - E01_2D_FallingShapes
  - E05_3D_PhysicsMaterials
  - E02_2D_Primitives
  - E02_3D_Material
enabled: true
created: 2026-06-11
---
*/