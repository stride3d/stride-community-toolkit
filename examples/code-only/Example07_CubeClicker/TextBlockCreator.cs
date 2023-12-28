using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Example07_CubeClicker;

public class TextBlockCreator
{
    public Grid? Grid { get; set; }

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

    private static Grid CreateGrid()
    {
        var grid = new Grid
        {
            Height = 100,
            VerticalAlignment = VerticalAlignment.Top,
            BackgroundColor = new Color(248, 177, 149, 100)
        };
        grid.RowDefinitions.Add(new StripDefinition() { Type = StripType.Auto });
        grid.RowDefinitions.Add(new StripDefinition() { Type = StripType.Auto });

        return grid;
    }

    public TextBlock CreateTextBlock(SpriteFont? _font)
    {
        if (_font is null)
            Console.WriteLine("Font is null");

        var textBlock = new TextBlock
        {
            Text = "",
            TextColor = Color.White,
            TextSize = 20,
            Margin = new Thickness(3, 3, 3, 0),
            Font = _font
        };

        Grid?.Children.Add(textBlock);

        return textBlock;
    }
}