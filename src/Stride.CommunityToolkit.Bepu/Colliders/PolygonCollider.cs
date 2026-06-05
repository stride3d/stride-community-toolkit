using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using static Stride.BepuPhysics.Definitions.DecomposedHulls;

namespace Stride.CommunityToolkit.Bepu.Colliders;

/// <summary>
/// Provides helpers to construct Bepu <see cref="ConvexHullCollider"/> instances
/// from a regular or custom polygon extruded into a prism.
/// </summary>
public static class PolygonCollider
{
    /// <summary>
    /// Creates a Bepu <see cref="ConvexHullCollider"/> from a polygon extruded along the Z axis.
    /// </summary>
    /// <param name="vertices">
    /// Optional custom polygon vertices in the XY plane. When provided, these take precedence over <paramref name="radius"/> and <paramref name="sides"/>.
    /// </param>
    /// <param name="radius">Optional circumradius for a regular polygon. When <c>null</c>, defaults from <see cref="PolygonProceduralModel"/> are used.</param>
    /// <param name="sides">Optional side count for a regular polygon. When <c>null</c>, defaults from <see cref="PolygonProceduralModel"/> are used.</param>
    /// <param name="depth">The prism depth along the Z axis. Must be greater than 0.</param>
    /// <returns>A <see cref="ConvexHullCollider"/> whose hull is computed from the extruded polygon prism.</returns>
    public static ConvexHullCollider Create(Vector2[]? vertices = null, float? radius = null, int? sides = null, float depth = 1f)
    {
        if (depth <= 0)
            throw new ArgumentOutOfRangeException(nameof(depth), "Depth must be greater than 0.");

        var polygonModel = CreatePolygonModel(vertices, radius, sides);
        var vertices2D = polygonModel.Vertices.Length > 0
            ? polygonModel.Vertices
            : PolygonProceduralModel.GenerateRegularPolygonVertices(polygonModel.Radius, polygonModel.Sides);
        var vertexCount = vertices2D.Length;
        var halfDepth = depth / 2f;

        var points = new Vector3[vertexCount * 2];
        for (var i = 0; i < vertexCount; i++)
        {
            points[i] = new Vector3(vertices2D[i].X, vertices2D[i].Y, halfDepth);
            points[i + vertexCount] = new Vector3(vertices2D[i].X, vertices2D[i].Y, -halfDepth);
        }

        var indexList = new List<uint>();

        for (var i = 1; i < vertexCount - 1; i++)
        {
            indexList.Add(0);
            indexList.Add((uint)i);
            indexList.Add((uint)(i + 1));
        }

        var backBase = (uint)vertexCount;
        for (var i = 1; i < vertexCount - 1; i++)
        {
            indexList.Add(backBase);
            indexList.Add((uint)(backBase + i + 1));
            indexList.Add((uint)(backBase + i));
        }

        for (var i = 0; i < vertexCount; i++)
        {
            var firstFrontIndex = (uint)i;
            var secondFrontIndex = (uint)((i + 1) % vertexCount);
            var firstBackIndex = (uint)(i + vertexCount);
            var secondBackIndex = (uint)((i + 1) % vertexCount + vertexCount);

            indexList.Add(firstFrontIndex);
            indexList.Add(secondFrontIndex);
            indexList.Add(secondBackIndex);
            indexList.Add(firstFrontIndex);
            indexList.Add(secondBackIndex);
            indexList.Add(firstBackIndex);
        }

        return new ConvexHullCollider
        {
            Hull = new DecomposedHulls(
            [
                new DecomposedMesh(
                [
                    new Hull(points, indexList.ToArray())
                ])
            ])
        };
    }

    private static PolygonProceduralModel CreatePolygonModel(Vector2[]? vertices, float? radius, int? sides)
    {
        if (vertices is { Length: > 0 })
            return new PolygonProceduralModel { Vertices = vertices };

        var polygonModel = new PolygonProceduralModel();

        if (radius.HasValue)
            polygonModel.Radius = radius.Value;

        if (sides.HasValue)
            polygonModel.Sides = sides.Value;

        return polygonModel;
    }
}