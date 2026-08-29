using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(start: (Scene rootScene) =>
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "DPI-Aware Window";

    game.SetupBase3DScene();
    game.AddSkybox();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = rootScene;
});

/*
---example-metadata
slug: dpi-aware
title:
  en: DPI-Aware Window
level: Beginner
category: Debug
complexity: 1
order: 110
description:
  en: |-
    The capsule scene again, with one difference that is not in the C# at all: an app.manifest declaring
    the process per-monitor DPI aware, referenced from the csproj. Without it Windows scales the window
    itself on a high-DPI display and the result is a blurred, upscaled image. The example exists because
    the fix is invisible in the source, so it is easy to conclude that Stride renders badly when the
    real cause is a missing manifest.
concepts:
  - Why a high-DPI display renders a blurred window without a manifest
  - "Declaring per-monitor DPI awareness in app.manifest"
  - "Wiring the manifest in with <ApplicationManifest>"
  - Referencing Stride.CommunityToolkit.Windows for Windows-only concerns
  - "Using helpers: SetupBase3DScene, AddSkybox, Create3DPrimitive"
tags:
  - 3D
  - Windows
  - DPI
  - Window
  - Manifest
  - Troubleshooting
related:
  - Example01_Basic3DScene
enabled: true
created: 2025-10-06
---
*/