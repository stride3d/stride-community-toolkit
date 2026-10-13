using Stride.CommunityToolkit.Rendering;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// Covers the ear-clipping triangulator and the glyph outlines and extrusion built on it.
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

        var totalArea = 0f;

        for (var i = 0; i < triangles.Count; i += 3)
        {
            var a = polygon[triangles[i]];
            var b = polygon[triangles[i + 1]];
            var c = polygon[triangles[i + 2]];

            var doubleArea = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

            Assert.True(doubleArea > 0, $"Triangle {i / 3} is degenerate or wound clockwise");

            totalArea += doubleArea * 0.5f;
        }

        Assert.Equal(Math.Abs(EarClipping.SignedArea(polygon)), totalArea, 4);
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

    // --- Glyph outlines -------------------------------------------------------------------------

    [Fact]
    public void EveryAuthoredGlyphTriangulatesCleanly()
    {
        // The area invariant doubles as a self-intersection check on every piece of every
        // hand-authored glyph, which is exactly the mistake glyph authoring invites
        foreach (var character in LetterMeshFactory.SupportedCharacters)
        {
            Assert.True(LetterMeshFactory.TryGetOutlines(character, out var outlines));
            Assert.NotEmpty(outlines);

            foreach (var outline in outlines)
            {
                AssertValidTriangulation(outline);
            }
        }
    }

    [Fact]
    public void GlyphPiecesStayInsideTheUnitBoxAndNeverOverlap()
    {
        // Pieces of one glyph may touch along edges but must not overlap: two overlapping pieces
        // put two caps on the same plane, which z-fights. Overlap of axis-aligned pieces is caught
        // by comparing summed piece area against a generous bound; a genuine overlap of bars on the
        // shared segment grid would double-count visibly.
        foreach (var character in LetterMeshFactory.SupportedCharacters)
        {
            Assert.True(LetterMeshFactory.TryGetOutlines(character, out var outlines));

            var totalArea = 0f;

            foreach (var outline in outlines)
            {
                foreach (var corner in outline)
                {
                    Assert.InRange(corner.X, 0f, 1f);
                    Assert.InRange(corner.Y, 0f, 1f);
                }

                totalArea += Math.Abs(EarClipping.SignedArea(outline));
            }

            // A glyph is strokes in a 0.7 x 1 box; its ink can never legitimately reach the area of
            // the whole box
            Assert.InRange(totalArea, 0.05f, 0.7f);
        }
    }

    [Fact]
    public void GlyphLookupIsCaseInsensitive()
    {
        Assert.True(LetterMeshFactory.TryGetOutlines('g', out _));
        Assert.True(LetterMeshFactory.TryGetOutlines('G', out _));
    }

    [Fact]
    public void UnknownCharacterHasNoGlyph()
        => Assert.False(LetterMeshFactory.TryGetOutlines('?', out _));

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
    public void ExtrusionRejectsNonPositiveDepth()
    {
        using var builder = new MeshBuilder { IndexType = IndexingType.Int32 };

        var position = builder.WithPosition<Vector3>();
        var normal = builder.WithNormal<Vector3>();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => builder.AddExtrudedPolygon([new(0, 0), new(1, 0), new(0, 1)], 0f, position, normal));
    }
}
