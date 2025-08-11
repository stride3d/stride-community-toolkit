using Stride.Graphics;
using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// A triangular prism with a triangular face visible from the side.
/// </summary>
public class TriangularPrismProceduralModel : PrimitiveProceduralModelBase
{
    /// <summary>
    /// Overall size of the prism in X (triangle base width), Y (triangle height) and Z (depth).
    /// </summary>
    public Vector3 Size { get; set; } = Vector3.One;

    private static readonly Vector2[] TextureCoordinates = [new(1, 0), new(1, 1), new(0, 1), new(0, 0)];

    /// <summary>
    /// Builds the mesh data for the current <see cref="Size"/> and UV scale settings.
    /// </summary>
    /// <returns>The generated geometric mesh data.</returns>
    protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData() => New(Size, UvScale.X, UvScale.Y);

    /// <summary>
    /// Creates a triangular prism mesh.
    /// </summary>
    /// <param name="size">Overall size (X = base width, Y = triangle height, Z = depth).</param>
    /// <param name="uScale">Optional U texture scale.</param>
    /// <param name="vScale">Optional V texture scale.</param>
    /// <param name="toLeftHanded">If true, flips winding for left-handed coordinate systems.</param>
    /// <returns>Generated mesh data for a triangular prism.</returns>
    public static GeometricMeshData<VertexPositionNormalTexture> New(Vector3 size, float uScale = 1.0f, float vScale = 1.0f, bool toLeftHanded = false)
    {
        // There are 3 vertices for each of the 2 triangle faces (6 total), and 4 vertices for each of the 3 rectangle faces (12 total).
        var vertices = new VertexPositionNormalTexture[18];

        // There are 3 indices for each triangle face (6 total), and 6 indices (2 triangles) for each rectangle face (18 total).
        var indices = new int[24];

        var textureCoordinates = new Vector2[4];

        for (var i = 0; i < 4; i++)
        {
            textureCoordinates[i] = TextureCoordinates[i] * new Vector2(uScale, vScale);
        }

        var equilateralHeight = (float)Math.Sqrt(size.X * size.X - Math.Pow(size.X / 2, 2)) / 2;

        size /= 2.0f;

        // Calculate the height of the equilateral triangle
        //var apex = (float)equilateralHeight - size.Y;

        // Vertices for the two triangle faces
        // Triangle face 1 (front)
        vertices[0] = new VertexPositionNormalTexture(new Vector3(-size.X, -equilateralHeight, size.Z), Vector3.UnitZ, textureCoordinates[0]);
        vertices[1] = new VertexPositionNormalTexture(new Vector3(0, equilateralHeight, size.Z), Vector3.UnitZ, textureCoordinates[1]);
        vertices[2] = new VertexPositionNormalTexture(new Vector3(size.X, -equilateralHeight, size.Z), Vector3.UnitZ, textureCoordinates[2]);

        // Triangle face 2 (back)
        vertices[3] = new VertexPositionNormalTexture(new Vector3(-size.X, -equilateralHeight, -size.Z), -Vector3.UnitZ, textureCoordinates[0]);
        vertices[4] = new VertexPositionNormalTexture(new Vector3(0, equilateralHeight, -size.Z), -Vector3.UnitZ, textureCoordinates[1]);
        vertices[5] = new VertexPositionNormalTexture(new Vector3(size.X, -equilateralHeight, -size.Z), -Vector3.UnitZ, textureCoordinates[2]);


        // Vertices for the three rectangle faces
        // Rectangle face 1 (bottom)
        vertices[6] = new VertexPositionNormalTexture(new Vector3(-size.X, -equilateralHeight, size.Z), -Vector3.UnitY, textureCoordinates[0]);
        vertices[7] = new VertexPositionNormalTexture(new Vector3(-size.X, -equilateralHeight, -size.Z), -Vector3.UnitY, textureCoordinates[1]);
        vertices[8] = new VertexPositionNormalTexture(new Vector3(size.X, -equilateralHeight, -size.Z), -Vector3.UnitY, textureCoordinates[2]);
        vertices[9] = new VertexPositionNormalTexture(new Vector3(size.X, -equilateralHeight, size.Z), -Vector3.UnitY, textureCoordinates[3]);

        // Rectangle face 2 (left side)
        vertices[10] = new VertexPositionNormalTexture(new Vector3(-size.X, -equilateralHeight, size.Z), -Vector3.UnitX, textureCoordinates[0]);
        vertices[11] = new VertexPositionNormalTexture(new Vector3(-size.X, -equilateralHeight, -size.Z), -Vector3.UnitX, textureCoordinates[1]);
        vertices[12] = new VertexPositionNormalTexture(new Vector3(0, equilateralHeight, -size.Z), -Vector3.UnitX, textureCoordinates[2]);
        vertices[13] = new VertexPositionNormalTexture(new Vector3(0, equilateralHeight, size.Z), -Vector3.UnitX, textureCoordinates[3]);

        // Rectangle face 3 (right side)
        vertices[14] = new VertexPositionNormalTexture(new Vector3(size.X, -equilateralHeight, size.Z), Vector3.UnitX, textureCoordinates[0]);
        vertices[15] = new VertexPositionNormalTexture(new Vector3(0, equilateralHeight, size.Z), Vector3.UnitX, textureCoordinates[1]);
        vertices[16] = new VertexPositionNormalTexture(new Vector3(0, equilateralHeight, -size.Z), Vector3.UnitX, textureCoordinates[2]);
        vertices[17] = new VertexPositionNormalTexture(new Vector3(size.X, -equilateralHeight, -size.Z), Vector3.UnitX, textureCoordinates[3]);

        //// Triangle face indices
        indices[0] = 0; indices[1] = 1; indices[2] = 2; // Front
        indices[3] = 3; indices[4] = 5; indices[5] = 4; // Back

        //// Rectangle face indices
        // Bottom
        indices[6] = 6; indices[7] = 9; indices[8] = 8;
        indices[9] = 6; indices[10] = 8; indices[11] = 7;

        // Left
        indices[12] = 10; indices[13] = 11; indices[14] = 12;
        indices[15] = 10; indices[16] = 12; indices[17] = 13;

        // Right
        indices[18] = 14; indices[19] = 15; indices[20] = 16;
        indices[21] = 14; indices[22] = 16; indices[23] = 17;

        return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, toLeftHanded) { Name = "TriangularPrism" };
    }
}