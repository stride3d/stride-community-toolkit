using Stride.CommunityToolkit.Rendering.Text;
using Stride.Graphics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The bevel: still a closed solid, no deeper than before, the caps pulled in, the normals turning
/// from the walls to the caps, and a bevel too wide for a glyph shrinking rather than folding.
/// </summary>
public class TextBevelTests
{
    private static MeshFont Font => MeshFont.Default;

    private static readonly TextMeshOptions Bevelled = new() { Depth = 0.3f, Bevel = new TextBevel(size: 0.02f, thickness: 0.03f, segments: 3) };

    [Fact]
    public void ABevelledGlyphIsAClosedSolidFacingOut()
    {
        foreach (var character in "BOg8@&iI")
        {
            var data = TextMesh.Create(Font, character.ToString(), Bevelled);

            TextMeshTests.AssertWatertight(data);
            TextMeshTests.AssertOutwardWinding(data);
        }
    }

    [Fact]
    public void TheDepthDoesNotGrow()
    {
        var data = TextMesh.Create(Font, "H", Bevelled);

        Assert.Equal(0.15f, data.Vertices.Max(v => v.Position.Z), 5);
        Assert.Equal(-0.15f, data.Vertices.Min(v => v.Position.Z), 5);
    }

    [Fact]
    public void TheCapsArePulledInAndTheWallsStayPut()
    {
        var plain = TextMesh.Create(Font, "H", new TextMeshOptions { Depth = 0.3f });
        var bevelled = TextMesh.Create(Font, "H", Bevelled);

        static float CapWidth(GeometricMeshData<VertexPositionNormalTexture> data) => data.Vertices.Where(v => v.Normal.Z > 0.99f).Max(v => v.Position.X) - data.Vertices.Where(v => v.Normal.Z > 0.99f).Min(v => v.Position.X);
        static float WallWidth(GeometricMeshData<VertexPositionNormalTexture> data) => data.Vertices.Where(v => MathF.Abs(v.Normal.Z) < 0.01f).Max(v => v.Position.X) - data.Vertices.Where(v => MathF.Abs(v.Normal.Z) < 0.01f).Min(v => v.Position.X);

        Assert.Equal(CapWidth(plain) - 2 * 0.02f, CapWidth(bevelled), 3);
        Assert.Equal(WallWidth(plain), WallWidth(bevelled), 4);
    }

    [Fact]
    public void TheNormalsTurnFromTheWallToTheCap()
    {
        var data = TextMesh.Create(Font, "H", Bevelled);
        var leaning = data.Vertices.Where(v => MathF.Abs(v.Normal.Z) is > 0.01f and < 0.99f).ToList();

        Assert.NotEmpty(leaning);
        Assert.All(data.Vertices, v => Assert.InRange(v.Normal.Length(), 0.999f, 1.001f));

        // On the front half the leaning normals lean forward, on the back half backward
        Assert.All(leaning, v => Assert.True(MathF.Sign(v.Normal.Z) == MathF.Sign(v.Position.Z), $"a bevel normal at z {v.Position.Z} leans the other way"));
    }

    [Fact]
    public void OneSegmentIsAChamfer()
    {
        var chamfer = TextMesh.Create(Font, "H", new TextMeshOptions { Depth = 0.3f, Bevel = new TextBevel(0.02f, 0.02f, segments: 1) });
        var round = TextMesh.Create(Font, "H", new TextMeshOptions { Depth = 0.3f, Bevel = new TextBevel(0.02f, 0.02f, segments: 4) });
        var plain = TextMesh.Create(Font, "H", new TextMeshOptions { Depth = 0.3f });

        // A chamfer adds one ring of quads per side on top of the walls; four segments add four
        Assert.True(plain.Vertices.Length < chamfer.Vertices.Length);
        Assert.True(chamfer.Vertices.Length < round.Vertices.Length);
        TextMeshTests.AssertWatertight(chamfer);
    }

    [Fact]
    public void ABevelTooWideForAGlyphShrinksInsteadOfFolding()
    {
        // A bevel of 0.2 against stems about 0.09 wide at this size: the cap would fold; the size halves until it fits
        var data = TextMesh.Create(Font, "I", new TextMeshOptions { Depth = 0.3f, Bevel = new TextBevel(size: 0.2f, thickness: 0.05f) });

        TextMeshTests.AssertWatertight(data);
        TextMeshTests.AssertOutwardWinding(data);
    }

    [Fact]
    public void TheThicknessIsCappedAtHalfTheDepth()
    {
        var data = TextMesh.Create(Font, "H", new TextMeshOptions { Depth = 0.1f, Bevel = new TextBevel(size: 0.01f, thickness: 1f) });

        TextMeshTests.AssertWatertight(data);
        Assert.Equal(0.05f, data.Vertices.Max(v => v.Position.Z), 5);
    }

    [Fact]
    public void TextureCoordinatesStayInTheBlock()
    {
        var data = TextMesh.Create(Font, "Hello", Bevelled);
        var capsAndBevels = data.Vertices.Where(v => MathF.Abs(v.Normal.Z) > 0.01f);

        Assert.All(capsAndBevels, v => Assert.InRange(v.TextureCoordinate.X, -0.001f, 1.001f));
        Assert.All(capsAndBevels, v => Assert.InRange(v.TextureCoordinate.Y, -0.001f, 1.001f));
    }

    [Fact]
    public void TheBevelRejectsNonsense()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextBevel(0f, 0.1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextBevel(0.1f, 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextBevel(0.1f, 0.1f, segments: 0));
    }

    [Fact]
    public void PerGlyphMeshesTakeTheBevelToo()
    {
        var glyphs = TextMesh.CreateGlyphs(Font, "AB", Bevelled);

        Assert.Equal(2, glyphs.Count);
        Assert.All(glyphs, g => TextMeshTests.AssertWatertight(g.Mesh));
    }
}