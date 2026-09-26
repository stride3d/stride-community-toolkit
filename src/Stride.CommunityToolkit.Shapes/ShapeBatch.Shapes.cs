using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

// The closed shapes: polygons, discs, rings, annuli, sectors, arcs and rectangles, each with its
// 2D and 3D overloads side by side. The state they capture and the plumbing they submit to live
// in ShapeBatch.cs.
public sealed partial class ShapeBatch
{
    /// <summary>
    /// Submits a convex polygon lying in the XY plane, the 2D case.
    /// </summary>
    /// <param name="vertices">The corners in local space, counter-clockwise, at most 8.</param>
    /// <param name="position">World position of the shape's local origin.</param>
    /// <param name="rotation">Rotation in radians about the Z axis.</param>
    /// <param name="color">The outline colour; the fill derives from it and <see cref="ShapeFill.Alpha"/>.</param>
    /// <param name="radius">Optional rounding radius around the polygon, in world units.</param>
    /// <exception cref="ArgumentException">Fewer than 1 or more than 8 vertices were given.</exception>
    public void DrawSolidPolygon(ReadOnlySpan<Vector2> vertices, Vector2 position, float rotation, Color color, float radius = 0f)
    {
        var (sin, cos) = MathF.SinCos(rotation);

        var plane = new ShapePlane(
            new Vector3(position.X, position.Y, 0f),
            new Vector3(cos, sin, 0f),
            new Vector3(-sin, cos, 0f),
            PlaneMode.Fixed);

        Add(vertices, plane, CurrentStyle(color), ShapeSlice.Whole, radius, 1f);
    }

    /// <summary>
    /// Submits a circle in the XY plane, the 2D case.
    /// </summary>
    /// <param name="center">World-space centre.</param>
    /// <param name="radius">Radius in world units.</param>
    /// <param name="color">The outline colour; the fill derives from it and <see cref="ShapeFill.Alpha"/>.</param>
    public void DrawSolidCircle(Vector2 center, float radius, Color color)
        => DrawSolidPolygon([Vector2.Zero], center, 0f, color, radius);

    /// <summary>
    /// Submits a convex polygon lying in an arbitrary plane in 3D.
    /// </summary>
    /// <param name="vertices">The corners in the plane's local space, counter-clockwise, at most 8.</param>
    /// <param name="position">World position of the shape's local origin.</param>
    /// <param name="axisX">The plane's X axis. Normalized for you.</param>
    /// <param name="axisY">The plane's Y axis. Normalized for you.</param>
    /// <param name="color">The outline colour; the fill derives from it and <see cref="ShapeFill.Alpha"/>.</param>
    /// <param name="radius">Optional rounding radius around the polygon, in world units.</param>
    /// <param name="scale">Uniform scale applied to the whole shape, radius included.</param>
    /// <exception cref="ArgumentException">Fewer than 1 or more than 8 vertices were given.</exception>
    public void DrawSolidPolygon(ReadOnlySpan<Vector2> vertices, Vector3 position, Vector3 axisX, Vector3 axisY, Color color, float radius = 0f, float scale = 1f)
        => Add(vertices,
            new ShapePlane(position, Vector3.Normalize(axisX), Vector3.Normalize(axisY), PlaneMode.Fixed),
            CurrentStyle(color), ShapeSlice.Whole, radius, scale);

    /// <summary>
    /// Submits a convex polygon in the plane a rotation puts the XY plane in.
    /// </summary>
    /// <param name="vertices">The corners in local space, counter-clockwise, at most 8.</param>
    /// <param name="position">World position of the shape's local origin.</param>
    /// <param name="rotation">Orientation of the shape's plane.</param>
    /// <param name="color">The outline colour; the fill derives from it and <see cref="ShapeFill.Alpha"/>.</param>
    /// <param name="radius">Optional rounding radius around the polygon, in world units.</param>
    /// <exception cref="ArgumentException">Fewer than 1 or more than 8 vertices were given.</exception>
    public void DrawSolidPolygon(ReadOnlySpan<Vector2> vertices, Vector3 position, Quaternion rotation, Color color, float radius = 0f)
        => Add(vertices,
            new ShapePlane(position, Vector3.Transform(Vector3.UnitX, rotation), Vector3.Transform(Vector3.UnitY, rotation), PlaneMode.Fixed),
            CurrentStyle(color), ShapeSlice.Whole, radius, 1f);

