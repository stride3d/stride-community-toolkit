using E04_StrideUI_DragAndDrop;
using Stride.CommunityToolkit.Bullet;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;

const int ShapeCount = 100;
const int RemovalThresholdY = -30;

UIManager? uiManager = null;
PrimitiveGenerator? shapeGenerator = null;

using var game = new Game();

game.Run(new SceneSetup3D { Profiler = true }.UseBullet().UseSkybox(), start: Start, update: Update);

void Start(Scene scene)
{
    game.AddEntityInfoRenderer(new() { EnableBackground = true });

    shapeGenerator = new PrimitiveGenerator(game, scene);
    uiManager = new UIManager(game.Content.Load<SpriteFont>("/Stride.Engine/StrideDefaultFont"), GenerateRandomSpheres);
    uiManager.Entity.Scene = scene;

    var capsule = game.CreatePrimitive(Primitive3D.Capsule());
    capsule.Transform.Position = new Vector3(0, 8, 0);
    capsule.Scene = scene;
}

void Update(Scene scene, GameTime time)
{
    // Backwards by index: taking an entity out of the scene while a foreach walks the same list
    // would skip the entity after it
    for (var i = scene.Entities.Count - 1; i >= 0; i--)
    {
        var entity = scene.Entities[i];

        if (entity.Transform.Position.Y < RemovalThresholdY)
        {
            entity.Scene = null;

            shapeGenerator?.Subtract(1);
            uiManager?.UpdateTextBlock($"Total Shapes: {shapeGenerator?.TotalShapes ?? 0}");
        }
    }
}

void GenerateRandomSpheres()
{
    var totalShapes = shapeGenerator?.Generate(ShapeCount, Primitive3D.Sphere(0.3f));

    uiManager?.UpdateTextBlock($"Total Shapes: {totalShapes ?? 0}");
}
/*
---example-metadata
slug: stride-ui-draggable-window-bullet
title:
  en: Stride UI - Draggable Window - Bullet Physics
level: Advanced
category: UI
complexity: 4
order: 130
description:
  en: |-
    The draggable window example running on the legacy Bullet physics engine. The UI code is unchanged -
    only the spheres the windows spawn are simulated differently. Useful as a direct comparison if you
    are maintaining a project that has not moved to Bepu yet.
concepts:
  - Running the draggable window scene on the legacy Bullet engine
  - "Switching engine by namespace: Stride.CommunityToolkit.Bullet in place of .Bepu"
  - Why the UI layer is unaffected by the physics engine underneath
tags:
  - 3D
  - UI
  - Stride UI
  - Drag and Drop
  - Bullet
  - Physics
  - Legacy
related:
  - E04_StrideUI_DragAndDrop
  - E01_3D_BasicScene_Bullet
enabled: true
created: 2025-01-26
---
*/