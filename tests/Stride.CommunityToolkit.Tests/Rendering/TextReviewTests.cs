using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Text.Meshes;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The cases the 2026-10-17 review found, one test per finding, on synthetic geometry and the
/// embedded font so no installed font is needed.
/// </summary>
public class TextReviewTests
{
    private static MeshFont Font => MeshFont.Default;

    // F1: a contour of a single point, which Consolas has 364 of, is skipped rather than crashing the decode

    [Fact]
    public void ASinglePointContourIsSkipped()
    {
        var contours = OutlineDecoder.Decode([new Vector2(0, 0)], [1], [0]);

        Assert.Empty(contours);
    }

    [Fact]
    public void ASinglePointContourDoesNotCostTheOthers()
    {
        // A square, then a lone point, then another square
        Vector2[] points = [new(0, 0), new(1, 0), new(1, 1), new(0, 1), new(5, 5), new(2, 0), new(3, 0), new(3, 1), new(2, 1)];
        byte[] tags = [1, 1, 1, 1, 1, 1, 1, 1, 1];

        var contours = OutlineDecoder.Decode(points, tags, [3, 4, 8]);

        Assert.Equal(2, contours.Length);
    }

    // F2: the triangulation does not depend on the size the text is built at

    [Theory]
    [InlineData("H", 0.01f)]
    [InlineData("Hello", 0.03f)]
    [InlineData("B", 0.03f)]
    [InlineData("B", 100f)]
    public void SmallAndLargeTextTriangulateLikeUnitText(string text, float size)
    {
        var unit = TextMesh.Create(Font, text, new TextMeshOptions { Size = 1f });
        var scaled = TextMesh.Create(Font, text, new TextMeshOptions { Size = size });

        Assert.Equal(unit.Indices.Length, scaled.Indices.Length);
        Assert.Equal(unit.Vertices.Length, scaled.Vertices.Length);
        MeshAssertions.AssertWatertight(scaled);
    }

    [Fact]
    public void ANearbyCornerIsNotSwallowedByAnEar()
    {
        // Corners 1 and 3 sit a twentieth of a percent of the extent apart: distinct corners, not the two
        // ends of a bridge. Taking them as one lost the triangle between them and the cap came out
        // a quarter too large (the second review's Times New Roman k)
        Vector2[] polygon =
        [
            new(0.008300781f, 0f),
            new(0.16357422f, 0.24902344f),
            new(0.27734375f, 0.35302734f),
            new(0.16357422f, 0.24951172f),
            new(0.16357422f, 0.69433594f),
        ];

        var triangles = EarClipping.Triangulate(polygon);
        var area = 0f;

        for (var i = 0; i < triangles.Count; i += 3)
        {
            area += EarClipping.SignedArea([polygon[triangles[i]], polygon[triangles[i + 1]], polygon[triangles[i + 2]]]);
        }

        Assert.Equal(9, triangles.Count);
        Assert.Equal(EarClipping.SignedArea(polygon), area, 1e-6f);
    }

    [Fact]
    public void AThinSliverIsStillATriangle()
    {
        // A polygon with a corner that is nearly, but not quite, flat: the sliver is part of the cap
        Vector2[] polygon = [new(0, 0), new(1, 0), new(1, 1), new(0.5f, 1.0000005f), new(0, 1)];

        var triangles = EarClipping.Triangulate(polygon);

        Assert.Equal(9, triangles.Count);
    }

    [Fact]
    public void TranslatedPolygonsTriangulateTheSame()
    {
        Vector2[] polygon = [new(0, 0), new(2, 0), new(2, 1), new(1, 1), new(1, 2), new(0, 2)];
        var moved = polygon.Select(p => p + new Vector2(1000f, -1000f)).ToArray();

        Assert.Equal(EarClipping.Triangulate(polygon), EarClipping.Triangulate(moved));
    }

    // F3: an inset that passes through a collapse is refused, and no bevel triangle faces inward

    [Fact]
    public void AnInsetThroughTheMiddleIsNotAFit()
    {
        Vector2[] square = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];
        var inset = LoopInset.Inset(square, 0.75f);

