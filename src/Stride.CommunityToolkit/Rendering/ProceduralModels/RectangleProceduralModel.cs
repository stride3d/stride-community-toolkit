using Stride.Graphics;
using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

public class RectangleProceduralModel : PrimitiveProceduralModelBase
{
    /// <summary>
    /// Gets or sets the size of the object as a two-dimensional vector.
    /// </summary>
    public Vector2 Size { get; set; } = Vector2.One;

    private static readonly Vector2[] TextureCoordinates = [new(1, 0), new(1, 1), new(0, 1), new(0, 0)];
    private static readonly Dictionary<Vector2, GeometricMeshData<VertexPositionNormalTexture>> MeshCache = [];

    protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
    {
        return New(Size, UvScale.X, UvScale.Y);
    }

    public static GeometricMeshData<VertexPositionNormalTexture> New(Vector2 size, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        // Create a cache key that includes all parameters that affect the mesh
        var key = new Vector2(size.X * uScale, size.Y * vScale);

        if (toLeftHanded) key *= -1; // Distinguish handedness in cache

        if (!MeshCache.TryGetValue(key, out var mesh))
        {
            // Create and cache the mesh if not found
            mesh = CreateMesh(size, uScale, vScale, toLeftHanded);
            MeshCache[key] = mesh;
        }

        return mesh;
    }

    public static GeometricMeshData<VertexPositionNormalTexture> CreateMesh(Vector2 size, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        Span<VertexPositionNormalTexture> vertices = stackalloc VertexPositionNormalTexture[4];
        Span<int> indices = stackalloc int[6];
        Span<Vector2> textureCoordinates = stackalloc Vector2[4];

        size /= 2.0f;
        var uvScale = new Vector2(uScale, vScale);

        for (var i = 0; i < 4; i++)
        {
            textureCoordinates[i] = TextureCoordinates[i] * uvScale;
        }

        // Four vertices
        vertices[0] = new VertexPositionNormalTexture(new Vector3(-size.X, -size.Y, 0), Vector3.UnitZ, textureCoordinates[0]);
        vertices[1] = new VertexPositionNormalTexture(new Vector3(-size.X, size.Y, 0), Vector3.UnitZ, textureCoordinates[1]);
        vertices[2] = new VertexPositionNormalTexture(new Vector3(size.X, size.Y, 0), Vector3.UnitZ, textureCoordinates[2]);
        vertices[3] = new VertexPositionNormalTexture(new Vector3(size.X, -size.Y, 0), Vector3.UnitZ, textureCoordinates[3]);

        // Triangle indices
        indices[0] = 0;
        indices[1] = 1;
        indices[2] = 2;
        indices[3] = 0;
        indices[4] = 2;
        indices[5] = 3;

        // Create the primitive object.
        return new GeometricMeshData<VertexPositionNormalTexture>(vertices.ToArray(), indices.ToArray(), toLeftHanded) { Name = "Rectangle" };
    }
}

public class TriangleProceduralModel : PrimitiveProceduralModelBase
{
    public Vector2 Size { get; set; } = Vector2.One;

    private static readonly Vector2[] TextureCoordinates = [new(0.5f, 1), new(0, 0), new(1, 0)];
    private static readonly Dictionary<Vector2, GeometricMeshData<VertexPositionNormalTexture>> MeshCache = [];

    protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
    {
        return New(Size, UvScale.X, UvScale.Y);
    }

    public static GeometricMeshData<VertexPositionNormalTexture> New(Vector2 size, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        // Create a cache key that includes all parameters that affect the mesh
        var key = new Vector2(size.X * uScale, size.Y * vScale);

        if (toLeftHanded) key *= -1; // Distinguish handedness in cache

        if (!MeshCache.TryGetValue(key, out var mesh))
        {
            // Create and cache the mesh if not found
            mesh = CreateMesh(size, uScale, vScale, toLeftHanded);
            MeshCache[key] = mesh;
        }

        return mesh;
    }

