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
    private int _width = 300;
    private int _height = 200;
    private int _buttonSize = 25;

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

    private void AddLoadButton()
    {
        var button = new Button
        {
            Content = GetCloseButtonTitle(),
            BackgroundColor = new Color(0, 0, 0, 200),
            Width = _buttonSize,
            Height = _buttonSize,
            Margin = new Thickness(_width - _buttonSize, 0, 0, 0),
        };

        button.PreviewTouchUp += LoadButton_PreviewTouchUp;

        Grid?.Children.Add(button);
    }

    private void LoadButton_PreviewTouchUp(object? sender, TouchEventArgs e)
    {

    }

    private UIElement GetCloseButtonTitle() => new TextBlock
    {
        Text = "x",
        Width = _buttonSize,
        Height = _buttonSize,
        TextColor = Color.White,
        TextSize = 20,
        //Font = _font,
        TextAlignment = TextAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };
}