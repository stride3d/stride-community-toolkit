using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

const int ShapeCount = 30;

// The shapes to drop, and the colour of each
(Primitive2DModelType Type, Color Colour)[] shapes =
[
    (Primitive2DModelType.Square, new Color(30, 70, 170)),
    (Primitive2DModelType.Rectangle, new Color(240, 150, 60)),
    (Primitive2DModelType.Circle, new Color(235, 100, 80)),
    (Primitive2DModelType.Capsule, new Color(110, 200, 110)),
    (Primitive2DModelType.Triangle, new Color(190, 130, 230)),
];

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase2DScene();

    for (var i = 0; i < ShapeCount; i++)
    {
        // Each kind of shape in turn
        var (type, colour) = shapes[i % shapes.Length];

        var entity = game.Create2DPrimitive(type, new()
        {
            Material = game.CreateFlatMaterial(colour)
        });

        // One above the other, each a little to one side, so the column topples into a pile
        entity.Transform.Position = new Vector3(0.1f * (i % 3 - 1), 8 + i * 1.5f, 0);
        entity.Scene = rootScene;
    }
}

/*
---example-metadata
slug: falling-shapes-2d
title:
  en: Basic 2D Scene (Falling Shapes)
  cs: Základní 2D scéna (Padající tvary)
level: Getting Started
category: Shapes
complexity: 1
order: 35
description:
  en: |-
    The next step after the basic 2D scene: thirty shapes of five kinds and five colours, dropped in
    a column that topples into a pile. It adds a loop, a list of shapes with a colour each, and a
    flat material per colour. Bepu physics does the rest. The 3D twin, E01_3D_FallingShapes, drops
    the same shapes in the same colours.
  cs: |-
    Další krok po základní 2D scéně: třicet tvarů pěti druhů a pěti barev, spuštěných ve sloupci,
    který se sesype na hromadu. Přidává cyklus, seznam tvarů s barvou pro každý a plochý materiál
    pro každou barvu. O zbytek se postará fyzika Bepu. 3D dvojče, E01_3D_FallingShapes, pouští
    stejné tvary ve stejných barvách.
concepts:
  - Creating many 2D primitives in a loop with Create2DPrimitive
  - A list of shape types with a colour each
  - Applying a flat material with CreateFlatMaterial
  - Positioning entities so that they fall into a pile
  - "Using helpers: SetupBase2DScene"
tags:
  - 2D
  - Bepu
  - Flat Material
  - Shapes
  - Primitive
  - Scene Setup
  - Transform
  - Position
related:
  - E01_2D_BasicScene
  - E01_3D_FallingShapes
  - E02_2D_Primitives
screenshotFrame: 150
enabled: true
created: 2026-10-03
---
*/