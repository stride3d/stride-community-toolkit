using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

// One small method per step, on purpose: the assembly order is the lesson. Load a font, build a
// TextBlock, put it on a Canvas, wrap the canvas in a UIComponent, and attach that to an entity.

SpriteFont? font = null;

using var game = new Game();

game.Run(new SceneSetup3D().UseBepu().UseSkybox(), start: Start);

void Start(Scene scene)
{
    AddCapsule(scene);
    LoadFont();
    AddWindow(scene);
}

void AddCapsule(Scene scene)
{
    var entity = game.CreatePrimitive(Primitive3D.Capsule());
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = scene;
}

// Text draws nothing without a font; this one ships with the engine, so the example needs no assets
void LoadFont() => font = game.Content.Load<SpriteFont>("/Stride.Engine/StrideDefaultFont");

void AddWindow(Scene scene) => CreateUIEntity().Scene = scene;

Entity CreateUIEntity() => new()
{
    new UIComponent
    {
        Page = new UIPage { RootElement = CreateCanvas() },
        RenderGroup = RenderGroup.Group31
    }
};

Canvas CreateCanvas()
{
    var canvas = new Canvas { Width = 300, Height = 100, BackgroundColor = new Color(248, 177, 149, 100) };

    canvas.Children.Add(CreateTextBlock(font));

    return canvas;
}

TextBlock CreateTextBlock(SpriteFont? font)
{
    if (font is null) Console.WriteLine("Font is null");

    return new TextBlock
    {
        Text = "Hello, World",
        TextColor = Color.White,
        TextSize = 20,
        Margin = new Thickness(3, 3, 3, 0),
        Font = font
    };
}
/*
---example-metadata
slug: stride-ui-capsule-with-rigid-body
title:
  en: Stride UI - Capsule and Window
level: Intermediate
category: UI
complexity: 3
order: 170
description:
  en: |-
    A capsule in a 3D scene with a "Hello, World" panel drawn over it using Stride's built-in UI. The
    interesting part is the assembly order, which the example splits into one small method per step:
    load a font, build a TextBlock, put it on a Canvas, wrap the canvas in a UIComponent, and attach
    that to an entity. A UI in Stride is an entity like any other, and it will not render without a font
    loaded first.
concepts:
  - Building a UI hierarchy from code with Canvas and TextBlock
  - Loading a SpriteFont before any text can be drawn
  - Hosting a UI on an entity through UIComponent
  - Keeping scene setup readable by splitting it per element
  - "Using helpers: SceneSetup3D, UseSkybox, CreatePrimitive"
tags:
  - 3D
  - UI
  - Stride UI
  - Canvas
  - TextBlock
  - Font
related:
  - E04_StrideUI_DragAndDrop
  - E04_CubeClicker
  - E04_ImGui
tocName: Stride UI - Capsule with rigid body and Window
enabled: true
created: 2023-09-16
---
*/