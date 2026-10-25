using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using static Stride.BepuPhysics.Definitions.DecomposedHulls;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Builds the convex hull collider of a flat polygon extruded along Z: the prism a 2D polygon shape collides as.
/// </summary>
public static class PolygonCollider
{
    /// <summary>
    /// Creates the collider of a regular polygon with <paramref name="sides"/> corners on a circle of
    /// <paramref name="radius"/>, extruded <paramref name="depth"/> along Z.
    /// </summary>
    public static ConvexHullCollider Create(float radius, int sides, float depth = 1f)
        => Create(PolygonProceduralModel.GenerateRegularPolygonVertices(radius, sides), depth);

    /// <summary>
    /// Creates the collider of <paramref name="polygon"/>, extruded <paramref name="depth"/> along Z.
    /// </summary>
    public static ConvexHullCollider Create(Primitive2D.RegularPolygonShape polygon, float depth = 1f)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        return Create(polygon.Radius, polygon.Sides, depth);
    }

    /// <summary>
    /// Creates the collider of <paramref name="polygon"/>, extruded <paramref name="depth"/> along Z.
    /// </summary>
    public static ConvexHullCollider Create(Primitive2D.PolygonShape polygon, float depth = 1f)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        return Create(polygon.Vertices, depth);
    }

    /// <summary>
    /// Creates the collider of the convex polygon with the given corners, in order around the outline, extruded
    /// <paramref name="depth"/> along Z.
    /// </summary>
    public static ConvexHullCollider Create(Vector2[] vertices, float depth = 1f)
    {
        ArgumentNullException.ThrowIfNull(vertices);

        if (vertices.Length < 3)
            throw new ArgumentException("A polygon needs at least three vertices.", nameof(vertices));

        if (depth <= 0)
            throw new ArgumentOutOfRangeException(nameof(depth), "Depth must be greater than 0.");

        var vertexCount = vertices.Length;
        var halfDepth = depth / 2f;

        // The prism as an explicit triangle mesh: the polygon at +halfDepth first, the same polygon
        // at -halfDepth second, so index i and i+vertexCount are the two ends of one side edge.
        var points = new Vector3[vertexCount * 2];

        for (var i = 0; i < vertexCount; i++)
        {
            points[i] = new Vector3(vertices[i].X, vertices[i].Y, halfDepth);
            points[i + vertexCount] = new Vector3(vertices[i].X, vertices[i].Y, -halfDepth);
        }

        // Two fans of vertexCount - 2 triangles for the caps and two triangles per side: 12 (vertexCount - 1) indices
        var indices = new uint[12 * (vertexCount - 1)];
        var next = 0;

        // Front cap: a triangle fan from vertex 0, wound so it faces +Z.
        for (var i = 1; i < vertexCount - 1; i++)
        {
            indices[next++] = 0;
            indices[next++] = (uint)i;
            indices[next++] = (uint)(i + 1);
        }

        // Back cap: the same fan with the last two indices swapped, so it faces -Z instead.
        var backBase = (uint)vertexCount;

        for (var i = 1; i < vertexCount - 1; i++)
        {
            indices[next++] = backBase;
            indices[next++] = (uint)(backBase + i + 1);
            indices[next++] = (uint)(backBase + i);
        }

        // Sides: one quad per edge, as two triangles. The modulo closes the loop from the last
        // vertex back to the first.
        for (var i = 0; i < vertexCount; i++)
        {
            var front = (uint)i;
            var nextFront = (uint)((i + 1) % vertexCount);
            var back = front + backBase;
            var nextBack = nextFront + backBase;

            indices[next++] = front;
            indices[next++] = nextFront;
            indices[next++] = nextBack;

            indices[next++] = front;
            indices[next++] = nextBack;
            indices[next++] = back;
        }

        // Bepu computes the actual hull from these points; the triangles are what it decomposes.
        return new ConvexHullCollider { Hull = new DecomposedHulls([new DecomposedMesh([new Hull(points, indices)])]) };
    }
}