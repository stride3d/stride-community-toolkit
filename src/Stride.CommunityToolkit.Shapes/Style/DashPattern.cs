namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// The dash pattern along an outline, in on-screen pixels - constant at any zoom, like the border.
/// <see cref="ShapeBatch.Dash"/> holds one; each draw call captures its values as it is made.
/// </summary>
/// <remarks>
/// <para>
/// Dashes run around circles, rings, annuli, sectors and arcs - starting at the start angle, so a
/// gauge's ticks begin where its sweep does - and along lines. A polygon with more than two
/// corners is always drawn solid; its outline has no single direction to dash along.
/// </para>
/// <para>
/// Around a circle or arc the pattern is stretched or squeezed by up to half a period so that a
/// whole number of dashes fills the turn, which is what makes a tick ring come out even at any
/// radius instead of ending in a stub. The gaps are cuts through the whole shape, so the fill and
/// glow stop at a dash end exactly as they stop at a sector's edge.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// shapes.Dash.Length = 6f;          // gap defaults to the same
/// shapes.Dash.Phase += 30f * dt;    // and the ring turns
/// shapes.DrawArc(center, radius, 0f, MathF.Tau, color);
/// shapes.Dash.Clear();
/// </code>
/// </example>
public sealed class DashPattern
{
    /// <summary>Length of each dash, in pixels. The default 0 draws solid.</summary>
    public float Length { get; set; }

    /// <summary>The gap between dashes, in pixels. The default 0 makes the gap the same length as the dash.</summary>
    public float Gap { get; set; }

    /// <summary>
    /// Where along the outline the pattern starts, in pixels. Advance it every frame and a dashed
    /// ring rotates, a dashed line marches - the cheapest animation a HUD has.
    /// </summary>
    public float Phase { get; set; }

    /// <summary>Sets the whole pattern at once.</summary>
    /// <param name="length">Length of each dash in pixels; 0 for solid.</param>
    /// <param name="gap">Gap in pixels; 0 for the same as the dash.</param>
    /// <param name="phase">Where the pattern starts, in pixels.</param>
    public void Set(float length, float gap = 0f, float phase = 0f)
    {
        Length = length;
        Gap = gap;
        Phase = phase;
    }

    /// <summary>Back to solid.</summary>
    public void Clear() => Set(0f);

    /// <summary>The pattern as a draw call captures it, with the gap resolved.</summary>
    internal DashStyle Capture() => new(Length, Gap > 0f ? Gap : Length, Phase);
}