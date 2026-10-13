using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// How a chart turns a polyline into a ribbon mesh: its width, colour, glow and the plane it lies in.
/// </summary>
/// <remarks>
/// Direct3D draws hardware lines one pixel wide with no way to change that, so a visible line has to be
/// built from triangles: each segment becomes a thin quad of <see cref="Width"/> world units, lying in the
/// plane perpendicular to <see cref="Normal"/>. A ribbon viewed edge-on is invisible, so pick the normal
/// that faces the camera for your scene: <see cref="Vector3.UnitZ"/> for a chart drawn in the XY plane,
/// <see cref="Vector3.UnitY"/> for one drawn on the ground.
/// </remarks>
internal sealed class PolylineOptions
{
    /// <summary>The ribbon width in world units.</summary>
    internal float Width { get; set; } = 0.05f;

    /// <summary>The line colour.</summary>
    internal Color Color { get; set; } = Color.White;

    /// <summary>
    /// Emissive strength. Values above <c>1</c> exceed the bloom threshold, so with post effects enabled
    /// (the toolkit's default 3D compositor) the line glows; <c>1</c> is a flat, unlit-looking line.
    /// </summary>
    internal float EmissiveIntensity { get; set; } = 1f;

    /// <summary>The normal of the plane the ribbon lies in. See the class remarks for how to choose it.</summary>
    internal Vector3 Normal { get; set; } = Vector3.UnitZ;

    /// <summary>When <see langword="true"/>, the last point is joined back to the first.</summary>
    internal bool Closed { get; set; }
}