using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Rendering.Lines;

/// <summary>
/// How a polyline is turned into a ribbon mesh by <see cref="PolylineMeshBuilder"/>.
/// </summary>
/// <remarks>
/// Direct3D draws hardware lines one pixel wide with no way to change that, so a visible line has to be
/// built from triangles: each segment becomes a thin quad of <see cref="Width"/> world units, lying in the
/// plane perpendicular to <see cref="Normal"/>. A ribbon viewed edge-on is invisible, so pick the normal
/// that faces the camera for your scene: <see cref="Vector3.UnitZ"/> for a chart drawn in the XY plane,
/// <see cref="Vector3.UnitY"/> for one drawn on the ground.
/// </remarks>
public sealed class PolylineOptions
{
    /// <summary>The ribbon width in world units.</summary>
    public float Width { get; set; } = 0.05f;

    /// <summary>The line colour.</summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>
    /// Emissive strength. Values above <c>1</c> exceed the bloom threshold, so with post effects enabled
    /// (the toolkit's default 3D compositor) the line glows; <c>1</c> is a flat, unlit-looking line.
    /// </summary>
    public float EmissiveIntensity { get; set; } = 1f;

    /// <summary>The normal of the plane the ribbon lies in. See the class remarks for how to choose it.</summary>
    public Vector3 Normal { get; set; } = Vector3.UnitZ;

    /// <summary>When <see langword="true"/>, the last point is joined back to the first.</summary>
    public bool Closed { get; set; }
}
