using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// Pins the one thing <see cref="DebugOverlay.SetPosition(Int2)"/> exists for: a custom position
/// takes effect in one call, where setting <see cref="DebugOverlay.CustomPosition"/> alone leaves
/// the overlay in its corner.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class DebugOverlayPositionTests
{
    [Fact]
    public void SetPosition_SwitchesToCustomAndStoresThePixels()
    {
        using var game = new Game();

        var overlay = DebugOverlay.GetOrCreate(game);

        overlay.SetPosition(40, 60);

        Assert.Null(overlay.Position);
        Assert.Equal(new Int2(40, 60), overlay.CustomPosition);
    }

    [Fact]
    public void CustomPositionAlone_LeavesTheCorner()
    {
        using var game = new Game();

        var overlay = DebugOverlay.GetOrCreate(game);
        var corner = overlay.Position;

        overlay.CustomPosition = new Int2(40, 60);

        Assert.Equal(corner, overlay.Position);
        Assert.NotNull(corner);
    }

    [Fact]
    public void SetPosition_WithACorner_LeavesTheCustomPixelsForLater()
    {
        using var game = new Game();

        var overlay = DebugOverlay.GetOrCreate(game);

        overlay.SetPosition(40, 60);
        overlay.SetPosition(ScreenAnchor.BottomLeft);

        Assert.Equal(ScreenAnchor.BottomLeft, overlay.Position);
        Assert.Equal(new Int2(40, 60), overlay.CustomPosition);

        overlay.SetPosition(null);

        Assert.Null(overlay.Position);
    }

    [Fact]
    public void CyclePosition_HandsACustomPositionBackToACorner()
    {
        using var game = new Game();

        var overlay = DebugOverlay.GetOrCreate(game);

        overlay.SetPosition(new Int2(10, 10));
        overlay.CyclePosition();

        // The key handler refuses to cycle a custom position; the method itself is the deliberate way back to the corners
        Assert.Equal(ScreenAnchor.TopLeft, overlay.Position);
        Assert.Equal(new Int2(10, 10), overlay.CustomPosition);
    }
}