    /// <summary>
    /// Submits a convex polygon that always faces the camera, screen-aligned - a marker that keeps
    /// its shape and orientation from any viewpoint.
    /// </summary>
    /// <param name="vertices">The corners in local space, counter-clockwise, at most 8.</param>
    /// <param name="position">World position of the shape's centre.</param>
    /// <param name="color">The outline colour; the fill derives from it and <see cref="ShapeFill.Alpha"/>.</param>
    /// <param name="radius">Optional rounding radius around the polygon, in world units.</param>
    /// <exception cref="ArgumentException">Fewer than 1 or more than 8 vertices were given.</exception>
    public void DrawBillboard(ReadOnlySpan<Vector2> vertices, Vector3 position, Color color, float radius = 0f)
        => Add(vertices,
            new ShapePlane(position, Vector3.UnitX, Vector3.UnitY, PlaneMode.Screen),
            CurrentStyle(color), ShapeSlice.Whole, radius, 1f);

    /// <summary>
    /// Submits a camera-facing circle: a point marker that stays perfectly round from any angle.
    /// </summary>
    /// <param name="center">World-space centre.</param>
    /// <param name="radius">Radius in world units.</param>
    /// <param name="color">The outline colour; the fill derives from it and <see cref="ShapeFill.Alpha"/>.</param>
    public void DrawBillboardCircle(Vector3 center, float radius, Color color)
        => DrawBillboard([Vector2.Zero], center, color, radius);

    /// <summary>
    /// Submits a camera-facing disc whose radius is measured in pixels on screen, so it is the same
    /// size at any distance - a marker or a scatter point that never shrinks as the camera pulls back.
    /// </summary>
    /// <param name="center">World position of the centre.</param>
    /// <param name="pixelRadius">Radius in pixels on a 100% display; follows the display scale like the border width.</param>
    /// <param name="color">The outline colour; the fill derives from it and <see cref="ShapeFill"/>.</param>
    /// <remarks>Pixel-measured shapes are always billboards: the conversion from pixels to world units is exact only in a screen-aligned plane.</remarks>
    public void DrawPixelDisc(Vector3 center, float pixelRadius, Color color)
        => Add([Vector2.Zero], new ShapePlane(center, Vector3.UnitX, Vector3.UnitY, PlaneMode.Screen), CurrentStyle(color), ShapeSlice.Whole with { PixelRadius = true }, pixelRadius, 1f);

    /// <summary>
    /// Submits a camera-facing ring whose radius is measured in pixels on screen, stroked
    /// <see cref="BorderWidth"/> pixels wide - a cursor marker or a selection halo that keeps its size
    /// at any distance.
    /// </summary>
    /// <param name="center">World position of the centre.</param>
    /// <param name="pixelRadius">Radius of the stroke's centreline in pixels on a 100% display.</param>
    /// <param name="color">The stroke colour.</param>
    public void DrawPixelRing(Vector3 center, float pixelRadius, Color color)
        => Add([Vector2.Zero], new ShapePlane(center, Vector3.UnitX, Vector3.UnitY, PlaneMode.Screen), OutlineStyle(color), Stroke with { PixelRadius = true }, pixelRadius, 1f);

    /// <summary>
    /// Submits a filled disc lying flat in the plane a normal defines - a ground marker, an
    /// area-of-effect indicator, a decal.
    /// </summary>
    /// <param name="center">World-space centre.</param>
    /// <param name="normal">Normal of the plane the disc lies in.</param>
    /// <param name="radius">Radius in world units.</param>
    /// <param name="color">The outline colour; the fill derives from it and <see cref="ShapeFill.Alpha"/>.</param>
    public void DrawDisc(Vector3 center, Vector3 normal, float radius, Color color)
        => Add([Vector2.Zero], ShapePlane.FromNormal(center, normal), CurrentStyle(color), ShapeSlice.Whole, radius, 1f);

    /// <summary>
    /// Submits an unfilled circle lying flat in the plane a normal defines - a selection ring or a
    /// range indicator that does not tint what it encircles.
    /// </summary>
    /// <param name="center">World-space centre.</param>
    /// <param name="normal">Normal of the plane the ring lies in.</param>
    /// <param name="radius">Radius in world units.</param>
    /// <param name="color">The ring colour.</param>
    /// <remarks>
    /// The ring is the shape, not the disc it encloses, so a <see cref="ShapeGlow.Width"/> glows on both
    /// sides of it. <see cref="ShapeFill.Alpha"/> does not apply.
    /// </remarks>
    public void DrawRing(Vector3 center, Vector3 normal, float radius, Color color)
        => Add([Vector2.Zero], ShapePlane.FromNormal(center, normal), OutlineStyle(color), Stroke, radius, 1f);

