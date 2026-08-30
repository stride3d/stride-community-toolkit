using Example08_DebugShapes.Scripts;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.DebugShapes.Code;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    SetupBaseScene();
    AddDebugComponent(rootScene);
}

void SetupBaseScene()
{
    game.AddGraphicsCompositor();
    game.Add3DCamera().Add3DCameraController(displayPosition: DisplayPosition.BottomRight);
    game.AddDirectionalLight();
    game.AddSkybox();
    game.Add3DGround();
    game.AddDebugShapes();
    game.AddProfiler();
    game.SetCameraPosition(new(13, 15, 27));
    game.SetCameraRotation(new(17, -17, 0));
}

void AddDebugComponent(Scene scene)
{
    var entity = new Entity("Debug Shapes")
    {
        new ShapeUpdater()
    };

    scene.Entities.Add(entity);
}
/*
---example-metadata
slug: debug-shapes
title:
  en: Debug Shapes
level: Intermediate
category: Debug
complexity: 3
order: 150
description:
  en: |-
    The full tour of the DebugShapes package: every immediate-mode primitive it can draw, exercised from
    a ShapeUpdater component so the shapes animate and the batching can be seen under load. Debug shapes
    are drawn per frame and never become entities, which is what makes them cheap enough to leave in
    while you work.
concepts:
  - Registering the debug shape renderer
  - Drawing every primitive the package offers
  - Why immediate-mode shapes cost nothing to create and destroy
  - Driving debug drawing from a component that updates each frame
  - "Requires the Stride.CommunityToolkit.DebugShapes package"
tags:
  - 3D
  - Debug
  - Debug Shapes
  - Immediate Mode
  - Gizmo
  - Visualisation
related:
  - Example08_DebugShapes_Usage
  - Example08_CollidableGizmo
media: stride-game-engine-example08-debug-shapes.webp
enabled: true
created: 2024-01-08
---
*/