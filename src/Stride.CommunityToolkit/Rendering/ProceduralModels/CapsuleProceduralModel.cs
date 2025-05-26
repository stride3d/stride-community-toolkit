using Stride.Graphics;
using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

public class CapsuleProceduralModel : PrimitiveProceduralModelBase
{
    /// <summary>
    /// Gets or sets the total height of the capsule (including the two semicircles).
    /// </summary>
    public float TotalHeight { get; set; } = 1.0f;

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
        return New(TotalHeight, Radius, Tessellation, UvScale.X, UvScale.Y);
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

        // Make sure the height is greater than twice the radius to ensure we have a rectangular section
        float totalHeight = Math.Max(height, radius * 2.01f); // Add a small buffer to ensure rectangle exists

        // Calculate the rectangular part height
        float rectHeight = Math.Max(0.01f, totalHeight - 2 * radius);

        // Calculate total number of vertices and indices
        int vertexCount = (tessellation + 1) * 2 + 2; // Two semicircles plus two center points
        int indexCount = tessellation * 6; // Two semicircles with triangles

        if (rectHeight > 0)
        {
            vertexCount += 4; // Add four vertices for the rectangle
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

        int currentVertex = 0;

        // ===== TOP SEMICIRCLE =====
        // Top center point
        int topCenterIndex = currentVertex;
        vertices[currentVertex++] = new VertexPositionNormalTexture(
            topCenter,
            normal,
            new Vector2(0.5f, 1.0f) * uvScale
        );

        // Top semicircle perimeter
        for (int i = 0; i <= tessellation; i++)
        {
            float angle = i * MathF.PI / tessellation;
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

        // ===== RECTANGLE MIDDLE PART =====
        int rectStartIndex = currentVertex;
        if (rectHeight > 0)
        {
            // Define rect vertices - same winding as top semicircle
            // Set up rectangle in counter-clockwise order
            vertices[currentVertex++] = new VertexPositionNormalTexture(
                new Vector3(-radius, halfRectHeight, 0),  // Top-left
                normal,
                new Vector2(0, 0.75f) * uvScale
            );

            vertices[currentVertex++] = new VertexPositionNormalTexture(
                new Vector3(-radius, -halfRectHeight, 0), // Bottom-left
                normal,
                new Vector2(0, 0.25f) * uvScale
            );

            vertices[currentVertex++] = new VertexPositionNormalTexture(
                new Vector3(radius, -halfRectHeight, 0),  // Bottom-right
                normal,
                new Vector2(1, 0.25f) * uvScale
            );

            vertices[currentVertex++] = new VertexPositionNormalTexture(
                new Vector3(radius, halfRectHeight, 0),   // Top-right
                normal,
                new Vector2(1, 0.75f) * uvScale
            );
        }

        // ===== BOTTOM SEMICIRCLE =====
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
            float angle = MathF.PI + i * MathF.PI / tessellation;
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

        // ===== INDEXING =====
        int currentIndex = 0;

        // Top semicircle triangles
        for (int i = 0; i < tessellation; i++)
        {
            indices[currentIndex++] = topCenterIndex;
            indices[currentIndex++] = topCenterIndex + i + 2;
            indices[currentIndex++] = topCenterIndex + i + 1;
        }

        // Rectangle triangles
        if (rectHeight > 0)
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

        // Bottom semicircle triangles
        for (int i = 0; i < tessellation; i++)
        {
            indices[currentIndex++] = bottomCenterIndex;
            indices[currentIndex++] = bottomCenterIndex + i + 2;
            indices[currentIndex++] = bottomCenterIndex + i + 1;
        }

        // Flip winding for left-handed coordinate system
        if (toLeftHanded)
        {
            for (int i = 0; i < indexCount; i += 3)
            {
                int temp = indices[i + 1];
                indices[i + 1] = indices[i + 2];
                indices[i + 2] = temp;
            }
        }

        return new GeometricMeshData<VertexPositionNormalTexture>(vertices.ToArray(), indices.ToArray(), toLeftHanded) { Name = "Capsule" };
    }
}