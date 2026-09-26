namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// Which part of a shape is kept: optionally only a band a given depth inside the outline, and
/// optionally only an angular range about the centre. The default keeps all of it. It also carries
/// the two ways a shape's points can mean something other than a convex polygon - a pixel-measured
/// radius, and a run of points stroked as a polyline.
/// </summary>
/// <param name="Hollow">Whether only the band <paramref name="RingWidth"/> deep inside the outline is the shape.</param>
/// <param name="RingWidth">Depth of the band from the outline inward, in world units; 0 leaves a stroke with no area.</param>
/// <param name="StartAngle">Where the kept angular range starts, radians from the plane's X axis.</param>
/// <param name="SweepAngle">Size of the kept range, counter-clockwise; 0 keeps the full turn.</param>
/// <param name="RoundCaps">Whether the range ends in semicircles rather than radial edges. Circles only.</param>
/// <param name="PixelRadius">Whether the radius and band depth are in pixels on screen rather than world units, converted per shape at its own depth.</param>
/// <param name="Polyline">Whether the points are a run to stroke - the shape is everything within the radius of the nearest segment - rather than a convex polygon.</param>
/// <param name="Space">Whether the points are 3D, in the batch.s space point buffer, and the run is stroked on screen rather than on a plane.</param>
/// <param name="RunOffset">For a piece of a very long polyline, the arc length along the whole run at its first point, so a dash pattern continues across the pieces.</param>
internal readonly record struct ShapeSlice(bool Hollow, float RingWidth, float StartAngle, float SweepAngle, bool RoundCaps, bool PixelRadius = false, bool Polyline = false, float RunOffset = 0f, bool Space = false)
{
    /// <summary>The whole shape, which is what every ordinary draw call submits.</summary>
    public static readonly ShapeSlice Whole = default;

    /// <summary>Bit 0 of the GPU flags: hollow.</summary>
    internal const int HollowFlag = 1;

    /// <summary>Bit 1 of the GPU flags: round caps.</summary>
    internal const int RoundCapsFlag = 2;

    /// <summary>Bit 3 of the GPU flags: radius and band depth are in pixels.</summary>
    internal const int PixelRadiusFlag = 8;

    /// <summary>Bit 4 of the GPU flags: the points are a polyline run.</summary>
    internal const int PolylineFlag = 16;

    /// <summary>Bit 6 of the GPU flags: the points are 3D and the run is stroked on screen.</summary>
    internal const int SpaceFlag = 64;

    /// <summary>The slice as the shader reads it.</summary>
    internal int Flags => (Hollow ? HollowFlag : 0) | (RoundCaps ? RoundCapsFlag : 0) | (PixelRadius ? PixelRadiusFlag : 0) | (Polyline ? PolylineFlag : 0) | (Space ? SpaceFlag : 0);

    /// <summary>
    /// Puts a sweep into the form the shader reads: counter-clockwise, and 0 for a full turn.
    /// Returns <c>false</c> for a sweep of nothing, which draws nothing.
    /// </summary>
    internal static bool TryNormalizeSweep(ref float startAngle, ref float sweepAngle)
    {
        if (sweepAngle == 0f) return false;

        // Clockwise is the same range walked from its other end
        if (sweepAngle < 0f)
        {
            startAngle += sweepAngle;
            sweepAngle = -sweepAngle;
        }

        if (sweepAngle >= MathF.Tau) sweepAngle = 0f;

        return true;
    }
}