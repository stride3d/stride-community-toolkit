using Stride.Graphics;
using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// Generates a textured rectangle (quad) in the XY plane.
/// </summary>
public class RectangleProceduralModel : PrimitiveProceduralModelBase
{
    /// <summary>
    /// Gets or sets the size of the object as a two-dimensional vector.
    /// </summary>
    public Vector2 Size { get; set; } = Vector2.One;

    private static readonly Vector2[] _textureCoordinates = [new(1, 0), new(1, 1), new(0, 1), new(0, 0)];
    private static readonly Dictionary<Vector2, GeometricMeshData<VertexPositionNormalTexture>> _meshCache = [];

    /// <inheritdoc />
    protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
    {
        return New(Size, UvScale.X, UvScale.Y);
    }

    /// <summary>
    /// Creates (or retrieves from cache) a rectangle mesh of the given size and UV scale.
    /// </summary>
    public static GeometricMeshData<VertexPositionNormalTexture> New(Vector2 size, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        // Create a cache key that includes all parameters that affect the mesh
        var key = new Vector2(size.X * uScale, size.Y * vScale);

        if (toLeftHanded) key *= -1; // Distinguish handedness in cache

        if (!_meshCache.TryGetValue(key, out var mesh))
        {
            // Create and cache the mesh if not found
            mesh = CreateMesh(size, uScale, vScale, toLeftHanded);
            _meshCache[key] = mesh;
        }

        return mesh;
    }

    /// <summary>
    /// Builds a new rectangle mesh (no caching).
    /// </summary>
    public static GeometricMeshData<VertexPositionNormalTexture> CreateMesh(Vector2 size, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        Span<VertexPositionNormalTexture> vertices = stackalloc VertexPositionNormalTexture[4];
        Span<int> indices = stackalloc int[6];
        Span<Vector2> textureCoordinates = stackalloc Vector2[4];

        size /= 2.0f;
        var uvScale = new Vector2(uScale, vScale);

        for (var i = 0; i < 4; i++)
        {
            textureCoordinates[i] = _textureCoordinates[i] * uvScale;
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