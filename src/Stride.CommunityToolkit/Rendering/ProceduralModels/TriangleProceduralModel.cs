using Stride.Graphics;
using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

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

        for (var i = 0; i < 3; i++)
        {
            textureCoordinates[i] = TextureCoordinates[i] * uvScale;
        }

        // Three vertices for triangle
        vertices[0] = new VertexPositionNormalTexture(new Vector3(0, size.Y, 0), Vector3.UnitZ, textureCoordinates[0]);
        vertices[1] = new VertexPositionNormalTexture(new Vector3(-size.X, -size.Y, 0), Vector3.UnitZ, textureCoordinates[1]);
        vertices[2] = new VertexPositionNormalTexture(new Vector3(size.X, -size.Y, 0), Vector3.UnitZ, textureCoordinates[2]);

        // One triangle
        indices[0] = 0;
        indices[1] = 2;
        indices[2] = 1;

        // Create the primitive object.
        return new GeometricMeshData<VertexPositionNormalTexture>(vertices.ToArray(), indices.ToArray(), toLeftHanded) { Name = "Triangle" };
    }
}