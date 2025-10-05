using Stride.Graphics;
using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// Procedurally generates a 2D capsule (stadium) mesh composed of two semicircles and a central rectangle.
/// Mesh instances are cached per dimension/tessellation/UV combination for reuse.
/// </summary>
public class Capsule2DProceduralModel : PrimitiveProceduralModelBase
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

    private static readonly Dictionary<(float Height, float Radius, int Tessellation, float UScale, float VScale, bool IsLeftHanded), GeometricMeshData<VertexPositionNormalTexture>> _meshCache = [];

    /// <summary>
    /// Creates mesh data using current property values, honoring UV scale.
    /// </summary>
    protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData() => New(TotalHeight, Radius, Tessellation, UvScale.X, UvScale.Y);

    /// <summary>
    /// Retrieves (or builds and caches) a capsule mesh with the specified dimensions and options.
    /// </summary>
    /// <param name="height">Total capsule height including semicircular ends.</param>
    /// <param name="radius">Radius of the semicircular ends.</param>
    /// <param name="tessellation">Segments used for each semicircle (minimum clamped internally).</param>
    /// <param name="uScale">Texture U scale factor.</param>
    /// <param name="vScale">Texture V scale factor.</param>
    /// <param name="toLeftHanded">If true, reverses winding for left‑handed coordinates.</param>
    /// <returns>Mesh data for the requested capsule.</returns>
    public static GeometricMeshData<VertexPositionNormalTexture> New(float height = 1.0f, float radius = 0.25f, int tessellation = 16, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        var cacheKey = (height, radius, tessellation, uScale, vScale, toLeftHanded);

        if (!_meshCache.TryGetValue(cacheKey, out var mesh))
        {
            mesh = CreateMesh(height, radius, tessellation, uScale, vScale, toLeftHanded);
            _meshCache[cacheKey] = mesh;
        }

        return mesh;
    }

    /// <summary>
    /// Builds a new capsule mesh (without caching) using provided parameters.
    /// </summary>
    /// <inheritdoc cref="New(float, float, int, float, float, bool)"/>
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
        Vector3 topCircleCenter = new Vector3(0, halfRectHeight, 0); // Removed "+ radius"
        Vector3 bottomCircleCenter = new Vector3(0, -halfRectHeight, 0); // Removed "- radius"

        // Set UV scale
        var uvScale = new Vector2(uScale, vScale);

        int currentVertex = 0;

        // ===== TOP SEMICIRCLE =====
        // Top center point
        int topCenterIndex = currentVertex;
        vertices[currentVertex++] = new VertexPositionNormalTexture(
            topCircleCenter,
            normal,
            new Vector2(0.5f, 0.75f) * uvScale // Adjusted UV to match rectangle top
        );

        // Top semicircle perimeter
        for (int i = 0; i <= tessellation; i++)
        {
            float angle = i * MathF.PI / tessellation;
            float x = radius * MathF.Cos(angle);
            float y = radius * MathF.Sin(angle);

            Vector2 texCoord = new Vector2(
                0.5f + (MathF.Cos(angle) * 0.5f),
                0.75f + (MathF.Sin(angle) * 0.25f) // Adjusted UV to match with rectangle
            );

            vertices[currentVertex++] = new VertexPositionNormalTexture(
                topCircleCenter + new Vector3(x, y, 0),
                normal,
                texCoord * uvScale
            );
        }

        // ===== RECTANGLE MIDDLE PART =====
        int rectStartIndex = currentVertex;
        if (rectHeight > 0)
        {
            // Define rect vertices - same winding as top semicircle
            // Top-left
            vertices[currentVertex++] = new VertexPositionNormalTexture(
                new Vector3(-radius, halfRectHeight, 0),
                normal,
                new Vector2(0, 0.75f) * uvScale
            );

            // Bottom-left
            vertices[currentVertex++] = new VertexPositionNormalTexture(
                new Vector3(-radius, -halfRectHeight, 0),
                normal,
                new Vector2(0, 0.25f) * uvScale
            );

            // Bottom-right
            vertices[currentVertex++] = new VertexPositionNormalTexture(
                new Vector3(radius, -halfRectHeight, 0),
                normal,
                new Vector2(1, 0.25f) * uvScale
            );

            // Top-right
            vertices[currentVertex++] = new VertexPositionNormalTexture(
                new Vector3(radius, halfRectHeight, 0),
                normal,
                new Vector2(1, 0.75f) * uvScale
            );
        }

        // ===== BOTTOM SEMICIRCLE =====
        // Bottom center point
        int bottomCenterIndex = currentVertex;
        vertices[currentVertex++] = new VertexPositionNormalTexture(
            bottomCircleCenter,
            normal,
            new Vector2(0.5f, 0.25f) * uvScale // Adjusted UV to match rectangle bottom
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
                0.25f + (MathF.Sin(angle) * 0.25f) // Adjusted UV to match with rectangle
            );

            vertices[currentVertex++] = new VertexPositionNormalTexture(
                bottomCircleCenter + new Vector3(x, y, 0),
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