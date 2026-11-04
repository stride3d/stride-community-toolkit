using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The cached 2D meshes are shared only while nothing will mutate them: a model at its default offset and scale gets
/// the cached instance, one with an offset or a scale gets its own vertices, since the base class applies both in place.
/// </summary>
public class CachedMeshDataTests
{
    [Fact]
    public void DefaultModelSharesTheCachedMesh()
    {
        var model = new ExposedCircle();

        Assert.Same(CircleProceduralModel.New(), model.MeshData());
    }

    [Fact]
    public void ScaledModelGetsItsOwnVertices()
    {
        var cached = CircleProceduralModel.New();
        var model = new ExposedCircle { Scale = new Vector3(2f) };

        var data = model.MeshData();

        Assert.NotSame(cached, data);
        Assert.NotSame(cached.Vertices, data.Vertices);
        Assert.Equal(cached.Vertices.Length, data.Vertices.Length);
        Assert.Same(cached.Indices, data.Indices);
    }

    [Fact]
    public void OffsetModelGetsItsOwnVertices()
    {
        var cached = CircleProceduralModel.New();
        var model = new ExposedCircle { LocalOffset = new Vector3(1f, 0f, 0f) };

        Assert.NotSame(cached.Vertices, model.MeshData().Vertices);
    }

    private sealed class ExposedCircle : CircleProceduralModel
    {
        public GeometricMeshData<VertexPositionNormalTexture> MeshData() => CreatePrimitiveMeshData();
    }
}