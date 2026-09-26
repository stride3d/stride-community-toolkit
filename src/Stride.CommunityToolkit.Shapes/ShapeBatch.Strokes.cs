using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

// The strokes: polylines in a plane and through space, lines and the wire box - the family that
// shares the run helpers. The state they capture and the plumbing they submit to live in
// ShapeBatch.cs.
public sealed partial class ShapeBatch
{
    /// <summary>
    /// Submits a run of points as one stroke of a world-space width, with round joins and caps - a
    /// plotted curve, a path, the outline of any shape including a concave one.
    /// </summary>
    /// <param name="points">The run, in the plane's own coordinates, of any length.</param>
    /// <param name="position">World position of the plane's origin.</param>
    /// <param name="axisX">The plane's X axis. Normalized for you.</param>
    /// <param name="axisY">The plane's Y axis. Normalized for you.</param>
    /// <param name="width">Stroke width in world units.</param>
    /// <param name="color">The stroke colour. Drawn solid, ignoring <see cref="ShapeFill.Alpha"/>.</param>
    /// <param name="closed">Whether the last point joins back to the first.</param>
    /// <remarks>
    /// Joins are round: the stroke is everything within half the width of the run itself, drawn as
    /// one shape. Only a run of more than 64 points is split, into pieces that share a point; where
    /// two pieces meet the round cap is drawn twice, which shows only under an <see cref="Opacity"/>
    /// below one, as a slightly brighter dot. <see cref="Dash"/> runs along the whole run.
    /// </remarks>
    public void DrawPolyline(ReadOnlySpan<Vector2> points, Vector3 position, Vector3 axisX, Vector3 axisY, float width, Color color, bool closed = false)
        => AddPolyline(points, new ShapePlane(position, Vector3.Normalize(axisX), Vector3.Normalize(axisY), PlaneMode.Fixed), SolidStyle(color), MathF.Max(width, 0.0001f) * 0.5f, closed);

    /// <summary>
    /// The 2D case of <see cref="DrawPolyline(ReadOnlySpan{Vector2}, Vector3, Vector3, Vector3, float, Color, bool)"/>: a stroke in the XY plane.
    /// </summary>
    public void DrawPolyline(ReadOnlySpan<Vector2> points, float width, Color color, bool closed = false)
        => DrawPolyline(points, Vector3.Zero, Vector3.UnitX, Vector3.UnitY, width, color, closed);

    /// <summary>
    /// Submits a run of points as one stroke a constant number of pixels wide at any distance, with
    /// round joins and caps - the <see cref="DrawPixelLine"/> of curves and frames.
    /// </summary>
    /// <param name="points">The run, in the plane's own coordinates, of any length.</param>
    /// <param name="position">World position of the plane's origin.</param>
    /// <param name="axisX">The plane's X axis. Normalized for you.</param>
    /// <param name="axisY">The plane's Y axis. Normalized for you.</param>
    /// <param name="pixelWidth">Stroke width in pixels on a 100% display.</param>
    /// <param name="color">The stroke colour.</param>
    /// <param name="closed">Whether the last point joins back to the first.</param>
    public void DrawPixelPolyline(ReadOnlySpan<Vector2> points, Vector3 position, Vector3 axisX, Vector3 axisY, float pixelWidth, Color color, bool closed = false)
        => AddPolyline(points, new ShapePlane(position, Vector3.Normalize(axisX), Vector3.Normalize(axisY), PlaneMode.Fixed), OutlineStyle(color, pixelWidth), 0f, closed);

    /// <summary>
    /// The 2D case of <see cref="DrawPixelPolyline(ReadOnlySpan{Vector2}, Vector3, Vector3, Vector3, float, Color, bool)"/>: a stroke in the XY plane.
    /// </summary>
    public void DrawPixelPolyline(ReadOnlySpan<Vector2> points, float pixelWidth, Color color, bool closed = false)
        => DrawPixelPolyline(points, Vector3.Zero, Vector3.UnitX, Vector3.UnitY, pixelWidth, color, closed);

    /// <summary>
    /// Submits a run of points anywhere in 3D as one stroke of a real world-space width, with round
    /// joins and caps - a rope, an orbit, a trail that leaves the plane it started on.
    /// </summary>
    /// <param name="points">The run, in world space, of any length.</param>
    /// <param name="width">Stroke width in world units.</param>
    /// <param name="color">The stroke colour. Drawn solid, ignoring <see cref="ShapeFill.Alpha"/>.</param>
    /// <param name="closed">Whether the last point joins back to the first.</param>
    /// <remarks>
    /// The stroke is measured on screen: every point is projected and the run is stroked in pixels,
    /// so it narrows with distance the way a rope does while <see cref="BorderWidth"/> stays a
    /// constant pixel width, and it faces the camera from every angle with no geometry behind it.
    /// A run of more than 64 points is split into pieces that share a point; each piece depth-tests
    /// as its nearest point, and <see cref="Dash"/> restarts its pattern at each piece. A run that
    /// crosses the camera's near plane is not supported.
    /// </remarks>
    public void DrawPolyline(ReadOnlySpan<Vector3> points, float width, Color color, bool closed = false)
        => AddSpacePolyline(points, SolidStyle(color), MathF.Max(width, 0.0001f) * 0.5f, closed);

