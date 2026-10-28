using Stride.CommunityToolkit.Rendering;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Xunit;
using Half = Stride.Core.Mathematics.Half;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// Covers the CPU side of <see cref="MeshBuilder"/>: element registration, stride and padding,
/// vertex and index bookkeeping, and the bounds that keep writes inside the mesh.
/// </summary>
/// <remarks>
/// <c>ToMeshDraw</c> needs a graphics device and is exercised by the three Example05 mesh examples
/// instead. Everything before it is plain buffer arithmetic, which is where the historical bugs
/// lived - convenience wrappers dropping their <c>pixelFormat</c>, and bounds checks that were off
/// by one, letting a caller silently write one vertex past the end of the mesh.
/// </remarks>
public class MeshBuilderTests
{
    // --- Element registration -------------------------------------------------------------------

    [Fact]
    public void ExplicitPixelFormatIsKeptByWithElement()
    {
        using var builder = new MeshBuilder();

        builder.WithElement<Vector3>(0, "POSITION", PixelFormat.R32G32B32A32_Float);

        Assert.Equal(PixelFormat.R32G32B32A32_Float, builder.VertexElements[0].VertexElement.Format);
    }

    [Fact]
    public void ConvenienceWrappersForwardTheirPixelFormat()
    {
        // Every wrapper used to accept a pixelFormat parameter and silently drop it, which made the
        // documented escape hatch for types ConvertTypeToFormat cannot map unreachable
        using var builder = new MeshBuilder();

        var position = builder.WithPosition<Vector3>(pixelFormat: PixelFormat.R32G32B32A32_Float);
        var normal = builder.WithNormal<Vector3>(pixelFormat: PixelFormat.R16G16B16A16_Float);
        var color = builder.WithColor<Color>(pixelFormat: PixelFormat.B8G8R8A8_UNorm);
        var texture = builder.WithTextureCoordinate<Vector2>(pixelFormat: PixelFormat.R16G16_Float);
        var tangent = builder.WithTangent<Vector3>(pixelFormat: PixelFormat.R32G32B32A32_Float);
        var biTangent = builder.WithBiTangent<Vector3>(pixelFormat: PixelFormat.R32G32B32A32_Float);
        var transformed = builder.WithPositionTransformed<Vector4>(pixelFormat: PixelFormat.R16G16B16A16_Float);

        Assert.Equal(PixelFormat.R32G32B32A32_Float, builder.VertexElements[position].VertexElement.Format);
        Assert.Equal(PixelFormat.R16G16B16A16_Float, builder.VertexElements[normal].VertexElement.Format);
        Assert.Equal(PixelFormat.B8G8R8A8_UNorm, builder.VertexElements[color].VertexElement.Format);
        Assert.Equal(PixelFormat.R16G16_Float, builder.VertexElements[texture].VertexElement.Format);
        Assert.Equal(PixelFormat.R32G32B32A32_Float, builder.VertexElements[tangent].VertexElement.Format);
        Assert.Equal(PixelFormat.R32G32B32A32_Float, builder.VertexElements[biTangent].VertexElement.Format);
        Assert.Equal(PixelFormat.R16G16B16A16_Float, builder.VertexElements[transformed].VertexElement.Format);
    }

    [Fact]
    public void ElementOffsetsFollowDeclarationOrder()
    {
        using var builder = new MeshBuilder();

        var position = builder.WithPosition<Vector3>();
        var color = builder.WithColor<Color>();

        Assert.Equal(0, builder.VertexElements[position].Offset);
        Assert.Equal(12, builder.VertexElements[position].Size);
        Assert.Equal(12, builder.VertexElements[color].Offset);
        Assert.Equal(4, builder.VertexElements[color].Size);
    }

    [Fact]
    public void ElementSizesArePaddedToMultiplesOfFour()
    {
        // Stride requires element offsets to be 4-byte aligned, so a 2-byte Half has to occupy a
        // 4-byte slot; the next element's offset is what makes the padding observable
        using var builder = new MeshBuilder();

        builder.WithElement<Half>(0, "PSIZE", PixelFormat.R16_Float);
        var next = builder.WithColor<Color>();

        Assert.Equal(4, builder.VertexElements[next].Offset);
    }

    [Fact]
    public void ElementsCannotBeAddedAfterVertices()
    {
        using var builder = new MeshBuilder();

        builder.WithPosition<Vector3>();
        builder.AddVertex();

        Assert.Throws<InvalidOperationException>(() => builder.WithColor<Color>());
        Assert.Throws<InvalidOperationException>(() => builder.WithPrimitiveType(PrimitiveType.LineList));
        Assert.Throws<InvalidOperationException>(() => builder.WithIndexType(IndexingType.Int32));
    }

    // --- Vertex data ----------------------------------------------------------------------------

