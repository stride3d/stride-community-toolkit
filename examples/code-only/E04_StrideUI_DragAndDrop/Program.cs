using E04_StrideUI_DragAndDrop;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
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

game.Run(new SceneSetup3D { Profiler = true }.UseBepu().UseSkybox(), start: Start, update: Update);

void Start(Scene scene)
{
    game.AddGrabber();
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
slug: stride-ui-draggable-window
title:
  en: Stride UI - Draggable Window
level: Advanced
category: UI
complexity: 4
order: 120
description:
  en: |-
    A windowing system built on Stride's UI: windows with title bars and close buttons that can be
    dragged around, and that come to the front when clicked. Z-order is the part that makes it feel
    real, and it needs a container tracking every window rather than logic on each one. The windows spawn
    falling spheres and keep a live count, with objects that drop out of the world cleaned up and
    subtracted again.
concepts:
  - Building a draggable window from Canvas and pointer events
  - Bringing a window to the front by managing z-order centrally
  - "Splitting responsibility: container, window, manager, generator"
  - Spawning scene objects from a UI button
  - Keeping a UI counter in step with the scene
  - Removing entities that fall out of the world
  - "Using helpers: SceneSetup3D, UseSkybox, CreatePrimitive"
tags:
  - 3D
  - UI
  - Stride UI
  - Drag and Drop
  - Canvas
  - Window
  - Z-Order
related:
  - E04_StrideUI_BasicWindow
  - E04_StrideUI_DragAndDrop_Bullet
  - E04_CubeClicker
enabled: true
created: 2024-10-05
---
*/