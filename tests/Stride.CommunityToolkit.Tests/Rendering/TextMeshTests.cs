using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Rendering.Text.Meshes;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The lettering as mesh data, no device: closed solids with the right counts, laid out by the
/// font's metrics, moved by the anchor.
/// </summary>
public class TextMeshTests
{
    private static MeshFont Font => MeshFont.Default;

    [Fact]
    public void AGlyphIsAClosedSolid()
    {
        var data = TextMesh.Create(Font, "B");

        AssertWatertight(data);
        Assert.All(data.Vertices, v => Assert.InRange(v.Normal.Length(), 0.999f, 1.001f));
    }

    [Fact]
    public void EveryLetterOfTheAlphabetIsAClosedSolid()
    {
        foreach (var character in "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789&@%?")
        {
            var data = TextMesh.Create(Font, character.ToString());

            AssertWatertight(data);
            AssertOutwardWinding(data);
        }
    }

    [Fact]
    public void TheCountsFollowTheCorners()
    {
        // One polygon with c corners and one hole: caps use c vertices each and walls 4c; the
        // triangulation of a polygon with h holes has c + 2h - 2 triangles per cap, and the walls 2c
        var shape = Font.GetGlyph('O').Flatten();
        var polygon = Assert.Single(shape.Polygons);
        var corners = polygon.Outer.Count + polygon.Holes.Sum(h => h.Count);

        var data = TextMesh.Create(Font, "O");

        Assert.Equal(corners * 6, data.Vertices.Length);
        Assert.InRange(data.Indices.Length, (2 * (corners - 2) + 2 * corners) * 3, (2 * (corners + 2 * polygon.Holes.Count - 2) + 2 * corners) * 3);
    }

    [Fact]
    public void TheDepthIsCentredOnZero()
    {
        var data = TextMesh.Create(Font, "H", new TextMeshOptions { Depth = 0.4f });

        Assert.Equal(0.2f, data.Vertices.Max(v => v.Position.Z), 5);
        Assert.Equal(-0.2f, data.Vertices.Min(v => v.Position.Z), 5);
    }

    [Fact]
    public void SizeScalesTheLettering()
    {
        var one = TextMesh.Measure(Font, "Hello");
        var two = TextMesh.Measure(Font, "Hello", new TextMeshOptions { Size = 2f });

        Assert.Equal(one.Maximum.X * 2f, two.Maximum.X, 4);
        Assert.Equal(one.Maximum.Y * 2f, two.Maximum.Y, 4);
    }

    [Fact]
    public void TheBaselineSitsAtZeroAndThePenAdvances()
    {
        var h = TextMesh.Create(Font, "H");
        var hh = TextMesh.Create(Font, "HH");

        // The H stands on the baseline, and starts its left side bearing in from the pen
        Assert.Equal(0f, h.Vertices.Min(v => v.Position.Y), 3);
        Assert.Equal(Font.GetGlyph('H').BoundsMin.X, h.Vertices.Min(v => v.Position.X), 3);
        Assert.Equal(h.Vertices.Max(v => v.Position.X) + Font.GetGlyph('H').Advance, hh.Vertices.Max(v => v.Position.X), 4);
    }

    [Fact]
    public void LetterSpacingPushesGlyphsApart()
    {
        var tight = TextMesh.Measure(Font, "HH");
        var loose = TextMesh.Measure(Font, "HH", new TextMeshOptions { LetterSpacing = 0.5f });

        // Spacing after the first H only: the line ends with its last glyph
        Assert.Equal(tight.Maximum.X + 0.5f, loose.Maximum.X, 4);
    }

    [Fact]
    public void LinesGoDownByTheLineHeight()
    {
        var two = TextMesh.Create(Font, "H\nH");
        var lower = two.Vertices.Min(v => v.Position.Y);

        Assert.Equal(-Font.LineHeight, lower, 3);
        Assert.Equal(Font.Ascent, TextMesh.Measure(Font, "H\nH").Maximum.Y, 4);
        Assert.Equal(-Font.LineHeight + Font.Descent, TextMesh.Measure(Font, "H\nH").Minimum.Y, 4);
    }

    [Fact]
    public void AlignmentMovesTheShorterLine()
    {
        var left = TextMesh.Create(Font, "HHHH\nH", new TextMeshOptions { Alignment = TextAlignment.Left });
        var right = TextMesh.Create(Font, "HHHH\nH", new TextMeshOptions { Alignment = TextAlignment.Right });
        var centre = TextMesh.Create(Font, "HHHH\nH", new TextMeshOptions { Alignment = TextAlignment.Center });

        static float SecondLineMinX(GeometricMeshData<VertexPositionNormalTexture> data) => data.Vertices.Where(v => v.Position.Y < -0.5f).Min(v => v.Position.X);

        Assert.True(SecondLineMinX(left) < SecondLineMinX(centre));
        Assert.True(SecondLineMinX(centre) < SecondLineMinX(right));
        Assert.Equal(left.Vertices.Max(v => v.Position.X), right.Vertices.Max(v => v.Position.X), 4);
    }