        // The corners crossed the centre: positive area, wrong loop
        Assert.True(EarClipping.SignedArea(inset) > 0f);
        Assert.False(LoopInset.Fits([square], [inset]));
        Assert.True(LoopInset.Fits([square], [LoopInset.Inset(square, 0.25f)]));
    }

    [Theory]
    [InlineData("i")]
    [InlineData("@")]
    [InlineData("I")]
    [InlineData("B")]
    public void AWideBevelNeverFacesInward(string text)
    {
        var data = TextMesh.Create(Font, text, new TextMeshOptions { Depth = 0.3f, Bevel = new TextBevel(0.2f, 0.03f) });

        MeshAssertions.AssertWatertight(data);
        MeshAssertions.AssertOutwardWinding(data);
    }

    [Fact]
    public void TheBevelShrinksForTheWholeGlyph()
    {
        // The dot and the stem of an i take the same width: no corner of the dot's cap sits further in
        // from its outline than a corner of the stem's does. Measured as the cap's inset from the wall
        var data = TextMesh.Create(Font, "i", new TextMeshOptions { Depth = 0.3f, Bevel = new TextBevel(0.2f, 0.03f) });
        var plain = TextMesh.Create(Font, "i", new TextMeshOptions { Depth = 0.3f });

        static bool Dot(VertexPositionNormalTexture v) => v.Normal.Z > 0.99f && v.Position.Y > 0.6f;
        static bool Stem(VertexPositionNormalTexture v) => v.Normal.Z > 0.99f && v.Position.Y < 0.4f;

        Assert.Equal(TestGeometry.Width(plain, Dot) - TestGeometry.Width(data, Dot), TestGeometry.Width(plain, Stem) - TestGeometry.Width(data, Stem), 3);
    }

    // F4: a cubic with parallel end tangents still gets its bends

    [Fact]
    public void AnInflectedCubicIsNotAChord()
    {
        var contour = new GlyphContour(Vector2.Zero,
        [
            OutlineSegment.Cubic(new Vector2(1, 1), new Vector2(0, -1), new Vector2(1, 0)),
            OutlineSegment.Line(new Vector2(1, -2)),
            OutlineSegment.Line(new Vector2(0, -2)),
            OutlineSegment.Line(Vector2.Zero),
        ]);

        var coarse = contour.Flatten(8).Length;
        var fine = contour.Flatten(32).Length;
        var finest = contour.Flatten(128).Length;

        Assert.True(coarse > 4, $"{coarse} corners at quality 8: the curve became its chord");
        Assert.True(fine > coarse);
        Assert.True(finest > fine);

        // The curve rises above the chord and dips below it
        var points = contour.Flatten(32);
        Assert.Contains(points, p => p.Y > 0.05f);
        Assert.Contains(points, p => p.Y < -0.05f && p.X < 1f && p.Y > -1f);
    }

    // F7: filled pieces that cross or overlap are kept as separate pieces, which is how composite glyphs
    // are drawn; a self-crossing contour is reported with the glyph named

    [Fact]
    public void CrossingPiecesAreKeptAsTwoPieces()
    {
        Vector2[] a = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];
        Vector2[] b = [new(0.5f, 0.5f), new(1.5f, 0.5f), new(1.5f, 1.5f), new(0.5f, 1.5f)];

        var shape = GlyphShape.FromContours([a, b]);

        Assert.Equal(2, shape.Polygons.Count);
        Assert.All(shape.Polygons, p => Assert.Empty(p.Holes));
    }

    [Fact]
    public void APieceMostlyInsideAnotherItCrossesIsNotItsHole()
    {
        // The small square pokes out of the big one on the right: a crossing, so two pieces, not a hole
        Vector2[] big = [new(0, 0), new(4, 0), new(4, 4), new(0, 4)];
        Vector2[] poking = [new(3, 1), new(5, 1), new(5, 3), new(3, 3)];

        var shape = GlyphShape.FromContours([big, poking]);

        Assert.Equal(2, shape.Polygons.Count);
        Assert.All(shape.Polygons, p => Assert.Empty(p.Holes));
    }

    [Theory]
    [InlineData(0.5f)]
    [InlineData(0f)]
    public void PiecesThatShareAStretchOfEdgeAreTwoPieces(float shift)
    {
        // Side by side with the top and bottom edges running along each other, and the same square twice
        Vector2[] a = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];
        Vector2[] b = [new(shift, 0), new(shift + 1, 0), new(shift + 1, 1), new(shift, 1)];

        var shape = GlyphShape.FromContours([a, b]);

        Assert.Equal(2, shape.Polygons.Count);
        Assert.All(shape.Polygons, p => Assert.Empty(p.Holes));
    }

    [Fact]
    public void PiecesThatOnlyTouchAtACornerNest()
    {
        // The small square touches the big one's edge from inside at one corner: still its hole
        Vector2[] outer = [new(0, 0), new(4, 0), new(4, 4), new(0, 4)];
        Vector2[] hole = [new(1, 1), new(3, 1), new(3, 3), new(1, 4)];

        var polygon = Assert.Single(GlyphShape.FromContours([outer, hole.Reverse().ToArray()]).Polygons);

        Assert.Single(polygon.Holes);
    }

    [Fact]
    public void AStrokeThroughALetterAndItsHoleIsAPieceOfItsOwn()
    {
        // The bar of a Ⱥ: it crosses the outside and the hole alike, and is neither's hole or outside
        Vector2[] outer = [new(0, 0), new(4, 0), new(4, 4), new(0, 4)];
        Vector2[] hole = [new(1, 1), new(1, 3), new(3, 3), new(3, 1)];
        Vector2[] bar = [new(-1, 1.8f), new(5, 1.8f), new(5, 2.2f), new(-1, 2.2f)];

        var shape = GlyphShape.FromContours([outer, hole, bar]);

        Assert.Equal(2, shape.Polygons.Count);
        Assert.Single(shape.Polygons.Single(p => p.Outer.Count == 4 && p.Holes.Count == 1).Holes);
        Assert.Empty(shape.Polygons.Single(p => p.Holes.Count == 0).Holes);
    }

    [Fact]
    public void ABowTieIsReportedNotDropped()
    {
        // Its signed area cancels to nothing, which is no reason to take it for nothing
        Vector2[] bowTie = [new(0, 0), new(1, 1), new(0, 1), new(1, 0)];

        Assert.Throws<InvalidDataException>(() => GlyphShape.FromContours([bowTie]));
    }

    [Fact]
    public void TheAccentedLettersOfTheEmbeddedFontBuildAsClosedSolids()
    {
        // The cedillas, ogoneks and the ring of Å cross the letters they belong to, or touch them at a
        // corner (ą, Į, ų): two pieces each, each closed
        foreach (var character in "ÅÇçęŞşţŲąĮų")
        {
            var data = TextMesh.Create(Font, character.ToString());

            MeshAssertions.AssertWatertight(data);
            MeshAssertions.AssertOutwardWinding(data);
        }
    }

    [Fact]
    public void ANestedContourWoundTheSameWayIsReported()
    {
        Vector2[] outer = [new(0, 0), new(4, 0), new(4, 4), new(0, 4)];
        Vector2[] inner = [new(1, 1), new(3, 1), new(3, 3), new(1, 3)];

        Assert.Throws<InvalidDataException>(() => GlyphShape.FromContours([outer, inner]));
        Assert.Single(GlyphShape.FromContours([outer, inner.Reverse().ToArray()]).Polygons);
    }

    [Fact]
    public void ASelfCrossingContourIsReported()
    {
        // One with area, crossing itself once
        Vector2[] crossed = [new(0, 0), new(3, 0), new(3, 2), new(1, 2), new(1, -1)];

        Assert.Throws<InvalidDataException>(() => GlyphShape.FromContours([crossed]));
    }

    // F8: the bevel's normals follow its ellipse

    [Fact]
    public void BevelNormalsFollowTheEllipse()
    {
        // On the straight left side of an H: at the first step of a 3-step bevel the profile's normal is
        // (thickness cos a, size sin a) in (outward, z), normalised, with a = 30 degrees
        var data = TextMesh.Create(Font, "H", new TextMeshOptions { Depth = 0.3f, Bevel = new TextBevel(0.01f, 0.03f, 3) });
        var expected = new Vector3(-0.03f * MathF.Cos(MathF.PI / 6f), 0f, 0.01f * MathF.Sin(MathF.PI / 6f));

        expected.Normalize();

        var leftSide = data.Vertices.Where(v => v.Normal.X < -0.5f && v.Normal.Z > 0.1f && v.Normal.Z < 0.5f && MathF.Abs(v.Normal.Y) < 0.01f).ToList();

        Assert.NotEmpty(leftSide);
        Assert.All(leftSide, v => Assert.InRange(Vector3.Dot(v.Normal, expected), 0.9999f, 1.0001f));
    }

    // F9: options that cannot make geometry are refused at the door

    [Theory]
    [InlineData(0f, 0.25f)]
    [InlineData(float.NaN, 0.25f)]
    [InlineData(float.PositiveInfinity, 0.25f)]
    [InlineData(1f, 0f)]
    [InlineData(1f, -1f)]
    [InlineData(1f, float.NaN)]
    public void InvalidSizeOrDepthIsRejected(float size, float depth)
    {
        var options = new TextMeshOptions { Size = size, Depth = depth };

        Assert.Throws<ArgumentOutOfRangeException>(() => TextMesh.Create(Font, "H", options));
        Assert.Throws<ArgumentOutOfRangeException>(() => TextMesh.CreateGlyphs(Font, "H", options));
        Assert.Throws<ArgumentOutOfRangeException>(() => TextMesh.Measure(Font, "H", options));
    }

    [Fact]
    public void OtherInvalidOptionsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TextMesh.Create(Font, "H", new TextMeshOptions { LineSpacing = 0f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => TextMesh.Create(Font, "H", new TextMeshOptions { LetterSpacing = float.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => TextMesh.Create(Font, "H", new TextMeshOptions { CurveQuality = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextBevel(float.NaN, 0.1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextBevel(0.1f, float.PositiveInfinity));
    }

    // Diagnostics: a glyph that cannot be built says which

    [Fact]
    public void AGlyphThatCannotBeBuiltIsNamed()
    {
        // A bevel of half an EM on an "l" halves six times and still folds
        var error = Assert.Throws<InvalidOperationException>(() => TextMesh.Create(Font, "l", new TextMeshOptions { Bevel = new TextBevel(40f, 0.05f) }));

        Assert.Contains("U+006C", error.Message);
        Assert.Contains("Open Sans", error.Message);
    }

    // Face index

    [Fact]
    public void AFaceThatDoesNotExistIsReported()
    {
        Assert.Equal(1, Font.FaceCount);
        Assert.Throws<InvalidDataException>(() => MeshFont.Load(TestGeometry.DefaultFontBytes(), "copy", faceIndex: 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => MeshFont.Load(TestGeometry.DefaultFontBytes(), "copy", faceIndex: -1));
    }

    // F6: a reader that was waiting finds the font gone, not a freed face

    [Fact]
    public void AReaderWaitingThroughDisposalIsRefused()
    {
        var font = MeshFont.Load(TestGeometry.DefaultFontBytes(), "copy");

        font.GetGlyph('A');

        Exception? seen = null;

        var reader = new Thread(() =>
        {
            try
            {
                font.GetGlyph('A');
            }
            catch (Exception e)
            {
                seen = e;
            }
        });

        // Hold the face lock so the reader queues behind it, dispose while it waits (the lock lets its
        // own thread back in), then let the reader through: it must find the font gone, not the cached
        // glyph of a freed face. The old order, checked before the lock, handed the glyph over
        lock (font.FaceLock)
        {
            reader.Start();

            Assert.True(SpinWait.SpinUntil(() => reader.ThreadState.HasFlag(ThreadState.WaitSleepJoin), 5000), "the reader never queued behind the lock");

            font.Dispose();
        }

        reader.Join();

        Assert.IsType<ObjectDisposedException>(seen);
    }

    [Fact]
    public void DisposalWaitsForAReaderInsideTheFace()
    {
        var font = MeshFont.Load(TestGeometry.DefaultFontBytes(), "copy");
        var disposed = new ManualResetEventSlim();

        // Holding the face lock stands for a reader inside FreeType; disposal must wait for it
        lock (font.FaceLock)
        {
            new Thread(() =>
            {
                font.Dispose();
                disposed.Set();
            }).Start();

            Assert.False(disposed.Wait(200), "disposal went ahead while a reader held the face");
        }

        Assert.True(disposed.Wait(5000), "disposal never finished once the reader let go");
        Assert.Throws<ObjectDisposedException>(() => font.GetGlyph('A'));
    }

    // Bounds

    [Fact]
    public void MeshDataBoundsCoverTheVertices()
    {
        var data = TextMesh.Create(Font, "Hello", new TextMeshOptions { Anchor = Anchor.MiddleCenter });
        var (box, sphere) = data.Bounds();

        Assert.All(data.Vertices, v => { var position = v.Position; Assert.Equal(ContainmentType.Contains, box.Contains(ref position)); });
        Assert.All(data.Vertices, v => Assert.True((v.Position - sphere.Center).Length() <= sphere.Radius + 1e-5f));

        // The ink sits inside the typographic block the anchor centred, bearings and all
        var block = TextMesh.Measure(Font, "Hello", new TextMeshOptions { Anchor = Anchor.MiddleCenter });

        Assert.True(box.Minimum.X >= block.Minimum.X - 1e-4f && box.Maximum.X <= block.Maximum.X + 1e-4f);
        Assert.True(box.Minimum.Y >= block.Minimum.Y - 1e-4f && box.Maximum.Y <= block.Maximum.Y + 1e-4f);
    }
}