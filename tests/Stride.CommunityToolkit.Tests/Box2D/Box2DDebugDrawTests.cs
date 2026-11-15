using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Box2D;

/// <summary>
/// Pins the one pure piece of <see cref="Box2DDebugDraw"/>: Box2D's hex colours become opaque
/// Stride colours with the channels in the right order. The drawing itself needs a
/// <c>ShapeBatch</c>, which needs a graphics device and a compositor, and is covered by the Box2D
/// examples' captures.
/// </summary>
public class Box2DDebugDrawTests
{
    [Theory]
    [InlineData(B2HexColor.b2_colorRed, 255, 0, 0)]
    [InlineData(B2HexColor.b2_colorLime, 0, 255, 0)]
    [InlineData(B2HexColor.b2_colorBlue, 0, 0, 255)]
    [InlineData(B2HexColor.b2_colorBlack, 0, 0, 0)]
    public void ToColor_SplitsTheHexIntoChannels(B2HexColor hex, int r, int g, int b)
    {
        var color = DebugDrawColors.ToColor(hex);

        Assert.Equal(new Color((byte)r, (byte)g, (byte)b), color);
    }
}
