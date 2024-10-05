using Example10_StrideUI_DragAndDrop.UI;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.UI;
using Stride.UI.Controls;

namespace Example10_StrideUI_DragAndDrop;

public class UIManager
{
    public Entity Entity { get; }

    private readonly DragAndDropContainer _rootElement;
    private readonly SpriteFont _font;
    private readonly Action? _onGenerateCubes;

    private int _windowId = 1;
    private TextBlock? _textBlock;
    private Vector3 _defaultWindowPosition = new(0.02f, 0.05f, 0);

    public UIManager(SpriteFont? font, Action? onGenerateCubes)
    {
        _font = font ?? throw new ArgumentNullException(nameof(font));

        _onGenerateCubes = onGenerateCubes;

        _rootElement = new DragAndDropContainer();

        Entity = CreateUIEntity(_rootElement);

        CreateMainWindow();
    }

    public void UpdateTextBlock(string text)
    {
        if (_textBlock is null) return;

        _textBlock.Text = text;
    }

    private static Entity CreateUIEntity(UIElement element) => [
        new UIComponent
        {
            Page = new UIPage { RootElement = element },
            RenderGroup = RenderGroup.Group31
        }];

    private void CreateMainWindow()
    {
        var canvas = CreateWindow("Main Window", _defaultWindowPosition);

        _textBlock = GetTextBlock("");
        _textBlock.Margin = new Thickness(10, 140, 0, 0);

        canvas.Children.Add(_textBlock);

        _rootElement.Children.Add(canvas);
    }

    private DragAndDropCanvas CreateWindow(string title, Vector3? position = null)
    {
        var canvas = new DragAndDropCanvas(title, _font!, position);

        // Set the Z index to ensure correct rendering order
        canvas.SetPanelZIndex(_rootElement.GetNewZIndex());

        // Add a button for creating a new window
        var newWindowButton = GetButton("New Window", new Vector2(10, 50));
        newWindowButton.PreviewTouchUp += NewWindowButton_PreviewTouchUp;
        canvas.Children.Add(newWindowButton);

        // Add a button for generating cubes
        var generateItemsButton = GetButton("Generate Items", new Vector2(10, 90));
        generateItemsButton.PreviewTouchUp += GenerateItemsButton_PreviewTouchUp;
        canvas.Children.Add(generateItemsButton);

        return canvas;
    }

    private void GenerateItemsButton_PreviewTouchUp(object? sender, TouchEventArgs e)
    {
        _onGenerateCubes?.Invoke();
    }

    private void NewWindowButton_PreviewTouchUp(object? sender, TouchEventArgs e)
        => _rootElement.Children.Add(CreateWindow($"Window {_windowId++}", _defaultWindowPosition));

    private Button GetButton(string title, Vector2 position) => new()
    {
        Content = GetTextBlock(title),
        BackgroundColor = new Color(100, 100, 100, 200),
        Margin = new Thickness(position.X, position.Y, 0, 0),
    };

    private TextBlock GetTextBlock(string title) => new()
    {
        Text = title,
        TextColor = Color.White,
        TextSize = 16,
        Font = _font,
        TextAlignment = TextAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };
}