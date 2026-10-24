using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The flat primitives and the prism: vertex and index counts, winding, bounds and normals, so a
/// rewrite of a mesh function cannot change the geometry unnoticed.
/// </summary>
public class ProceduralModel2DTests
{
    [Theory]
    [InlineData(4)]
    [InlineData(16)]
    [InlineData(32)]
    public void Capsule_IsTwoFansAndARectangle(int tessellation)
    {
        // Even tessellations, so the arcs sample their topmost and lowest points exactly
        var mesh = Capsule2DProceduralModel.CreateMesh(1.2f, 0.35f, tessellation);

        Assert.Equal((tessellation + 2) * 2 + 4, mesh.Vertices.Length);
        Assert.Equal(tessellation * 6 + 6, mesh.Indices.Length);
        AssertFacesTheViewer(mesh);
        AssertBounds(mesh, new Vector2(0.35f, 0.6f));
    }

    [Fact]
    public void Capsule_NeverLosesItsRectangle()
    {
        // A height below twice the radius is stretched until a straight side of 0.01 remains
        var mesh = Capsule2DProceduralModel.CreateMesh(0.1f, 0.5f, 8);

        AssertBounds(mesh, new Vector2(0.5f, 0.505f));
    }

    [Fact]
    public void Circle_IsOneFan()
    {
        var mesh = CircleProceduralModel.CreateMesh(0.5f, 12);

        Assert.Equal(13, mesh.Vertices.Length);
        Assert.Equal(36, mesh.Indices.Length);
        AssertFacesTheViewer(mesh);
        AssertBounds(mesh, new Vector2(0.5f, 0.5f));
    }

    [Fact]
    public void Rectangle_IsTwoTriangles_WithTheTextureScaledAcrossIt()
    {
        var mesh = RectangleProceduralModel.CreateMesh(new Vector2(2f, 1f), 2f, 1f);

        Assert.Equal(4, mesh.Vertices.Length);
        Assert.Equal([0, 1, 2, 0, 2, 3], mesh.Indices);
        AssertFacesTheViewer(mesh);
        AssertBounds(mesh, new Vector2(1f, 0.5f));
        Assert.All(mesh.Vertices, vertex => Assert.InRange(vertex.TextureCoordinate.X, 0f, 2f));
        Assert.All(mesh.Vertices, vertex => Assert.InRange(vertex.TextureCoordinate.Y, 0f, 1f));
    }

    [Fact]
    public void Triangle_IsOneTriangle_ApexUp()
    {
        var mesh = TriangleProceduralModel.CreateMesh(new Vector2(1f, 1f));

        Assert.Equal(3, mesh.Vertices.Length);
        Assert.Equal([0, 2, 1], mesh.Indices);
        Assert.Equal(new Vector3(0f, 0.5f, 0f), mesh.Vertices[0].Position);
        AssertFacesTheViewer(mesh);
        AssertBounds(mesh, new Vector2(0.5f, 0.5f));
    }

    [Fact]
    public void Polygon_FansFromItsFirstCorner()
    {
        var mesh = PolygonProceduralModel.CreateMesh(PolygonProceduralModel.GenerateRegularPolygonVertices(0.5f, 6));

        Assert.Equal(6, mesh.Vertices.Length);
        Assert.Equal(12, mesh.Indices.Length);
        AssertFacesTheViewer(mesh);
    }

    [Fact]
    public void TriangularPrism_HasOutwardNormals()
    {
        var mesh = TriangularPrismProceduralModel.New(new Vector3(1f, 1f, 1f));

        Assert.Equal(18, mesh.Vertices.Length);
        Assert.Equal(24, mesh.Indices.Length);

        // The prism is centred on the origin, so every face's normal points the way its centre lies
        for (var i = 0; i < mesh.Indices.Length; i += 3)
        {
            var a = mesh.Vertices[mesh.Indices[i]];
            var b = mesh.Vertices[mesh.Indices[i + 1]];
            var c = mesh.Vertices[mesh.Indices[i + 2]];
            var centre = (a.Position + b.Position + c.Position) / 3f;

            Assert.True(Vector3.Dot(a.Normal, centre) > 0f, $"Triangle {i / 3} has its normal pointing inwards");
            Assert.Equal(1f, a.Normal.Length(), 4);
        }
    }

    /// <summary>
    /// Every triangle winds clockwise as seen from +Z, which is the front face for Stride's default
    /// rasterizer, and every normal points at the viewer.
    /// </summary>
    private static void AssertFacesTheViewer(GeometricMeshData<VertexPositionNormalTexture> mesh)
    {
        for (var i = 0; i < mesh.Indices.Length; i += 3)
        {
            var a = mesh.Vertices[mesh.Indices[i]].Position;
            var b = mesh.Vertices[mesh.Indices[i + 1]].Position;
            var c = mesh.Vertices[mesh.Indices[i + 2]].Position;
            var turn = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

            Assert.True(turn < 0f, $"Triangle {i / 3} winds counter-clockwise");
        }

        Assert.All(mesh.Vertices, vertex => Assert.Equal(Vector3.UnitZ, vertex.Normal));
    }

    /// <summary>The mesh fills exactly the box from -<paramref name="halfExtents"/> to +<paramref name="halfExtents"/> in XY, flat at Z = 0.</summary>
    private static void AssertBounds(GeometricMeshData<VertexPositionNormalTexture> mesh, Vector2 halfExtents)
    {
        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);

        foreach (var vertex in mesh.Vertices)
        {
            min = Vector3.Min(min, vertex.Position);
            max = Vector3.Max(max, vertex.Position);
        }

        Assert.Equal(-halfExtents.X, min.X, 4);
        Assert.Equal(-halfExtents.Y, min.Y, 4);
        Assert.Equal(halfExtents.X, max.X, 4);
        Assert.Equal(halfExtents.Y, max.Y, 4);
        Assert.Equal(0f, min.Z);
        Assert.Equal(0f, max.Z);
    }
}