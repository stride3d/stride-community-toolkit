namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// One filled piece of a <see cref="GlyphShape"/>: an outside polygon and the holes in it.
/// </summary>
public sealed class GlyphPolygon
{
    private readonly Vector2[] _outer;
    private readonly Vector2[][] _holes;

    /// <summary>
    /// Creates a polygon with holes.
    /// </summary>
    /// <param name="outer">The outside, counter-clockwise, without the first corner repeated.</param>
    /// <param name="holes">The holes, clockwise, each without the first corner repeated.</param>
    /// <exception cref="ArgumentNullException">The outer polygon or the holes are null.</exception>
    public GlyphPolygon(Vector2[] outer, IEnumerable<Vector2[]> holes)
    {
        ArgumentNullException.ThrowIfNull(outer);
        ArgumentNullException.ThrowIfNull(holes);

        _outer = outer;
        _holes = [.. holes];
    }

    /// <summary>The outside, counter-clockwise.</summary>
    public IReadOnlyList<Vector2> Outer => _outer;

    /// <summary>The holes, clockwise.</summary>
    public IReadOnlyList<IReadOnlyList<Vector2>> Holes => _holes;
}