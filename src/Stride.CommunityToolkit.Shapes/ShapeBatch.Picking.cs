using Stride.Core.Mathematics;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Shapes;

// Hit-testing: which tagged shape is under a screen position, answered from what the batch drew
// last frame with the same distance functions the shader paints with. The draw calls and the
// plumbing they submit to live in ShapeBatch.cs.
public sealed partial class ShapeBatch
{
    /// <summary>
    /// The tag every shape drawn from now on carries, or <c>null</c> (the default) for shapes that
    /// cannot be picked. Current state captured per draw call, like <see cref="Fill"/> or
    /// <see cref="Glow"/>: set it, draw, set the next one. A hit hands the tag back as
    /// <see cref="ShapeHit.Tag"/>, so it is whatever the caller wants to get back - a station, an
    /// index, a button. Only tagged shapes are recorded for picking; a batch that never tags pays
    /// nothing.
    /// </summary>
    public object? Tag { get; set; }

    // This frame's tagged shapes and their points, as the caller gave them
    private List<ShapePickRecord> _picks = [];
    private List<Vector2> _pickPoints = [];
    private List<Vector4> _pickSpacePoints = [];

    // The frame last drawn: what a pick is answered from. A script asks between the previous draw
    // and the next one, when the instance list is already empty, so the records are handed over
    // at Reset rather than dropped.
    private List<ShapePickRecord> _lastPicks = [];
    private List<Vector2> _lastPickPoints = [];
    private List<Vector4> _lastPickSpacePoints = [];

    /// <summary>
    /// The view the batch was last drawn in, as the render feature saw it: what a pick places the
    /// mouse with. Null until the batch has been drawn once.
    /// </summary>
    internal ShapeView? LastView { get; set; }

    /// <summary>
    /// Whether a pick can be answered yet: the batch has been drawn at least once, so it knows the
    /// view. False on the first frame.
    /// </summary>
    public bool CanPick => LastView is not null;

    /// <summary>
    /// How much of the world one on-screen pixel covers at a point, as the last drawn frame saw it -
    /// the same figure the shader uses for a pixel-measured width or radius, in the display's scaled
    /// pixels when <see cref="AutoScale"/> is on. What a line needs to stop short of a pixel-radius
    /// ring by the ring's radius, whatever the distance; 0 until the batch has been drawn once.
    /// </summary>
    /// <param name="point">The point in the world.</param>
    public float WorldPerPixel(Vector3 point) => LastView?.WorldPerPixel(point) ?? 0f;

    /// <summary>
    /// The topmost tagged shape under a screen position, as of the frame last drawn: the nearest to
    /// the camera, and at equal depth the one drawn last. A screen shape is over everything.
    /// </summary>
    /// <param name="screenPosition">The position, normalised (0,0) top left to (1,1) bottom right - <c>Input.MousePosition</c> as it comes.</param>
    /// <param name="hit">The shape found.</param>
    /// <param name="slackPixels">How far outside the outline, in pixels, still counts as a hit: a few pixels make a thin line or ring clickable. The border already counts as the shape.</param>
    /// <returns>Whether any tagged shape is under the position.</returns>
    /// <remarks>
    /// A pick sees what the batch drew and nothing else: a shape behind scene geometry still
    /// picks, because the batch has no depth buffer to consult, and a shape with no tag never does.
    /// A dashed outline picks as if solid.
    /// </remarks>
    public bool TryPick(Vector2 screenPosition, out ShapeHit hit, float slackPixels = 0f)
    {
        hit = default;

        if (LastView is not { } view) return false;

        var found = false;
        var bestIndex = -1;

        foreach (ref readonly var record in CollectionsMarshal.AsSpan(_lastPicks))
        {
            if (!Test(record, view, screenPosition, slackPixels, out var candidate)) continue;

            if (!found || candidate.Depth < hit.Depth || (candidate.Depth == hit.Depth && record.Index > bestIndex))
            {
                hit = candidate;
                bestIndex = record.Index;
                found = true;
            }
        }

        return found;
    }

