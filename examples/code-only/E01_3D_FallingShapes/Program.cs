using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

const int ShapeCount = 30;

// The shapes to drop, and the colour of each
(PrimitiveModelType Type, Color Colour)[] shapes =
[
    (PrimitiveModelType.Cube, new Color(30, 70, 170)),
    (PrimitiveModelType.RectangularPrism, new Color(240, 150, 60)),
    (PrimitiveModelType.Sphere, new Color(235, 100, 80)),
    (PrimitiveModelType.Capsule, new Color(110, 200, 110)),
    (PrimitiveModelType.TriangularPrism, new Color(190, 130, 230)),
];

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();
    game.AddSkybox();

    for (var i = 0; i < ShapeCount; i++)
    {
        // Each kind of shape in turn
        var (type, colour) = shapes[i % shapes.Length];

        var entity = game.Create3DPrimitive(type, new()
        {
            Material = game.CreateMaterial(colour)
        });

        // One above the other, each a little to one side, so the column topples into a pile
        entity.Transform.Position = new Vector3(0.1f * (i % 3 - 1), 8 + i * 1.5f, 0);
        entity.Scene = rootScene;
    }
}

/*
---example-metadata
slug: falling-shapes-3d
title:
  en: Basic 3D Scene (Falling Shapes)
  cs: Základní 3D scéna (Padající tvary)
level: Getting Started
category: Shapes
complexity: 1
order: 15
description:
  en: |-
    The next step after the basic 3D scene: thirty shapes of five kinds and five colours, dropped in
    a column that topples into a pile. It adds a loop, a list of shapes with a colour each, and a
    material per colour. Bepu physics does the rest. The 2D twin, E01_2D_FallingShapes, drops the
    same shapes in the same colours.
  cs: |-
    Další krok po základní 3D scéně: třicet tvarů pěti druhů a pěti barev, spuštěných ve sloupci,
    který se sesype na hromadu. Přidává cyklus, seznam tvarů s barvou pro každý a materiál pro
    každou barvu. O zbytek se postará fyzika Bepu. 2D dvojče, E01_2D_FallingShapes, pouští stejné
    tvary ve stejných barvách.
concepts:
  - Creating many 3D primitives in a loop with Create3DPrimitive
  - A list of shape types with a colour each
  - Applying a material with CreateMaterial
  - Positioning entities so that they fall into a pile
  - "Using helpers: SetupBase3DScene, AddSkybox"
tags:
  - 3D
  - Bepu
  - Material
  - Shapes
  - Primitive
  - Scene Setup
  - Skybox
  - Transform
  - Position
related:
  - E01_3D_BasicScene
  - E01_2D_FallingShapes
  - E02_3D_Primitives
screenshotFrame: 150
enabled: true
created: 2026-10-03
---
*/