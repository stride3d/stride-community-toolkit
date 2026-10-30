using E04_StrideUI_DragAndDrop.UI;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.UI;
using Stride.UI.Controls;

namespace E04_StrideUI_DragAndDrop;

public class UIManager
{
    /// <summary>
    /// The root UI entity containing all UI elements.
    /// </summary>
    public Entity Entity { get; }

    private readonly DragAndDropContainer _rootElement = new();
    private readonly SpriteFont _font;
    private readonly Action _onGenerateCubes;
    private readonly Vector3 _defaultWindowPosition = new(0.02f, 0.05f, 0);

    private int _windowId = 1;
    private TextBlock? _textBlock;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIManager"/> class.
    /// </summary>
    /// <param name="font">The font used in the UI.</param>
    /// <param name="onGenerateCubes">The action the Generate Items button calls.</param>
    /// <exception cref="ArgumentNullException">Thrown if font or onGenerateCubes is null.</exception>
    public UIManager(SpriteFont? font, Action? onGenerateCubes)
    {
        _font = font ?? throw new ArgumentNullException(nameof(font));
        _onGenerateCubes = onGenerateCubes ?? throw new ArgumentNullException(nameof(onGenerateCubes));

        Entity = new Entity
        {
            new UIComponent
            {
                Page = new UIPage { RootElement = _rootElement },
                RenderGroup = RenderGroup.Group31
            }
        };

        CreateMainWindow();
    }

    /// <summary>
    /// Updates the text displayed in the text block.
    /// </summary>
    /// <param name="text">The text to display.</param>
    public void UpdateTextBlock(string text)
    {
        if (_textBlock is not null) _textBlock.Text = text;
    }

    private void CreateMainWindow()
    {
        var canvas = CreateWindow("Main Window", _defaultWindowPosition);

        _textBlock = CreateTextBlock("");
        _textBlock.Margin = new Thickness(10, 140, 0, 0);

        canvas.Children.Add(_textBlock);

        _rootElement.Children.Add(canvas);
    }

    private DragAndDropCanvas CreateWindow(string title, Vector3? position = null)
    {
        var canvas = new DragAndDropCanvas(title, _font, position);

        canvas.SetPanelZIndex(_rootElement.GetNewZIndex());

        var newWindowButton = CreateButton("New Window", new Vector2(10, 50), NewWindowButton_PreviewTouchUp);
        canvas.Children.Add(newWindowButton);

        var generateItemsButton = CreateButton("Generate Items", new Vector2(10, 90), GenerateItemsButton_PreviewTouchUp);
        canvas.Children.Add(generateItemsButton);

        return canvas;
    }

    private void GenerateItemsButton_PreviewTouchUp(object? sender, TouchEventArgs e) => _onGenerateCubes();

    private void NewWindowButton_PreviewTouchUp(object? sender, TouchEventArgs e)
        => _rootElement.Children.Add(CreateWindow($"Window {_windowId++}", _defaultWindowPosition));

    /// <summary>
    /// Creates a button with specified title and position.
    /// </summary>
    /// <param name="title">The button title.</param>
    /// <param name="position">The position of the button.</param>
    /// <param name="onTouchUp">The event handler for touch up event.</param>
    /// <returns>A new button UI element.</returns>
    private Button CreateButton(string title, Vector2 position, EventHandler<TouchEventArgs> onTouchUp)
    {
        var button = new Button
        {
            Content = CreateTextBlock(title),
            BackgroundColor = new Color(100, 100, 100, 200),
            Margin = new Thickness(position.X, position.Y, 0, 0),
        };

        button.PreviewTouchUp += onTouchUp;

        return button;
    }

    private TextBlock CreateTextBlock(string text) => new()
    {
        Text = text,
        TextColor = Color.White,
        TextSize = 16,
        Font = _font,
        TextAlignment = TextAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };
}