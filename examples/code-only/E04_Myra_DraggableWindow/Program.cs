using E04_Myra_DraggableWindow;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Engine;
using Stride.Games;

// Whether the second health bar has been added yet
var healthBarAdded = false;

using var game = new Game();

game.Run(new SceneSetup3D().UseBepu().UseSkybox(), start: Start, update: Update);

void Start(Scene rootScene) => game.AddSceneRenderer(new MyraSceneRenderer());

// The second health bar is added while the game runs, once the renderer has put the view in the services
void Update(Scene rootScene, GameTime time)
{
    if (healthBarAdded || game.Services.GetService<MainView>() is not { } mainView) return;

    mainView.Widgets.Add(UIUtils.CreateHealthBar(-50, "#FFD961FF"));

    healthBarAdded = true;
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
  - E04_StrideUI_BasicWindow
  - E04_StrideUI_DragAndDrop
  - E04_ImGui
media: stride-game-engine-example04-myra-ui-draggable-window.webp
enabled: true
created: 2023-09-15
---
*/