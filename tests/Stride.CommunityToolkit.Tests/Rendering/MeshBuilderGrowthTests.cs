using Stride.CommunityToolkit.Rendering;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// <see cref="MeshBuilder"/> past its first 256 vertices and indices, where both pooled buffers grow.
/// </summary>
public class MeshBuilderGrowthTests
{
    [Theory]
    [InlineData(IndexingType.Int16)]
    [InlineData(IndexingType.Int32)]
    public void Buffers_Grow_AndEveryElementReadsBack(IndexingType indexType)
    {
        using var builder = new MeshBuilder { IndexType = indexType };
        var position = builder.WithPosition<Vector3>();

        for (var i = 0; i < 300; i++)
        {
            builder.AddVertex();
            builder.SetElement(position, new Vector3(i, 0f, 0f));
            builder.AddIndex(i);
        }

        Assert.Equal(300, builder.VertexCount);
        Assert.Equal(300, builder.IndexCount);

        for (var i = 0; i < 300; i++)
        {
            Assert.Equal(i, builder.GetIndex(i));
            Assert.Equal(new Vector3(i, 0f, 0f), builder.GetElement<Vector3>(i, position));
        }
    }

    [Fact]
    public void AddIndex_RejectsAVertexThatDoesNotExist()
    {
        using var builder = new MeshBuilder { IndexType = IndexingType.Int32 };

        builder.WithPosition<Vector3>();
        builder.AddVertex();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddIndex(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddIndex(-1));
    }
}