    /// <summary>
    /// Every tagged shape under a screen position, as of the frame last drawn, front to back: nearest
    /// depth first, and at equal depth the shape drawn last first.
    /// </summary>
    /// <param name="screenPosition">The position, normalised (0,0) top left to (1,1) bottom right.</param>
    /// <param name="slackPixels">How far outside the outline, in pixels, still counts as a hit.</param>
    public IReadOnlyList<ShapeHit> PickAll(Vector2 screenPosition, float slackPixels = 0f)
    {
        if (LastView is not { } view) return [];

        List<(ShapeHit Hit, int Index)> hits = [];

        foreach (ref readonly var record in CollectionsMarshal.AsSpan(_lastPicks))
        {
            if (Test(record, view, screenPosition, slackPixels, out var hit)) hits.Add((hit, record.Index));
        }

        hits.Sort(static (a, b) => a.Hit.Depth != b.Hit.Depth ? a.Hit.Depth.CompareTo(b.Hit.Depth) : b.Index.CompareTo(a.Index));

        var result = new ShapeHit[hits.Count];

        for (var i = 0; i < hits.Count; i++) result[i] = hits[i].Hit;

        return result;
    }

    /// <summary>
    /// The topmost tagged shape a world ray hits, for a caller with a ray of its own. Screen shapes
    /// and space strokes are measured on the screen and have no answer for a ray; use the screen
    /// position overload for them.
    /// </summary>
    /// <param name="ray">The ray, in world units.</param>
    /// <param name="hit">The shape found.</param>
    /// <param name="slackPixels">How far outside the outline, in pixels, still counts as a hit.</param>
    public bool TryPick(Ray ray, out ShapeHit hit, float slackPixels = 0f)
    {
        hit = default;

        if (LastView is not { } view) return false;

        var found = false;
        var bestIndex = -1;

        foreach (ref readonly var record in CollectionsMarshal.AsSpan(_lastPicks))
        {
            if (record.Screen || record.Slice.Space) continue;
            if (!TestPlane(record, view, ray, slackPixels, out var candidate)) continue;

            if (!found || candidate.Depth < hit.Depth || (candidate.Depth == hit.Depth && record.Index > bestIndex))
            {
                hit = candidate;
                bestIndex = record.Index;
                found = true;
            }
        }

        return found;
    }

    private void RecordPick(ReadOnlySpan<Vector2> vertices, in ShapePlane plane, in ShapeStyle style, in ShapeSlice slice, float radius, float scale)
    {
        if (Tag is not { } tag) return;

        var offset = _pickPoints.Count;

        foreach (var vertex in vertices) _pickPoints.Add(vertex);

        _picks.Add(new ShapePickRecord(tag, _picks.Count, plane, style.Screen, slice, radius, scale, style.BorderWidth, offset, vertices.Length));
    }

    private void RecordSpacePick(ReadOnlySpan<Vector3> points, in ShapeStyle style, float radius)
    {
        if (Tag is not { } tag) return;

        var offset = _pickSpacePoints.Count;

        foreach (var point in points) _pickSpacePoints.Add(new Vector4(point, radius));

        _picks.Add(new ShapePickRecord(tag, _picks.Count, SpacePlane, style.Screen, SpaceStroke, radius, 1f, style.BorderWidth, offset, points.Length));
    }

    /// <summary>This frame's records become the ones a pick is answered from; the next frame starts empty.</summary>
    private void HandOverPicks()
    {
        (_lastPicks, _picks) = (_picks, _lastPicks);
        (_lastPickPoints, _pickPoints) = (_pickPoints, _lastPickPoints);
        (_lastPickSpacePoints, _pickSpacePoints) = (_pickSpacePoints, _lastPickSpacePoints);

        _picks.Clear();
        _pickPoints.Clear();
        _pickSpacePoints.Clear();
    }

    private bool Test(in ShapePickRecord record, in ShapeView view, Vector2 screenPosition, float slackPixels, out ShapeHit hit)
    {
        if (record.Slice.Space) return TestSpace(record, view, screenPosition, slackPixels, out hit);
        if (record.Screen) return TestScreen(record, view, screenPosition, slackPixels, out hit);

        return TestPlane(record, view, view.RayAt(screenPosition), slackPixels, out hit);
    }

