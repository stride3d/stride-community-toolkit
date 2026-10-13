using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// Keys are data on a <see cref="TextElement"/> and the overlay decorates them: one property restyles
/// every help line in every example, which is the point of not baking the brackets into the text.
/// </summary>
public class DebugOverlayKeyTests
{
    [Fact]
    public void FormatKeys_DecoratesEachKeyAndJoinsThem()
    {
        using var game = new Game();
        var overlay = DebugOverlay.GetOrCreate(game);

        Assert.Equal("[H]", overlay.FormatKeys(["H"]));
        Assert.Equal("[Q] [E]", overlay.FormatKeys(["Q", "E"]));
        Assert.Equal("[Arrow keys]", overlay.FormatKeys(["Arrow keys"]));
        Assert.Equal(string.Empty, overlay.FormatKeys([]));
    }

    [Fact]
    public void FormatKeys_FollowsTheFormatAndSeparator()
    {
        using var game = new Game();
        var overlay = DebugOverlay.GetOrCreate(game);

        overlay.KeyFormat = "{0}:";
        overlay.KeySeparator = ", ";

        Assert.Equal("Q:, E:", overlay.FormatKeys(["Q", "E"]));
    }

    [Fact]
    public void TextElement_KeyConstructors_KeepTextColourAndKeys()
    {
        var one = new TextElement("H", "Reset camera", Color.Gold);
        var two = new TextElement(["Q", "E"], "Ascend / descend");
        var plain = new TextElement("Position: 0, 0", Color.Yellow);

        Assert.Equal("Reset camera", one.Text);
        Assert.Equal(Color.Gold, one.Color);
        Assert.Equal(["H"], one.Keys);

        Assert.Equal(["Q", "E"], two.Keys);
        Assert.Null(two.Color);

        Assert.Null(plain.Keys);
        Assert.Null(plain.Marker);
        Assert.False(plain.Indented);
    }
}