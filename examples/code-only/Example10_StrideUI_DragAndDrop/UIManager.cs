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
    /// <summary>
    /// The root UI entity containing all UI elements.
    /// </summary>
    public Entity Entity { get; }

    private readonly DragAndDropContainer _rootElement;
    private readonly SpriteFont _font;
    private readonly Action? _onGenerateCubes;

    private int _windowId = 1;
    private TextBlock? _textBlock;
    private Vector3 _defaultWindowPosition = new(0.02f, 0.05f, 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="UIManager"/> class.
    /// </summary>
    /// <param name="font">The font used in the UI.</param>
    /// <param name="onGenerateCubes">The action to be triggered when generating cubes.</param>
    /// <exception cref="ArgumentNullException">Thrown if font or onGenerateCubes is null.</exception>
    public UIManager(SpriteFont? font, Action? onGenerateCubes)
    {
        _font = font ?? throw new ArgumentNullException(nameof(font));

        _onGenerateCubes = onGenerateCubes ?? throw new ArgumentNullException(nameof(onGenerateCubes));

        _rootElement = new DragAndDropContainer();

        Entity = CreateUIEntity(_rootElement);

        CreateMainWindow();
    }

    /// <summary>
    /// Updates the text displayed in the text block.
    /// </summary>
    /// <param name="text">The text to display.</param>
    public void UpdateTextBlock(string text)
    {
        if (_textBlock is null) return;

        _textBlock.Text = text;
    }

    /// <summary>
    /// Creates the root UI entity.
    /// </summary>
    /// <param name="element">The root UI element.</param>
    /// <returns>A new entity containing the UI element.</returns>
    private static Entity CreateUIEntity(UIElement element) => [
        new UIComponent
        {
            Page = new UIPage { RootElement = element },
            RenderGroup = RenderGroup.Group31
        }];

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
        var canvas = new DragAndDropCanvas(title, _font!, position);

        // Set the Z index to ensure correct rendering order
        canvas.SetPanelZIndex(_rootElement.GetNewZIndex());

        // Add a button for creating a new window
        var newWindowButton = CreateButton("New Window", new Vector2(10, 50), NewWindowButton_PreviewTouchUp);
        canvas.Children.Add(newWindowButton);

        // Add a button for generating cubes
        var generateItemsButton = CreateButton("Generate Items", new Vector2(10, 90), GenerateItemsButton_PreviewTouchUp);
        canvas.Children.Add(generateItemsButton);

        return canvas;
    }

    private void GenerateItemsButton_PreviewTouchUp(object? sender, TouchEventArgs e)
    {
        _onGenerateCubes?.Invoke();
    }

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
        var button = new Button()
        {
            Content = CreateTextBlock(title),
            BackgroundColor = new Color(100, 100, 100, 200),
            Margin = new Thickness(position.X, position.Y, 0, 0),
        };

        button.PreviewTouchUp += onTouchUp;

        return button;
    }

    /// <summary>
    /// Creates a text block with specified text.
    /// </summary>
    /// <param name="text">The text to display in the block.</param>
    /// <returns>A new text block.</returns>
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