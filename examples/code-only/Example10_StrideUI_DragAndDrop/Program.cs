using Example10_StrideUI_DragAndDrop;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Renderers;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;

UIManager? _uiManager = null;
PrimitiveGenerator? _shapeGenerator = null;

const int ShapeCount = 100;
const int RemovalThresholdY = -30;
const string TotalCubes = "Total Shapes: ";

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    // Setup the base 3D scene with default lighting, camera, etc.
    game.SetupBase3DScene();

    // Add debugging aids: entity names, positions
    game.AddEntityDebugSceneRenderer(new()
    {
        EnableBackground = true
    });

    game.AddSkybox();
    game.AddProfiler();

    _shapeGenerator = new PrimitiveGenerator(game, scene);

    var font = game.Content.Load<SpriteFont>("/Stride.Engine/StrideDefaultFont");

    // Create and display the UI components on screen
    CreateAndAddUI(scene, font);

    // Add an example 3D capsule entity to the scene for visual reference
    AddExampleShape(scene);
}

void Update(Scene scene, GameTime time)
{
    foreach (var entity in scene.Entities)
    {
        if (entity.Transform.Position.Y < RemovalThresholdY)
        {
            entity.Scene = null;

            _shapeGenerator?.SubtractTotalCubes(1);

            _uiManager?.UpdateTextBlock($"{TotalCubes} {_shapeGenerator?.TotalShapes ?? 0}");
        }
    }
}

void CreateAndAddUI(Scene scene, SpriteFont font)
{
    _uiManager = new UIManager(font, GenerateRandomSpheres);

    _uiManager.Entity.Scene = scene;
}

void AddExampleShape(Scene scene)
{
    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = scene;
}

void GenerateRandomSpheres()
{
    var totalShapes = _shapeGenerator?.Generate(ShapeCount, PrimitiveModelType.Sphere);

    _uiManager?.UpdateTextBlock($"{TotalCubes} {totalShapes ?? 0}");
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
  - "Using helpers: SetupBase3DScene, AddSkybox, Create3DPrimitive"
tags:
  - 3D
  - UI
  - Stride UI
  - Drag and Drop
  - Canvas
  - Window
  - Z-Order
related:
  - Example03_StrideUI_CapsuleAndWindow
  - Example10_StrideUI_DragAndDrop_BulletPhysics
  - Example07_CubeClicker
media: stride-game-engine-example-10-draggable-window.webp
enabled: true
created: 2024-10-05
---
*/