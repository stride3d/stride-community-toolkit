using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.UI.Controls;
using Stride.UI.Panels;
using Stride.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;

namespace Example06_CubeClicker;
public class TextBlockCreator
{
    Canvas Canvas { get; set; }
    Entity _uiPage;
    public Entity CreateUIEntity(SpriteFont font)
    {
        if (_uiPage != null)
            _uiPage.Scene = null;
        return _uiPage = new Entity
        {
            new UIComponent
            {
                Page = new UIPage { RootElement = Canvas = CreateCanvas(font) },
                RenderGroup = RenderGroup.Group31
            }
        };
    }

    Canvas CreateCanvas(SpriteFont font)
    {
        var canvas = new Canvas { Width = 300, Height = 100, BackgroundColor = new Color(248, 177, 149, 100) };
        return canvas;
    }

    public TextBlock CreateTextBlock(SpriteFont? _font)
    {
        if (_font is null)
        {
            Console.WriteLine("Font is null");
        }
        var textBlock = new TextBlock
        {
            Text = "",
            TextColor = Color.White,
            TextSize = 20,
            Margin = new Thickness(3, 3, 3, 0),
            Font = _font
        };
        Canvas.Children.Add(textBlock);
        return textBlock;
    }
}