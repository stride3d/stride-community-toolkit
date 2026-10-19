using Stride.CommunityToolkit.Rendering.Text.Meshes;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The embedded font through FreeType: real glyphs with the contour counts a reader of Open Sans expects.
/// No graphics device is involved.
/// </summary>
public class MeshFontTests
{
    [Fact]
    public void TheDefaultFontIsOpenSans()
    {
        var font = MeshFont.Default;

        Assert.Equal("Open Sans", font.Name);
        Assert.Equal(2048, font.UnitsPerEm);
        Assert.InRange(font.Ascent, 0.9f, 1.2f);
        Assert.InRange(font.Descent, -0.4f, -0.2f);
        Assert.InRange(font.LineHeight, 1.2f, 1.5f);
        Assert.Same(font, MeshFont.Default);
    }

    [Fact]
    public void AHasOneHole_BHasTwo_IHasTwoPieces()
    {
        var font = MeshFont.Default;

        var a = Assert.Single(font.GetGlyph('A').Flatten().Polygons);
        Assert.Single(a.Holes);

        var b = Assert.Single(font.GetGlyph('B').Flatten().Polygons);
        Assert.Equal(2, b.Holes.Count);

        var i = font.GetGlyph('i').Flatten();
        Assert.Equal(2, i.Polygons.Count);
        Assert.All(i.Polygons, p => Assert.Empty(p.Holes));
    }

    [Fact]
    public void GlyphsSitOnTheBaselineInEmUnits()
    {
        var h = MeshFont.Default.GetGlyph('H');

        Assert.InRange(h.BoundsMin.Y, -0.01f, 0.01f);
        Assert.InRange(h.BoundsMax.Y, 0.6f, 0.8f);
        Assert.InRange(h.Advance, 0.5f, 0.9f);
        Assert.False(h.IsMissing);
        Assert.False(h.IsEmpty);
    }

    [Fact]
    public void ASpaceAdvancesWithoutDrawing()
    {
        var space = MeshFont.Default.GetGlyph(' ');

        Assert.True(space.IsEmpty);
        Assert.True(space.Flatten().IsEmpty);
        Assert.InRange(space.Advance, 0.1f, 0.5f);
    }

    [Fact]
    public void AMissingCharacterGivesTheMissingGlyph()
    {
        var font = MeshFont.Default;

        Assert.True(font.HasGlyph('A'));
        Assert.False(font.HasGlyph(''));

        var missing = font.GetGlyph('');

        Assert.True(missing.IsMissing);
        Assert.Same(missing, font.GetGlyph(''));
    }

    [Fact]
    public void CurvesAreTrueTypeQuadratics()
    {
        var o = MeshFont.Default.GetGlyph('O');

        Assert.Equal(2, o.Contours.Count);
        Assert.Contains(o.Contours[0].Segments, s => s.Kind == OutlineSegmentKind.Quadratic);
        Assert.DoesNotContain(o.Contours[0].Segments, s => s.Kind == OutlineSegmentKind.Cubic);
        Assert.Equal(o.Contours[0].Start, o.Contours[0].Segments[^1].End);
    }

    [Fact]
    public void MoreQualityMeansMoreCorners()
    {
        var o = MeshFont.Default.GetGlyph('O');

        var coarse = o.Contours[0].Flatten(curveQuality: 8).Length;
        var fine = o.Contours[0].Flatten(curveQuality: 64).Length;

        Assert.True(fine > coarse * 2, $"{fine} corners at 64 against {coarse} at 8");
    }

    [Fact]
    public void KerningComesOnlyFromTheKernTable_WhichOpenSansLacks()
        => Assert.Equal(0f, MeshFont.Default.GetKerning('A', 'V'));

    [Fact]
    public void TheSameBytesLoadAsASecondFont()
    {
        using var stream = typeof(MeshFont).Assembly.GetManifestResourceStream("Stride.CommunityToolkit.Rendering.Text.Fonts.OpenSans-Regular.ttf")!;
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);

        using var font = MeshFont.Load(bytes, "copy");

        Assert.Equal("Open Sans", font.Name);
        Assert.NotSame(MeshFont.Default, font);
        Assert.Equal(MeshFont.Default.GetGlyph('g').Advance, font.GetGlyph('g').Advance);
    }

    [Fact]
    public void ADisposedFontRefuses()
    {
        var font = MeshFont.Load([.. ReadDefaultBytes()], "copy");

        font.Dispose();

        Assert.Throws<ObjectDisposedException>(() => font.GetGlyph('A'));
    }

    [Fact]
    public void BytesThatAreNotAFontAreRejected()
        => Assert.Throws<InvalidDataException>(() => MeshFont.Load(new byte[256], "noise"));

    [Fact]
    public void AMissingFileIsReported()
        => Assert.Throws<FileNotFoundException>(() => MeshFont.Load(Path.Combine(Path.GetTempPath(), "no-such-font-" + Guid.NewGuid() + ".ttf")));

    [Fact]
    public void AnUnknownFamilyIsNull()
        => Assert.Null(MeshFont.FromFamily("No Such Family " + Guid.NewGuid()));

    [Fact]
    public void FlattenedGlyphsAreInEmUnits()
    {
        var shape = MeshFont.Default.GetGlyph('H').Flatten();

        var corners = shape.Polygons[0].Outer;

        Assert.All(corners, c => Assert.InRange(c.X, -0.1f, 1f));
        Assert.All(corners, c => Assert.InRange(c.Y, -0.1f, 1f));
        Assert.Equal(12, corners.Count);
    }

    private static byte[] ReadDefaultBytes()
    {
        using var stream = typeof(MeshFont).Assembly.GetManifestResourceStream("Stride.CommunityToolkit.Rendering.Text.Fonts.OpenSans-Regular.ttf")!;
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }
}