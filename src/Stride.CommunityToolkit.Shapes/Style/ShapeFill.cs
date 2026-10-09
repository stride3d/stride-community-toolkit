using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// How a shape's interior is painted. <see cref="ShapeBatch.Fill"/> holds one; each draw call
/// captures its values as it is made.
/// </summary>
/// <remarks>
/// <para>
/// With no <see cref="Color"/>, the fill is the outline colour. At the default <see cref="Alpha"/>
/// of 1 that is a solid shape in the colour of the draw call. Below 1 the fill is dimmed and made
/// translucent, as in the Box2D testbed; <see cref="TestbedAlpha"/> is the testbed's value. With a
/// colour, the fill is that colour at its own alpha times <see cref="Alpha"/>, not dimmed.
/// </para>
/// <para>
/// A gradient across the fill is <see cref="ShapeBatch.Gradient"/>.
/// </para>
/// </remarks>
public sealed class ShapeFill
{
    /// <summary>
    /// The fill intensity the Box2D testbed draws with. Assign it to <see cref="Alpha"/>, with no
    /// <see cref="Color"/>, for the testbed look: a bright outline around a dimmed, see-through inside.
    /// </summary>
    public static readonly float TestbedAlpha = 0.6f;

    /// <summary>
    /// The fill's own colour, or <c>null</c> (the default) to fill with the outline colour. Set it when
    /// the two should differ, such as a bar with a darker edge.
    /// </summary>
    /// <remarks>
    /// The colour is used as given, including its own alpha; <see cref="Alpha"/> scales only its
    /// opacity. Without a colour, <see cref="Alpha"/> also darkens the outline colour.
    /// </remarks>
    public Color? Color { get; set; }

    /// <summary>
    /// Fill intensity, 0 to 1; 0 leaves an unfilled outline. Defaults to 1, a solid fill.
    /// </summary>
    public float Alpha { get; set; } = 1f;

    /// <summary>Sets both at once.</summary>
    /// <param name="color">The fill's own colour, or <c>null</c> for the outline colour.</param>
    /// <param name="alpha">Fill intensity, 0 to 1.</param>
    public void Set(Color? color, float alpha = 1f)
    {
        Color = color;
        Alpha = alpha;
    }
}