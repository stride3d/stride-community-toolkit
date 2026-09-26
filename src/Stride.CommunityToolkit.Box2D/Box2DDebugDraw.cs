using Box2D.NET;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Box2D's own debug drawing, rendered through a <see cref="ShapeBatch"/>: every shape, joint,
/// contact point, force, bounding box, island and body name the testbed can show, from one call a
/// frame, with the testbed's toggles as properties.
/// </summary>
/// <remarks>
/// <para>
/// <c>b2World_Draw</c> walks the world and calls back for each primitive - a solid polygon with a
/// transform and rounding radius, a circle, a capsule, a line, a point. Those map almost one to one
/// onto the batch, which draws polygons with rounding and pixel-constant borders. Shapes are on by
/// default, as in Box2D; joints are on here as well, since drawing them is the usual reason to use
/// this. The rest is off until asked for.
/// </para>
/// <para>
/// Sizes: lines are <see cref="LinePixels"/> wide on screen at any zoom; a point's size is in
/// screen pixels in the testbed and has no equivalent in the batch, so it is scaled by
/// <see cref="PointScale"/> into world units. Text has no renderer of its own; set
/// <see cref="DrawString"/> to route body names and joint labels wherever you like.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var debugDraw = new Box2DDebugDraw(shapeBatch) { DrawContactPoints = true };
///
/// // each frame, after the simulation update
/// debugDraw.Draw(simulation);
/// </code>
/// </example>
public sealed class Box2DDebugDraw
{
    private const int MaxPolygonVertices = 8;
    private const float TransformAxisLength = 0.4f;

    private readonly B2DebugDraw _draw;
    private readonly ShapeBatch _batch;

    /// <summary>
    /// Creates the adapter over a batch. Add the batch with <c>game.AddShapeBatch()</c> first.
    /// </summary>
    public Box2DDebugDraw(ShapeBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        _batch = batch;
        _draw = b2DefaultDebugDraw();
        _draw.drawJoints = true;

        _draw.DrawPolygonFcn = DrawPolygon;
        _draw.DrawSolidPolygonFcn = DrawSolidPolygon;
        _draw.DrawCircleFcn = DrawCircle;
        _draw.DrawSolidCircleFcn = DrawSolidCircle;
        _draw.DrawSolidCapsuleFcn = DrawSolidCapsule;
        _draw.drawLineFcn = DrawLine;
        _draw.DrawTransformFcn = DrawTransform;
        _draw.DrawPointFcn = DrawPoint;
        _draw.DrawStringFcn = DrawText;
    }

    /// <summary>Every shape, in Box2D's body-state colours. On by default.</summary>
    public bool DrawShapes { get => _draw.drawShapes; set => _draw.drawShapes = value; }

    /// <summary>Joints as lines between their anchors. On by default here, unlike in Box2D.</summary>
    public bool DrawJoints { get => _draw.drawJoints; set => _draw.drawJoints = value; }

    /// <summary>Joint limits, targets and the like, drawn on top of the joints.</summary>
    public bool DrawJointExtras { get => _draw.drawJointExtras; set => _draw.drawJointExtras = value; }

    /// <summary>Axis-aligned bounding boxes of every shape.</summary>
    public bool DrawBounds { get => _draw.drawBounds; set => _draw.drawBounds = value; }

    /// <summary>A marker at each body's centre of mass, with its mass as text.</summary>
    public bool DrawMass { get => _draw.drawMass; set => _draw.drawMass = value; }

    /// <summary>Body names, through <see cref="DrawString"/>.</summary>
    public bool DrawBodyNames { get => _draw.drawBodyNames; set => _draw.drawBodyNames = value; }

    /// <summary>Contact points.</summary>
    public bool DrawContactPoints { get => _draw.drawContactPoints; set => _draw.drawContactPoints = value; }

    /// <summary>Contact normals.</summary>
    public bool DrawContactNormals { get => _draw.drawContactNormals; set => _draw.drawContactNormals = value; }

    /// <summary>Contact normal impulses, as lines scaled by <see cref="ForceScale"/>.</summary>
    public bool DrawContactForces { get => _draw.drawContactForces; set => _draw.drawContactForces = value; }

    /// <summary>Contact friction impulses, as lines scaled by <see cref="ForceScale"/>.</summary>
    public bool DrawFrictionForces { get => _draw.drawFrictionForces; set => _draw.drawFrictionForces = value; }

    /// <summary>Contact feature ids as text.</summary>
    public bool DrawContactFeatures { get => _draw.drawContactFeatures; set => _draw.drawContactFeatures = value; }

    /// <summary>Colour each shape by its constraint-graph colour instead of its body state.</summary>
    public bool DrawGraphColors { get => _draw.drawGraphColors; set => _draw.drawGraphColors = value; }

    /// <summary>Sleep islands, as bounding boxes.</summary>
    public bool DrawIslands { get => _draw.drawIslands; set => _draw.drawIslands = value; }

