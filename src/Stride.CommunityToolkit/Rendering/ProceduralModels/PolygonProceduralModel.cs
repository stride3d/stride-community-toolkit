using Stride.Graphics;
using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// Generates a planar polygon mesh (convex fan triangulation) from an arbitrary set of 2D vertices.
/// </summary>
public class PolygonProceduralModel : PrimitiveProceduralModelBase
{
    private Vector2[] _vertices = [];

    /// <summary>
    /// Gets or sets the vertex positions in the XY plane defining a custom polygon outline.
    /// </summary>
    /// <remarks>
    /// When empty, a regular polygon is generated from <see cref="Radius"/> and <see cref="Sides"/>.
    /// Custom vertices must contain at least 3 unique points and should be ordered around a convex outline.
    /// </remarks>
    public Vector2[] Vertices
    {
        get => _vertices;
        set => _vertices = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the circumradius of the regular polygon used when <see cref="Vertices"/> is empty.
    /// </summary>
    public float Radius { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the number of sides of the regular polygon used when <see cref="Vertices"/> is empty.
    /// </summary>
    /// <remarks>The value must be greater than or equal to 3.</remarks>
    public int Sides { get; set; } = 6;

    private static readonly Dictionary<string, GeometricMeshData<VertexPositionNormalTexture>> _meshCache = [];

    /// <inheritdoc />
    protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
    {
        if (Vertices.Length > 0)
            return New(Vertices, UvScale.X, UvScale.Y);

        return New(GenerateRegularPolygonVertices(Radius, Sides), UvScale.X, UvScale.Y);
    }

    /// <summary>
    /// Generates vertices for a regular polygon centered at the origin.
    /// </summary>
    /// <param name="radius">The polygon circumradius. Must be greater than 0.</param>
    /// <param name="sides">The number of polygon sides. Must be greater than or equal to 3.</param>
    /// <returns>The generated polygon vertices in the XY plane.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="radius"/> is not positive or <paramref name="sides"/> is less than 3.</exception>
    public static Vector2[] GenerateRegularPolygonVertices(float radius, int sides)
    {
        if (radius <= 0)
            throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be greater than 0.");

        if (sides < 3)
            throw new ArgumentOutOfRangeException(nameof(sides), "Sides must be greater than or equal to 3.");

        var vertices = new Vector2[sides];
        var angleStep = MathF.Tau / sides;

        for (var i = 0; i < sides; i++)
        {
            var angle = i * angleStep - MathF.PI / 2;
            vertices[i] = new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
        }

        return vertices;
    }

    /// <summary>
    /// Convenience factory for an isosceles triangle centered at the origin.
    /// </summary>
    /// <param name="size">The triangle width and height.</param>
    /// <returns>A polygon model configured with three triangle vertices.</returns>
    public static PolygonProceduralModel CreateTriangle(Vector2 size)
    {
        return new PolygonProceduralModel
        {
            Vertices =
            [
                new(0, size.Y / 2),
                new(-size.X / 2, -size.Y / 2),
                new(size.X / 2, -size.Y / 2)
            ]
        };
    }

    /// <summary>
    /// Convenience factory for an axis-aligned rectangle centered at the origin.
    /// </summary>
    /// <param name="size">The rectangle width and height.</param>
    /// <returns>A polygon model configured with four rectangle vertices.</returns>
    public static PolygonProceduralModel CreateRectangle(Vector2 size)
    {
        return new PolygonProceduralModel
        {
            Vertices =
            [
                new(-size.X / 2, -size.Y / 2),
                new(-size.X / 2, size.Y / 2),
                new(size.X / 2, size.Y / 2),
                new(size.X / 2, -size.Y / 2)
            ]
        };
    }

    /// <summary>
    /// Creates (or retrieves from cache) a mesh for the supplied polygon vertex list.
    /// </summary>
    /// <param name="vertices">The polygon vertices in the XY plane.</param>
    /// <param name="uScale">The U coordinate scale factor.</param>
    /// <param name="vScale">The V coordinate scale factor.</param>
    /// <param name="toLeftHanded">Whether to convert the mesh to left-handed coordinates.</param>
    /// <returns>A geometric mesh data instance representing the polygon.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="vertices"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="vertices"/> contains fewer than 3 points or duplicate points.</exception>
    public static GeometricMeshData<VertexPositionNormalTexture> New(Vector2[] vertices, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        ValidatePoints(vertices);

        var hash = string.Join(",", vertices.Select(vertex => $"{vertex.X},{vertex.Y}"));
        var cacheKey = $"{hash}_{uScale}_{vScale}_{toLeftHanded}";

        if (!_meshCache.TryGetValue(cacheKey, out var mesh))
        {
            mesh = CreateMesh(vertices, uScale, vScale, toLeftHanded);
            _meshCache[cacheKey] = mesh;
        }

        return mesh;
    }

    /// <summary>
    /// Builds a new mesh for the given points (no caching). Assumes convex ordering; uses fan triangulation.
    /// </summary>
    /// <param name="points">The polygon vertices in the XY plane.</param>
    /// <param name="uScale">The U coordinate scale factor.</param>
    /// <param name="vScale">The V coordinate scale factor.</param>
    /// <param name="toLeftHanded">Whether to convert the mesh to left-handed coordinates.</param>
    /// <returns>A geometric mesh data instance representing the polygon.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="points"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="points"/> contains fewer than 3 points, duplicate points, or does not span both axes.</exception>
    public static GeometricMeshData<VertexPositionNormalTexture> CreateMesh(Vector2[] points, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        ValidatePoints(points);

        var vertexCount = points.Length;

        Span<VertexPositionNormalTexture> vertices = new VertexPositionNormalTexture[vertexCount];
        Span<int> indices = new int[(vertexCount - 2) * 3];

        Vector2 centroid = Vector2.Zero;
        foreach (var point in points)
            centroid += point;

        centroid /= vertexCount;

        var maxX = 0f;
        var maxY = 0f;

        foreach (var point in points)
        {
            var relativePosition = point - centroid;
            maxX = Math.Max(maxX, Math.Abs(relativePosition.X));
            maxY = Math.Max(maxY, Math.Abs(relativePosition.Y));
        }

        if (maxX <= 0 || maxY <= 0)
            throw new ArgumentException("Polygon points must span both X and Y axes.", nameof(points));

        for (var i = 0; i < vertexCount; i++)
        {
            var relativePosition = points[i] - centroid;

            Vector2 textureCoordinates = new(
                (relativePosition.X / maxX + 1) * 0.5f * uScale,
                (relativePosition.Y / maxY + 1) * 0.5f * vScale
            );

            vertices[i] = new VertexPositionNormalTexture(
                new Vector3(points[i].X, points[i].Y, 0),
                Vector3.UnitZ,
                textureCoordinates
            );
        }

        for (var i = 0; i < vertexCount - 2; i++)
        {
            indices[i * 3] = 0;
            indices[i * 3 + 1] = i + 2;
            indices[i * 3 + 2] = i + 1;
        }

        return new GeometricMeshData<VertexPositionNormalTexture>(vertices.ToArray(), indices.ToArray(), toLeftHanded) { Name = "Polygon" };
    }

    private static void ValidatePoints(Vector2[] points)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (points.Length < 3)
            throw new ArgumentException("A polygon must have at least 3 vertices.", nameof(points));

        if (points.Distinct().Count() != points.Length)
            throw new ArgumentException("Polygon vertices must be unique.", nameof(points));
    }
}