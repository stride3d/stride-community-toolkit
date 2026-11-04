using E04_ImGui;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ImGui;
using Stride.CommunityToolkit.ImGui.DebugTools;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

Entity? cube = null;
ExampleWindow? window = null;

using var game = new Game();

game.Run(new SceneSetup3D { Profiler = true }.UseBepu().UseSkybox(), start: Start, update: Update);

void Start(Scene scene)
{
    game.AddEntityInfoRenderer(new()
    {
        EnableBackground = true
    });

    // Each of these registers itself with the game on construction, so the instance is deliberately
    // not kept: the game owns them from here on.
    _ = new ImGuiSystem(game.Services);
    _ = new HierarchyView(game.Services);
    _ = new PerfMonitor(game.Services);
    Inspector.FindFreeInspector(game.Services).Target = game.SceneSystem.SceneInstance;

    // A capped frame rate makes the profiler's numbers easier to read
    game.SetMaxFPS(60);

    // A cube of the example's own, steered from the window below
    cube = game.CreatePrimitive(Primitive3D.Cube(), new PrimitiveEntityOptions { Body = BodyMode.None, Material = game.CreateMaterial(new Color(255, 160, 60)) });
    cube.Transform.Position = new Vector3(0, 1, 0);
    cube.Scene = scene;

    // A window of this example's own: a menu, a slider and live stats, described again every frame from the values as they are
    window = new ExampleWindow(game.Services, game, cube);
}

void Update(Scene scene, GameTime time)
{
    if (cube is null || window is null) return;

    cube.Transform.Rotation *= Quaternion.RotationY(window.SpinSpeed * MathUtil.TwoPi * (float)time.Elapsed.TotalSeconds);
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
    the values happen to be. The toolkit owns the setup, initialising the integration, feeding it input
    and drawing it, so the example is the debug tools plus one window of its own: a menu, a slider that
    spins a cube, and live stats.
concepts:
  - Initialising the ImGui integration
  - Drawing windows, menus and controls every frame
  - Why immediate mode removes the need to sync UI state
  - Showing live stats in a debug panel
tags:
  - 3D
  - UI
  - ImGui
  - Immediate Mode
  - Debug
  - Tools
related:
  - E04_ImGuiNet
  - E04_StrideUI_BasicWindow
enabled: true
created: 2024-10-26
---
*/