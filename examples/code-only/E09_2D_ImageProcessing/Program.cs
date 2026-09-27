using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Extensions;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Utilities;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Images;
using Stride.Rendering.Sprites;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

using var game = new Game();

// A tall window for the 9 by 9 grid of 1000-pixel tiles. This runs before the device exists;
// game.Window.SetSize(...) inside Start would resize an already-created window, which also works
// but shows a flash at the default size first. The rendering settings a Game Studio project
// carries (DefaultBackBufferWidth and Height through game.UseGameSettings) are not enough on their
// own: the engine clamps them to the window it has already created, so they can shrink a window
// but never grow it past 1280 by 720. SetWindowSize sizes the window and the back buffer together.
game.SetWindowSize(1000, 1080);

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3D();

    // A colour picture: sRGB and premultiplied, as the content pipeline would have imported it. Texture.Load
    // with its defaults reads it as linear, and every mid-tone comes out pale
    var texture = new TextureLoader(game.GraphicsDevice).Color("input.png");

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
  - E02_3D_Material
  - E09_3D_SceneRenderer
enabled: true
created: 2023-12-21
---
*/