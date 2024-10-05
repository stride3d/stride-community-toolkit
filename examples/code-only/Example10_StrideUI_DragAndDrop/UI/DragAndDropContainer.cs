using Stride.Core.Mathematics;
using Stride.UI;
using Stride.UI.Panels;

namespace Example10_StrideUI_DragAndDrop.UI;

public class DragAndDropContainer : Canvas
{
    private UIElement? _draggedElement;
    private int _lastZIndex = 1;
    private Vector2? _dragOffset;

    public DragAndDropContainer()
    {
        CanBeHitByUser = true;

        PreviewTouchMove += OnTouchMove;
        PreviewTouchUp += OnTouchUp;
    }

    /// <summary>
    /// Generates a new Z-index for the dragged element to ensure it stays on top.
    /// </summary>
    public int GetNewZIndex() => _lastZIndex++;

    /// <summary>
    /// Sets the offset position for the dragging action.
    /// </summary>
    /// <param name="vector">The offset vector to be set.</param>
    public void SetOffset(Vector2 vector) => _dragOffset = vector;

    /// <summary>
    /// Sets the current element to be dragged.
    /// </summary>
    /// <param name="element">The UI element to be dragged.</param>
    public void SetDraggedElement(UIElement element) => _draggedElement = element;

    /// <summary>
    /// Handles the touch move event to update the dragged element's position.
    /// </summary>
    private void OnTouchMove(object? sender, TouchEventArgs e)
        => _draggedElement?.SetCanvasRelativePosition((Vector3)(e.ScreenPosition - _dragOffset ?? Vector2.Zero));

    /// <summary>
    /// Handles the touch up event to stop the drag operation.
    /// </summary>
    private void OnTouchUp(object? sender, TouchEventArgs e)
        => _draggedElement = null;

    public void CleanUp()
    {
        PreviewTouchMove -= OnTouchMove;
        PreviewTouchUp -= OnTouchUp;
    }
}