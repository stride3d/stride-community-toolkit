using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// How a shape's interior is painted. <see cref="ShapeBatch.Fill"/> holds one; each draw call
/// captures its values as it is made.
/// </summary>
/// <remarks>
/// <para>
/// With no <see cref="Color"/>, the fill is the outline colour dimmed and made translucent by
/// <see cref="Alpha"/> - the Box2D testbed's formula, kept so the physics examples match it side by
/// side. With a colour, it is that colour at its own alpha times <see cref="Alpha"/>, which is what
/// a dark panel behind a bright border needs and what the testbed formula cannot produce.
/// </para>
/// <para>
/// A gradient across the fill is <see cref="ShapeBatch.Gradient"/>.
/// </para>
/// </remarks>
public sealed class ShapeFill
{
    /// <summary>
    /// The fill's own colour, or <c>null</c> (the default) to fill with the outline colour, which is
    /// what the Box2D testbed does. Set it when the two should differ - a chart marker filled in its
    /// series colour but outlined in a neutral one, a bar with a darker edge, a light cursor ring
    /// with a dark halo.
    /// </summary>
    /// <remarks>
    /// The colour is used as given, including its own alpha, with <see cref="Alpha"/> scaling only
    /// its opacity. That differs from the default path, where <see cref="Alpha"/> also darkens the
    /// outline colour the way the testbed does - which would turn a colour you chose deliberately
    /// into a muddy version of itself.
    /// </remarks>
    public Color? Color { get; set; }

    /// <summary>
    /// Fill intensity, 0 to 1; 0 leaves an unfilled outline. Defaults to 0.6, the testbed's value.
    /// </summary>
    public float Alpha { get; set; } = 0.6f;

    /// <summary>Sets both at once.</summary>
    /// <param name="color">The fill's own colour, or <c>null</c> for the outline colour.</param>
    /// <param name="alpha">Fill intensity, 0 to 1.</param>
    public void Set(Color? color, float alpha = 1f)
    {
        Color = color;
        Alpha = alpha;
    }
}