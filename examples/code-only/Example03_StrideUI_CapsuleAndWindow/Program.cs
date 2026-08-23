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

SpriteFont? _font;

using var game = new Game();

game.Run(start: Start);

void Start(Scene scene)
{
    game.SetupBase3DScene();
    game.AddSkybox();

    AddCapsule(scene);

    LoadFont();

    AddWindow(scene);
}

void AddCapsule(Scene scene)
{
    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = scene;
}

void LoadFont()
{
    _font = game.Content.Load<SpriteFont>("/Stride.Engine/StrideDefaultFont");
}

void AddWindow(Scene scene)
{
    var uiEntity = CreateUIEntity();

    uiEntity.Scene = scene;
}

Entity CreateUIEntity()
{
    return new Entity
    {
        new UIComponent
        {
            Page = new UIPage { RootElement = CreateCanvas() },
            RenderGroup = RenderGroup.Group31
        }
    };
}

Canvas CreateCanvas()
{
    var canvas = new Canvas { Width = 300, Height = 100, BackgroundColor = new Color(248, 177, 149, 100) };

    canvas.Children.Add(CreateTextBlock(_font));

    return canvas;
}

TextBlock CreateTextBlock(SpriteFont? _font)
{
    if (_font is null)
    {
        Console.WriteLine("Font is null");
    }

    return new TextBlock
    {
        Text = "Hello, World",
        TextColor = Color.White,
        TextSize = 20,
        Margin = new Thickness(3, 3, 3, 0),
        Font = _font
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
  - "Using helpers: SetupBase3DScene, AddSkybox, Create3DPrimitive"
tags:
  - 3D
  - UI
  - Stride UI
  - Canvas
  - TextBlock
  - Font
related:
  - Example10_StrideUI_DragAndDrop
  - Example07_CubeClicker
  - Example11_ImGui
media: stride-game-engine-example03-stride-ui-basic-window.webp
tocName: Stride UI - Capsule with rigid body and Window
enabled: true
created: 2023-09-16
---
*/