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
    [InlineData(Anchor.TopLeft, 0f, 0f)]
    [InlineData(Anchor.TopCenter, 0.5f, 0f)]
    [InlineData(Anchor.TopRight, 1f, 0f)]
    [InlineData(Anchor.MiddleLeft, 0f, 0.5f)]
    [InlineData(Anchor.MiddleCenter, 0.5f, 0.5f)]
    [InlineData(Anchor.MiddleRight, 1f, 0.5f)]
    [InlineData(Anchor.BottomLeft, 0f, 1f)]
    [InlineData(Anchor.BottomCenter, 0.5f, 1f)]
    [InlineData(Anchor.BottomRight, 1f, 1f)]
    public void AnchorFactor_IsTheFractionOfTheTextBeforeThePoint(Anchor anchor, float x, float y)
        => Assert.Equal(new Vector2(x, y), ScreenTextDrawer.GetAnchorFactor(anchor));

    [Theory]
    [InlineData(Anchor.TopLeft, 0f, 0f)]
    [InlineData(Anchor.TopCenter, 50f, 0f)]
    [InlineData(Anchor.TopRight, 100f, 0f)]
    [InlineData(Anchor.MiddleLeft, 0f, 25f)]
    [InlineData(Anchor.MiddleCenter, 50f, 25f)]
    [InlineData(Anchor.MiddleRight, 100f, 25f)]
    [InlineData(Anchor.BottomLeft, 0f, 50f)]
    [InlineData(Anchor.BottomCenter, 50f, 50f)]
    [InlineData(Anchor.BottomRight, 100f, 50f)]
    public void Anchor_At_IsTheCornerEdgeOrCentreOfTheRectangle(Anchor anchor, float x, float y)
        => Assert.Equal(new Vector2(x, y), anchor.At(new Vector2(100f, 50f)));

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