    [Fact]
    public void TheAnchorMovesTheBlockNotTheLetters()
    {
        var box = TextMesh.Measure(Font, "Hello", new TextMeshOptions { Anchor = TextAnchor.MiddleCenter });

        Assert.Equal(0f, (box.Minimum.X + box.Maximum.X) * 0.5f, 4);
        Assert.Equal(0f, (box.Minimum.Y + box.Maximum.Y) * 0.5f, 4);

        var topRight = TextMesh.Measure(Font, "Hello", new TextMeshOptions { Anchor = TextAnchor.TopRight });

        Assert.Equal(0f, topRight.Maximum.X, 4);
        Assert.Equal(0f, topRight.Maximum.Y, 4);

        // The block is the font's ascent to descent, whatever the letters: "x" and "H" measure the same height
        Assert.Equal(TextMesh.Measure(Font, "x").Maximum.Y, TextMesh.Measure(Font, "H").Maximum.Y, 4);
    }

    [Fact]
    public void NothingToDrawIsAnEmptyMesh()
    {
        var data = TextMesh.Create(Font, " \n ");

        Assert.Empty(data.Vertices);
        Assert.Empty(data.Indices);
        Assert.Empty(TextMesh.CreateGlyphs(Font, "   "));
    }

    [Fact]
    public void GlyphsComeBackOneByOne_CentredAndPlaced()
    {
        var glyphs = TextMesh.CreateGlyphs(Font, "A B");

        Assert.Equal(2, glyphs.Count);
        Assert.Equal('A', glyphs[0].Codepoint);
        Assert.Equal('B', glyphs[1].Codepoint);
        Assert.True(glyphs[1].Position.X > glyphs[0].Position.X);

        foreach (var glyph in glyphs)
        {
            var min = new Vector2(glyph.Mesh.Vertices.Min(v => v.Position.X), glyph.Mesh.Vertices.Min(v => v.Position.Y));
            var max = new Vector2(glyph.Mesh.Vertices.Max(v => v.Position.X), glyph.Mesh.Vertices.Max(v => v.Position.Y));

            Assert.Equal(0f, (min.X + max.X) * 0.5f, 3);
            Assert.Equal(0f, (min.Y + max.Y) * 0.5f, 3);
            AssertWatertight(glyph.Mesh);
        }

        // Put together, the glyphs land where the one-mesh version put them
        var whole = TextMesh.Create(Font, "A B");
        var placedMax = glyphs.Max((GlyphMesh g) => g.Position.X + g.Mesh.Vertices.Max((VertexPositionNormalTexture v) => v.Position.X));

        Assert.Equal(whole.Vertices.Max(v => v.Position.X), placedMax, 3);
    }

    [Fact]
    public void CapTextureCoordinatesSpanTheBlock()
    {
        var data = TextMesh.Create(Font, "Hello");
        var caps = data.Vertices.Where(v => MathF.Abs(v.Normal.Z) > 0.5f).ToList();

        Assert.InRange(caps.Min(v => v.TextureCoordinate.X), 0f, 0.05f);
        Assert.InRange(caps.Max(v => v.TextureCoordinate.X), 0.95f, 1f);
        Assert.All(caps, v => Assert.InRange(v.TextureCoordinate.Y, 0f, 1f));
    }

    [Fact]
    public void AMissingCharacterDrawsTheMissingGlyph()
    {
        var data = TextMesh.Create(Font, "");

        Assert.NotEmpty(data.Vertices);
    }

    /// <summary>
    /// Every triangle winds clockwise as seen from the side its normal points to, which is the side
    /// Stride draws: its geometric normal must point against its lighting normal, for every triangle
    /// with any area.
    /// </summary>
    internal static void AssertOutwardWinding(GeometricMeshData<VertexPositionNormalTexture> data)
    {
        var inward = 0;

        for (var i = 0; i < data.Indices.Length; i += 3)
        {
            var a = data.Vertices[data.Indices[i]];
            var b = data.Vertices[data.Indices[i + 1]];
            var c = data.Vertices[data.Indices[i + 2]];
            var geometric = Vector3.Cross(b.Position - a.Position, c.Position - b.Position);

            if (geometric.LengthSquared() < 1e-16f) continue;

            if (Vector3.Dot(geometric, a.Normal + b.Normal + c.Normal) >= 0f) inward++;
        }

        Assert.True(inward == 0, $"{inward} triangles wind the wrong way against their normals");
    }

    /// <summary>
    /// Every edge of a closed surface is shared by exactly two triangles, one in each direction. The
    /// caps share their corners and the walls own their vertices, so edges are matched by position,
    /// exactly: the parts are built from the same corners and the same depths, and a hair-wide gap
    /// between two of them is a gap. Two closed pieces that touch (the ogonek of an ą) share an edge,
    /// which then appears twice each way; what matters is that each way is matched by the other.
    /// </summary>
    internal static void AssertWatertight(GeometricMeshData<VertexPositionNormalTexture> data)
    {
        Assert.NotEmpty(data.Vertices);
        Assert.Equal(0, data.Indices.Length % 3);
        Assert.All(data.Indices, i => Assert.InRange(i, 0, data.Vertices.Length - 1));

        var edges = new Dictionary<(Vector3, Vector3), int>();

        for (var i = 0; i < data.Indices.Length; i += 3)
        {
            for (var k = 0; k < 3; k++)
            {
                var a = data.Vertices[data.Indices[i + k]].Position;
                var b = data.Vertices[data.Indices[i + (k + 1) % 3]].Position;

                edges[(a, b)] = edges.GetValueOrDefault((a, b)) + 1;
            }
        }

        foreach (var ((a, b), count) in edges)
        {
            Assert.True(count == edges.GetValueOrDefault((b, a)), $"edge {a} -> {b} appears {count} times and {edges.GetValueOrDefault((b, a))} times the other way: an open surface");
        }
    }
}