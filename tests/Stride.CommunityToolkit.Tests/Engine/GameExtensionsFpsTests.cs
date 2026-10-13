using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// Pins what the frame-rate caps do with zero: no cap, where the division used to give an infinite
/// interval and throw.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class GameExtensionsFpsTests
{
    [Fact]
    public void SetMaxFPS_ZeroRemovesTheCap()
    {
        using var game = new Game();

        game.SetMaxFPS(60);

        Assert.Equal(TimeSpan.FromMilliseconds(1000.0 / 60), game.WindowMinimumUpdateRate.MinimumElapsedTime);

        game.SetMaxFPS(0);

        Assert.Equal(TimeSpan.Zero, game.WindowMinimumUpdateRate.MinimumElapsedTime);
    }

    [Fact]
    public void SetFocusLostFPS_ZeroRemovesTheCap()
    {
        using var game = new Game();

        game.SetFocusLostFPS(0);

        Assert.Equal(TimeSpan.Zero, game.MinimizedMinimumUpdateRate.MinimumElapsedTime);
    }
}