    [Fact]
    public void ElementValuesRoundTrip()
    {
        using var builder = new MeshBuilder();

        var position = builder.WithPosition<Vector3>();
        var color = builder.WithColor<Color>();

        builder.AddVertex();
        builder.SetElement(position, new Vector3(1, 2, 3));
        builder.SetElement(color, Color.Red);

        Assert.Equal(new Vector3(1, 2, 3), builder.GetElement<Vector3>(position));
        Assert.Equal(Color.Red, builder.GetElement<Color>(color));
    }

    [Fact]
    public void ValuesSurviveTheBufferGrowingPastItsInitialCapacity()
    {
        // The vertex buffer starts at a 256-vertex capacity and doubles by copying into a freshly
        // rented array; the values on both sides of that copy have to come through intact
        using var builder = new MeshBuilder();

        var position = builder.WithPosition<Vector3>();

        for (var i = 0; i < 600; i++)
        {
            builder.AddVertex();
            builder.SetElement(position, new Vector3(i, 2f * i, 3f * i));
        }

        Assert.Equal(600, builder.VertexCount);

        foreach (var i in new[] { 0, 255, 256, 511, 512, 599 })
        {
            Assert.Equal(new Vector3(i, 2f * i, 3f * i), builder.GetElement<Vector3>(i, position));
        }
    }

    [Fact]
    public void WritingPastTheLastVertexThrows()
    {
        // The bounds check used to be off by one: vertex index == VertexCount slipped through and
        // wrote into pooled-array slack - no exception, just corruption waiting for the next renter
        using var builder = new MeshBuilder();

        var position = builder.WithPosition<Vector3>();

        builder.AddVertex();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetElement(1, position, Vector3.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.GetElement<Vector3>(1, position));
    }

    [Fact]
    public void UnknownElementIndexThrows()
    {
        using var builder = new MeshBuilder();

        builder.WithPosition<Vector3>();
        builder.AddVertex();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.GetElement<Vector3>(0, elementIndex: 1));
    }

    [Fact]
    public void MismatchedElementSizeThrows()
    {
        using var builder = new MeshBuilder();

        var position = builder.WithPosition<Vector3>();

        builder.AddVertex();

        Assert.Throws<ArgumentException>(() => builder.SetElement(position, new Vector2(1, 2)));
    }

    // --- Indices --------------------------------------------------------------------------------

    [Fact]
    public void IndexMustReferenceAnExistingVertex()
    {
        // Index == VertexCount used to be accepted, leaving an index that points one past the mesh
        using var builder = new MeshBuilder { IndexType = IndexingType.Int32 };

        builder.WithPosition<Vector3>();
        builder.AddVertex();

        builder.AddIndex(0);

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddIndex(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddIndex(-1));
        Assert.Equal(1, builder.IndexCount);
    }

    [Fact]
    public void AddIndexWithoutIndexingConfiguredThrows()
    {
        using var builder = new MeshBuilder();

        builder.WithPosition<Vector3>();
        builder.AddVertex();

        Assert.Throws<InvalidOperationException>(() => builder.AddIndex(0));
    }

    [Fact]
    public void Int16IndexingRejectsIndicesBeyondShortRange()
    {
        using var builder = new MeshBuilder { IndexType = IndexingType.Int16 };

        // No elements registered, so vertices cost nothing and 40k of them are cheap to add
        for (var i = 0; i <= 40_000; i++) builder.AddVertex();

        builder.AddIndex(short.MaxValue);

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddIndex(short.MaxValue + 1));
    }

    [Fact]
    public void IndexCountGrowsPastTheInitialCapacity()
    {
        using var builder = new MeshBuilder { IndexType = IndexingType.Int32 };

        builder.WithPosition<Vector3>();
        builder.AddVertex();

        for (var i = 0; i < 600; i++) builder.AddIndex(0);

        Assert.Equal(600, builder.IndexCount);
    }

    // --- Lifecycle ------------------------------------------------------------------------------

    [Fact]
    public void ClearResetsEverythingAndAllowsReconfiguration()
    {
        using var builder = new MeshBuilder { IndexType = IndexingType.Int16 };

        builder.WithPosition<Vector3>();
        builder.AddVertex();
        builder.AddIndex(0);

        builder.Clear();

        Assert.Equal(0, builder.VertexCount);
        Assert.Equal(0, builder.IndexCount);
        Assert.Empty(builder.VertexElements);
        Assert.Equal(IndexingType.None, builder.IndexType);

        // A cleared builder is a fresh builder: reconfiguration must be allowed again
        builder.WithIndexType(IndexingType.Int32);
        builder.WithColor<Color>();
        builder.AddVertex();

        Assert.Equal(1, builder.VertexCount);
    }
}