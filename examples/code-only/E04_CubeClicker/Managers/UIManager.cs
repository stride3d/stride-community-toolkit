using E04_CubeClicker.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Events;
using Stride.UI.Panels;

namespace E04_CubeClicker.Managers;

public class UIManager
{
    private const string EntityName = "GameUI";
    private const string IntroText = "Left-clicking on a cube creates a new one, while right-clicking will remove it.";

    private readonly SpriteFont _font;
    private readonly Grid _grid;
    private readonly TextBlock _message;
    private readonly List<(TextBlock Text, MouseButton Type)> _clickableTextBlocks = [];
    private readonly List<IClickable> _clickables;

    public required EventHandler<RoutedEventArgs> LoadDataHandler { get; init; }
    public required EventHandler<RoutedEventArgs> SaveDataHandler { get; init; }
    public required EventHandler<RoutedEventArgs> DeleteDataHandler { get; init; }

    public UIManager(SpriteFont font, List<IClickable> clickables)
    {
        _font = font;
        _grid = CreateGrid();
        _message = CreateMessageTextBlock();
        _clickables = clickables;
    }

    public Entity CreateUI()
    {
        var entity = new Entity(EntityName)
        {
            new UIComponent
            {
                Page = new UIPage { RootElement = _grid },
                RenderGroup = RenderGroup.Group31
            }
        };

        AddTextBlocks();
        AddButton("Load Data", 0, LoadDataHandler);
        AddButton("Save Data", 1, SaveDataHandler);

        // Beside the save button: a margin does it, a StackPanel would too
        AddButton("Delete Data", 1, DeleteDataHandler, new Thickness(223, 3, 0, 3));

        return entity;
    }

    private void AddTextBlocks()
    {
        var row = 0;

        foreach (var item in _clickables)
        {
            var textBlock = CreateTextBlock();

            textBlock.SetGridColumn(0);
            textBlock.SetGridRow(row++);

            _grid.Children.Add(textBlock);
            _clickableTextBlocks.Add((textBlock, item.Type));
        }

        UpdateClickTextBlocks(_clickables);
    }

    public void UpdateClickTextBlocks(List<IClickable> clickables)
    {
        foreach (var item in clickables)
        {
            var textBlock = _clickableTextBlocks.FirstOrDefault(w => w.Type == item.Type).Text;

            if (textBlock is null) continue;

            textBlock.Text = item.ToString();
        }
    }

    public void UpdateMessage(string text) => _message.Text = text;

    private void AddButton(string title, int row, EventHandler<RoutedEventArgs> handler, Thickness? margin = null)
    {
        var button = CreateButton(title);

        button.SetGridColumn(1);
        button.SetGridRow(row);
        button.Click += handler;

        if (margin is { } thickness) button.Margin = thickness;

        _grid.Children.Add(button);
    }

    private static Grid CreateGrid() => new()
    {
        VerticalAlignment = VerticalAlignment.Top,
        BackgroundColor = new Color(248, 177, 149, 100),
        RowDefinitions = { new(StripType.Auto), new(StripType.Auto), new(StripType.Auto) },
        ColumnDefinitions = { new(StripType.Star, 1), new(StripType.Star, 1) }
    };

    private TextBlock CreateMessageTextBlock()
    {
        var textBlock = CreateTextBlock(IntroText, 16);

        textBlock.SetGridColumn(0);
        textBlock.SetGridRow(2);
        textBlock.SetGridColumnSpan(2);
        textBlock.HorizontalAlignment = HorizontalAlignment.Center;

        _grid.Children.Add(textBlock);

        return textBlock;
    }

    private TextBlock CreateTextBlock(string? text = null, float textSize = 20, TextAlignment textAlignment = TextAlignment.Left) => new()
    {
        Text = text,
        TextColor = Color.White,
        Margin = new Thickness(3, 0, 3, 0),
        TextSize = textSize,
        Font = _font,
        TextAlignment = textAlignment,
        VerticalAlignment = VerticalAlignment.Center
    };

    private Button CreateButton(string title) => new()
    {
        Content = CreateTextBlock(title, 14, TextAlignment.Center),
        Padding = new Thickness(5, 5, 5, 5),
        BackgroundColor = new Color(0, 0, 0, 200),
        Width = 104,
        Margin = new Thickness(0, 3, 0, 3),
    };
}