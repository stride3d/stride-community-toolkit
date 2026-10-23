using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Text.Meshes;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// Covers the ear-clipping triangulator, with and without holes, and the extrusion built on it.
/// </summary>
public class EarClippingTests
{
    /// <summary>
    /// The invariant that catches almost everything at once: a correct triangulation of a simple
    /// polygon has exactly n-2 triangles, every triangle wound counter-clockwise, and their areas
    /// summing to the polygon's own area. A self-intersecting outline, a swallowed corner or a
    /// flipped triangle all break it.
    /// </summary>
    private static void AssertValidTriangulation(IReadOnlyList<Vector2> polygon)
    {
        var triangles = EarClipping.Triangulate(polygon);

        Assert.Equal((polygon.Count - 2) * 3, triangles.Count);
        Assert.Equal(Math.Abs(EarClipping.SignedArea(polygon)), AreaOf(polygon, triangles), 4);
    }

    /// <summary>
    /// The same invariant with holes: the triangles cover the outside minus the holes and every one
    /// winds counter-clockwise. There are at most n + 2h - 2 of them: a bridge can make a flat
    /// corner, and a flat ear is clipped without a triangle.
    /// </summary>
    private static void AssertValidTriangulation(IReadOnlyList<Vector2> outer, IReadOnlyList<IReadOnlyList<Vector2>> holes)
    {
        var corners = new List<Vector2>(outer);

        foreach (var hole in holes) corners.AddRange(hole);

        var triangles = EarClipping.Triangulate(outer, holes);
        var expected = Math.Abs(EarClipping.SignedArea(outer)) - holes.Sum(h => Math.Abs(EarClipping.SignedArea(h)));

        Assert.Equal(expected, AreaOf(corners, triangles), 4);
        Assert.InRange(triangles.Count / 3, corners.Count - 2, corners.Count + 2 * holes.Count - 2);
    }

    private static float AreaOf(IReadOnlyList<Vector2> corners, List<int> triangles)
    {
        var totalArea = 0f;

        for (var i = 0; i < triangles.Count; i += 3)
        {
            var a = corners[triangles[i]];
            var b = corners[triangles[i + 1]];
            var c = corners[triangles[i + 2]];
            var doubleArea = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

            Assert.True(doubleArea > 0, $"Triangle {i / 3} is degenerate or wound clockwise");

            totalArea += doubleArea * 0.5f;
        }

        return totalArea;
    }

    [Fact]
    public void SquareBecomesTwoTriangles()
        => AssertValidTriangulation([new(0, 0), new(1, 0), new(1, 1), new(0, 1)]);

    [Fact]
    public void ClockwiseInputIsHandled()
        => AssertValidTriangulation([new(0, 1), new(1, 1), new(1, 0), new(0, 0)]);

    [Fact]
    public void ConcavePolygonIsHandled()
        // An L shape: the reflex corner at (1, 1) is exactly what plain fan triangulation gets wrong
        => AssertValidTriangulation([new(0, 0), new(2, 0), new(2, 1), new(1, 1), new(1, 2), new(0, 2)]);

    [Fact]
    public void SpiralWithManyReflexCornersIsHandled()
        // A U shape whose opening forces several non-adjacent corners into each candidate ear
        => AssertValidTriangulation(
        [
            new(0, 0), new(3, 0), new(3, 3), new(2, 3), new(2, 1), new(1, 1), new(1, 3), new(0, 3),
        ]);

    [Fact]
    public void TriangleReturnsItself()
    {
        var triangles = EarClipping.Triangulate([new(0, 0), new(1, 0), new(0, 1)]);

        Assert.Equal(3, triangles.Count);
    }

    [Fact]
    public void TooFewCornersThrows()
        => Assert.Throws<ArgumentException>(() => EarClipping.Triangulate([new(0, 0), new(1, 0)]));

