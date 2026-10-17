using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// Flattening and nesting: curves become polygons with a predictable number of corners, and contours
/// sort into outsides and holes by how they sit inside each other.
/// </summary>
public class GlyphShapeTests
{
    [Fact]
    public void AQuarterTurnGetsAQuarterOfTheQualityInSegments()
    {
        // A quadratic from (0,0) through (1,0) to (1,1) turns its tangent through 90 degrees
        var contour = new GlyphContour(Vector2.Zero,
        [
            OutlineSegment.Quadratic(new Vector2(1, 0), new Vector2(1, 1)),
            OutlineSegment.Line(new Vector2(0, 1)),
            OutlineSegment.Line(Vector2.Zero),
        ]);

        // start, 8 curve points, (0,1): the last curve point is (1,1) itself
        Assert.Equal(1 + 8 + 1, contour.Flatten(curveQuality: 32).Length);
        Assert.Equal(1 + 2 + 1, contour.Flatten(curveQuality: 8).Length);
    }

    [Fact]
    public void AStraightCurveGetsOneSegment()
    {
        var contour = new GlyphContour(Vector2.Zero,
        [
            OutlineSegment.Quadratic(new Vector2(0.5f, 0), new Vector2(1, 0)),
            OutlineSegment.Line(new Vector2(1, 1)),
            OutlineSegment.Line(Vector2.Zero),
        ]);

        // The curve's one point is (1,0); it also sits on no straight line between its neighbours
        Assert.Equal(3, contour.Flatten().Length);
    }

    [Fact]
    public void CollinearAndRepeatedPointsAreDropped()
    {
        var contour = new GlyphContour(Vector2.Zero,
        [
            OutlineSegment.Line(new Vector2(0.5f, 0)),
            OutlineSegment.Line(new Vector2(0.5f, 0)),
            OutlineSegment.Line(new Vector2(1, 0)),
            OutlineSegment.Line(new Vector2(1, 1)),
            OutlineSegment.Line(Vector2.Zero),
        ]);

        Assert.Equal([Vector2.Zero, new Vector2(1, 0), new Vector2(1, 1)], contour.Flatten());
    }

    [Fact]
    public void AContourInsideAnotherIsItsHole_WoundTheOtherWay()
    {
        // A font winds a hole the other way from its outside; that is what makes it a hole
        var shape = GlyphShape.FromContours([Square(0, 0, 4), Reverse(Square(1, 1, 2))]);

        var polygon = Assert.Single(shape.Polygons);
        var hole = Assert.Single(polygon.Holes);

        Assert.True(EarClipping.SignedArea(polygon.Outer) > 0, "outer is counter-clockwise");
        Assert.True(EarClipping.SignedArea(hole) < 0, "hole is clockwise");
    }

    [Fact]
    public void TheOutsidesWindingDoesNotMatter()
    {
        // A clockwise outside with a counter-clockwise hole: still opposite, still a hole
        var shape = GlyphShape.FromContours([Reverse(Square(0, 0, 4)), Square(1, 1, 2)]);

        var polygon = Assert.Single(shape.Polygons);

        Assert.True(EarClipping.SignedArea(polygon.Outer) > 0);
        Assert.True(EarClipping.SignedArea(Assert.Single(polygon.Holes)) < 0);
    }

    [Fact]
    public void SeparateContoursAreSeparatePolygons()
    {
        var shape = GlyphShape.FromContours([Square(0, 0, 1), Square(0, 2, 1)]);

        Assert.Equal(2, shape.Polygons.Count);
        Assert.All(shape.Polygons, p => Assert.Empty(p.Holes));
    }

    [Fact]
    public void AnIslandInsideAHoleIsAnOutsideAgain()
    {
        // The dot of a copyright sign: ring, hole, dot, each wound the other way from the one around it
        var shape = GlyphShape.FromContours([Square(0, 0, 6), Reverse(Square(1, 1, 4)), Square(2, 2, 2)]);

        Assert.Equal(2, shape.Polygons.Count);
        Assert.Single(shape.Polygons[0].Holes);
        Assert.Empty(shape.Polygons[1].Holes);
    }

    [Fact]
    public void DegenerateContoursAreIgnored()
    {
        var shape = GlyphShape.FromContours([Square(0, 0, 1), [Vector2.Zero, Vector2.UnitX], [Vector2.Zero, Vector2.UnitX, Vector2.UnitX * 2]]);

        Assert.Single(shape.Polygons);
    }

    [Fact]
    public void NoContoursIsEmpty() => Assert.True(GlyphShape.FromContours([]).IsEmpty);

    private static Vector2[] Square(float x, float y, float size)
        => [new(x, y), new(x + size, y), new(x + size, y + size), new(x, y + size)];

    private static Vector2[] Reverse(Vector2[] polygon) => [.. polygon.Reverse()];
}