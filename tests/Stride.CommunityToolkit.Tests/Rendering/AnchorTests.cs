using Stride.CommunityToolkit.Rendering;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// Pins where each <see cref="Anchor"/> lands on a rectangle: the corners at its extremes, the edge
/// anchors half way along, the centre in the middle - the one table the overlay, anchored text and the
/// shape batch all read.
/// </summary>
public class AnchorTests
{
    [Theory]
    [InlineData(Anchor.TopLeft, 0f, 0f)]
    [InlineData(Anchor.TopCenter, 400f, 0f)]
    [InlineData(Anchor.TopRight, 800f, 0f)]
    [InlineData(Anchor.MiddleLeft, 0f, 300f)]
    [InlineData(Anchor.MiddleCenter, 400f, 300f)]
    [InlineData(Anchor.MiddleRight, 800f, 300f)]
    [InlineData(Anchor.BottomLeft, 0f, 600f)]
    [InlineData(Anchor.BottomCenter, 400f, 600f)]
    [InlineData(Anchor.BottomRight, 800f, 600f)]
    public void At_PlacesEachAnchorOnTheRectangle(Anchor anchor, float x, float y)
    {
        var point = anchor.At(new Vector2(800f, 600f));

        Assert.Equal(new Vector2(x, y), point);
    }
}