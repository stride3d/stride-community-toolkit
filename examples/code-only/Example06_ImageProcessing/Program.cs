using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Extensions;
using Stride.CommunityToolkit.Rendering.Utilities;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Images;
using Stride.Rendering.Sprites;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using System.Reflection;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.Window.SetSize(new Int2(1000, 1080));
    game.SetupBase3D();

    var directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
    var filePath = Path.Combine(directory, "input.png");
    using var input = File.Open(filePath, FileMode.Open);
    var texture = Texture.Load(game.GraphicsDevice, input);

    var grid = new UniformGrid
    {
        Width = 1000,
        Height = 1000,
        Columns = 9,
        Rows = 9,
        Margin = new Thickness(8, 8, 8, 8)
    };

    grid.Children.Add(CreateCard(texture));

    for (var a = 0; a < 9; a++)
    {
        var anchor = (Anchor)a;
        for (var s = 0; s < 4; s++)
        {
            var stretch = (Stretch)s;

            using (var canvas = game.CreateTextureCanvas(new Size2(1024, 1024)))
            {
                canvas.DrawTexture(texture, new Rectangle(0, 128, 256, 256), new Rectangle(128, 256, 768, 512), null, stretch, anchor, SamplingPattern.Expanded);
                var card = CreateCard(canvas.ToTexture());
                card.SetGridColumn(a);
                card.SetGridRow(s * 2 + 1);
                grid.Children.Add(card);
            }

            using (var canvas = game.CreateTextureCanvas(new Size2(1024, 1024)))
            {

                canvas.DrawTexture(texture, new Rectangle(0, 128, 256, 256), new Rectangle(256, 128, 512, 768), null, stretch, anchor);
                var card = CreateCard(canvas.ToTexture());
                card.SetGridColumn(a);
                card.SetGridRow(s * 2 + 2);
                grid.Children.Add(card);
            }
        }
    }

    var entity = new Entity { Scene = rootScene };
    entity.Add(new UIComponent { Page = new UIPage { RootElement = grid } });
}

static Border CreateCard(Texture texture)
{
    var card = new Border
    {
        BorderColor = new Color(25, 25, 25),
        BackgroundColor = new Color(120, 120, 120),
        BorderThickness = new Thickness(2, 2, 2, 2),
        Padding = new Thickness(8, 8, 8, 8),
        Margin = new Thickness(4, 4, 4, 4),
        Content = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Children =
            {
                new ImageElement
                {
                    Source = new SpriteFromTexture { Texture = texture }
                }
            }
        }
    };

    return card;
}
/*
---example-metadata
slug: image-processing
title:
  en: Image Processing (TextureCanvas)
level: Advanced
category: Rendering
complexity: 4
order: 40
description:
  en: |-
    Every combination of anchor and stretch that TextureCanvas can apply, drawn as a grid of thumbnails
    so the options can be compared rather than read about. Each cell blits a region of a source texture
    into a destination rectangle with one setting changed, and renders the result into a UI card. The
    canvases are disposed as they go - each one owns a GPU render target, and a grid of them left open
    would be an easy leak.
concepts:
  - Drawing into an offscreen target with CreateTextureCanvas
  - "Blitting a source rectangle into a destination rectangle"
  - "How Anchor and Stretch interact when the aspect ratios differ"
  - "Choosing a SamplingPattern for the resample"
  - Turning a canvas into a Texture for display
  - Disposing each canvas so its render target is released
  - Laying results out in a UniformGrid of UI cards
tags:
  - 2D
  - Rendering
  - Texture
  - TextureCanvas
  - Image Processing
  - UI
  - Disposal
related:
  - Example01_Material
  - Example09_Renderer
media: stride-game-engine-example-06-image-processing.webp
enabled: true
created: 2023-12-21
---
*/