    /// <summary>A flat shape in the world: the ray meets its plane, and the plane's field decides.</summary>
    private bool TestPlane(in ShapePickRecord record, in ShapeView view, Ray ray, float slackPixels, out ShapeHit hit)
    {
        hit = default;

        var plane = record.Plane;
        var origin = plane.Origin;
        var axisX = plane.AxisX;
        var axisY = plane.AxisY;

        // Resolve the plane the way the vertex stage does: a screen-aligned billboard takes the
        // camera's axes, an axial one keeps its X and swings about it to face the eye
        switch (plane.Mode)
        {
            case PlaneMode.Screen:
                axisX = view.CameraRight;
                axisY = view.CameraUp;
                break;
            case PlaneMode.Axial:
                var side = Vector3.Cross(view.EyePosition - origin, axisX);
                var sideLength = side.Length();
                axisY = sideLength > 0.000001f ? side / sideLength : view.CameraUp;
                break;
        }

        var normal = Vector3.Cross(axisX, axisY);
        var normalLength = normal.Length();

        if (normalLength < 0.000001f) return false;

        normal /= normalLength;

        // Intersect by hand: one dot product for a plane through a known point (Stride's
        // Plane(point, normal) constructor builds the mirror plane)
        var facing = Vector3.Dot(normal, ray.Direction);

        if (MathF.Abs(facing) < 0.000001f) return false;

        var along = Vector3.Dot(normal, origin - ray.Position) / facing;

        if (along < 0f) return false;

        var point = ray.Position + ray.Direction * along;
        var offset = point - origin;
        var local = new Vector2(Vector3.Dot(offset, axisX), Vector3.Dot(offset, axisY)) / record.Scale;

        // A pixel-measured radius is converted to world units at the shape's own depth, as the
        // vertex stage does, so a marker is the same size on screen at any distance
        var radius = record.Radius;
        var ringWidth = record.Slice.RingWidth;

        if (record.Slice.PixelRadius)
        {
            var pixelToWorld = view.WorldPerPixel(origin);

            radius *= pixelToWorld;
            ringWidth *= pixelToWorld;
        }

        var points = CollectionsMarshal.AsSpan(_lastPickPoints).Slice(record.Offset, record.Count);
        var distance = ShapeDistance.SdShape(local, points, record.Slice, radius, ringWidth) * record.Scale;

        // The border straddles the outline, so half of it lies outside and is still the shape
        var worldPerPixel = view.WorldPerPixel(point);
        var reach = (0.5f * record.BorderWidth + slackPixels) * worldPerPixel;

        if (distance > reach) return false;

        hit = new ShapeHit(record.Tag, point, local, distance, view.Depth(point));

        return true;
    }

    /// <summary>A shape on the screen: pixels straight from the position, no ray, over everything.</summary>
    private bool TestScreen(in ShapePickRecord record, in ShapeView view, Vector2 screenPosition, float slackPixels, out ShapeHit hit)
    {
        hit = default;

        // Scaled pixels from the top left, the units screen shapes are drawn in
        var pixel = screenPosition * view.ViewSize / view.ScreenScale;
        var plane = record.Plane;
        var axisX = plane.AxisX;
        var axisY = plane.AxisY;

        switch (plane.Mode)
        {
            case PlaneMode.Screen:
                axisX = Vector3.UnitX;
                axisY = Vector3.UnitY;
                break;
            case PlaneMode.Axial:
                // The in-plane perpendicular, as the vertex stage takes it: cross(Z, axisX)
                axisY = Vector3.Normalize(new Vector3(-axisX.Y, axisX.X, 0f));
                break;
        }

        var point = new Vector3(pixel, 0f);
        var offset = point - plane.Origin;
        var local = new Vector2(Vector3.Dot(offset, axisX), Vector3.Dot(offset, axisY)) / record.Scale;
        var points = CollectionsMarshal.AsSpan(_lastPickPoints).Slice(record.Offset, record.Count);

        // On the screen a pixel already is the unit, so a pixel radius needs no conversion
        var distance = ShapeDistance.SdShape(local, points, record.Slice, record.Radius, record.Slice.RingWidth) * record.Scale;
        var reach = 0.5f * record.BorderWidth + slackPixels;

        if (distance > reach) return false;

        hit = new ShapeHit(record.Tag, point, local, distance, -1f);

        return true;
    }

    /// <summary>A space stroke: measured on the screen, in pixels, against the run's projection.</summary>
    private bool TestSpace(in ShapePickRecord record, in ShapeView view, Vector2 screenPosition, float slackPixels, out ShapeHit hit)
    {
        hit = default;

        var pixel = screenPosition * view.ViewSize;
        var points = CollectionsMarshal.AsSpan(_lastPickSpacePoints).Slice(record.Offset, record.Count);
        var distance = ShapeDistance.SdSpacePolyline(pixel, points, view, out var nearest, out var depth);

        // The border is in scaled pixels; the run was measured in physical ones
        var reach = (0.5f * record.BorderWidth + slackPixels) * view.ScreenScale;

        if (distance > reach) return false;

        hit = new ShapeHit(record.Tag, nearest, Vector2.Zero, distance / view.ScreenScale, depth);

        return true;
    }
}