    /// <summary>
    /// Submits a filled ring - a disc with a hole - lying flat in the plane a normal defines, with
    /// the outline drawn around both edges. A donut, a range band, a thick unit ring.
    /// </summary>
    /// <param name="center">World-space centre.</param>
    /// <param name="normal">Normal of the plane the annulus lies in.</param>
    /// <param name="outerRadius">Outer radius in world units.</param>
    /// <param name="innerRadius">Radius of the hole in world units, smaller than the outer one.</param>
    /// <param name="color">The outline colour; the fill derives from it and <see cref="ShapeFill.Alpha"/>.</param>
    public void DrawAnnulus(Vector3 center, Vector3 normal, float outerRadius, float innerRadius, Color color)
        => AddSector(ShapePlane.FromNormal(center, normal), outerRadius, innerRadius, 0f, MathF.Tau, color);

    /// <summary>
    /// Submits a filled ring in the XY plane, the 2D case of <see cref="DrawAnnulus(Vector3, Vector3, float, float, Color)"/>.
    /// </summary>
    /// <param name="center">World-space centre.</param>
    /// <param name="outerRadius">Outer radius in world units.</param>
    /// <param name="innerRadius">Radius of the hole in world units, smaller than the outer one.</param>
    /// <param name="color">The outline colour; the fill derives from it and <see cref="ShapeFill.Alpha"/>.</param>
    public void DrawAnnulus(Vector2 center, float outerRadius, float innerRadius, Color color)
        => AddSector(ShapePlane.XY(center), outerRadius, innerRadius, 0f, MathF.Tau, color);

    /// <summary>
    /// Submits a filled slice of a disc, cut by two radial edges, lying flat in the plane a normal
    /// defines: a pie wedge, a field-of-view cone, a cooldown sweep. With an inner radius it is a
    /// slice of a ring instead - a donut chart segment, a radial progress bar with square ends.
    /// </summary>
    /// <param name="center">World-space centre the slice is cut from.</param>
    /// <param name="normal">Normal of the plane the slice lies in.</param>
    /// <param name="radius">Outer radius in world units.</param>
    /// <param name="startAngle">Where the slice starts, in radians. See the remarks for where 0 is.</param>
    /// <param name="sweepAngle">How far it extends, in radians. Positive is counter-clockwise, negative clockwise; a full turn or more is the whole ring or disc.</param>
    /// <param name="color">The outline colour; the fill derives from it and <see cref="ShapeFill.Alpha"/>.</param>
    /// <param name="innerRadius">Radius of the hole, in world units; 0 (the default) cuts from the centre.</param>
    /// <remarks>
    /// Angles increase counter-clockwise as seen from the side the normal points to. Zero lies
    /// along the plane's X axis, which is world X for a slice lying on the ground (normal up) and
    /// for one standing in the XY plane (normal +Z); add an offset to the start angle to turn it.
    /// </remarks>
    public void DrawSector(Vector3 center, Vector3 normal, float radius, float startAngle, float sweepAngle, Color color, float innerRadius = 0f)
        => AddSector(ShapePlane.FromNormal(center, normal), radius, innerRadius, startAngle, sweepAngle, color);

    /// <summary>
    /// Submits a filled slice of a disc or ring in the XY plane, the 2D case of
    /// <see cref="DrawSector(Vector3, Vector3, float, float, float, Color, float)"/>. Angles are
    /// counter-clockwise from the X axis.
    /// </summary>
    /// <param name="center">World-space centre the slice is cut from.</param>
    /// <param name="radius">Outer radius in world units.</param>
    /// <param name="startAngle">Where the slice starts, in radians from the X axis.</param>
    /// <param name="sweepAngle">How far it extends, in radians. Positive is counter-clockwise, negative clockwise; a full turn or more is the whole ring or disc.</param>
    /// <param name="color">The outline colour; the fill derives from it and <see cref="ShapeFill.Alpha"/>.</param>
    /// <param name="innerRadius">Radius of the hole, in world units; 0 (the default) cuts from the centre.</param>
    public void DrawSector(Vector2 center, float radius, float startAngle, float sweepAngle, Color color, float innerRadius = 0f)
        => AddSector(ShapePlane.XY(center), radius, innerRadius, startAngle, sweepAngle, color);

