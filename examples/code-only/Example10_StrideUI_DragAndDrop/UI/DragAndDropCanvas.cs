using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Example10_StrideUI_DragAndDrop.UI;

/// <summary>
/// Represents a draggable and resizable window with a close button.
/// </summary>
public class DragAndDropCanvas : Canvas
{
    private readonly SpriteFont _font;
    private int _canvasWidth = 300;
    private int _canvasHeight = 200;
    private int _closeButtonSize = 25;

    /// <summary>
    /// Initializes a new instance of the <see cref="DragAndDropCanvas"/> class.
    /// </summary>
    /// <param name="title">The title to display at the top of the window.</param>
    /// <param name="font">The font to use for the title and close button.</param>
    /// <param name="position">Optional starting position for the canvas.</param>
    public DragAndDropCanvas(string title, SpriteFont font, Vector3? position = null)
    {
        BackgroundColor = new Color(0, 0, 0, 200);
        Width = _canvasWidth;
        Height = _canvasHeight;
        CanBeHitByUser = true;

        this.SetCanvasRelativePosition(position ?? Vector3.Zero);

        _font = font;

        AddTitle(title);
        AddDividerLine();
        AddCloseButton();

        PreviewTouchDown += OnTouchDown;
    }

    /// <summary>
    /// Adds a title to the top of the canvas.
    /// </summary>
    private void AddTitle(string title) => Children.Add(new TextBlock
    {
        Text = title,
        TextColor = Color.White,
        TextSize = 20,
        Font = _font,
        Margin = new Thickness(3, 3, 3, 0),
    });

    /// <summary>
    /// Adds a dividing line below the title.
    /// </summary>
    private void AddDividerLine() => Children.Add(new Border
    {
        BorderColor = Color.White,
        BorderThickness = new Thickness(0, 0, 0, 2),
        Width = _canvasWidth,
        Height = 27
    });

    /// <summary>
    /// Adds a close button to the canvas.
    /// </summary>
    private void AddCloseButton()
    {
        var button = new Button
        {
            Content = GetCloseButtonTitle(),
            BackgroundColor = new Color(0, 0, 0, 200),
            Width = _closeButtonSize,
            Height = _closeButtonSize,
            Margin = new Thickness(_canvasWidth - _closeButtonSize, 0, 0, 0),
        };

        button.PreviewTouchUp += OnCloseButtonTouchUp;

        Children.Add(button);
    }

    /// <summary>
    /// Creates the content for the close button (an "x" character).
    /// </summary>
    private UIElement GetCloseButtonTitle() => new TextBlock
    {
        Text = "x",
        Width = _closeButtonSize,
        Height = _closeButtonSize,
        TextColor = Color.White,
        TextSize = 20,
        Font = _font,
        TextAlignment = TextAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

    /// <summary>
    /// Handles the touch event when the close button is pressed.
    /// </summary>
    private void OnCloseButtonTouchUp(object? sender, TouchEventArgs e)
    {
        if (Parent is not Canvas parent) return;

        PreviewTouchDown -= OnTouchDown;

        parent.Children.Remove(this);
    }

    /// <summary>
    /// Handles the touch down event for drag-and-drop behaviour.
    /// </summary>
    private void OnTouchDown(object? sender, TouchEventArgs e)
    {
        if (sender is not UIElement dragElement) return;

        if (dragElement.Parent is not DragAndDropContainer dragAndDropContainer) return;

        dragElement.SetPanelZIndex(dragAndDropContainer.GetNewZIndex());

        dragAndDropContainer.SetDraggedElement(dragElement);

        dragAndDropContainer.SetOffset(e.ScreenPosition - (Vector2)dragElement.GetCanvasRelativePosition());
    }
}