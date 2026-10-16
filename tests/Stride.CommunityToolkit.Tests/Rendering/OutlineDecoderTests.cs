using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// Pins FreeType's outline conventions, one hand-built outline per rule, so a font's curves cannot be
/// misread without a test saying which rule broke.
/// </summary>
public class OutlineDecoderTests
{
    private const byte On = 1;
    private const byte Conic = 0;
    private const byte Cubic = 2;

    private static readonly Vector2 A = new(0, 0);
    private static readonly Vector2 B = new(1, 0);
    private static readonly Vector2 C = new(1, 1);
    private static readonly Vector2 D = new(0, 1);

    [Fact]
    public void OnPointsMakeLines_AndTheContourCloses()
    {
        var contour = Decode([A, B, C], [On, On, On], [2]);

        Assert.Equal(A, contour.Start);
        Assert.Equal([OutlineSegment.Line(B), OutlineSegment.Line(C), OutlineSegment.Line(A)], contour.Segments);
    }

    [Fact]
    public void OneConicBetweenOnPointsIsAQuadratic()
    {
        var contour = Decode([A, B, C], [On, Conic, On], [2]);

        Assert.Equal([OutlineSegment.Quadratic(B, C), OutlineSegment.Line(A)], contour.Segments);
    }

    [Fact]
    public void TwoConicsInARowImplyTheOnPointBetweenThem()
    {
        var contour = Decode([A, B, C, D], [On, Conic, Conic, On], [3]);

        var middle = (B + C) * 0.5f;

        Assert.Equal([OutlineSegment.Quadratic(B, middle), OutlineSegment.Quadratic(C, D), OutlineSegment.Line(A)], contour.Segments);
    }

    [Fact]
    public void AContourStartingWithAConicStartsFromTheLastOnPoint()
    {
        // B is the control; the contour is read as D -> (B) -> C -> D
        var contour = Decode([B, C, D], [Conic, On, On], [2]);

        Assert.Equal(D, contour.Start);
        Assert.Equal([OutlineSegment.Quadratic(B, C), OutlineSegment.Line(D)], contour.Segments);
    }

    [Fact]
    public void AContourOfConicsOnlyStartsHalfWayBetweenTheFirstAndLast()
    {
        // Four conic controls around a square: a TrueType circle with no on-curve point at all
        var contour = Decode([A, B, C, D], [Conic, Conic, Conic, Conic], [3]);

        var start = (A + D) * 0.5f;

        Assert.Equal(start, contour.Start);
        Assert.Equal(4, contour.Segments.Count);
        Assert.All(contour.Segments, s => Assert.Equal(OutlineSegmentKind.Quadratic, s.Kind));
        Assert.Equal(start, contour.Segments[^1].End);
    }

    [Fact]
    public void TwoCubicControlsMakeACubic()
    {
        var contour = Decode([A, B, C, D], [On, Cubic, Cubic, On], [3]);

        Assert.Equal([OutlineSegment.Cubic(B, C, D), OutlineSegment.Line(A)], contour.Segments);
    }

    [Fact]
    public void ACubicCanCloseTheContour()
    {
        var contour = Decode([A, B, C], [On, Cubic, Cubic], [2]);

        Assert.Equal([OutlineSegment.Cubic(B, C, A)], contour.Segments);
    }

    [Fact]
    public void AContourStartingWithACubicIsInvalid()
        => Assert.Throws<InvalidDataException>(() => Decode([A, B, C], [Cubic, Cubic, On], [2]));

    [Fact]
    public void ALoneCubicControlIsInvalid()
        => Assert.Throws<InvalidDataException>(() => Decode([A, B, C], [On, Cubic, On], [2]));

    [Fact]
    public void ContourEndsSplitThePoints()
    {
        var contours = OutlineDecoder.Decode([A, B, C, D, C, B], [On, On, On, On, On, On], [2, 5]);

        Assert.Equal(2, contours.Length);
        Assert.Equal(A, contours[0].Start);
        Assert.Equal(D, contours[1].Start);
    }

    [Fact]
    public void TheHighTagBitsAreIgnored()
    {
        // FreeType sets bit 2 for "drop-out control" points; only the low two bits name the point kind
        var contour = Decode([A, B, C], [On | 4, Conic | 4, On | 4], [2]);

        Assert.Equal(OutlineSegmentKind.Quadratic, contour.Segments[0].Kind);
    }

    private static GlyphContour Decode(Vector2[] points, byte[] tags, ushort[] ends)
        => Assert.Single(OutlineDecoder.Decode(points, tags, ends));
}