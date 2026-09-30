using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// A gradient across a shape's fill. <see cref="ShapeBatch.Gradient"/> holds one; each draw call
/// captures its values as it is made.
/// </summary>
/// <remarks>
/// <para>
/// With a <see cref="Color"/> set, the fill starts as the batch's fill colour at one extreme of the
/// shape and reaches this colour at the opposite extreme, along <see cref="Direction"/>. It spans
/// the shape's own extent, so a bar and a disc each run edge to edge whatever their size; a sector
/// spans its wedge, not the disc it was cut from. Alpha counts: a fill that runs to its own colour
/// at alpha 0 fades out.
/// </para>
/// <para>
/// The gradient is the fill's alone; the border and glow keep their colours.
/// <see cref="ShapeFill.Alpha"/> scales both ends.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// shapes.Fill.Color = new Color(90, 190, 255, 120);
/// shapes.Gradient.Color = new Color(90, 190, 255, 0);   // fades to nothing
/// shapes.Gradient.Direction = Vector2.UnitX;            // left to right
/// shapes.DrawRectangle(center, Vector3.UnitX, Vector3.UnitY, size, outline);
/// shapes.Gradient.Clear();
/// </code>
/// </example>
public sealed class FillGradient
{
    /// <summary>The colour the fill runs to, or <c>null</c> (the default) for a flat fill.</summary>
    public Color? Color { get; set; }

    /// <summary>
    /// The direction the gradient runs in, in the shape's local axes - for a 2D shape, world X and
    /// Y; for a rectangle on a plane, that plane's axes. Defaults to +Y, bottom to top. The length
    /// does not matter.
    /// </summary>
    public Vector2 Direction { get; set; } = Vector2.UnitY;

    /// <summary>Sets the gradient at once.</summary>
    /// <param name="color">The colour the fill runs to.</param>
    /// <param name="direction">The direction it runs in; <c>null</c> keeps the current one.</param>
    public void Set(Color color, Vector2? direction = null)
    {
        Color = color;

        if (direction is { } d) Direction = d;
    }

    /// <summary>Back to a flat fill. The direction is kept.</summary>
    public void Clear() => Color = null;
}