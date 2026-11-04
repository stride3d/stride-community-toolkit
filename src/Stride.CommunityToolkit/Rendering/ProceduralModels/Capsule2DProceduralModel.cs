using Stride.Graphics;
using Stride.Rendering.ProceduralModels;
using System.Collections.Concurrent;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// Procedurally generates a 2D capsule (stadium) mesh composed of two semicircles and a central rectangle.
/// Mesh instances are cached per dimension/tessellation/UV combination for reuse.
/// </summary>
public class Capsule2DProceduralModel : PrimitiveProceduralModelBase
{
    /// <summary>
    /// Gets or sets the height of the capsule: the whole shape end to end, the two semicircles included.
    /// </summary>
    public float Height { get; set; } = 1.2f;

    /// <summary>
    /// Gets or sets the radius of the capsule.
    /// </summary>
    public float Radius { get; set; } = 0.35f;

    /// <summary>
    /// Gets or sets the tessellation for the semicircle ends (higher values = smoother curves).
    /// </summary>
    public int Tessellation { get; set; } = 16;

    private static readonly ConcurrentDictionary<(float Height, float Radius, int Tessellation, float UScale, float VScale, bool IsLeftHanded), GeometricMeshData<VertexPositionNormalTexture>> _meshCache = [];

    /// <summary>
    /// Creates mesh data using current property values, honoring UV scale.
    /// </summary>
    protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData() => CachedMeshData.ForModel(New(Height, Radius, Tessellation, UvScale.X, UvScale.Y), LocalOffset, Scale);

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
        => _meshCache.GetOrAdd((height, radius, tessellation, uScale, vScale, toLeftHanded), _ => CreateMesh(height, radius, tessellation, uScale, vScale, toLeftHanded));

    /// <summary>
    /// Builds a new capsule mesh (without caching) using provided parameters.
    /// </summary>
    /// <inheritdoc cref="New(float, float, int, float, float, bool)"/>
    public static GeometricMeshData<VertexPositionNormalTexture> CreateMesh(float height = 1.0f, float radius = 0.25f, int tessellation = 16, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        if (radius <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be greater than zero.");
        }

        // Each semicircle maps to a quarter of the texture's height, the rectangle to the half between them
        const float uvCenterX = 0.5f;
        const float uvTopY = 0.75f;
        const float uvBottomY = 0.25f;
        const float uvSemicircleHeight = 0.25f;

        tessellation = Math.Max(4, tessellation);

        // The straight part is never shorter than a sliver, so the shape always has its rectangle
        var halfRectHeight = Math.Max(0.01f, Math.Max(height, radius * 2.01f) - 2 * radius) * 0.5f;
        var uvScale = new Vector2(uScale, vScale);
        var normal = Vector3.UnitZ;

        // Two fans of tessellation triangles each, and a rectangle of two
        var vertices = new VertexPositionNormalTexture[(tessellation + 2) * 2 + 4];
        var indices = new int[tessellation * 6 + 6];
        var vertex = 0;
        var index = 0;

        // Top semicircle: the arc from +X round to -X
        var top = Semicircle(new Vector3(0, halfRectHeight, 0), 0f, uvTopY);

        // The rectangle between the two centres: top-left, bottom-left, bottom-right, top-right
        var rectangle = vertex;

        vertices[vertex++] = new(new Vector3(-radius, halfRectHeight, 0), normal, new Vector2(0, uvTopY) * uvScale);
        vertices[vertex++] = new(new Vector3(-radius, -halfRectHeight, 0), normal, new Vector2(0, uvBottomY) * uvScale);
        vertices[vertex++] = new(new Vector3(radius, -halfRectHeight, 0), normal, new Vector2(1, uvBottomY) * uvScale);
        vertices[vertex++] = new(new Vector3(radius, halfRectHeight, 0), normal, new Vector2(1, uvTopY) * uvScale);

        // Bottom semicircle: the arc from -X round to +X
        var bottom = Semicircle(new Vector3(0, -halfRectHeight, 0), MathF.PI, uvBottomY);

        Fan(top);

        indices[index++] = rectangle;
        indices[index++] = rectangle + 2;
        indices[index++] = rectangle + 1;
        indices[index++] = rectangle;
        indices[index++] = rectangle + 3;
        indices[index++] = rectangle + 2;

        Fan(bottom);

        return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, toLeftHanded) { Name = "Capsule2D" };

        // The centre, then tessellation + 1 points along the arc; returns the centre's index
        int Semicircle(Vector3 center, float startAngle, float uvY)
        {
            var centerIndex = vertex;

            vertices[vertex++] = new(center, normal, new Vector2(uvCenterX, uvY) * uvScale);

            for (var i = 0; i <= tessellation; i++)
            {
                var angle = startAngle + i * MathF.PI / tessellation;
                var cos = MathF.Cos(angle);
                var sin = MathF.Sin(angle);

                vertices[vertex++] = new(center + new Vector3(radius * cos, radius * sin, 0), normal, new Vector2(uvCenterX + cos * 0.5f, uvY + sin * uvSemicircleHeight) * uvScale);
            }

            return centerIndex;
        }

        // One triangle per arc segment, wound to face +Z
        void Fan(int centerIndex)
        {
            for (var i = 0; i < tessellation; i++)
            {
                indices[index++] = centerIndex;
                indices[index++] = centerIndex + i + 2;
                indices[index++] = centerIndex + i + 1;
            }
        }
    }
}