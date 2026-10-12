using Stride.CommunityToolkit.Rendering;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// Pins where each <see cref="ScreenAnchor"/> lands on a rectangle: the corners at its extremes, the edge
/// anchors half way along, the centre in the middle - the one table the overlay, anchored text and the
/// shape batch all read.
/// </summary>
public class ScreenAnchorTests
{
    [Theory]
    [InlineData(ScreenAnchor.TopLeft, 0f, 0f)]
    [InlineData(ScreenAnchor.TopCenter, 400f, 0f)]
    [InlineData(ScreenAnchor.TopRight, 800f, 0f)]
    [InlineData(ScreenAnchor.MiddleLeft, 0f, 300f)]
    [InlineData(ScreenAnchor.MiddleCenter, 400f, 300f)]
    [InlineData(ScreenAnchor.MiddleRight, 800f, 300f)]
    [InlineData(ScreenAnchor.BottomLeft, 0f, 600f)]
    [InlineData(ScreenAnchor.BottomCenter, 400f, 600f)]
    [InlineData(ScreenAnchor.BottomRight, 800f, 600f)]
    public void At_PlacesEachAnchorOnTheRectangle(ScreenAnchor anchor, float x, float y)
    {
        var point = anchor.At(new Vector2(800f, 600f));

        Assert.Equal(new Vector2(x, y), point);
    }
}