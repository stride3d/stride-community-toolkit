using Stride.Graphics;
using Stride.Rendering.ProceduralModels;
using System.Collections.Concurrent;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// Procedurally generates a filled 2D circle mesh oriented on the XY plane.
/// Meshes are cached per (radius, tessellation, UV scale, handedness) for reuse.
/// </summary>
public class CircleProceduralModel : PrimitiveProceduralModelBase
{
    /// <summary>
    /// Circle radius.
    /// </summary>
    public float Radius { get; set; } = 0.5f;

    /// <summary>
    /// Number of perimeter segments (higher = smoother circle).
    /// </summary>
    public int Tessellation { get; set; } = 32;

    private static readonly ConcurrentDictionary<(float Radius, int Tessellation, float UScale, float VScale, bool IsLeftHanded), GeometricMeshData<VertexPositionNormalTexture>> _meshCache = [];

    /// <summary>
    /// Creates mesh data for the current radius, tessellation, and UV scale settings.
    /// </summary>
    protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData() => CachedMeshData.ForModel(New(Radius, Tessellation, UvScale.X, UvScale.Y), LocalOffset, Scale);

    /// <summary>
    /// Retrieves (or builds and caches) a circle mesh with the specified parameters.
    /// </summary>
    /// <param name="radius">Radius of the circle.</param>
    /// <param name="tessellation">Number of segments around the perimeter.</param>
    /// <param name="uScale">Texture U scale factor.</param>
    /// <param name="vScale">Texture V scale factor.</param>
    /// <param name="toLeftHanded">If true, converts to left-handed coordinate system.</param>
    /// <returns>Mesh data for a circle.</returns>
    public static GeometricMeshData<VertexPositionNormalTexture> New(float radius = 0.5f, int tessellation = 32, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
        => _meshCache.GetOrAdd((radius, tessellation, uScale, vScale, toLeftHanded), _ => CreateMesh(radius, tessellation, uScale, vScale, toLeftHanded));

    /// <summary>
    /// Builds a new circle mesh (without caching) using provided parameters.
    /// </summary>
    /// <inheritdoc cref="New(float, int, float, float, bool)"/>
    public static GeometricMeshData<VertexPositionNormalTexture> CreateMesh(float radius = 0.5f, int tessellation = 32, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        var vertices = new VertexPositionNormalTexture[tessellation + 1];
        var indices = new int[tessellation * 3];

        var normal = Vector3.UnitZ;
        var uvScale = new Vector2(uScale, vScale);

        // The centre, then the perimeter counter-clockwise from +X; the texture maps the unit circle to its middle
        vertices[0] = new(Vector3.Zero, normal, new Vector2(0.5f, 0.5f) * uvScale);

        for (var i = 0; i < tessellation; i++)
        {
            var angle = i * MathUtil.TwoPi / tessellation;
            var cos = MathF.Cos(angle);
            var sin = MathF.Sin(angle);

            vertices[i + 1] = new(new Vector3(radius * cos, radius * sin, 0), normal, new Vector2(0.5f + cos * 0.5f, 0.5f + sin * 0.5f) * uvScale);
        }

        // Create triangles
        for (var i = 0; i < tessellation; i++)
        {
            indices[i * 3] = 0; // Center vertex
            indices[i * 3 + 1] = 1 + ((i + 1) % tessellation);
            indices[i * 3 + 2] = 1 + i;
        }

        return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, toLeftHanded) { Name = "Circle" };
    }
}