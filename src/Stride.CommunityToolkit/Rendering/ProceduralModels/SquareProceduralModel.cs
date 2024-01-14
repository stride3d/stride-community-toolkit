using Stride.Graphics;
using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

public class SquareProceduralModel : PrimitiveProceduralModelBase
{
    public Vector2 Size { get; set; } = Vector2.One;

    private static readonly Vector2[] TextureCoordinates = [new(1, 0), new(1, 1), new(0, 1), new(0, 0)];

    protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
    {
        return New(Size, UvScale.X, UvScale.Y);
    }

    public static GeometricMeshData<VertexPositionNormalTexture> New(Vector2 size, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        var vertices = new VertexPositionNormalTexture[4];
        var indices = new int[6];

        var textureCoordinates = new Vector2[4];

        for (var i = 0; i < 4; i++)
        {
            textureCoordinates[i] = TextureCoordinates[i] * new Vector2(uScale, vScale);
        }

        size /= 2.0f;

        // Four vertices per square.
        vertices[0] = new VertexPositionNormalTexture(new Vector3(-size.X, -size.Y, 0), Vector3.UnitZ, textureCoordinates[0]);
        vertices[1] = new VertexPositionNormalTexture(new Vector3(-size.X, size.Y, 0), Vector3.UnitZ, textureCoordinates[1]);
        vertices[2] = new VertexPositionNormalTexture(new Vector3(size.X, size.Y, 0), Vector3.UnitZ, textureCoordinates[2]);
        vertices[3] = new VertexPositionNormalTexture(new Vector3(size.X, -size.Y, 0), Vector3.UnitZ, textureCoordinates[3]);

        // Six indices (two triangles) per square.
        indices[0] = 0;
        indices[1] = 1;
        indices[2] = 2;
        indices[3] = 0;
        indices[4] = 2;
        indices[5] = 3;

        // Create the primitive object.
        return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, toLeftHanded) { Name = "Square" };
    }
}