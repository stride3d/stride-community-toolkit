using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The arithmetic the text renderers share: the anchor grid, premultiplied colour and the distance fade.
/// </summary>
public class ScreenTextDrawerTests
{
    [Theory]
    [InlineData(TextAnchor.TopLeft, 0f, 0f)]
    [InlineData(TextAnchor.TopCenter, 0.5f, 0f)]
    [InlineData(TextAnchor.TopRight, 1f, 0f)]
    [InlineData(TextAnchor.MiddleLeft, 0f, 0.5f)]
    [InlineData(TextAnchor.MiddleCenter, 0.5f, 0.5f)]
    [InlineData(TextAnchor.MiddleRight, 1f, 0.5f)]
    [InlineData(TextAnchor.BottomLeft, 0f, 1f)]
    [InlineData(TextAnchor.BottomCenter, 0.5f, 1f)]
    [InlineData(TextAnchor.BottomRight, 1f, 1f)]
    public void AnchorFactor_IsTheFractionOfTheTextBeforeThePoint(TextAnchor anchor, float x, float y)
        => Assert.Equal(new Vector2(x, y), ScreenTextDrawer.GetAnchorFactor(anchor));

    [Fact]
    public void ScreenAnchor_AndTextAnchor_ShareOneLayout()
    {
        // EntityTextRenderer casts one to the other for anchored text, so the names must line up in order
        Assert.Equal(Enum.GetNames<ScreenAnchor>(), Enum.GetNames<TextAnchor>());
    }

    [Fact]
    public void Premultiplied_ScalesTheChannelsByTheAlpha()
    {
        var colour = ScreenTextDrawer.Premultiplied(new Color(255, 128, 0, 255), 0.5f);

        Assert.Equal(0.5f, colour.R, 3);
        Assert.Equal(128f / 255f * 0.5f, colour.G, 3);
        Assert.Equal(0f, colour.B, 3);
        Assert.Equal(0.5f, colour.A, 3);
    }

    [Fact]
    public void DistanceFade_CutsOffAndFadesBetweenTheTwoDistances()
    {
        var camera = Vector3.Zero;
        var opacity = 1f;

        // Neither distance set: nothing happens
        Assert.True(ScreenTextDrawer.TryApplyDistanceFade(0f, 0f, camera, new Vector3(100f, 0f, 0f), ref opacity));
        Assert.Equal(1f, opacity);

        // Beyond the cutoff
        Assert.False(ScreenTextDrawer.TryApplyDistanceFade(0f, 10f, camera, new Vector3(11f, 0f, 0f), ref opacity));

        // Half way through the fade
        opacity = 1f;
        Assert.True(ScreenTextDrawer.TryApplyDistanceFade(10f, 20f, camera, new Vector3(15f, 0f, 0f), ref opacity));
        Assert.Equal(0.5f, opacity, 3);

        // Nearer than the fade start
        opacity = 1f;
        Assert.True(ScreenTextDrawer.TryApplyDistanceFade(10f, 20f, camera, new Vector3(5f, 0f, 0f), ref opacity));
        Assert.Equal(1f, opacity);
    }
}