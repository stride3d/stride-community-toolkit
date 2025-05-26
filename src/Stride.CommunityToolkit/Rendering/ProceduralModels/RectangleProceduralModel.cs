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

public class CapsuleProceduralModel : PrimitiveProceduralModelBase
{
    /// <summary>
    /// Gets or sets the total height of the capsule (including the two semicircles).
    /// </summary>
    public float Height { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the radius of the capsule.
    /// </summary>
    public float Radius { get; set; } = 0.25f;

    /// <summary>
    /// Gets or sets the tessellation for the semicircle ends (higher values = smoother curves).
    /// </summary>
    public int Tessellation { get; set; } = 16;

    private static readonly Dictionary<(float Height, float Radius, int Tessellation, float UScale, float VScale, bool IsLeftHanded), GeometricMeshData<VertexPositionNormalTexture>> MeshCache = [];

    protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
    {
        return New(Height, Radius, Tessellation, UvScale.X, UvScale.Y);
    }

    public static GeometricMeshData<VertexPositionNormalTexture> New(float height = 1.0f, float radius = 0.25f, int tessellation = 16, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        var cacheKey = (height, radius, tessellation, uScale, vScale, toLeftHanded);

        if (!MeshCache.TryGetValue(cacheKey, out var mesh))
        {
            mesh = CreateMesh(height, radius, tessellation, uScale, vScale, toLeftHanded);
            MeshCache[cacheKey] = mesh;
        }

        return mesh;
    }

    public static GeometricMeshData<VertexPositionNormalTexture> CreateMesh(float height = 1.0f, float radius = 0.25f, int tessellation = 16, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        // Cap the minimum tessellation to prevent issues
        tessellation = Math.Max(4, tessellation);

        // Calculate the rectangular part height (total height minus the two semicircle diameters)
        float rectHeight = Math.Max(0, height - 2 * radius);

        // Calculate total number of vertices and indices
        int vertexCount = (tessellation * 2) + 2; // Two semicircles plus two center points
        int indexCount = tessellation * 6; // Two semicircles with triangles

        if (rectHeight > 0)
        {
            vertexCount += 2; // Add two vertices for the rectangle
            indexCount += 6; // Add two triangles for the rectangle
        }

        // Use stack allocation for reasonable sizes
        Span<VertexPositionNormalTexture> vertices = vertexCount <= 128
            ? stackalloc VertexPositionNormalTexture[vertexCount]
            : new VertexPositionNormalTexture[vertexCount];

        Span<int> indices = indexCount <= 384
            ? stackalloc int[indexCount]
            : new int[indexCount];

        Vector3 normal = Vector3.UnitZ;

        // Calculate center points of circles
        float halfRectHeight = rectHeight / 2;
        Vector3 topCenter = new Vector3(0, halfRectHeight + radius, 0);
        Vector3 bottomCenter = new Vector3(0, -(halfRectHeight + radius), 0);

        // Set UV scale
        var uvScale = new Vector2(uScale, vScale);

        // Create top semicircle vertices
        int currentVertex = 0;

        // Top center point
        vertices[currentVertex++] = new VertexPositionNormalTexture(
            topCenter,
            normal,
            new Vector2(0.5f, 1.0f) * uvScale
        );

        // Top semicircle perimeter
        for (int i = 0; i <= tessellation; i++)
        {
            float angle = (float)i / tessellation * MathF.PI;
            float x = radius * MathF.Cos(angle);
            float y = radius * MathF.Sin(angle);

            Vector2 texCoord = new Vector2(
                0.5f + (MathF.Cos(angle) * 0.5f),
                1.0f
            );

            vertices[currentVertex++] = new VertexPositionNormalTexture(
                topCenter + new Vector3(x, y, 0),
                normal,
                texCoord * uvScale
            );
        }

        // Rectangle vertices (if any)
        int rectStartIndex = currentVertex;
        if (rectHeight > 0)
        {
            vertices[currentVertex++] = new VertexPositionNormalTexture(
                new Vector3(-radius, halfRectHeight, 0),
                normal,
                new Vector2(0, 0.75f) * uvScale
            );

            vertices[currentVertex++] = new VertexPositionNormalTexture(
                new Vector3(radius, halfRectHeight, 0),
                normal,
                new Vector2(1, 0.75f) * uvScale
            );

            vertices[currentVertex++] = new VertexPositionNormalTexture(
                new Vector3(radius, -halfRectHeight, 0),
                normal,
                new Vector2(1, 0.25f) * uvScale
            );

            vertices[currentVertex++] = new VertexPositionNormalTexture(
                new Vector3(-radius, -halfRectHeight, 0),
                normal,
                new Vector2(0, 0.25f) * uvScale
            );
        }

        // Bottom center point
        int bottomCenterIndex = currentVertex;
        vertices[currentVertex++] = new VertexPositionNormalTexture(
            bottomCenter,
            normal,
            new Vector2(0.5f, 0.0f) * uvScale
        );

        // Bottom semicircle perimeter
        int bottomStartIndex = currentVertex;
        for (int i = 0; i <= tessellation; i++)
        {
            float angle = MathF.PI + (float)i / tessellation * MathF.PI;
            float x = radius * MathF.Cos(angle);
            float y = radius * MathF.Sin(angle);

            Vector2 texCoord = new Vector2(
                0.5f + (MathF.Cos(angle) * 0.5f),
                0.0f
            );

            vertices[currentVertex++] = new VertexPositionNormalTexture(
                bottomCenter + new Vector3(x, y, 0),
                normal,
                texCoord * uvScale
            );
        }

        // Create indices
        int currentIndex = 0;

        // Top semicircle triangles
        for (int i = 0; i < tessellation; i++)
        {
            if (!toLeftHanded)
            {
                indices[currentIndex++] = 0; // Top center
                indices[currentIndex++] = i + 1;
                indices[currentIndex++] = i + 2;
            }
            else
            {
                indices[currentIndex++] = 0; // Top center
                indices[currentIndex++] = i + 2;
                indices[currentIndex++] = i + 1;
            }
        }

        // Rectangle triangles (if any)
        if (rectHeight > 0)
        {
            if (!toLeftHanded)
            {
                // First triangle
                indices[currentIndex++] = rectStartIndex;
                indices[currentIndex++] = rectStartIndex + 1;
                indices[currentIndex++] = rectStartIndex + 2;

                // Second triangle
                indices[currentIndex++] = rectStartIndex;
                indices[currentIndex++] = rectStartIndex + 2;
                indices[currentIndex++] = rectStartIndex + 3;
            }
            else
            {
                // First triangle
                indices[currentIndex++] = rectStartIndex;
                indices[currentIndex++] = rectStartIndex + 2;
                indices[currentIndex++] = rectStartIndex + 1;

                // Second triangle
                indices[currentIndex++] = rectStartIndex;
                indices[currentIndex++] = rectStartIndex + 3;
                indices[currentIndex++] = rectStartIndex + 2;
            }
        }

        // Bottom semicircle triangles
        for (int i = 0; i < tessellation; i++)
        {
            if (!toLeftHanded)
            {
                indices[currentIndex++] = bottomCenterIndex; // Bottom center
                indices[currentIndex++] = bottomStartIndex + i + 1;
                indices[currentIndex++] = bottomStartIndex + i;
            }
            else
            {
                indices[currentIndex++] = bottomCenterIndex; // Bottom center
                indices[currentIndex++] = bottomStartIndex + i;
                indices[currentIndex++] = bottomStartIndex + i + 1;
            }
        }

        return new GeometricMeshData<VertexPositionNormalTexture>(vertices.ToArray(), indices.ToArray(), toLeftHanded) { Name = "Capsule" };
    }
}