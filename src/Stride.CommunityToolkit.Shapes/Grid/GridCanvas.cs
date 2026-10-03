using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// What a <see cref="ReferenceGrid"/> draws with: its two batches, a pool of text labels, and the
/// strokes every grid is made of. One frame is one call to <see cref="Draw"/>.
/// </summary>
internal sealed class GridCanvas
{
    /// <summary>How far numbers stay from the window's edge, in pixels.</summary>
    internal const float EdgeMargin = 6f;

    private readonly List<EntityTextComponent> _labels = [];
    private readonly Entity _parent;
    private int _labelsUsed;

    /// <summary>Creates a canvas for a grid to draw on.</summary>
    /// <param name="settings">The grid whose properties say what to draw.</param>
    /// <param name="world">The batch for world lines. Depth tested, so the scene hides them.</param>
    /// <param name="overlay">The batch for screen lines, drawn over everything.</param>
    /// <param name="parent">The entity the label entities are added under.</param>
    internal GridCanvas(ReferenceGrid settings, ShapeBatch world, ShapeBatch overlay, Entity parent)
    {
        Settings = settings;
        World = world;
        Overlay = overlay;
        _parent = parent;
    }

    internal ReferenceGrid Settings { get; }

    internal ShapeBatch World { get; }

    internal ShapeBatch Overlay { get; }

    /// <summary>Submits this frame's lines and places its labels.</summary>
    /// <param name="view">The camera's view of this frame.</param>
    /// <param name="mouse">The mouse position, normalised (0,0) top left to (1,1) bottom right.</param>
    internal void Draw(in GridView view, Vector2 mouse)
    {
        _labelsUsed = 0;

        if (Settings.Visible)
        {
            Overlay.Screen = true;

            if (Settings.Space == GridSpace.Screen) ScreenGrid.Draw(this);
            else if (IsGround(view)) GroundGrid.Draw(this, view);
            else FlatGrid.Draw(this, view);

            if (Settings.ShowCursor) GridCursor.Draw(this, view, mouse);

            Overlay.Screen = false;
        }

        // Labels are entities and stay from frame to frame, so the ones not used this time are hidden
        for (var i = _labelsUsed; i < _labels.Count; i++) _labels[i].IsVisible = false;
    }

    /// <summary>Whether the world grid lies on the ground plane XZ, and not in the plane XY.</summary>
    internal bool IsGround(in GridView view)
        => Settings.Plane == GridPlane.XZ || (Settings.Plane == GridPlane.Auto && !view.Orthographic);

    /// <summary>A minor or a major grid line, one pixel wide.</summary>
    internal void Line(ShapeBatch batch, Vector3 start, Vector3 end, bool major)
    {
        batch.Opacity = major ? Settings.MajorOpacity : Settings.MinorOpacity;
        batch.DrawPixelLine(start, end, 1f, Settings.LineColor);
        batch.Opacity = 1f;
    }

    /// <summary>An axis, two pixels wide, in its own colour.</summary>
    internal static void Axis(ShapeBatch batch, Vector3 start, Vector3 end, Color colour)
    {
        batch.Opacity = 0.9f;
        batch.DrawPixelLine(start, end, 2f, colour);
        batch.Opacity = 1f;
    }

    /// <summary>A number at a world point, when that point is in front of the camera and inside the window.</summary>
    internal void WorldLabel(in GridView view, string text, Vector3 world, Color colour)
    {
        if (!view.Project(world, out var at) || at.X < 0f || at.Y < 0f || at.X > view.Size.X || at.Y > view.Size.Y) return;

        Label(text, at + new Vector2(4f, 2f), TextAnchor.TopLeft, colour);
    }

    /// <summary>Takes the next label from the pool, making one when the pool is used up.</summary>
    internal EntityTextComponent Label(string text, Vector2 position, TextAnchor anchor, Color colour)
    {
        if (_labelsUsed == _labels.Count)
        {
            var created = new EntityTextComponent
            {
                Text = "",
                PositionMode = TextPositionMode.Screen,
                EnableShadow = true,
            };

            _parent.AddChild(new Entity("Grid label") { created });
            _labels.Add(created);
        }

        var label = _labels[_labelsUsed++];

        label.Text = text;
        label.ScreenPosition = position;
        label.Anchor = anchor;
        label.TextColor = colour;
        label.FontSize = Settings.FontSize;
        label.EnableBackground = false;
        label.IsVisible = true;

        return label;
    }

    /// <summary>Takes the label entities out from under their parent.</summary>
    internal void RemoveLabels()
    {
        foreach (var label in _labels) _parent.RemoveChild(label.Entity);

        _labels.Clear();
        _labelsUsed = 0;
    }
}