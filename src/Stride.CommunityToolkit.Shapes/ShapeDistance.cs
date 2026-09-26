using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// The distance functions behind <c>ShapeShader</c>, on the CPU: the same signed distances
/// <c>Effects/ShapeDistance.sdsl</c> computes per fragment, negative inside, so a pick agrees with
/// the pixels. Points are in the shape's own plane units; a space run is measured on screen.
/// </summary>
internal static class ShapeDistance
{
    /// <summary>
    /// Signed distance to a shape's outline, combined the way the pixel stage combines it: the base
    /// field (a point, a convex polygon or a run) pushed out by the rounding radius, then cut to a
    /// hollow band and to the wedge of an angular cut. Dashes are ignored: a dashed outline picks
    /// as if solid.
    /// </summary>
    /// <param name="p">The point, relative to the position the shape was drawn at, in its plane's units.</param>
    /// <param name="points">The shape's points, as drawn.</param>
    /// <param name="slice">The cut, band and run flags.</param>
    /// <param name="radius">The rounding radius, in the same units as the points.</param>
    /// <param name="ringWidth">The band's depth, in the same units.</param>
    public static float SdShape(Vector2 p, ReadOnlySpan<Vector2> points, in ShapeSlice slice, float radius, float ringWidth)
    {
        var cut = slice.SweepAngle > 0f;

        // A round-capped arc is its own field: the band's centreline is an arc and the shape is
        // everything within half the band width of it. Only a circle has a centreline arc.
        if (slice.RoundCaps && points.Length == 1)
        {
            return SdArc(p - points[0], radius, ringWidth, slice.StartAngle, slice.SweepAngle, cut);
        }

        var dw = slice.Polyline
            ? SdPolyline(p, points)
            : points.Length == 1 ? Vector2.Distance(p, points[0]) : SdConvexPolygon(p, points);

        var s = dw - radius;

        // Keep only the band ringWidth deep inside the outline
        if (slice.Hollow && !slice.Polyline)
        {
            s = MathF.Max(s, -(s + ringWidth));
        }

        // Intersect with the wedge between the cut's two edge directions
        if (cut && !slice.Polyline)
        {
            s = MathF.Max(s, SdWedge(p - points[0], slice.StartAngle, slice.SweepAngle));
        }

        return s;
    }

    /// <summary>Signed distance to a convex polygon with counter-clockwise winding, negative inside.</summary>
    public static float SdConvexPolygon(Vector2 p, ReadOnlySpan<Vector2> points)
    {
        var first = points[0];
        var d = Vector2.DistanceSquared(p, first);
        var side = -1f;
        var previous = points[^1];

        foreach (var current in points)
        {
            var e = current - previous;
            var w = p - previous;
            var ee = MathF.Max(Vector2.Dot(e, e), 0.000001f);
            var b = w - e * Math.Clamp(Vector2.Dot(w, e) / ee, 0f, 1f);

            d = MathF.Min(d, Vector2.Dot(b, b));

            // Outside any edge is outside the polygon
            if (Cross(w, e) >= 0f) side = 1f;

            previous = current;
        }

        return side * MathF.Sqrt(d);
    }

    /// <summary>Distance to a run of points: the nearest of its segments. A stroke of some radius around it is the run with round joins and caps.</summary>
    public static float SdPolyline(Vector2 p, ReadOnlySpan<Vector2> points)
    {
        var previous = points[0];
        var d = Vector2.DistanceSquared(p, previous);

        for (var i = 1; i < points.Length; i++)
        {
            var current = points[i];
            var e = current - previous;
            var w = p - previous;
            var ee = MathF.Max(Vector2.Dot(e, e), 0.000001f);
            var b = w - e * Math.Clamp(Vector2.Dot(w, e) / ee, 0f, 1f);

            d = MathF.Min(d, Vector2.Dot(b, b));
            previous = current;
        }

        return MathF.Sqrt(d);
    }

    /// <summary>
    /// Signed distance to the wedge between two edge directions from the origin, sweeping
    /// counter-clockwise from the first, negative inside. Exact along the edges and inside; outside
    /// a corner it is the Chebyshev distance, which keeps the corner sharp.
    /// </summary>
    public static float SdWedge(Vector2 p, float startAngle, float sweepAngle)
    {
        var (sin1, cos1) = MathF.SinCos(startAngle);
        var (sin2, cos2) = MathF.SinCos(startAngle + sweepAngle);
        var u1 = new Vector2(cos1, sin1);
        var u2 = new Vector2(cos2, sin2);

        // Which side of each edge the point lies on, positive on the wedge's side; past a half turn
        // the wedge is the complement of the narrow one between the same edges
        var c1 = Cross(u1, p);
        var c2 = Cross(p, u2);
        var inside = sweepAngle <= MathF.PI ? c1 >= 0f && c2 >= 0f : c1 > 0f || c2 > 0f;

        // Distance to each edge as a ray: perpendicular while alongside it, to the origin behind it
        var d1 = Vector2.Dot(p, u1) > 0f ? MathF.Abs(c1) : p.Length();
        var d2 = Vector2.Dot(p, u2) > 0f ? MathF.Abs(c2) : p.Length();

        return (inside ? -1f : 1f) * MathF.Min(d1, d2);
    }