    [Fact]
    public void SignedAreaIsPositiveForCounterClockwise()
    {
        Assert.Equal(1f, EarClipping.SignedArea([new(0, 0), new(1, 0), new(1, 1), new(0, 1)]), 4);
        Assert.Equal(-1f, EarClipping.SignedArea([new(0, 1), new(1, 1), new(1, 0), new(0, 0)]), 4);
    }

    // --- Holes ----------------------------------------------------------------------------------

    [Fact]
    public void ASquareWithASquareHoleIsARing()
        => AssertValidTriangulation(Square(0, 0, 4), [Square(1, 1, 2)]);

    [Fact]
    public void HoleWindingDoesNotMatter()
    {
        AssertValidTriangulation(Square(0, 0, 4), [Square(1, 1, 2).Reverse().ToArray()]);
        AssertValidTriangulation(Square(0, 0, 4).Reverse().ToArray(), [Square(1, 1, 2)]);
    }

    [Fact]
    public void TwoHolesSideBySide()
        // A B: two holes, the right one bridged first; the second bridge must not cross the first
        => AssertValidTriangulation(Square(0, 0, 6), [Square(1, 1, 1.5f), Square(3.5f, 1, 1.5f)]);

    [Fact]
    public void TwoHolesStacked()
        // An 8: the holes share their X range, so the upper hole's ray meets the first bridge's edges
        => AssertValidTriangulation(Square(0, 0, 4), [Square(1, 0.5f, 2), Square(1, 3f, 0.8f)]);

    [Fact]
    public void AHoleLevelWithAnotherHolesCorner()
        // The second hole's ray runs along the first hole's top edge; the corner on the ray is the bridge
        => AssertValidTriangulation(Square(0, 0, 6), [Square(1, 1, 1.5f), Square(3.5f, 1, 1.5f)]);

    [Fact]
    public void AHoleWhoseRayMeetsAReflexCorner()
        // The outside has a notch to the right of the hole, exactly where the bridge wants to go
        => AssertValidTriangulation(
            [new(0, 0), new(6, 0), new(6, 2), new(3.5f, 2.2f), new(6, 2.4f), new(6, 5), new(0, 5)],
            [[new(1, 1.5f), new(2, 1.5f), new(2, 3), new(1, 3)]]);

    [Fact]
    public void NoHolesIsThePlainCase()
    {
        var triangles = EarClipping.Triangulate(Square(0, 0, 1), []);

        Assert.Equal(6, triangles.Count);
    }

    [Fact]
    public void AHoleOutsideThePolygonIsReported()
        => Assert.Throws<InvalidOperationException>(() => EarClipping.Triangulate(Square(0, 0, 1), [Square(5, 5, 1)]));

    [Fact]
    public void EveryGlyphOfTheEmbeddedFontTriangulates()
    {
        // Real outlines: curves flattened, holes classified, bridged and clipped, with the area
        // invariant as the proof for every polygon of every character
        foreach (var character in "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@&%§€#")
        {
            foreach (var polygon in MeshFont.Default.GetGlyph(character).Flatten().Polygons)
            {
                AssertValidTriangulation(polygon.Outer, polygon.Holes);
            }
        }
    }

    private static Vector2[] Square(float x, float y, float size)
        => [new(x, y), new(x + size, y), new(x + size, y + size), new(x, y + size)];

    // --- Extrusion ------------------------------------------------------------------------------

    [Fact]
    public void ExtrudedSquareHasTheExpectedCounts()
    {
        using var builder = new MeshBuilder { IndexType = IndexingType.Int32 };

        var position = builder.WithPosition<Vector3>();
        var normal = builder.WithNormal<Vector3>();

        builder.AddExtrudedPolygon([new(0, 0), new(1, 0), new(1, 1), new(0, 1)], 0.5f, position, normal);

        // Caps share corners within themselves (2n) while every wall owns its four vertices (4n);
        // triangles are 2(n-2) for the caps plus 2n for the walls
        Assert.Equal(24, builder.VertexCount);
        Assert.Equal(36, builder.IndexCount);
    }

