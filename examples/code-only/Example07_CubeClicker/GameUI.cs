using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Events;
using Stride.UI.Panels;

namespace Example07_CubeClicker;

public class GameUI
{
    public const string EntityName = "GameUI";
    public const string LoadButtonName = "LoadButton";

    private readonly SpriteFont _font;
    private readonly DataSaver<UiData> _dataSaver;

    public Grid? Grid { get; set; }
    private int _width = 300;
    private int _height = 200;
    private int _buttonSize = 25;

    public GameUI(SpriteFont font, DataSaver<UiData> dataSaver)
    {
        _font = font;
        _dataSaver = dataSaver;
    }

    public Entity Create()
    {
        var entity = new Entity(EntityName)
        {
            new UIComponent
            {
                Page = new UIPage { RootElement = Grid = CreateGrid() },
                RenderGroup = RenderGroup.Group31
            }
        };

        AddLoadButton();
        AddSaveButton();

        return entity;
    }

    private static Grid CreateGrid()
    {
        var grid = new Grid
        {
            //Height = 100,
            VerticalAlignment = VerticalAlignment.Center,
            BackgroundColor = new Color(248, 177, 149, 100),
            Margin = new Thickness(5, 5, 5, 5)
        };

        grid.RowDefinitions.Add(new StripDefinition() { Type = StripType.Auto });
        grid.RowDefinitions.Add(new StripDefinition() { Type = StripType.Auto });
        grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 1));
        grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 1));

        return grid;
    }

    private void AddLoadButton()
    {
        var button = CreateButton("Load Data", LoadButtonName);
        button.SetGridColumn(1);
        button.SetGridRow(0);
        button.Click += SaveClickButtonAsync;

        Grid?.Children.Add(button);
    }

    private void AddSaveButton()
    {
        var button = CreateButton("Save Data");
        button.SetGridColumn(1);
        button.SetGridRow(1);
        button.Click += SaveClickButtonAsync;

        Grid?.Children.Add(button);
    }

    private Button CreateButton(string title, string? name = null) => new Button
    {
        Content = GetButtonTitle(title),
        BackgroundColor = new Color(0, 0, 0, 200),
        Width = 100,
        Margin = new Thickness(3, 0, 3, 0),
        Name = name
    };

    private async void SaveClickButtonAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _dataSaver.SaveAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during save operation: {ex.Message}");
        }
    }

    private UIElement GetButtonTitle(string text) => new TextBlock
    {
        Text = text,
        //Width = _buttonSize,
        //Height = _buttonSize,
        TextColor = Color.White,
        TextSize = 20,
        Font = _font,
        TextAlignment = TextAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };
}