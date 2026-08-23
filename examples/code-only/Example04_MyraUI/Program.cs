using Example04_MyraUI;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Engine;
using Stride.Games;

using var game = new Game();

// State flag to track health bar visibility
bool isHealthBarVisible = false;

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    SetupBase3DScene();
}

void Update(Scene rootScene, GameTime time)
{
    InitializeHealthBar();
}

void SetupBase3DScene()
{
    game.AddGraphicsCompositor()
        .AddCleanUIStage() //optional
        .AddSceneRenderer(new MyraSceneRenderer());
    game.Add3DCamera().Add3DCameraController();
    game.AddDirectionalLight();
    game.AddSkybox();
    game.Add3DGround();
}


// Initializes the health bar if it is not already visible.
void InitializeHealthBar()
{
    if (isHealthBarVisible) return;

    var mainView = game.Services.GetService<MainView>();

    if (mainView == null) return;

    // Create and add a new health bar to the main view
    mainView.Widgets.Add(UIUtils.CreateHealthBar(-50, "#FFD961FF"));

    isHealthBarVisible = true;
}
/*
---example-metadata
slug: myra-ui-draggable-window-and-services
title:
  en: Myra UI - Draggable Window and Services
level: Advanced
category: UI
complexity: 4
order: 140
description:
  en: |-
    Myra, an external UI library, hosted inside Stride: a draggable window, two health bars - one
    declared in the view, one added while it runs - and service lookup through GetService to keep the UI
    from depending directly on the game. Hosting a foreign UI toolkit means giving it a scene renderer
    of its own and a place in the compositor, which is what MyraSceneRenderer does.
    Currently disabled - it does not build against Stride 4.4.
concepts:
  - Hosting an external UI library inside Stride
  - Rendering a foreign UI through a custom scene renderer
  - Building a draggable window from Myra widgets
  - Adding widgets statically and at runtime
  - "Decoupling UI from the game with GetService()"
tags:
  - 3D
  - UI
  - Myra
  - Third Party
  - Draggable
  - Services
related:
  - Example03_StrideUI_CapsuleAndWindow
  - Example10_StrideUI_DragAndDrop
  - Example11_ImGui
media: stride-game-engine-example04-myra-ui-draggable-window.webp
enabled: false
created: 2023-09-15
---
*/