    [Fact]
    public void ExtrusionSplitsFrontAndBackAcrossTheDepth()
    {
        using var builder = new MeshBuilder { IndexType = IndexingType.Int32 };

        var position = builder.WithPosition<Vector3>();
        var normal = builder.WithNormal<Vector3>();

        builder.AddExtrudedPolygon([new(0, 0), new(1, 0), new(1, 1), new(0, 1)], 0.5f, position, normal);

        // The first corner of each cap: front at +depth/2 facing +Z, back at -depth/2 facing -Z
        Assert.Equal(new Vector3(0, 0, 0.25f), builder.GetElement<Vector3>(0, position));
        Assert.Equal(Vector3.UnitZ, builder.GetElement<Vector3>(0, normal));
        Assert.Equal(new Vector3(0, 0, -0.25f), builder.GetElement<Vector3>(4, position));
        Assert.Equal(-Vector3.UnitZ, builder.GetElement<Vector3>(4, normal));
    }

    [Fact]
    public void ExtrudedFacesWindClockwiseTowardTheViewer()
    {
        // Stride's default rasterizer follows Direct3D: front faces are CLOCKWISE on screen
        // (RasterizerStateDescription.DefaultFrontFaceCounterClockwise = false). This was once wound
        // the other way, which culled the front cap and left the camera looking into a hollow shell -
        // every wall interior visible at once, parallax moving the wrong way.
        using var builder = new MeshBuilder { IndexType = IndexingType.Int32 };

        var position = builder.WithPosition<Vector3>();
        var normal = builder.WithNormal<Vector3>();

        builder.AddExtrudedPolygon([new(0, 0), new(1, 0), new(1, 1), new(0, 1)], 0.5f, position, normal);

        // Walk every triangle; whichever side its lighting normal says is outward, the triangle must
        // appear clockwise from that side - i.e. its right-hand geometric normal must point INWARD,
        // opposite the lighting normal
        for (var i = 0; i < builder.IndexCount; i += 3)
        {
            var i0 = builder.GetIndex(i);
            var i1 = builder.GetIndex(i + 1);
            var i2 = builder.GetIndex(i + 2);

            var a = builder.GetElement<Vector3>(i0, position);
            var b = builder.GetElement<Vector3>(i1, position);
            var c = builder.GetElement<Vector3>(i2, position);

            var geometricNormal = Vector3.Cross(b - a, c - b);
            var lightingNormal = builder.GetElement<Vector3>(i0, normal);

            Assert.True(Vector3.Dot(geometricNormal, lightingNormal) < 0,
                $"Triangle at index {i} winds counter-clockwise toward its outward normal, so Stride would cull it");
        }
    }

    [Fact]
    public void TextMeshFacesWindClockwiseTowardTheViewer()
    {
        // The same rule for the lettering, where the back cap keeps the triangulation's order and the
        // front cap reverses it, and every wall quad is emitted clockwise from outside
        var data = TextMesh.Create(MeshFont.Default, "Ab8");

        for (var i = 0; i < data.Indices.Length; i += 3)
        {
            var a = data.Vertices[data.Indices[i]];
            var b = data.Vertices[data.Indices[i + 1]];
            var c = data.Vertices[data.Indices[i + 2]];
            var geometricNormal = Vector3.Cross(b.Position - a.Position, c.Position - b.Position);

            Assert.True(Vector3.Dot(geometricNormal, a.Normal) < 0, $"Triangle at index {i} winds counter-clockwise toward its outward normal");
        }
    }

    [Fact]
    public void ExtrusionRejectsNonPositiveDepth()
    {
        using var builder = new MeshBuilder { IndexType = IndexingType.Int32 };

        var position = builder.WithPosition<Vector3>();
        var normal = builder.WithNormal<Vector3>();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => builder.AddExtrudedPolygon([new(0, 0), new(1, 0), new(0, 1)], 0f, position, normal));
    }
}