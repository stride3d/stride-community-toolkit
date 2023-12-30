using Example07_CubeClicker.Scripts;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Events;
using Stride.UI.Panels;

namespace Example07_CubeClicker;

public class GameUI
{
    private const string EntityName = "GameUI";
    private const string LoadButtonText = "Load Data";
    private const string SaveButtonText = "Save Data";
    private const string DeleteButtonText = "Delete Data";
    private const string IntroText = "Left-clicking on a cube creates a new one, while right-clicking will remove it.";
    private readonly Color _gridBackgroundColor = new(248, 177, 149, 100);
    private readonly SpriteFont _font;
    private readonly DataSaver<UiData> _dataSaver;
    private readonly CubeCollector _cubeCollector;
    private readonly List<(TextBlock Text, MouseButton Type)> _clickableTextBlocks = [];
    private readonly Grid _grid;
    private readonly TextBlock _message;

    public GameUI(SpriteFont font, DataSaver<UiData> dataSaver,CubeCollector collector)
    {
        _font = font;
        _grid = CreateGrid();
        _dataSaver = dataSaver;
        _cubeCollector = collector;
        _message = CreateMessageTextBlock();
    }

    public Entity Create()
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
        AddLoadButton();
        AddSaveButton();
        AddDeleteButton();

        return entity;
    }

    public void HandleClick(MouseButton type)
    {
        var clickable = _dataSaver.Data.Clickables.FirstOrDefault(x => x.Type == type);

        if (clickable is null) return;

        clickable.HandleClick();

        UpdateClickTextBlocks();
    }

    private void AddTextBlocks()
    {
        var row = 0;

        foreach (var item in _dataSaver.Data.Clickables)
        {
            var textBlock = CreateTextBlock();
            textBlock.SetGridColumn(0);
            textBlock.SetGridRow(row++);

            _grid?.Children.Add(textBlock);

            _clickableTextBlocks.Add((textBlock, item.Type));
        }

        UpdateClickTextBlocks();
    }

    private void UpdateClickTextBlocks()
    {
        foreach (var item in _dataSaver.Data.Clickables)
        {
            var textBlock = _clickableTextBlocks.FirstOrDefault(w => w.Type == item.Type).Text;

            if (textBlock is null) continue;

            textBlock.Text = item.ToString();
        }
    }

    private void AddLoadButton()
    {
        var button = CreateButton(LoadButtonText);
        button.SetGridColumn(1);
        button.SetGridRow(0);
        button.Click += LoadDataAsync;

        _grid?.Children.Add(button);
    }

    private void AddSaveButton()
    {
        var button = CreateButton(SaveButtonText);
        button.SetGridColumn(1);
        button.SetGridRow(1);
        button.Click += SaveDataAsync;

        _grid?.Children.Add(button);
    }

    private void AddDeleteButton()
    {
        var button = CreateButton(DeleteButtonText);
        button.SetGridColumn(1);
        button.SetGridRow(1);
        button.Click += DeleteData;
        button.Width = 104;

        // We can use margin or use StackPanel to place the buttons next to each other
        button.Margin = new Thickness(223, 3, 0, 3);

        _grid?.Children.Add(button);
    }

    private async void LoadDataAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (await _dataSaver.TryLoadAsync() && await _cubeCollector.LoadCubeDataAsync())
            {
                _message.Text = "Data loaded. Start clicking.";
            }
            else
            {
                _message.Text = "No data found. Save data.";
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during load operation: {ex.Message}");
        }

        UpdateClickTextBlocks();
    }

    private async void SaveDataAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _dataSaver.SaveAsync();
            await _cubeCollector.SaveCubeDataAsync();
            _message.Text = "Data saved. Keep clicking.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during save operation: {ex.Message}");
        }
    }

    private void DeleteData(object? sender, RoutedEventArgs e)
    {
        try
        {
            _dataSaver.Delete();

            _message.Text = "Data deleted.";

            _dataSaver.Data = UiData.Default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during delete operation: {ex.Message}");
        }

        UpdateClickTextBlocks();
    }

    private Grid CreateGrid() => new()
    {
        VerticalAlignment = VerticalAlignment.Top,
        BackgroundColor = _gridBackgroundColor,
        RowDefinitions = {
                new StripDefinition() { Type = StripType.Auto },
                new StripDefinition() { Type = StripType.Auto },
                new StripDefinition() { Type = StripType.Auto }
            },
        ColumnDefinitions =
            {
                new StripDefinition(StripType.Star, 1),
                new StripDefinition(StripType.Star, 1)
            }
    };

    private TextBlock CreateMessageTextBlock()
    {
        var textBlock = CreateTextBlock(IntroText, 16);
        textBlock.SetGridColumn(0);
        textBlock.SetGridRow(2);
        textBlock.SetGridColumnSpan(2);
        textBlock.HorizontalAlignment = HorizontalAlignment.Center;

        _grid?.Children.Add(textBlock);

        return textBlock;
    }

    private TextBlock CreateTextBlock(string? text = null, float textSize = 20, TextAlignment textAlignment = TextAlignment.Left)
        => new()
        {
            Text = text,
            TextColor = Color.White,
            Margin = new Thickness(3, 0, 3, 0),
            TextSize = textSize,
            Font = _font,
            TextAlignment = textAlignment,
            VerticalAlignment = VerticalAlignment.Center
        };

    private Button CreateButton(string title, string? name = null)
    {
        var text = CreateTextBlock(title, 14, TextAlignment.Center);

        return new()
        {
            Content = text,
            Padding = new Thickness(5, 5, 5, 5),
            BackgroundColor = new Color(0, 0, 0, 200),
            Width = 104,
            Margin = new Thickness(0, 3, 0, 3),
            Name = name
        };
    }
}