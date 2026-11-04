using Stride.Graphics;
using Stride.Rendering.ProceduralModels;
using System.Collections.Concurrent;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// Generates an isosceles triangle primitive in the XY plane.
/// </summary>
public class TriangleProceduralModel : PrimitiveProceduralModelBase
{
    /// <summary>
    /// Base size of the triangle in local space.
    /// </summary>
    public Vector2 Size { get; set; } = Vector2.One;

    private static readonly Vector2[] _textureCoordinates = [new(0.5f, 1), new(0, 0), new(1, 0)];
    private static readonly ConcurrentDictionary<MeshCacheKey, GeometricMeshData<VertexPositionNormalTexture>> _meshCache = [];

    private readonly record struct MeshCacheKey(Vector2 Size, float UScale, float VScale, bool ToLeftHanded);

    /// <inheritdoc />
    protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        => CachedMeshData.ForModel(New(Size, UvScale.X, UvScale.Y), LocalOffset, Scale);

    /// <summary>
    /// Creates (or retrieves from cache) a triangle mesh of the given size and UV scale.
    /// </summary>
    /// <param name="size">The size of the triangle.</param>
    /// <param name="uScale">The U coordinate scale factor.</param>
    /// <param name="vScale">The V coordinate scale factor.</param>
    /// <param name="toLeftHanded">Whether to convert the mesh to left-handed coordinates.</param>
    /// <returns>A geometric mesh data instance representing the triangle.</returns>
    public static GeometricMeshData<VertexPositionNormalTexture> New(Vector2 size, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
        => _meshCache.GetOrAdd(new MeshCacheKey(size, uScale, vScale, toLeftHanded), _ => CreateMesh(size, uScale, vScale, toLeftHanded));

    /// <summary>
    /// Builds a new triangle mesh (no caching).
    /// </summary>
    /// <param name="size">The size of the triangle.</param>
    /// <param name="uScale">The U coordinate scale factor.</param>
    /// <param name="vScale">The V coordinate scale factor.</param>
    /// <param name="toLeftHanded">Whether to convert the mesh to left-handed coordinates.</param>
    /// <returns>A geometric mesh data instance representing the triangle.</returns>
    public static GeometricMeshData<VertexPositionNormalTexture> CreateMesh(Vector2 size, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        size /= 2.0f;
        var uvScale = new Vector2(uScale, vScale);

        // The apex first, then the base from left to right
        VertexPositionNormalTexture[] vertices =
        [
            new(new Vector3(0, size.Y, 0), Vector3.UnitZ, _textureCoordinates[0] * uvScale),
            new(new Vector3(-size.X, -size.Y, 0), Vector3.UnitZ, _textureCoordinates[1] * uvScale),
            new(new Vector3(size.X, -size.Y, 0), Vector3.UnitZ, _textureCoordinates[2] * uvScale),
        ];

        return new GeometricMeshData<VertexPositionNormalTexture>(vertices, [0, 2, 1], toLeftHanded) { Name = "Triangle" };
    }
}