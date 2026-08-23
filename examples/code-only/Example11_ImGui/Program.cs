using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.ImGui;
using Stride.CommunityToolkit.Renderers;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

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

    new ImGuiSystem(game.Services, game.GraphicsDeviceManager);
    new HierarchyView(game.Services);
    new PerfMonitor(game.Services);
    Inspector.FindFreeInspector(game.Services).Target = game.SceneSystem.SceneInstance;

    // makes the profiling much easier to read.
    game.SetMaxFPS(60);
}
/*
---example-metadata
slug: imgui-ui
title:
  en: ImGui UI
level: Intermediate
category: UI
complexity: 3
order: 180
description:
  en: |-
    An ImGui overlay for in-game tools, debug panels and live tweaking. Immediate-mode UI suits this job
    because there is no widget tree to keep in sync - you describe the panel every frame from whatever
    the values happen to be. The example is short because the toolkit owns the setup: initialising the
    integration, feeding it input and drawing it are already handled.
concepts:
  - Initialising the ImGui integration
  - Drawing windows, menus and controls every frame
  - Why immediate mode removes the need to sync UI state
  - Toggling the overlay from a key press
  - Showing live stats in a debug panel
tags:
  - 3D
  - UI
  - ImGui
  - Immediate Mode
  - Debug
  - Tools
related:
  - Example11_ImGuiNet
  - Example03_StrideUI_CapsuleAndWindow
media: stride-game-engine-example-11-imgui-ui.webp
enabled: true
created: 2024-10-26
---
*/