    public static GeometricMeshData<VertexPositionNormalTexture> CreateMesh(Vector2 size, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        Span<VertexPositionNormalTexture> vertices = stackalloc VertexPositionNormalTexture[3];
        Span<int> indices = stackalloc int[3];
        Span<Vector2> textureCoordinates = stackalloc Vector2[3];

        size /= 2.0f;
        var uvScale = new Vector2(uScale, vScale);

        for (var i = 0; i < 4; i++)
        {
            textureCoordinates[i] = TextureCoordinates[i] * uvScale;
        }

        // Three vertices for triangle
        vertices[0] = new VertexPositionNormalTexture(new Vector3(0, size.Y, 0), Vector3.UnitZ, textureCoordinates[0]);
        vertices[1] = new VertexPositionNormalTexture(new Vector3(-size.X, -size.Y, 0), Vector3.UnitZ, textureCoordinates[1]);
        vertices[2] = new VertexPositionNormalTexture(new Vector3(size.X, -size.Y, 0), Vector3.UnitZ, textureCoordinates[2]);

        // One triangle
        indices[0] = 0;
        indices[1] = 1;
        indices[2] = 2;

        // Create the primitive object.
        return new GeometricMeshData<VertexPositionNormalTexture>(vertices.ToArray(), indices.ToArray(), toLeftHanded) { Name = "Triangle" };
    }
}

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

public class CircleProceduralModel : PrimitiveProceduralModelBase
{
    public float Radius { get; set; } = 0.5f;
    public int Tessellation { get; set; } = 32;

    private static readonly Dictionary<(float Radius, int Tessellation, float UScale, float VScale, bool IsLeftHanded), GeometricMeshData<VertexPositionNormalTexture>> MeshCache = [];

    protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
    {
        return New(Radius, Tessellation, UvScale.X, UvScale.Y);
    }

    public static GeometricMeshData<VertexPositionNormalTexture> New(float radius = 0.5f, int tessellation = 32, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        var cacheKey = (radius, tessellation, uScale, vScale, toLeftHanded);

        if (!MeshCache.TryGetValue(cacheKey, out var mesh))
        {
            mesh = CreateMesh(radius, tessellation, uScale, vScale, toLeftHanded);
            MeshCache[cacheKey] = mesh;
        }

        return mesh;
    }

    public static GeometricMeshData<VertexPositionNormalTexture> CreateMesh(float radius = 0.5f, int tessellation = 32, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        // Use stack allocation for small arrays
        Span<VertexPositionNormalTexture> vertices = tessellation <= 128
            ? stackalloc VertexPositionNormalTexture[tessellation + 1]
            : new VertexPositionNormalTexture[tessellation + 1];

        Span<int> indices = tessellation <= 128
            ? stackalloc int[tessellation * 3]
            : new int[tessellation * 3];

        // Create circle on the XY plane (front-facing) rather than XZ plane
        Vector3 normal = Vector3.UnitZ; // Face along Z-axis like other 2D primitives

        // Center vertex
        vertices[0] = new VertexPositionNormalTexture(Vector3.Zero, normal, new Vector2(0.5f, 0.5f) * new Vector2(uScale, vScale));

        // Create perimeter vertices
        for (int i = 0; i < tessellation; i++)
        {
            float angle = (float)(i * 2.0 * Math.PI / tessellation);
            // Use sin for X and cos for Y to create a circle on XY plane
            float x = radius * (float)Math.Cos(angle); // Changed from sin to cos
            float y = radius * (float)Math.Sin(angle); // Changed from cos to sin

            Vector2 texCoord = new Vector2(
                0.5f + ((float)Math.Cos(angle) * 0.5f), // Changed to match vertex position
                0.5f + ((float)Math.Sin(angle) * 0.5f)  // Changed to match vertex position
            );

            vertices[i + 1] = new VertexPositionNormalTexture(
                new Vector3(x, y, 0), // Using XY plane instead of XZ
                normal,
                texCoord * new Vector2(uScale, vScale)
            );
        }

        // Create triangles in the correct winding order
        for (int i = 0; i < tessellation; i++)
        {
            indices[i * 3] = 0; // Center vertex

            // Ensure correct winding order
            if (toLeftHanded)
            {
                indices[i * 3 + 1] = 1 + ((i + 1) % tessellation);
                indices[i * 3 + 2] = 1 + i;
            }
            else
            {
                indices[i * 3 + 1] = 1 + i;
                indices[i * 3 + 2] = 1 + ((i + 1) % tessellation);
            }
        }

        return new GeometricMeshData<VertexPositionNormalTexture>(vertices.ToArray(), indices.ToArray(), toLeftHanded) { Name = "Circle" };
    }
}