namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// A rounded edge between the caps and the walls of 3D text: a quarter-ellipse fillet, the same on the
/// front and the back.
/// </summary>
/// <remarks>
/// A bevel wider than a glyph's thinnest stem would fold the cap over itself; <see cref="TextMesh"/>
/// halves <see cref="Size"/> for that glyph until it fits, up to six times, before giving up on it. In
/// world units, like <see cref="TextMeshOptions.Depth"/>.
/// </remarks>
public sealed record TextBevel
{
    /// <summary>
    /// Creates a bevel.
    /// </summary>
    /// <param name="size">How far the bevel reaches in from the outline across the cap, in world units.</param>
    /// <param name="thickness">How far the bevel reaches along the depth, from the cap towards the middle, in world units; capped at half the depth.</param>
    /// <param name="segments">How many steps the rounding takes; 1 is a chamfer, 3 reads as round, 6 is smooth up close.</param>
    /// <exception cref="ArgumentOutOfRangeException">The size or the thickness is not positive, or there are no segments.</exception>
    public TextBevel(float size, float thickness, int segments = 3)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(thickness);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(segments);

        Size = size;
        Thickness = thickness;
        Segments = segments;
    }

    /// <summary>How far the bevel reaches in from the outline across the cap, in world units.</summary>
    public float Size { get; }

    /// <summary>How far the bevel reaches along the depth, from the cap towards the middle, in world units.</summary>
    public float Thickness { get; }

    /// <summary>How many steps the rounding takes.</summary>
    public int Segments { get; }
}