    /// <summary>Length per unit of force for the force lines.</summary>
    public float ForceScale { get => _draw.forceScale; set => _draw.forceScale = value; }

    /// <summary>Size of the joint drawings.</summary>
    public float JointScale { get => _draw.jointScale; set => _draw.jointScale = value; }

    /// <summary>On-screen width of every line, in pixels.</summary>
    public float LinePixels { get; set; } = 1.5f;

    /// <summary>World units per unit of a point's size; Box2D sizes points in screen pixels.</summary>
    public float PointScale { get; set; } = 0.02f;

    /// <summary>
    /// Where text goes: world position, the text, its colour. Nothing is drawn when unset.
    /// </summary>
    public Action<Vector2, string, Color>? DrawString { get; set; }

    /// <summary>Submits the whole world to the batch. Call once per frame, after stepping.</summary>
    public void Draw(B2WorldId world) => b2World_Draw(world, _draw);

    /// <inheritdoc cref="Draw(B2WorldId)"/>
    public void Draw(Box2DSimulation simulation)
    {
        ArgumentNullException.ThrowIfNull(simulation);

        Draw(simulation.GetWorldId());
    }

    private void DrawPolygon(ReadOnlySpan<B2Vec2> vertices, int vertexCount, B2HexColor color, object context)
    {
        var stride = DebugDrawColors.ToColor(color);

        for (var i = 0; i < vertexCount; i++)
        {
            var a = vertices[i];
            var b = vertices[(i + 1) % vertexCount];

            _batch.DrawPixelLine(new Vector3(a.X, a.Y, 0), new Vector3(b.X, b.Y, 0), LinePixels, stride);
        }
    }

    private void DrawSolidPolygon(in B2Transform transform, ReadOnlySpan<B2Vec2> vertices, int vertexCount, float radius, B2HexColor color, object context)
    {
        Span<Vector2> local = stackalloc Vector2[MaxPolygonVertices];
        var count = Math.Min(vertexCount, MaxPolygonVertices);

        for (var i = 0; i < count; i++)
            local[i] = new Vector2(vertices[i].X, vertices[i].Y);

        _batch.DrawSolidPolygon(local[..count], new Vector2(transform.p.X, transform.p.Y), b2Rot_GetAngle(transform.q), DebugDrawColors.ToColor(color), radius);
    }

    private void DrawCircle(in B2Vec2 center, float radius, B2HexColor color, object context)
        => _batch.DrawAnnulus(new Vector2(center.X, center.Y), radius, radius * 0.9f, DebugDrawColors.ToColor(color));

    private void DrawSolidCircle(in B2Transform transform, float radius, B2HexColor color, object context)
    {
        var stride = DebugDrawColors.ToColor(color);
        var centre = new Vector2(transform.p.X, transform.p.Y);

        _batch.DrawSolidCircle(centre, radius, stride);

        // The testbed draws the x-axis so a rolling circle is seen to roll.
        var axis = b2Rot_GetXAxis(transform.q);
        _batch.DrawPixelLine(new Vector3(centre, 0), new Vector3(centre.X + axis.X * radius, centre.Y + axis.Y * radius, 0), LinePixels, stride);
    }

    private void DrawSolidCapsule(in B2Vec2 p1, in B2Vec2 p2, float radius, B2HexColor color, object context)
    {
        Span<Vector2> ends = [new Vector2(p1.X, p1.Y), new Vector2(p2.X, p2.Y)];

        _batch.DrawSolidPolygon(ends, Vector2.Zero, 0f, DebugDrawColors.ToColor(color), radius);
    }

    private void DrawLine(in B2Vec2 p1, in B2Vec2 p2, B2HexColor color, object context)
        => _batch.DrawPixelLine(new Vector3(p1.X, p1.Y, 0), new Vector3(p2.X, p2.Y, 0), LinePixels, DebugDrawColors.ToColor(color));

    private void DrawTransform(in B2Transform transform, object context)
    {
        var origin = new Vector3(transform.p.X, transform.p.Y, 0);
        var x = b2Rot_GetXAxis(transform.q);
        var y = b2Rot_GetYAxis(transform.q);

        _batch.DrawPixelLine(origin, origin + new Vector3(x.X, x.Y, 0) * TransformAxisLength, LinePixels, Color.Red);
        _batch.DrawPixelLine(origin, origin + new Vector3(y.X, y.Y, 0) * TransformAxisLength, LinePixels, Color.Green);
    }

    private void DrawPoint(in B2Vec2 p, float size, B2HexColor color, object context)
        => _batch.DrawSolidCircle(new Vector2(p.X, p.Y), size * PointScale, DebugDrawColors.ToColor(color));

    private void DrawText(in B2Vec2 p, string s, B2HexColor color, object context)
        => DrawString?.Invoke(new Vector2(p.X, p.Y), s, DebugDrawColors.ToColor(color));
}