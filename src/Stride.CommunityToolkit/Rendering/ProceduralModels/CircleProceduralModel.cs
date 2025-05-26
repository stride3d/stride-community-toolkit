using Stride.Graphics;
using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

public class CircleProceduralModel : PrimitiveProceduralModelBase
{
    public float Radius { get; set; } = 0.5f;
    public int Tessellation { get; set; } = 32;

    private static readonly Dictionary<(float Radius, int Tessellation, float UScale, float VScale, bool IsLeftHanded), GeometricMeshData<VertexPositionNormalTexture>> MeshCache = [];

    protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
    {
        return New(Radius, Tessellation, UvScale.X, UvScale.Y);
    }

    public static GeometricMeshData<VertexPositionNormalTexture> New(float radius = 0.5f, int tessellation = 32, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        var cacheKey = (radius, tessellation, uScale, vScale, toLeftHanded);

        if (!MeshCache.TryGetValue(cacheKey, out var mesh))
        {
            mesh = CreateMesh(radius, tessellation, uScale, vScale, toLeftHanded);
            MeshCache[cacheKey] = mesh;
        }

        return mesh;
    }

    public static GeometricMeshData<VertexPositionNormalTexture> CreateMesh(float radius = 0.5f, int tessellation = 32, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        // Use stack allocation for small arrays
        Span<VertexPositionNormalTexture> vertices = tessellation <= 128
            ? stackalloc VertexPositionNormalTexture[tessellation + 1]
            : new VertexPositionNormalTexture[tessellation + 1];

        Span<int> indices = tessellation <= 128
            ? stackalloc int[tessellation * 3]
            : new int[tessellation * 3];

        Vector3 normal = Vector3.UnitZ;

        // Center vertex
        vertices[0] = new VertexPositionNormalTexture(Vector3.Zero, normal, new Vector2(0.5f, 0.5f) * new Vector2(uScale, vScale));

        // Create perimeter vertices - FLIPPED X and Y coordinates
        for (int i = 0; i < tessellation; i++)
        {
            float angle = (float)(i * 2.0 * Math.PI / tessellation);

            // The key change: swap X and Y coordinates and negate one coordinate
            // to match the orientation of your Rectangle model
            float x = radius * (float)Math.Cos(angle);
            float y = radius * (float)Math.Sin(angle);

            Vector2 texCoord = new Vector2(
                0.5f + ((float)Math.Cos(angle) * 0.5f),
                0.5f + ((float)Math.Sin(angle) * 0.5f)
            );

            vertices[i + 1] = new VertexPositionNormalTexture(
                new Vector3(x, y, 0),
                normal,
                texCoord * new Vector2(uScale, vScale)
            );
        }

        // Create triangles - REVERSE the winding order to flip the circle
        for (int i = 0; i < tessellation; i++)
        {
            indices[i * 3] = 0; // Center vertex

            // Reversed winding order
            if (toLeftHanded)
            {
                indices[i * 3 + 1] = 1 + i;
                indices[i * 3 + 2] = 1 + ((i + 1) % tessellation);
            }
            else
            {
                indices[i * 3 + 1] = 1 + ((i + 1) % tessellation);
                indices[i * 3 + 2] = 1 + i;
            }
        }

        return new GeometricMeshData<VertexPositionNormalTexture>(vertices.ToArray(), indices.ToArray(), toLeftHanded) { Name = "Circle" };
    }
}