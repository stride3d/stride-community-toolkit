using Stride.Graphics;
using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

public class PolygonProceduralModel : PrimitiveProceduralModelBase
{
    public Vector2[] Vertices { get; set; } = [];

    private static readonly Dictionary<string, GeometricMeshData<VertexPositionNormalTexture>> MeshCache = [];

    protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
    {
        return New(Vertices, UvScale.X, UvScale.Y);
    }

    // Helper methods for common shapes
    public static PolygonProceduralModel CreateTriangle(Vector2 size)
    {
        return new PolygonProceduralModel
        {
            Vertices =
            [
                new Vector2(0, size.Y / 2),
                new Vector2(-size.X / 2, -size.Y / 2),
                new Vector2(size.X / 2, -size.Y / 2)
            ]
        };
    }

    public static PolygonProceduralModel CreateRectangle(Vector2 size)
    {
        return new PolygonProceduralModel
        {
            Vertices =
            [
                new Vector2(-size.X / 2, -size.Y / 2),
                new Vector2(-size.X / 2, size.Y / 2),
                new Vector2(size.X / 2, size.Y / 2),
                new Vector2(size.X / 2, -size.Y / 2)
            ]
        };
    }

    public static GeometricMeshData<VertexPositionNormalTexture> New(Vector2[] vertices, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        if (vertices.Length < 3)
        {
            throw new ArgumentException("A polygon must have at least 3 vertices", nameof(vertices));
        }

        // Create hash for cache key
        var hash = string.Join(",", vertices.Select(v => $"{v.X},{v.Y}"));
        var cacheKey = $"{hash}_{uScale}_{vScale}_{toLeftHanded}";

        if (!MeshCache.TryGetValue(cacheKey, out var mesh))
        {
            mesh = CreateMesh(vertices, uScale, vScale, toLeftHanded);
            MeshCache[cacheKey] = mesh;
        }

        return mesh;
    }

    public static GeometricMeshData<VertexPositionNormalTexture> CreateMesh(Vector2[] points, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        int vertexCount = points.Length;

        // For simple triangulation, we'll use a fan approach
        // This only works reliably for convex polygons
        Span<VertexPositionNormalTexture> vertices = new VertexPositionNormalTexture[vertexCount];
        Span<int> indices = new int[(vertexCount - 2) * 3];

        // Calculate centroid for UV mapping
        Vector2 centroid = Vector2.Zero;
        foreach (var point in points)
        {
            centroid += point;
        }

        centroid /= vertexCount;

        // Create vertices
        for (int i = 0; i < vertexCount; i++)
        {
            // Normalize UV coordinates based on position relative to centroid and bounds
            Vector2 relativePos = points[i] - centroid;

            Vector2 uv = new Vector2(
                (relativePos.X / points.Max(p => Math.Abs(p.X - centroid.X)) + 1) * 0.5f * uScale,
                (relativePos.Y / points.Max(p => Math.Abs(p.Y - centroid.Y)) + 1) * 0.5f * vScale
            );

            vertices[i] = new VertexPositionNormalTexture(
                new Vector3(points[i].X, points[i].Y, 0),
                Vector3.UnitZ,
                uv
            );
        }

        // Create triangle indices (fan triangulation)
        for (int i = 0; i < vertexCount - 2; i++)
        {
            indices[i * 3] = 0;
            indices[i * 3 + 1] = i + 1;
            indices[i * 3 + 2] = i + 2;
        }

        return new GeometricMeshData<VertexPositionNormalTexture>(vertices.ToArray(), indices.ToArray(), toLeftHanded) { Name = "Polygon" };
    }
}