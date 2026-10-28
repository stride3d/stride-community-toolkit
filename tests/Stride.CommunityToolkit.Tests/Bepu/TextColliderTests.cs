using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Rendering.Text.Meshes;
using Stride.CommunityToolkit.Tests.Rendering;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Bepu;

/// <summary>
/// The hulls of lettering: one per glyph, shared by every body that shows the same text with the same
/// font, and never by a different font that happens to carry the same name.
/// </summary>
public class TextColliderTests
{
    [Fact]
    public void TheSameTextInTheSameFontSharesItsHulls()
    {
        var options = new TextMeshOptions { Size = 0.7f };

        var first = TextCollider.CreateHulls(MeshFont.Default, "Share", options);
        var second = TextCollider.CreateHulls(MeshFont.Default, "Share", new TextMeshOptions { Size = 0.7f });

        Assert.Same(first, second);
        Assert.Equal(5, TextCollider.HullCount(new ConvexHullCollider { Hull = first }));
    }

    [Fact]
    public void ChangedOptionsGetTheirOwnHulls()
    {
        var plain = TextCollider.CreateHulls(MeshFont.Default, "Own", new TextMeshOptions { Size = 0.7f });
        var deeper = TextCollider.CreateHulls(MeshFont.Default, "Own", new TextMeshOptions { Size = 0.7f, Depth = 0.5f });

        Assert.NotSame(plain, deeper);
    }

    [Fact]
    public void TwoFontsOfTheSameNameKeepTheirOwnHulls()
    {
        // Two copies of the embedded font are two fonts named Open Sans; a cache keyed by the name
        // handed the second the first's geometry (the second review's F10)
        using var one = MeshFont.Load(TestGeometry.DefaultFontBytes(), "copy");
        using var two = MeshFont.Load(TestGeometry.DefaultFontBytes(), "copy");

        Assert.Equal(one.Name, two.Name);

        var fromOne = TextCollider.CreateHulls(one, "Name");
        var fromTwo = TextCollider.CreateHulls(two, "Name");

        Assert.NotSame(fromOne, fromTwo);
        Assert.Same(fromOne, TextCollider.CreateHulls(one, "Name"));
    }

    [Fact]
    public void TextWithNoShapeHasNoCollider()
    {
        Assert.Throws<ArgumentException>(() => TextCollider.CreateHulls(MeshFont.Default, "   "));
    }
}