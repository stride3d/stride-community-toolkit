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
    public Grid Grid { get; set; }
    public Entity CreateUIEntity(SpriteFont font)
    {
        return new Entity
        {
            new UIComponent
            {
                Page = new UIPage { RootElement = Grid = CreateGrid() },
                RenderGroup = RenderGroup.Group31
            }
        };
    }

    Grid CreateGrid()
    {
        
        var canvas = new Grid {
            Width = 700, Height = 100, BackgroundColor = new Color(248, 177, 149, 100) };
        canvas.RowDefinitions.Add(new StripDefinition() { Type = StripType.Auto });
        canvas.RowDefinitions.Add(new StripDefinition() { Type = StripType.Auto });
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
        Grid.Children.Add(textBlock);
        return textBlock;
    }
}
