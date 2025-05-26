using Stride.Graphics;
using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

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

        // Create top semicircle vertices
        int currentVertex = 0;

        // Top center point
        vertices[currentVertex++] = new VertexPositionNormalTexture(
            topCenter,
            normal,
            new Vector2(0.5f, 1.0f) * uvScale
        );

        // Top semicircle perimeter - counter-clockwise for correct winding
        for (int i = 0; i <= tessellation; i++)
        {
            // Use full circle formula but only iterate half way
            float angle = (float)i / tessellation * MathF.PI;

            // Generate points in counter-clockwise direction
            float x = radius * MathF.Cos(angle);
            float y = radius * MathF.Sin(angle);

            // For top circle, y values are positive
            if (toLeftHanded)
                x = -x; // Flip X for left-handed coordinate system

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
            // Define rectangle vertices in counter-clockwise order for correct winding
            float signX = toLeftHanded ? -1.0f : 1.0f; // Flip X for left-handed coordinates

            // Bottom-left of rect
            vertices[currentVertex++] = new VertexPositionNormalTexture(
                new Vector3(-radius * signX, -halfRectHeight, 0),
                normal,
                new Vector2(0, 0.25f) * uvScale
            );

            // Top-left of rect
            vertices[currentVertex++] = new VertexPositionNormalTexture(
                new Vector3(-radius * signX, halfRectHeight, 0),
                normal,
                new Vector2(0, 0.75f) * uvScale
            );

            // Top-right of rect
            vertices[currentVertex++] = new VertexPositionNormalTexture(
                new Vector3(radius * signX, halfRectHeight, 0),
                normal,
                new Vector2(1, 0.75f) * uvScale
            );

            // Bottom-right of rect
            vertices[currentVertex++] = new VertexPositionNormalTexture(
                new Vector3(radius * signX, -halfRectHeight, 0),
                normal,
                new Vector2(1, 0.25f) * uvScale
            );
        }

        // Bottom center point
        int bottomCenterIndex = currentVertex;
        vertices[currentVertex++] = new VertexPositionNormalTexture(
            bottomCenter,
            normal,
            new Vector2(0.5f, 0.0f) * uvScale
        );

        // Bottom semicircle perimeter - counter-clockwise for correct winding
        int bottomStartIndex = currentVertex;
        for (int i = 0; i <= tessellation; i++)
        {
            // Use full circle formula but only iterate half way
            float angle = MathF.PI + (float)i / tessellation * MathF.PI;

            // Generate points in counter-clockwise direction
            float x = radius * MathF.Cos(angle);
            float y = radius * MathF.Sin(angle);

            if (toLeftHanded)
                x = -x; // Flip X for left-handed coordinate system

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

        // Top semicircle triangles - ensure correct winding order
        for (int i = 0; i < tessellation; i++)
        {
            if (!toLeftHanded)
            {
                indices[currentIndex++] = 0; // Top center
                indices[currentIndex++] = i + 2;
                indices[currentIndex++] = i + 1;
            }
            else
            {
                indices[currentIndex++] = 0; // Top center
                indices[currentIndex++] = i + 1;
                indices[currentIndex++] = i + 2;
            }
        }

        // Rectangle triangles (if any) - ensure correct winding order
        if (rectHeight > 0)
        {
            if (!toLeftHanded)
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
            else
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
        }

        // Bottom semicircle triangles - ensure correct winding order
        for (int i = 0; i < tessellation; i++)
        {
            if (!toLeftHanded)
            {
                indices[currentIndex++] = bottomCenterIndex; // Bottom center
                indices[currentIndex++] = bottomStartIndex + i;
                indices[currentIndex++] = bottomStartIndex + i + 1;
            }
            else
            {
                indices[currentIndex++] = bottomCenterIndex; // Bottom center
                indices[currentIndex++] = bottomStartIndex + i + 1;
                indices[currentIndex++] = bottomStartIndex + i;
            }
        }

        return new GeometricMeshData<VertexPositionNormalTexture>(vertices.ToArray(), indices.ToArray(), toLeftHanded) { Name = "Capsule" };
    }
}