    /// <summary>
    /// Submits an arc of a circle with round ends, lying flat in the plane a normal defines. With no
    /// width it is a stroke the border's pixel width - a partial <see cref="DrawRing"/>; with one it
    /// is a filled, outlined band of that world width centred on the radius - a radial progress bar.
    /// </summary>
    /// <param name="center">World-space centre of the circle.</param>
    /// <param name="normal">Normal of the plane the arc lies in.</param>
    /// <param name="radius">Radius of the arc's centreline in world units.</param>
    /// <param name="startAngle">Where the arc starts, in radians. Zero is along the plane's X axis; see <see cref="DrawSector(Vector3, Vector3, float, float, float, Color, float)"/>.</param>
    /// <param name="sweepAngle">How far it extends, in radians. Positive is counter-clockwise, negative clockwise; a full turn or more closes the ring.</param>
    /// <param name="color">The outline colour; with a width the fill derives from it and <see cref="ShapeFill.Alpha"/>.</param>
    /// <param name="width">Width of the band in world units, or 0 (the default) for a stroke.</param>
    /// <remarks>
    /// The ends are semicircles, which is what a progress ring wants. For square, radial ends use
    /// <see cref="DrawSector(Vector3, Vector3, float, float, float, Color, float)"/> with an inner radius.
    /// </remarks>
    public void DrawArc(Vector3 center, Vector3 normal, float radius, float startAngle, float sweepAngle, Color color, float width = 0f)
        => AddArc(ShapePlane.FromNormal(center, normal), radius, startAngle, sweepAngle, color, width);

    /// <summary>
    /// Submits an arc of a circle with round ends in the XY plane, the 2D case of
    /// <see cref="DrawArc(Vector3, Vector3, float, float, float, Color, float)"/>. Angles are
    /// counter-clockwise from the X axis.
    /// </summary>
    /// <param name="center">World-space centre of the circle.</param>
    /// <param name="radius">Radius of the arc's centreline in world units.</param>
    /// <param name="startAngle">Where the arc starts, in radians from the X axis.</param>
    /// <param name="sweepAngle">How far it extends, in radians. Positive is counter-clockwise, negative clockwise; a full turn or more closes the ring.</param>
    /// <param name="color">The outline colour; with a width the fill derives from it and <see cref="ShapeFill.Alpha"/>.</param>
    /// <param name="width">Width of the band in world units, or 0 (the default) for a stroke.</param>
    public void DrawArc(Vector2 center, float radius, float startAngle, float sweepAngle, Color color, float width = 0f)
        => AddArc(ShapePlane.XY(center), radius, startAngle, sweepAngle, color, width);

    /// <summary>
    /// Submits a rectangle lying in an arbitrary plane - a panel on a wall, a floor tile, a decal.
    /// </summary>
    /// <param name="center">World position of the rectangle's centre.</param>
    /// <param name="axisX">The plane's X axis. Normalized for you.</param>
    /// <param name="axisY">The plane's Y axis. Normalized for you.</param>
    /// <param name="size">Width along X and height along Y, in world units.</param>
    /// <param name="color">The outline colour; the fill derives from it and <see cref="ShapeFill.Alpha"/>.</param>
    /// <param name="cornerRadius">Optional corner rounding, in world units.</param>
    public void DrawRectangle(Vector3 center, Vector3 axisX, Vector3 axisY, Vector2 size, Color color, float cornerRadius = 0f)
    {
        // The rounding radius grows the shape, so shrink the corners to keep the size as asked
        var halfWidth = MathF.Max(size.X * 0.5f - cornerRadius, 0.0001f);
        var halfHeight = MathF.Max(size.Y * 0.5f - cornerRadius, 0.0001f);

        ReadOnlySpan<Vector2> corners =
        [
            new(-halfWidth, -halfHeight),
            new(halfWidth, -halfHeight),
            new(halfWidth, halfHeight),
            new(-halfWidth, halfHeight),
        ];

        Add(corners,
            new ShapePlane(center, Vector3.Normalize(axisX), Vector3.Normalize(axisY), PlaneMode.Fixed),
            CurrentStyle(color), ShapeSlice.Whole, cornerRadius, 1f);
    }
}