    /// <summary>
    /// Submits a run of points anywhere in 3D as one stroke a constant number of pixels wide at any
    /// distance, with round joins and caps - the <see cref="DrawPixelLine"/> of space curves, and
    /// what a trail through a 3D scene is drawn with.
    /// </summary>
    /// <param name="points">The run, in world space, of any length.</param>
    /// <param name="pixelWidth">Stroke width in pixels on a 100% display.</param>
    /// <param name="color">The stroke colour.</param>
    /// <param name="closed">Whether the last point joins back to the first.</param>
    /// <remarks>
    /// Same behaviour and limits as <see cref="DrawPolyline(ReadOnlySpan{Vector3}, float, Color, bool)"/>:
    /// per-fragment depth, pieces of 64 points with the dash pattern restarting at each, no crossing
    /// of the near plane.
    /// </remarks>
    public void DrawPixelPolyline(ReadOnlySpan<Vector3> points, float pixelWidth, Color color, bool closed = false)
        => AddSpacePolyline(points, OutlineStyle(color, pixelWidth), 0f, closed);

    /// <summary>
    /// Submits a thick line between two points in 3D: a capsule swung about its own axis to face
    /// the camera, so it reads as a round-capped line of the width you ask for from any angle.
    /// </summary>
    /// <param name="start">World-space start point.</param>
    /// <param name="end">World-space end point.</param>
    /// <param name="width">Line width in world units.</param>
    /// <param name="color">The line colour.</param>
    /// <remarks>
    /// Unlike hardware line rendering, which clamps to one pixel on most drivers, this is a real
    /// world-space width. The line is drawn solid, ignoring <see cref="ShapeFill.Alpha"/>.
    /// </remarks>
    public void DrawLine(Vector3 start, Vector3 end, float width, Color color)
    {
        var direction = end - start;
        var length = direction.Length();
        var lineRadius = MathF.Max(width, 0.0001f) * 0.5f;
        var center = (start + end) * 0.5f;

        // Shorter than it is wide: the round caps have swallowed the segment, so it is just a dot
        if (length <= lineRadius * 2f)
        {
            DrawBillboardCircle(center, lineRadius, color);

            return;
        }

        // The caps add the radius back at each end, so the segment stops short of the endpoints
        var halfSegment = length * 0.5f - lineRadius;

        ReadOnlySpan<Vector2> segment = [new(-halfSegment, 0f), new(halfSegment, 0f)];

        // Solid: an outline-only line would be two thin rails rather than a line
        Add(segment,
            new ShapePlane(center, direction / length, Vector3.UnitY, PlaneMode.Axial),
            SolidStyle(color), ShapeSlice.Whole, lineRadius, 1f);
    }

    /// <summary>
    /// Submits a line whose width is measured in on-screen pixels rather than world units, so it
    /// keeps exactly the same thickness however far away it is - grid lines, axis rules, leader
    /// lines, anything that should read as drawn on the screen rather than placed in the scene.
    /// </summary>
    /// <param name="start">World-space start point.</param>
    /// <param name="end">World-space end point.</param>
    /// <param name="pixelWidth">Line width in on-screen pixels.</param>
    /// <param name="color">The line colour.</param>
    /// <remarks>
    /// This is <see cref="DrawLine"/> with its world width collapsed to nothing, which leaves the
    /// outline - already measured in pixels - drawing the whole line. <see cref="BorderWidth"/> and
    /// <see cref="ShapeFill.Alpha"/> do not apply; <paramref name="pixelWidth"/> is the width.
    /// </remarks>
    public void DrawPixelLine(Vector3 start, Vector3 end, float pixelWidth, Color color)
    {
        var direction = end - start;
        var length = direction.Length();

        if (length <= float.Epsilon) return;

        var halfSegment = length * 0.5f;

        ReadOnlySpan<Vector2> segment = [new(-halfSegment, 0f), new(halfSegment, 0f)];

        Add(segment,
            new ShapePlane((start + end) * 0.5f, direction / length, Vector3.UnitY, PlaneMode.Axial),
            OutlineStyle(color, pixelWidth), ShapeSlice.Whole, 0f, 1f);
    }

    /// <summary>
    /// Submits the twelve edges of an axis-aligned box as thick lines - a bounds or selection
    /// volume whose edges keep their width at any distance.
    /// </summary>
    /// <param name="center">World-space centre of the box.</param>
    /// <param name="size">Full extent along each axis, in world units.</param>
    /// <param name="width">Edge width in world units.</param>
    /// <param name="color">The edge colour.</param>
    public void DrawWireBox(Vector3 center, Vector3 size, float width, Color color)
    {
        var half = size * 0.5f;

        Span<Vector3> corners =
        [
            center + new Vector3(-half.X, -half.Y, -half.Z),
            center + new Vector3(half.X, -half.Y, -half.Z),
            center + new Vector3(half.X, -half.Y, half.Z),
            center + new Vector3(-half.X, -half.Y, half.Z),
            center + new Vector3(-half.X, half.Y, -half.Z),
            center + new Vector3(half.X, half.Y, -half.Z),
            center + new Vector3(half.X, half.Y, half.Z),
            center + new Vector3(-half.X, half.Y, half.Z),
        ];

        for (var i = 0; i < 4; i++)
        {
            var next = (i + 1) % 4;

            DrawLine(corners[i], corners[next], width, color);
            DrawLine(corners[i + 4], corners[next + 4], width, color);
            DrawLine(corners[i], corners[i + 4], width, color);
        }
    }
}