    /// <summary>
    /// Signed distance to a round-capped arc: the band's centreline is an arc of the given sweep,
    /// or the whole circle when not cut, and the shape is everything within half the band width of
    /// it, which is what makes the ends semicircles.
    /// </summary>
    public static float SdArc(Vector2 p, float radius, float ringWidth, float startAngle, float sweepAngle, bool cut)
    {
        // Rotate so the arc is symmetric about +Y; the endpoint is then a fixed half-sweep away
        var halfSweep = cut ? 0.5f * sweepAngle : MathF.PI;
        var turn = 0.5f * MathF.PI - (startAngle + halfSweep);
        var (sinTurn, cosTurn) = MathF.SinCos(turn);
        var (sinHalf, cosHalf) = MathF.SinCos(halfSweep);
        var sc = new Vector2(sinHalf, cosHalf);

        var q = new Vector2(p.X * cosTurn - p.Y * sinTurn, p.X * sinTurn + p.Y * cosTurn);
        q.X = MathF.Abs(q.X);

        var halfBand = 0.5f * ringWidth;
        var centreline = radius - halfBand;

        // Past the endpoint's direction the nearest point of the arc is the endpoint itself
        var toArc = sc.Y * q.X > sc.X * q.Y ? Vector2.Distance(q, sc * centreline) : MathF.Abs(q.Length() - centreline);

        return toArc - halfBand;
    }

    /// <summary>
    /// Signed distance in physical pixels from a screen position to a space run: the nearest of its
    /// segments, each one's stroke radius interpolated between its ends, negative inside the stroke.
    /// Also the world point of the run nearest the position and its depth.
    /// </summary>
    /// <param name="pixel">The screen position in physical pixels, y down.</param>
    /// <param name="points">The run: xyz a world position, w the stroke radius there in world units, 0 for a stroke measured in pixels.</param>
    /// <param name="view">The view the run was drawn in.</param>
    /// <param name="nearest">The world point of the run nearest the screen position.</param>
    /// <param name="depth">Its depth, 0 at the near plane and 1 at the far one.</param>
    public static float SdSpacePolyline(Vector2 pixel, ReadOnlySpan<Vector4> points, in ShapeView view, out Vector3 nearest, out float depth)
    {
        var pixelsPerUnit = view.PixelScale * view.ScreenScale;
        var previous = Project(points[0], view, pixelsPerUnit);
        var previousWorld = new Vector3(points[0].X, points[0].Y, points[0].Z);
        var previousW = MathF.Max(view.ClipW(previousWorld), 0.0001f);
        var d = Vector2.Distance(pixel, new Vector2(previous.X, previous.Y)) - previous.Z;

        nearest = previousWorld;
        depth = previous.W;

        for (var i = 1; i < points.Length; i++)
        {
            var current = Project(points[i], view, pixelsPerUnit);
            var currentWorld = new Vector3(points[i].X, points[i].Y, points[i].Z);
            var currentW = MathF.Max(view.ClipW(currentWorld), 0.0001f);
            var e = new Vector2(current.X - previous.X, current.Y - previous.Y);
            var toPixel = new Vector2(pixel.X - previous.X, pixel.Y - previous.Y);
            var t = Math.Clamp(Vector2.Dot(toPixel, e) / MathF.Max(Vector2.Dot(e, e), 0.000001f), 0f, 1f);
            var b = toPixel - e * t;
            var segment = b.Length() - MathUtil.Lerp(previous.Z, current.Z, t);

            if (segment < d)
            {
                d = segment;
                depth = MathUtil.Lerp(previous.W, current.W, t);

                // The screen parameter is not the world one: a world point along a projected segment
                // comes back perspective-correctly through 1 / w
                var weightPrevious = (1f - t) / previousW;
                var weightCurrent = t / currentW;

                nearest = (previousWorld * weightPrevious + currentWorld * weightCurrent) / (weightPrevious + weightCurrent);
            }

            previous = current;
            previousWorld = currentWorld;
            previousW = currentW;
        }

        return d;
    }

    /// <summary>A space point on screen: xy in physical pixels, y down; z the stroke radius there in pixels; w the depth.</summary>
    private static Vector4 Project(Vector4 point, in ShapeView view, float pixelsPerUnit)
    {
        var clip = Vector4.Transform(new Vector4(point.X, point.Y, point.Z, 1f), view.ViewProjection);
        var w = MathF.Max(clip.W, 0.0001f);
        var ndcX = clip.X / w;
        var ndcY = clip.Y / w;

        return new Vector4((ndcX * 0.5f + 0.5f) * view.ViewSize.X, (0.5f - ndcY * 0.5f) * view.ViewSize.Y, point.W * pixelsPerUnit / w, clip.Z / w);
    }

    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
}